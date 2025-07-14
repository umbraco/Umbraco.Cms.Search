using Examine;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;

namespace Umbraco.Cms.Search.Provider.Examine.Mapping;

public interface IExamineMapper
{
    IEnumerable<FacetResult> MapFacets(ISearchResults searchResults, IEnumerable<Facet> queryFacets);
}