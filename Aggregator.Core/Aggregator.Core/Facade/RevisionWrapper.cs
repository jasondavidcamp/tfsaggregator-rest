using Aggregator.Core.Interfaces;

namespace Aggregator.Core.Facade
{
    public class RevisionWrapper : IRevision
    {
        public RevisionWrapper(int index, IFieldCollection fields, IWorkItemLinkExposedCollection workItemLinks)
        {
            this.Index = index;
            this.Fields = fields;
            this.WorkItemLinks = workItemLinks;
        }

        public object this[string name] => this.Fields[name].Value;

        public IFieldCollection Fields { get; }

        public int Index { get; }

        public IWorkItemLinkExposedCollection WorkItemLinks { get; }
    }
}
