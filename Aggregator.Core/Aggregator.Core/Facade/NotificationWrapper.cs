using System;
using Aggregator.Core.Interfaces;

using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Server;

namespace Aggregator.Core.Facade
{
    public class NotificationWrapper : INotification
    {
        private readonly NotificationType notification;

        private readonly WorkItemChangedEvent eventArgs;

        public NotificationWrapper(NotificationType notification, WorkItemChangedEvent eventArgs)
        {
            this.notification = notification;
            this.eventArgs = eventArgs;
        }

        public Interfaces.ChangeTypes ChangeType
        {
            get
            {
                return (Interfaces.ChangeTypes)this.eventArgs.ChangeType;
            }
        }

        public int WorkItemId
        {
            get
            {
                if (int.TryParse(this.eventArgs.WorkItemId, out int workItemId))
                {
                    return workItemId;
                }

                throw new InvalidOperationException($"Unexpected work item id '{this.eventArgs.WorkItemId}'.");
            }
        }

        public string ProjectUri
        {
            get
            {
                // HACK but moving Notification code to RequestContext may cure...
                return string.Format("vstfs:///Classification/TeamProject/{0}", this.eventArgs.ProjectNodeId);
            }
        }

        public string ChangerTeamFoundationId
        {
            get
            {
                return this.eventArgs.ChangerTeamFoundationId;
            }
        }
    }
}
