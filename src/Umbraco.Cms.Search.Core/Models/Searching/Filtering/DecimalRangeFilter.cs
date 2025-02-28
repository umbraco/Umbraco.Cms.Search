namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record DecimalRangeFilter(string FieldName, decimal? MinimumValue, decimal? MaximumValue, bool Negate)
    : RangeFilter<decimal?>(FieldName, MinimumValue, MaximumValue, Negate)
{
}