namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public record IntegerRangeFilter(string FieldName, FilterRange<int?>[] Ranges, bool Negate)
    : RangeFilter<int?>(FieldName, Ranges, Negate)
{
    public static IntegerRangeFilter Single(string fieldName, int? minimumValue, int? maximumValue, bool negate)
        => new (fieldName, [new FilterRange<int?>(minimumValue, maximumValue)], negate);
}