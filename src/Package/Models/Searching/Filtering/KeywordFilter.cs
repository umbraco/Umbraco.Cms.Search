namespace Package.Models.Searching.Filtering;

public record KeywordFilter(string FieldName, string[] Values, bool Negate) : ExactFilter<string>(FieldName, Values, Negate)
{
}