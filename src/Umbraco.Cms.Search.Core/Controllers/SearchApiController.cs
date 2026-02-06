using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.ViewModels;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Core.Controllers;

[ApiVersion("1.0")]
public class SearchApiController : ApiControllerBase
{
    private readonly ISearcherResolver _searcherResolver;

    public SearchApiController(ISearcherResolver searcherResolver)
        => _searcherResolver = searcherResolver;

    [HttpPost("search")]
    [ProducesResponseType<SearchResultViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromBody] SearchRequestModel request, int skip = 0, int take = 100)
    {
        if (string.IsNullOrWhiteSpace(request.IndexAlias))
        {
            return BadRequest("The indexAlias parameter must be provided and cannot be empty.");
        }

        ISearcher? searcher = _searcherResolver.GetSearcher(request.IndexAlias);
        if (searcher is null)
        {
            return NotFound($"No searcher was found for the index alias '{request.IndexAlias}'.");
        }

        SearchResult result = await searcher.SearchAsync(
            request.IndexAlias,
            request.Query,
            request.Filters,
            request.Facets,
            request.Sorters,
            request.Culture,
            request.Segment,
            request.AccessContext,
            skip,
            take);

        return Ok(new SearchResultViewModel
        {
            Total = result.Total,
            Documents = result.Documents.Select(d => new DocumentViewModel
            {
                Id = d.Id,
                ObjectType = d.ObjectType,
            }),
            Facets = result.Facets.Select(f => new FacetResultViewModel
            {
                FieldName = f.FieldName,
                Values = f.Values,
            }),
        });
    }
}
