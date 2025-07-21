namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record DateTimeOffsetRangeFilterRange(DateTimeOffset? Min, DateTimeOffset? Max)
    : RangeFilterRange<DateTimeOffset?>(Min, Max)
{
}