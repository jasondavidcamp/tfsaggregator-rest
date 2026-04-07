using Aggregator.Core.Context;
using Aggregator.Core.Interfaces;

namespace Aggregator.Core.Facade
{
    internal class WorkItemLinkExposedWrapper : IWorkItemLinkExposed
    {
        private readonly RestWorkItemRelation item;

        private readonly IRuntimeContext context;

        public WorkItemLinkExposedWrapper(RestWorkItemRelation item, IRuntimeContext context)
        {
            this.item = item;
            this.context = context;
        }

        public string LinkTypeEndImmutableName => this.item.LinkTypeEndImmutableName;

        public int TargetId => this.item.TargetId;

        public IWorkItemExposed Target => this.context.WorkItemRepository.GetWorkItem(this.item.TargetId);
    }
}
