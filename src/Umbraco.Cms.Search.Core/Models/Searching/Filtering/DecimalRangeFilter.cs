namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record DecimalRangeFilter(string FieldName, FilterRange<decimal?>[] Ranges, bool Negate)
    : RangeFilter<decimal?>(FieldName, Ranges, Negate)
{
    public static DecimalRangeFilter Single(string fieldName, decimal? minimumValue, decimal? maximumValue, bool negate)
        => new (fieldName, [new FilterRange<decimal?>(minimumValue, maximumValue)], negate);
}