namespace Umbraco.Cms.Search.Core.Models.Searching.Faceting;

public record DecimalRangeFacetRange(string Key, decimal? Min, decimal? Max)
    : RangeFacetRange<decimal?>(Key, Min, Max)
{
}