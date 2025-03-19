using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;

namespace Umbraco.Cms.Search.BackOffice.Trees;

public class MediaSearchableTree : ISearchableTree
{
    private readonly IEntityService _entityService;
    private readonly ISearchService _searchService;

    public MediaSearchableTree(IEntityService entityService, ISearchService searchService)
    {
        _entityService = entityService;
        _searchService = searchService;
    }

    public string TreeAlias => Constants.Trees.Media;

    public async Task<EntitySearchResults> SearchAsync(string query, int pageSize, long pageIndex, string searchFrom = null)
    {
        var (skip, take) = ConvertPagingToSkipTake(pageSize, pageIndex);
        var searchResult = await _searchService.SearchAsync(Core.Constants.IndexAliases.DraftMedia, query, null, null, null, null, null, null, skip, take);

        var searchResultKeys = searchResult.Documents.Select(document => document.Key).ToArray();
        if (searchResultKeys.Length is 0)
        {
            return new EntitySearchResults([], searchResult.Total);
        }
        
        var entities = _entityService
            .GetAll(UmbracoObjectTypes.Media, searchResultKeys)
            .OfType<IContentEntitySlim>();

        var searchResultEntities = entities.Select(entity => new SearchResultEntity
        {
            Id = entity.Id,
            Alias = entity.ContentTypeAlias,
            Name = entity.Name,
            Icon = entity.ContentTypeIcon,
            Key = entity.Key,
            ParentId = entity.ParentId,
            Path = entity.Path,
            Score = 1,
            Trashed = entity.Trashed,
            Udi = new GuidUdi(Constants.UdiEntityType.Media, entity.Key)
        });

        return new EntitySearchResults(searchResultEntities, searchResult.Total);
    }

    private static (int Skip, int Take) ConvertPagingToSkipTake(int pageSize, long pageIndex)
        => ((int)(pageSize * (pageIndex)), pageSize);
}