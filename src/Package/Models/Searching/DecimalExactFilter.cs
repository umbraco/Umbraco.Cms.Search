namespace Package.Models.Searching;

public record DecimalExactFilter(string Key, decimal[] Values, bool Negate) : ExactFilter<decimal>(Key, Values, Negate)
{
}