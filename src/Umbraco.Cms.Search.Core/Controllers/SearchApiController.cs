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

            // Default to an empty lookup; for unknown or unsupported object types
            // we will skip the entity lookup and return documents with null name/icon.
            Dictionary<Guid, IEntitySlim> namesByKey = new();

            if (group.Key != UmbracoObjectTypes.Unknown)
            {
                try
                {
                    namesByKey = _entityService
                        .GetAll(group.Key, keys)
                        .ToDictionary(e => e.Key, e => e);
                }
                catch (ArgumentException)
                {
                    // If the object type is not supported by IEntityService.GetAll,
                    // fall back to an empty lookup so we still return documents.
                    namesByKey = new Dictionary<Guid, IEntitySlim>();
                }
            }
            foreach (Document document in groupDocuments)
            {
                IEntitySlim? entity = namesByKey.GetValueOrDefault(document.Id);
                yield return new DocumentViewModel
                {
                    Id = document.Id,
                    ObjectType = document.ObjectType,
                    Name = entity?.Name,
                    Icon = GetIconForEntity(entity),
                };
            }
        }
    }

    private static string? GetIconForEntity(IEntitySlim? entity)
    {
        if (entity is IContentEntitySlim slim)
        {
            return slim.ContentTypeIcon;
        }

        return null;
    }
}
