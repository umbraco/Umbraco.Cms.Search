namespace Umbraco.Cms.Search.Core.Models.Searching.Faceting;

// marker interface for exact facets
public interface IExactFacet
{
    string FieldName { get; }
}