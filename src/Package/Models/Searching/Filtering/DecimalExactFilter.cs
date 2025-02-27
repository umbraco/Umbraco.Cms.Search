namespace Package.Models.Searching.Filtering;

public record DecimalExactFilter(string FieldName, decimal[] Values, bool Negate) : ExactFilter<decimal>(FieldName, Values, Negate)
{
}