using System;

using Aggregator.Core.Context;
using Aggregator.Core.Interfaces;

namespace Aggregator.Core
{
    public class RateLimiter
    {
        private readonly TimeSpan interval = TimeSpan.FromSeconds(10);

        private readonly int changes = 1;

        private readonly bool enabled;

        public RateLimiter(IRuntimeContext context)
        {
            if (context.Settings?.RateLimit != null)
            {
                this.enabled = true;
                this.interval = context.Settings.RateLimit.Interval;
                this.changes = context.Settings.RateLimit.Changes;
            }
        }

        public bool ShouldLimit(IWorkItem wi)
        {
            if (!this.enabled || wi == null || !wi.IsDirty)
            {
                return false;
            }

            try
            {
                if (!(wi.Fields["System.ChangedDate"].OriginalValue is DateTime previousChangedDate))
                {
                    return false;
                }

                DateTime watermark = DateTime.UtcNow;
                bool isRecentChange = watermark - previousChangedDate.ToUniversalTime() < this.interval;

                // The REST-backed implementation intentionally does not load full revision history.
                return isRecentChange && wi.Revision > this.changes + 1;
            }
            catch
            {
                return false;
            }
        }
    }
}
