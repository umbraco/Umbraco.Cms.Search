namespace Package.Models.Searching.Faceting;

public record KeywordFacet(string FieldName) : ExactFacet(FieldName)
{
}