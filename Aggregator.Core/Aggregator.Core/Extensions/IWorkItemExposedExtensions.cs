using System;
using System.Globalization;

using Aggregator.Core.Interfaces;
using Aggregator.Core.Navigation;

namespace Aggregator.Core.Extensions
{
    public static class IWorkItemExposedExtensions
    {
        /// <summary>
        /// Used to convert a field to a number.  If anything goes wrong then the default value is returned.
        /// </summary>
        /// <param name="self">The work item to get the field data from</param>
        /// <param name="fieldName">The name of the field to be retrieved</param>
        /// <param name="defaultValue">Value to be returned if something goes wrong.</param>
        /// <returns></returns>
        public static TType GetField<TType>(this IWorkItemExposed self, string fieldName, TType defaultValue)
        {
            try
            {
                object rawValue = self[fieldName];
                if (rawValue == null)
                {
                    return defaultValue;
                }

                if (rawValue is TType directMatch)
                {
                    return directMatch;
                }

                Type targetType = Nullable.GetUnderlyingType(typeof(TType)) ?? typeof(TType);
                if (targetType.IsEnum)
                {
                    if (rawValue is string enumString)
                    {
                        return (TType)Enum.Parse(targetType, enumString, true);
                    }

                    return (TType)Enum.ToObject(targetType, rawValue);
                }

                if (targetType == typeof(Guid))
                {
                    return (TType)(object)Guid.Parse(Convert.ToString(rawValue, CultureInfo.InvariantCulture));
                }

                object convertedValue = Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
                return (TType)convertedValue;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static bool HasParent(this IWorkItemExposed self)
        {
            return self.HasRelation(WorkItemImplementationBase.ParentRelationship);
        }

        public static bool HasChildren(this IWorkItemExposed self)
        {
            return self.HasRelation(WorkItemImplementationBase.ChildRelationship);
        }

        // fluent API for GetRelatives
        public static FluentQuery WhereTypeIs(this IWorkItemExposed wi, string workItemType)
        {
            return new FluentQuery(wi).WhereTypeIs(workItemType);
        }

        public static FluentQuery AtMost(this IWorkItemExposed wi, int levels)
        {
            return new FluentQuery(wi).AtMost(levels);
        }

        public static FluentQuery FollowingLinks(this IWorkItemExposed wi, string linkType)
        {
            return new FluentQuery(wi).FollowingLinks(linkType);
        }
    }
}
