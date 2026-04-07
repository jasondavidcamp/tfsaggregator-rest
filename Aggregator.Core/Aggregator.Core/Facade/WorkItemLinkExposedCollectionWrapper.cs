using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Aggregator.Core.Context;
using Aggregator.Core.Interfaces;

namespace Aggregator.Core.Facade
{
    internal class WorkItemLinkExposedCollectionWrapper : IWorkItemLinkExposedCollection
    {
        private readonly IEnumerable<RestWorkItemRelation> workItemLinkCollection;

        private readonly IRuntimeContext context;

        public WorkItemLinkExposedCollectionWrapper(IEnumerable<RestWorkItemRelation> workItemLinkCollection, IRuntimeContext context)
        {
            this.workItemLinkCollection = workItemLinkCollection ?? Enumerable.Empty<RestWorkItemRelation>();
            this.context = context;
        }

        public IEnumerator<IWorkItemLinkExposed> GetEnumerator()
        {
            foreach (RestWorkItemRelation item in this.workItemLinkCollection)
            {
                yield return new WorkItemLinkExposedWrapper(item, this.context);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
