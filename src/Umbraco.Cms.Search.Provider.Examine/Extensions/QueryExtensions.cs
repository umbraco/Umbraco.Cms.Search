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
                query.Not().RangeQuery<T>([fieldName], integerRange.MinimumValue, integerRange.MaximumValue, true, false);
            }
        }
        else
        {
            query.And().Group(nestedQuery =>
            {
                var rangeQuery = nestedQuery.RangeQuery<T>([fieldName], ranges[0].MinimumValue, ranges[0].MaximumValue, true, false);
                for (var i = 1; i < ranges.Length; i++)
                {
                    rangeQuery.Or().RangeQuery<T>([fieldName], ranges[i].MinimumValue, ranges[i].MaximumValue, true, false);
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