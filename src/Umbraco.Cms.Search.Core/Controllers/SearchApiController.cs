using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.ViewModels;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.Core.Controllers;

[ApiVersion("1.0")]
public class SearchApiController : ApiControllerBase
{
    private readonly ISearcherResolver _searcherResolver;
    private readonly IEntityService _entityService;

    public SearchApiController(ISearcherResolver searcherResolver, IEntityService entityService)
    {
        _searcherResolver = searcherResolver;
        _entityService = entityService;
    }

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
            Documents = ToDocumentViewModels(result.Documents),
            Facets = result.Facets.Select(f => new FacetResultViewModel
            {
                FieldName = f.FieldName,
                Values = f.Values,
            }),
        });
    }

    private IEnumerable<DocumentViewModel> ToDocumentViewModels(IEnumerable<Document> documents)
    {
        foreach (IGrouping<UmbracoObjectTypes, Document> group in documents.GroupBy(d => d.ObjectType))
        {
            Document[] groupDocuments = group.ToArray();
            Guid[] keys = groupDocuments.Select(d => d.Id).ToArray();
            var namesByKey = _entityService
                .GetAll(group.Key, keys)
                .ToDictionary(e => e.Key, e => e.Name ?? string.Empty);

            foreach (Document document in groupDocuments)
            {
                yield return new DocumentViewModel
                {
                    Id = document.Id,
                    ObjectType = document.ObjectType,
                    Name = namesByKey.GetValueOrDefault(document.Id),
                };
            }
        }
    }
}
