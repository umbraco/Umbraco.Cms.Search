namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record DateTimeOffsetRangeFilter(string FieldName, DateTimeOffset? MinimumValue, DateTimeOffset? MaximumValue, bool Negate)
    : RangeFilter<DateTimeOffset?>(FieldName, MinimumValue, MaximumValue, Negate)
{
}