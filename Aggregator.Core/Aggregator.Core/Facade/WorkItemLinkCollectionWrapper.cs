using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Aggregator.Core.Context;
using Aggregator.Core.Interfaces;

namespace Aggregator.Core.Facade
{
    internal class WorkItemLinkCollectionWrapper : IWorkItemLinkCollection
    {
        private readonly IEnumerable<RestWorkItemRelation> workItemLinkCollection;

        private readonly IRuntimeContext context;

        public WorkItemLinkCollectionWrapper(IEnumerable<RestWorkItemRelation> workItemLinkCollection, IRuntimeContext context)
        {
            this.workItemLinkCollection = workItemLinkCollection ?? Enumerable.Empty<RestWorkItemRelation>();
            this.context = context;
        }

        public IEnumerator<IWorkItemLink> GetEnumerator()
        {
            foreach (RestWorkItemRelation item in this.workItemLinkCollection)
            {
                yield return new WorkItemLinkWrapper(item, this.context);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(IWorkItemLink link)
        {
            throw new InvalidOperationException("Add is valid on mocks only");
        }

        public bool Contains(IWorkItemLink link)
        {
            return this.workItemLinkCollection.Any(item =>
                string.Equals(item.LinkTypeEndImmutableName, link.LinkTypeEndImmutableName, StringComparison.OrdinalIgnoreCase)
                && item.TargetId == link.TargetId);
        }
    }
}
