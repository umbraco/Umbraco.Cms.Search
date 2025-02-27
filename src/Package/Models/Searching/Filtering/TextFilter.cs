namespace Package.Models.Searching.Filtering;

public record TextFilter(string FieldName, string[] Values, bool Negate) : ContainsFilter<string>(FieldName, Values, Negate)
{
}