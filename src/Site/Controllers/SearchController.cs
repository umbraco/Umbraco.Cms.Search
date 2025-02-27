using Microsoft.AspNetCore.Mvc;
using Package.Models.Searching;
using Package.Services;
using Site.Services;
using Umbraco.Cms.Core.Services;

namespace Site.Controllers;

[ApiController]
public class SearchController : Controller
{
    private readonly ISearchService _searchService;
    private readonly IContentService _contentService;

    public SearchController(ISearchService searchService, IContentService contentService)
    {
        _searchService = searchService;
        _contentService = contentService;
    }

    [Route("/search/query")]
    public async Task<IActionResult> Search(string? query = null, [FromQuery]string[]? filters = null, [FromQuery]string[]? facets = null, string? culture = null, string? segment = null)
    {
        var filterDictionary = SplitParameters(filters);
        var filterValues = filterDictionary?.Select(kvp => kvp.Key.InvariantContains("integer")
            ? new IntegerExactFilter(kvp.Key, kvp.Value.Select(v => int.TryParse(v, out var i) ? i : -1).Where(i => i > 0).ToArray(), false)
            : (Filter)new StringExactFilter(kvp.Key, kvp.Value, false)
        ).ToArray();

        var facetValues = facets?.Select(f => f.InvariantContains("integer")
            ? new IntegerExactFacet(f)
            : (Facet)new StringExactFacet(f)
        ).ToArray();
        
        var result = await _searchService.SearchAsync(query, filterValues, facetValues, culture, segment, 0, 100);

        return Ok(new
        {
            Total = result.Total,
            Documents = result.Ids.Select(id => _contentService.GetById(id)!.GetCultureName(culture)).ToArray(),
            Facets = result.Facets
        });

        Dictionary<string, string[]>? SplitParameters(string[]? parameters)
            => parameters?.Any() is true
                ? parameters
                    .Select(f => f.Split(':', StringSplitOptions.RemoveEmptyEntries))
                    .Where(parts => parts.Length == 2)
                    .ToDictionary(part => part[0], part => part[1].Split(',').Except(["*"]).ToArray())
                : null;
    }

    [Route("/search/dump")]
    public IActionResult Dump()
    {
        var dump = ((InMemoryIndexAndSearchService)_searchService).Dump();
        return Ok(dump.Select(kvp => new
        {
            Name = _contentService.GetById(kvp.Key)!.Name,
            Values = kvp.Value
        }));
    }
}