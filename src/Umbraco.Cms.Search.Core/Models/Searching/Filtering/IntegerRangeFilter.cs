namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record IntegerRangeFilter(string FieldName, int? MinimumValue, int? MaximumValue, bool Negate)
    : RangeFilter<int?>(FieldName, MinimumValue, MaximumValue, Negate)
{
}