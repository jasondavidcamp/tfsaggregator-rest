using System;
using System.Collections.Generic;
using System.Xml;

using Aggregator.Core.Interfaces;

using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Aggregator.Core.Facade
{
    internal sealed class RestWorkItemData
    {
        public int Id { get; set; }

        public int Revision { get; set; }

        public string TypeName { get; set; }

        public Uri Uri { get; set; }

        public DateTime RevisedDate { get; set; }

        public IDictionary<string, object> Fields { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public IList<RestWorkItemRelation> Relations { get; } = new List<RestWorkItemRelation>();
    }

    internal sealed class RestWorkItemRelation : IEquatable<RestWorkItemRelation>
    {
        public string LinkTypeEndImmutableName { get; set; }

        public int TargetId { get; set; }

        public bool Equals(RestWorkItemRelation other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(this.LinkTypeEndImmutableName, other.LinkTypeEndImmutableName, StringComparison.OrdinalIgnoreCase)
                && this.TargetId == other.TargetId;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as RestWorkItemRelation);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(this.LinkTypeEndImmutableName ?? string.Empty);
                hashCode = (hashCode * 397) ^ this.TargetId;
                return hashCode;
            }
        }
    }

    internal sealed class WorkItemFieldState
    {
        public WorkItemFieldState(int workItemId, string referenceName, object currentValue)
        {
            this.WorkItemId = workItemId;
            this.Name = referenceName;
            this.ReferenceName = referenceName;
            this.CurrentValue = currentValue;
            this.OriginalValue = currentValue;
        }

        public int WorkItemId { get; }

        public string Name { get; set; }

        public string ReferenceName { get; }

        public object CurrentValue { get; set; }

        public object OriginalValue { get; private set; }

        public FieldStatus Status { get; set; } = FieldStatus.Valid;

        public Type ExplicitDataType { get; set; }

        public Type DataType => this.ExplicitDataType ?? this.CurrentValue?.GetType() ?? this.OriginalValue?.GetType() ?? typeof(object);

        public void AcceptCurrentValue()
        {
            this.OriginalValue = this.CurrentValue;

            if (this.ExplicitDataType == null && this.CurrentValue != null)
            {
                this.ExplicitDataType = this.CurrentValue.GetType();
            }

            this.Status = FieldStatus.Valid;
        }
    }

    internal sealed class RestWorkItemType : IWorkItemType
    {
        public RestWorkItemType(string name)
        {
            this.Name = name ?? string.Empty;
        }

        public string Name { get; }

        public XmlDocument Export(bool includeGlobalListsFlag)
        {
            throw new NotSupportedException("Work item type metadata is not supported by the REST-backed repository.");
        }
    }
}
