namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record DateTimeOffsetRangeFilter(string FieldName, FilterRange<DateTimeOffset?>[] Ranges, bool Negate)
    : RangeFilter<DateTimeOffset?>(FieldName, Ranges, Negate)
{
    public static DateTimeOffsetRangeFilter Single(string fieldName, DateTimeOffset? minimumValue, DateTimeOffset? maximumValue, bool negate)
        => new (fieldName, [new FilterRange<DateTimeOffset?>(minimumValue, maximumValue)], negate);
}