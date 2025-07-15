using Examine.Search;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Umbraco.Cms.Search.Provider.Examine.Extensions;

public static class QueryExtensions
{
    public static void AddRangeFilter<T>(this IBooleanOperation query, string fieldName, RangeFilter<T> filter) where T : struct
    {
        var ranges = filter.Ranges;
        if (filter.Negate)
        {
            foreach (var integerRange in ranges)
            {
                query.Not().RangeQuery<T>([fieldName], integerRange.MinimumValue, integerRange.MaximumValue);
            }
        }
        else
        {
            query.And().Group(nestedQuery =>
            {
                var rangeQuery = nestedQuery.RangeQuery<T>([fieldName], ranges[0].MinimumValue, ranges[0].MaximumValue);
                for (var i = 1; i < ranges.Length; i++)
                {
                    rangeQuery.Or().RangeQuery<T>([fieldName], ranges[i].MinimumValue, ranges[i].MaximumValue);
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
                // Examine does not support decimals out of the box, so convert to double, we might loose some precision here (after 17 digits).
                query.Not().Group(nestedQuery => nestedQuery.Field(fieldName, filterValue));
            }
        }
        else
        {
            foreach (var decimalFilterValue in filter.Values)
            {
                // Examine does not support decimals out of the box, so convert to double, we might loose some precision here (after 17 digits).
                query.And().Group(nestedQuery => nestedQuery.Field(fieldName, decimalFilterValue));
            }
        }
    }
    
    public static void AddExactFilter<T>(this IBooleanOperation query, string fieldName, ExactFilter<T> filter, Func<T> valueConverter) where T : struct
    {
        if (filter.Negate)
        {
            foreach (var filterValue in filter.Values)
            {
                // Examine does not support decimals out of the box, so convert to double, we might loose some precision here (after 17 digits).
                query.Not().Group(nestedQuery => nestedQuery.Field(fieldName, filterValue));
            }
        }
        else
        {
            foreach (var decimalFilterValue in filter.Values)
            {
                // Examine does not support decimals out of the box, so convert to double, we might loose some precision here (after 17 digits).
                query.And().Group(nestedQuery => nestedQuery.Field(fieldName, decimalFilterValue));
            }
        }
    }
}