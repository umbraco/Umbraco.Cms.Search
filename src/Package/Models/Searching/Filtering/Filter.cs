namespace Package.Models.Searching.Filtering;

public abstract record Filter(string FieldName, bool Negate)
{
}