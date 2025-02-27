namespace Package.Models.Searching.Faceting;

public record StringExactFacet(string FieldName) : ExactFacet(FieldName)
{
}