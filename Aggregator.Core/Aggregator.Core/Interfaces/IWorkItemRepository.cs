using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aggregator.Core.Interfaces
{
    /// <summary>
    /// Decouples Core from the underlying work item data access implementation.
    /// </summary>
    public interface IWorkItemRepository : IWorkItemRepositoryExposed
    {
        ReadOnlyCollection<IWorkItem> LoadedWorkItems { get; }

        ReadOnlyCollection<IWorkItem> CreatedWorkItems { get; }
    }
}
