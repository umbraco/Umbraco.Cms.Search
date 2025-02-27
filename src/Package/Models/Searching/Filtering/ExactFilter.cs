namespace Package.Models.Searching.Filtering;

public abstract record ExactFilter<T>(string FieldName, T[] Values, bool Negate) : Filter(FieldName, Negate)
{
}