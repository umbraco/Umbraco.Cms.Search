namespace Package.Models.Searching.Faceting;

public record IntegerExactFacet(string FieldName) : ExactFacet(FieldName)
{
}