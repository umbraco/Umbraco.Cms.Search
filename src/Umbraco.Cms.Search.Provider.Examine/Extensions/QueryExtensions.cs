using Examine.Search;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Provider.Examine.Models.Searching.Filtering;

namespace Umbraco.Cms.Search.Provider.Examine.Extensions;

internal static class QueryExtensions
{
    public static void AddRangeFilter<T>(this IBooleanOperation query, string fieldName, bool negate, IEnumerable<FilterRange<T>> ranges)
        where T : struct
    {
        FilterRange<T>[] rangesAsArray = ranges as FilterRange<T>[] ?? ranges.ToArray();
        if (negate)
        {
            foreach (var range in rangesAsArray)
            {
                query.Not().RangeQuery<T>([fieldName], range.MinValue, range.MaxValue, true, false);
            }
        }
        else
        {
            query.And().Group(nestedQuery =>
            {
                var rangeQuery = nestedQuery.RangeQuery<T>([fieldName], rangesAsArray[0].MinValue, rangesAsArray[0].MaxValue, true, false);
                for (var i = 1; i < rangesAsArray.Length; i++)
                {
                    rangeQuery.Or().RangeQuery<T>([fieldName], rangesAsArray[i].MinValue, rangesAsArray[i].MaxValue, true, false);
                }

                return rangeQuery;
            });
        }
    }

    public static void AddExactFilter<T>(this IBooleanOperation query, string fieldName, ExactFilter<T> filter) where T : struct
    {
        if (filter.Negate)
        {
            foreach (var filterValue in filter.Values)
            {
                query.Not().Group(nestedQuery => nestedQuery.Field(fieldName, filterValue));
            }
        }
        else
        {
            if (filter.Values.Any() is false)
            {
                return;
            }
            query.And().Group(nestedQuery =>
            {
                var nestedBooleanOperation = nestedQuery.Field(fieldName, filter.Values[0]);
                for (var i = 1; i < filter.Values.Length; i++)
                {
                    nestedBooleanOperation.Or().Field(fieldName, filter.Values[i]);
                }

                return nestedBooleanOperation;
            });

        }
    }
}
