using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;

using Aggregator.Core.Configuration;
using Aggregator.Core.Context;
using Aggregator.Core.Interfaces;
using Aggregator.Core.Monitoring;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aggregator.Core.Facade
{
    /// <summary>
    /// Singleton used to access Azure DevOps work item data over REST. This keeps us from connecting
    /// each and every time we get an update. Keeps track of all WorkItems pulled in memory that should be saved later.
    /// </summary>
    public partial class WorkItemRepository : IWorkItemRepository, IDisposable
    {
        private const string WorkItemApiVersion = "7.0";

        private readonly ILogEvents logger;

        private readonly IRuntimeContext context;

        private readonly Dictionary<int, IWorkItem> loadedWorkItems = new Dictionary<int, IWorkItem>();

        private readonly List<IWorkItem> createdWorkItems = new List<IWorkItem>();

        private readonly HttpClient httpClient;

        private readonly Uri projectCollectionUri;

        public WorkItemRepository(IRuntimeContext context)
        {
            this.logger = context.Logger;
            this.context = context;

            var connectionInfo = context.GetConnectionInfo();
            this.logger.Connecting(connectionInfo);

            this.projectCollectionUri = EnsureTrailingSlash(connectionInfo.ProjectCollectionUri);
            this.httpClient = CreateHttpClient(connectionInfo);
        }

        public IWorkItem GetWorkItem(int workItemId)
        {
            if (!this.loadedWorkItems.TryGetValue(workItemId, out var result))
            {
                result = new WorkItemWrapper(this.LoadWorkItem(workItemId), this.context, this);
                this.loadedWorkItems.Add(workItemId, result);
            }

            return result;
        }

        public ReadOnlyCollection<IWorkItem> LoadedWorkItems => new ReadOnlyCollection<IWorkItem>(this.loadedWorkItems.Values.ToList());

        public ReadOnlyCollection<IWorkItem> CreatedWorkItems => new ReadOnlyCollection<IWorkItem>(this.createdWorkItems);

        public IWorkItem MakeNewWorkItem(string projectName, string workItemTypeName)
        {
            throw new NotSupportedException("Creating work items is outside the supported REST migration scope.");
        }

        public IWorkItem MakeNewWorkItem(IWorkItem inSameProjectAs, string workItemTypeName)
        {
            throw new NotSupportedException("Creating work items is outside the supported REST migration scope.");
        }

        public IWorkItem MakeNewWorkItem(IWorkItemExposed inSameProjectAs, string workItemTypeName)
        {
            throw new NotSupportedException("Creating work items is outside the supported REST migration scope.");
        }

        public IEnumerable<string> GetGlobalList(string globalListName)
        {
            throw new NotSupportedException("Global lists are not supported by the REST-backed repository.");
        }

        // HACK public to allow Unit Testing
        public static IEnumerable<string> ParseGlobalList(XmlDocument globalListsDoc, string globalListName)
        {
            var ns = new XmlNamespaceManager(globalListsDoc.NameTable);
            ns.AddNamespace("gl", "http://schemas.microsoft.com/VisualStudio/2005/workitemtracking/globallists");

            string xpath = string.Format("/gl:GLOBALLISTS/GLOBALLIST[@name='{0}']/LISTITEM/@value", globalListName);
            var nodes = globalListsDoc.SelectNodes(xpath, ns);

            foreach (XmlAttribute node in nodes.Cast<XmlAttribute>())
            {
                yield return node.Value;
            }
        }

        public void AddItemToGlobalList(string globalListName, string item)
        {
            throw new NotSupportedException("Global lists are not supported by the REST-backed repository.");
        }

        public void RemoveItemFromGlobalList(string globalListName, string item)
        {
            throw new NotSupportedException("Global lists are not supported by the REST-backed repository.");
        }

        // HACK public to allow Unit Testing
        public enum EditAction { Add, Remove }

        // HACK public to allow Unit Testing
        public static bool EditGlobalList(XmlDocument globalListsDoc, string globalListName, string item, EditAction action)
        {
            // prepare
            var ns = new XmlNamespaceManager(globalListsDoc.NameTable);
            ns.AddNamespace("gl", "http://schemas.microsoft.com/VisualStudio/2005/workitemtracking/globallists");
            string xpath = string.Format($"/gl:GLOBALLISTS");
            var rootNode = globalListsDoc.SelectSingleNode(xpath, ns);
            bool anyChange = false;

            // check if GL exists
            xpath = string.Format(
                $"/gl:GLOBALLISTS/GLOBALLIST[@name='{globalListName}']");
            var globalListNode = globalListsDoc.SelectSingleNode(xpath, ns);
            if (globalListNode == null && action == EditAction.Add)
            {
                globalListNode = globalListsDoc.CreateElement("GLOBALLIST");
                var nameAttr = globalListsDoc.CreateAttribute("name");
                nameAttr.Value = globalListName;
                globalListNode.Attributes.Append(nameAttr);
                rootNode.AppendChild(globalListNode);
                anyChange = true;
            }

            // check if item already added
            xpath = string.Format(
                $"/gl:GLOBALLISTS/GLOBALLIST[@name='{globalListName}']/LISTITEM[@value='{item}']");
            var itemNode = globalListsDoc.SelectSingleNode(xpath, ns);
            if (itemNode == null && action == EditAction.Add)
            {
                itemNode = globalListsDoc.CreateElement("LISTITEM");
                var valueAttr = globalListsDoc.CreateAttribute("value");
                valueAttr.Value = item;
                itemNode.Attributes.Append(valueAttr);
                globalListNode.AppendChild(itemNode);
                anyChange = true;
            }
            else if (itemNode != null && action == EditAction.Remove)
            {
                globalListNode.RemoveChild(itemNode);
                anyChange = true;
            }

            return anyChange;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.httpClient?.Dispose();
            }
        }

        internal void SaveWorkItem(WorkItemWrapper workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            var dirtyFields = workItem.GetDirtyFieldValues();
            if (dirtyFields.Count == 0)
            {
                return;
            }

            var operations = new List<object>
            {
                new { op = "test", path = "/rev", value = workItem.Revision }
            };

            operations.AddRange(dirtyFields.Select(field => new
            {
                op = "add",
                path = "/fields/" + EscapeJsonPointer(field.Key),
                value = field.Value
            }));

            using (var request = new HttpRequestMessage(new HttpMethod("PATCH"), this.BuildWorkItemUri(workItem.Id)))
            {
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(operations),
                    Encoding.UTF8,
                    "application/json-patch+json");

                var savedWorkItem = this.SendAndParse(request);
                workItem.RefreshFromData(savedWorkItem);
            }
        }

        private RestWorkItemData LoadWorkItem(int workItemId)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, this.BuildWorkItemUri(workItemId)))
            {
                return this.SendAndParse(request);
            }
        }

        private RestWorkItemData SendAndParse(HttpRequestMessage request)
        {
            using (var response = this.httpClient.SendAsync(request).GetAwaiter().GetResult())
            {
                string payload = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Azure DevOps REST call to '{request.RequestUri}' failed with {(int)response.StatusCode} {response.ReasonPhrase}: {payload}");
                }

                return this.ParseWorkItem(JObject.Parse(payload));
            }
        }

        private RestWorkItemData ParseWorkItem(JObject payload)
        {
            int id = payload.Value<int>("id");
            var data = new RestWorkItemData
            {
                Id = id,
                Revision = payload.Value<int?>("rev") ?? 0,
                Uri = this.ParseWorkItemUri(payload.Value<string>("url"), id)
            };

            JObject fieldObject = payload["fields"] as JObject;
            if (fieldObject != null)
            {
                foreach (var field in fieldObject.Properties())
                {
                    data.Fields[field.Name] = ParseFieldValue(field.Value);
                }
            }

            data.TypeName = data.Fields.TryGetValue("System.WorkItemType", out var typeName)
                ? Convert.ToString(typeName)
                : string.Empty;

            if (data.Fields.TryGetValue("System.ChangedDate", out var revisedDate) && revisedDate is DateTime changedDate)
            {
                data.RevisedDate = changedDate;
            }

            JArray relations = payload["relations"] as JArray;
            if (relations != null)
            {
                foreach (JObject relation in relations.OfType<JObject>())
                {
                    string relationType = relation.Value<string>("rel");
                    if (!string.Equals(relationType, WorkItemImplementationBase.ParentRelationship, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(relationType, WorkItemImplementationBase.ChildRelationship, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (TryGetRelatedWorkItemId(relation.Value<string>("url"), out var relatedId))
                    {
                        data.Relations.Add(new RestWorkItemRelation
                        {
                            LinkTypeEndImmutableName = relationType,
                            TargetId = relatedId
                        });
                    }
                }
            }

            return data;
        }

        private Uri BuildWorkItemUri(int workItemId)
        {
            return new Uri(
                this.projectCollectionUri,
                $"_apis/wit/workitems/{workItemId}?$expand=relations&api-version={WorkItemApiVersion}");
        }

        private Uri ParseWorkItemUri(string url, int workItemId)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri;
            }

            return new Uri(this.projectCollectionUri, $"_apis/wit/workitems/{workItemId}");
        }

        private static bool TryGetRelatedWorkItemId(string relationUrl, out int workItemId)
        {
            workItemId = 0;
            if (string.IsNullOrWhiteSpace(relationUrl))
            {
                return false;
            }

            if (Uri.TryCreate(relationUrl, UriKind.RelativeOrAbsolute, out var relationUri) && relationUri.IsAbsoluteUri)
            {
                return int.TryParse(relationUri.Segments.Last().Trim('/'), out workItemId);
            }

            string lastSegment = relationUrl.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return int.TryParse(lastSegment, out workItemId);
        }

        private static object ParseFieldValue(JToken token)
        {
            switch (token?.Type)
            {
                case null:
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;

                case JTokenType.Integer:
                {
                    long integer = token.Value<long>();
                    if (integer >= int.MinValue && integer <= int.MaxValue)
                    {
                        return (int)integer;
                    }

                    return integer;
                }

                case JTokenType.Float:
                    return token.Value<double>();

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.Date:
                    return token.Value<DateTime>();

                case JTokenType.Object:
                {
                    var obj = (JObject)token;
                    string uniqueName = obj.Value<string>("uniqueName");
                    if (!string.IsNullOrWhiteSpace(uniqueName))
                    {
                        return uniqueName;
                    }

                    string displayName = obj.Value<string>("displayName");
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        return displayName;
                    }

                    return obj.ToString(Newtonsoft.Json.Formatting.None);
                }

                default:
                    return token.Value<string>();
            }
        }

        private static string EscapeJsonPointer(string value)
        {
            return (value ?? string.Empty)
                .Replace("~", "~0")
                .Replace("/", "~1");
        }

        private static Uri EnsureTrailingSlash(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            string absoluteUri = uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
                ? uri.AbsoluteUri
                : uri.AbsoluteUri + "/";
            return new Uri(absoluteUri, UriKind.Absolute);
        }

        private static HttpClient CreateHttpClient(ConnectionInfo connectionInfo)
        {
            var handler = connectionInfo.Token?.CreateHttpClientHandler()
                ?? new HttpClientHandler
                {
                    PreAuthenticate = true,
                    UseDefaultCredentials = true
                };

            var client = new HttpClient(handler, disposeHandler: true);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            connectionInfo.Token?.Apply(client);
            return client;
        }
    }
}
