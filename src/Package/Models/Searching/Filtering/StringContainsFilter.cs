namespace Package.Models.Searching.Filtering;

public record StringContainsFilter(string FieldName, string[] Values, bool Negate) : ContainsFilter<string>(FieldName, Values, Negate)
{
}