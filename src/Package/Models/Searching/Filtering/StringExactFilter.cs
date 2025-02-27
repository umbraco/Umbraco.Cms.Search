namespace Package.Models.Searching.Filtering;

public record StringExactFilter(string FieldName, string[] Values, bool Negate) : ExactFilter<string>(FieldName, Values, Negate)
{
}