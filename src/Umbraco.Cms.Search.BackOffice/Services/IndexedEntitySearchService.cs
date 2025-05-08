using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Cms.Search.BackOffice.Services;

internal sealed class IndexedEntitySearchService : IndexedSearchServiceBase, IIndexedEntitySearchService
{
    private readonly ISearchService _searchService;
    private readonly IEntityService _entityService;

    public IndexedEntitySearchService(ISearchService searchService, IEntityService entityService)
    {
        _searchService = searchService;
        _entityService = entityService;
    }

    public PagedModel<IEntitySlim> Search(UmbracoObjectTypes objectType, string query, int skip = 0, int take = 100, bool ignoreUserStartNodes = false)
        => SearchAsync(objectType, query, null, null, null, skip, take, ignoreUserStartNodes).GetAwaiter().GetResult();

    public async Task<PagedModel<IEntitySlim>> SearchAsync(
        UmbracoObjectTypes objectType,
        string query,
        Guid? parentId,
        IEnumerable<Guid>? contentTypeIds,
        bool? trashed,
        int skip = 0,
        int take = 100,
        bool ignoreUserStartNodes = false)
    {
        var indexAlias = objectType switch
        {
            UmbracoObjectTypes.Document => Constants.IndexAliases.DraftContent,
            UmbracoObjectTypes.Media => Constants.IndexAliases.DraftMedia,
            UmbracoObjectTypes.Member => Constants.IndexAliases.DraftMembers,
            _ => throw new ArgumentOutOfRangeException(nameof(objectType), objectType, null)
        };

        var filters = ParseFilters(query, parentId, out var effectiveQuery);

        var contentTypeIdsAsArray = contentTypeIds as Guid[] ?? contentTypeIds?.ToArray();
        if (contentTypeIdsAsArray?.Length > 0)
        {
            filters.Add(
                new KeywordFilter(
                    Core.Constants.FieldNames.ContentTypeId,
                    contentTypeIdsAsArray.Select(contentTypeId => contentTypeId.AsKeyword()).ToArray(),
                    false
                )
            );
        }
        
        // TODO: add trashed state filtering
        // TODO: add user start nodes filtering
        
        var result = await _searchService.SearchAsync(
            indexAlias,
            query: effectiveQuery,
            filters: filters,
            facets: null,
            sorters: null,
            culture: null,
            segment: null,
            accessContext: null,
            skip,
            take);

        var resultKeys = result.Documents.Select(d => d.Id).ToArray();
        var resultEntities = resultKeys.Any()
            ? _entityService.GetAll(objectType, resultKeys).ToArray()
            : [];

        return new PagedModel<IEntitySlim> { Items = resultEntities, Total = result.Total };
    }
}
