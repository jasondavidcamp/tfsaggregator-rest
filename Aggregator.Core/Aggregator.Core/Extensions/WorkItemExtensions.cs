using System.Linq;
using System.Text;

using Aggregator.Core.Interfaces;

namespace Aggregator.Core.Extensions
{
    public static class WorkItemExtensions
    {
        public static string GetInvalidWorkItemFieldsList(this IWorkItem wi)
        {
            StringBuilder sb = new StringBuilder();
            if (wi.IsValid())
            {
                sb.Append("None");
            }
            else
            {
                foreach (object field in wi.Validate().Cast<object>())
                {
                    if (field is IField wrappedField)
                    {
                        sb.AppendLine(wrappedField.ReferenceName);
                    }
                    else if (field != null)
                    {
                        sb.AppendLine(field.ToString());
                    }
                }
            }

            return sb.ToString();
        }
    }
}
