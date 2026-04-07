using Aggregator.Core.Context;
using Aggregator.Core.Interfaces;

namespace Aggregator.Core.Facade
{
    internal class WorkItemLinkWrapper : IWorkItemLink
    {
        private readonly RestWorkItemRelation link;

        private readonly IWorkItemRepository store;

        public WorkItemLinkWrapper(RestWorkItemRelation link, IRuntimeContext context)
        {
            this.link = link;
            this.store = context.WorkItemRepository;
        }

        public string LinkTypeEndImmutableName => this.link.LinkTypeEndImmutableName;

        public int TargetId => this.link.TargetId;

        public bool IsNew => false;

        public IWorkItem Target => this.store.GetWorkItem(this.TargetId);
    }
}
