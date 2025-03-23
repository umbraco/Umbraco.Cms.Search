namespace Umbraco.Cms.Search.Core.Models.Searching.Filtering;

public abstract record RangeFilter<T>(string FieldName, FilterRange<T>[] Ranges, bool Negate)
    : Filter(FieldName, Negate), IRangeFilter
{
}