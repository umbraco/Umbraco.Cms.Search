using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.BackOffice.Trees;

public class ContentSearchableTree : ISearchableTreeWithCulture
{
    private readonly IEntityService _entityService;
    private readonly ISearchService _searchService;

    public ContentSearchableTree(IEntityService entityService, ISearchService searchService)
    {
        _entityService = entityService;
        _searchService = searchService;
    }

    public string TreeAlias => Constants.Trees.Content;

    public async Task<EntitySearchResults> SearchAsync(string query, int pageSize, long pageIndex, string searchFrom = null)
        => await SearchAsync(query, pageSize, pageIndex, searchFrom, null);

    public async Task<EntitySearchResults> SearchAsync(string query, int pageSize, long pageIndex, string? searchFrom = null, string? culture = null)
    {
        var (skip, take) = ConvertPagingToSkipTake(pageSize, pageIndex);
        var searchResult = await _searchService.SearchAsync("TODO", query, null, null, null, culture.NullOrWhiteSpaceAsNull(), null, null, skip, take);

        var searchResultKeysAsArray = searchResult.Keys as Guid[] ?? searchResult.Keys.ToArray();
        if (searchResultKeysAsArray.Length is 0)
        {
            return new EntitySearchResults([], searchResult.Total);
        }
        
        var entities = _entityService
            .GetAll(UmbracoObjectTypes.Document, searchResultKeysAsArray)
            .OfType<IDocumentEntitySlim>();

        var searchResultEntities = entities.Select(entity => new SearchResultEntity
        {
            Id = entity.Id,
            Alias = entity.ContentTypeAlias,
            Name = culture is not null && entity.CultureNames.TryGetValue(culture, out var name)
                ? name
                : entity.Name,
            Icon = entity.ContentTypeIcon,
            Key = entity.Key,
            ParentId = entity.ParentId,
            Path = entity.Path,
            Score = 1,
            Trashed = entity.Trashed,
            Udi = new GuidUdi(Constants.UdiEntityType.Document, entity.Key)
        });

        return new EntitySearchResults(searchResultEntities, searchResult.Total);
    }

    private static (int Skip, int Take) ConvertPagingToSkipTake(int pageSize, long pageIndex)
        => ((int)(pageSize * (pageIndex)), pageSize);
}