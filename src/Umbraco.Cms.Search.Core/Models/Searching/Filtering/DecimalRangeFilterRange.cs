namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record DecimalRangeFilterRange(decimal? Min, decimal? Max)
    : RangeFilterRange<decimal?>(Min, Max)
{
}