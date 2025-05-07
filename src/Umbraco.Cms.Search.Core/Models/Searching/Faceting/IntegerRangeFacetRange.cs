namespace Umbraco.Cms.Search.Core.Models.Searching.Faceting;

public record IntegerRangeFacetRange(string Key, int? Min, int? Max)
    : RangeFacetRange<int?>(Key, Min, Max)
{
}