using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Aggregator.Core.Context;
using Aggregator.Core.Interfaces;
using Aggregator.Core.Navigation;

namespace Aggregator.Core.Facade
{
    public class WorkItemWrapper : WorkItemImplementationBase, IWorkItem
    {
        private readonly WorkItemRepository repository;

        private readonly IRuntimeContext context;

        private IDictionary<string, WorkItemFieldState> fields = new Dictionary<string, WorkItemFieldState>(StringComparer.OrdinalIgnoreCase);

        private IList<RestWorkItemRelation> relations = new List<RestWorkItemRelation>();

        private readonly HashSet<string> dirtyFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private int id;

        private Uri uri;

        private DateTime revisedDate;

        private int revision;

        private string typeName;

        internal WorkItemWrapper(RestWorkItemData workItem, IRuntimeContext context, WorkItemRepository repository)
            : base(context)
        {
            this.context = context;
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.RefreshFromData(workItem ?? throw new ArgumentNullException(nameof(workItem)));
        }

        public IWorkItemType Type => new RestWorkItemType(this.TypeName);

        public bool ShouldLimit(RateLimiter limiter)
        {
            return limiter?.ShouldLimit(this) ?? false;
        }

        public string TypeName => this.typeName;

        public string History
        {
            get
            {
                return this.GetOrCreateField("System.History").CurrentValue as string;
            }

            set
            {
                this.SetFieldValue("System.History", value);
            }
        }

        public int Id => this.id;

        public object this[string name]
        {
            get
            {
                return this.GetOrCreateField(name).CurrentValue;
            }

            set
            {
                this.SetFieldValue(name, value);
            }
        }

        public IFieldCollection Fields => new FieldCollectionWrapper(this, this.context);

        public Uri Uri => this.uri;

        public bool IsValid()
        {
            return true;
        }

        public ArrayList Validate()
        {
            return new ArrayList();
        }

        public void PartialOpen()
        {
            // Work item data is fully loaded through REST.
        }

        public void Save()
        {
            this.repository.SaveWorkItem(this);
        }

        public void RevertChanges()
        {
            foreach (var fieldName in this.dirtyFields.ToArray())
            {
                var field = this.fields[fieldName];
                field.CurrentValue = field.OriginalValue;
                field.Status = Microsoft.TeamFoundation.WorkItemTracking.Client.FieldStatus.Valid;
            }

            this.dirtyFields.Clear();
        }

        public void TryOpen()
        {
            // Work item data is fully loaded through REST.
        }

        public bool IsDirty => this.dirtyFields.Count > 0;

        public override IWorkItemLinkCollection WorkItemLinksImpl => new WorkItemLinkCollectionWrapper(this.relations, this.context);

        public IWorkItemLinkExposedCollection WorkItemLinks => new WorkItemLinkExposedCollectionWrapper(this.relations, this.context);

        public DateTime RevisedDate => this.revisedDate;

        public int Revision => this.revision;

        public IRevision LastRevision => new RevisionWrapper(this.revision, this.Fields, this.WorkItemLinks);

        public IRevision PreviousRevision => new RevisionWrapper(Math.Max(this.revision - 1, 1), this.Fields, this.WorkItemLinks);

        public IRevision NextRevision => new RevisionWrapper(this.revision, this.Fields, this.WorkItemLinks);

        public IEnumerable<IWorkItemExposed> GetRelatives(FluentQuery query)
        {
            return WorkItemLazyVisitor
                .MakeRelativesLazyVisitor(this, query);
        }

        public void TransitionToState(string state, string comment)
        {
            throw new NotSupportedException("Workflow transitions are not supported by the REST-backed repository.");
        }

        public void AddWorkItemLink(IWorkItemExposed destination, string linkTypeName)
        {
            throw new NotSupportedException("Work item link updates are not supported by the REST-backed repository.");
        }

        public void AddHyperlink(string destination)
        {
            this.AddHyperlink(destination, string.Empty);
        }

        public void AddHyperlink(string destination, string comment)
        {
            throw new NotSupportedException("Hyperlinks are not supported by the REST-backed repository.");
        }

        public void RemoveWorkItemLink(IWorkItemLinkExposed link)
        {
            throw new NotSupportedException("Work item link updates are not supported by the REST-backed repository.");
        }

        internal IEnumerable<WorkItemFieldState> GetFields()
        {
            return this.fields.Values;
        }

        internal WorkItemFieldState GetOrCreateField(string referenceName)
        {
            if (string.IsNullOrWhiteSpace(referenceName))
            {
                throw new ArgumentNullException(nameof(referenceName));
            }

            if (!this.fields.TryGetValue(referenceName, out var field))
            {
                field = new WorkItemFieldState(this.Id, referenceName, null);
                this.fields.Add(referenceName, field);
            }

            return field;
        }

        internal void SetFieldValue(string referenceName, object value)
        {
            var field = this.GetOrCreateField(referenceName);
            if (ValuesEqual(field.CurrentValue, value))
            {
                return;
            }

            field.CurrentValue = value;
            field.Status = Microsoft.TeamFoundation.WorkItemTracking.Client.FieldStatus.Valid;

            if (ValuesEqual(field.OriginalValue, value))
            {
                this.dirtyFields.Remove(referenceName);
            }
            else
            {
                this.dirtyFields.Add(referenceName);
            }
        }

        internal IReadOnlyDictionary<string, object> GetDirtyFieldValues()
        {
            return this.dirtyFields.ToDictionary(
                fieldName => fieldName,
                fieldName => this.fields[fieldName].CurrentValue,
                StringComparer.OrdinalIgnoreCase);
        }

        internal void RefreshFromData(RestWorkItemData workItem)
        {
            this.id = workItem.Id;
            this.typeName = workItem.TypeName ?? string.Empty;
            this.uri = workItem.Uri;
            this.revision = workItem.Revision;
            this.revisedDate = workItem.RevisedDate;

            var refreshedFields = new Dictionary<string, WorkItemFieldState>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in workItem.Fields)
            {
                var state = new WorkItemFieldState(this.id, field.Key, field.Value);
                if (field.Value != null)
                {
                    state.ExplicitDataType = field.Value.GetType();
                }

                refreshedFields[field.Key] = state;
            }

            this.fields = refreshedFields;
            this.relations = new List<RestWorkItemRelation>(workItem.Relations ?? Array.Empty<RestWorkItemRelation>());
            this.dirtyFields.Clear();
        }

        private static bool ValuesEqual(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            if (left is DateTime leftDateTime && right is DateTime rightDateTime)
            {
                return leftDateTime.ToUniversalTime() == rightDateTime.ToUniversalTime();
            }

            return object.Equals(left, right);
        }
    }
}
