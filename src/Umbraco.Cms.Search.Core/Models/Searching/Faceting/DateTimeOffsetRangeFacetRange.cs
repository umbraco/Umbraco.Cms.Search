namespace Umbraco.Cms.Search.Core.Models.Searching.Faceting;

public record DateTimeOffsetRangeFacetRange(string Key, DateTimeOffset? Min, DateTimeOffset? Max)
    : RangeFacetRange<DateTimeOffset?>(Key, Min, Max)
{
}