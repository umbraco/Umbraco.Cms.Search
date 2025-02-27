namespace Package.Models.Searching.Filtering;

public abstract record RangeFilter<T>(string FieldName, T MinimumValue, T MaximumValue, bool Negate)
    : Filter(FieldName, Negate), IRangeFilter
{
}