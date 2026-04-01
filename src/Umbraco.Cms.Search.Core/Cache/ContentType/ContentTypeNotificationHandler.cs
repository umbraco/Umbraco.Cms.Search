using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Cache.ContentType;

internal sealed class ContentTypeNotificationHandler
    : ContentNotificationHandlerBase<ContentTypeCacheRefresher.JsonPayload>,
        IDistributedCacheNotificationHandler<ContentTypeChangedNotification>
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentService _contentService;

    public ContentTypeNotificationHandler(
        DistributedCache distributedCache,
        IOriginProvider originProvider,
        IIndexDocumentService indexDocumentService,
        IContentTypeService contentTypeService,
        IContentService contentService)
        : base(distributedCache, originProvider, indexDocumentService)
    {
        _contentTypeService = contentTypeService;
        _contentService = contentService;
    }

    protected override Guid CacheRefresherUniqueId => ContentTypeCacheRefresher.UniqueId;

    public void Handle(ContentTypeChangedNotification notification)
    {
        ContentTypeChange<IContentType>[] changes = notification.Changes.ToArray();

        ContentTypeCacheRefresher.JsonPayload[] payloads = changes
            .Select(change => new ContentTypeCacheRefresher.JsonPayload(change.Item.Key, change.ChangeTypes))
            .ToArray();

        FlushDocumentIndexCacheForAffectedContent(changes);

        HandlePayloads(payloads);
    }

    private void FlushDocumentIndexCacheForAffectedContent(ContentTypeChange<IContentType>[] changes)
    {
        int[] directContentTypeIds = changes
            .Where(change => change.ChangeTypes is not ContentTypeChangeTypes.None)
            .Select(change => change.Item.Id)
            .Distinct()
            .ToArray();

        if (directContentTypeIds.Length == 0)
        {
            return;
        }

        int[] allContentTypeIds = ExpandWithDependentContentTypes(directContentTypeIds);
        Guid[] contentKeys = GetContentKeysOfTypes(allContentTypeIds);
        FlushDocumentIndexCacheForContentKeys(contentKeys);
    }

    private int[] ExpandWithDependentContentTypes(int[] contentTypeIds)
    {
        var contentTypeIdSet = new HashSet<int>(contentTypeIds);
        int[] dependentTypeIds = _contentTypeService.GetAll()
            .Where(ct => ct.CompositionIds().Any(id => contentTypeIdSet.Contains(id)))
            .Select(ct => ct.Id)
            .ToArray();

        return contentTypeIds.Union(dependentTypeIds).ToArray();
    }

    private Guid[] GetContentKeysOfTypes(int[] contentTypeIds)
    {
        var keys = new List<Guid>();
        var pageIndex = 0L;

        while (true)
        {
            IContent[] page = _contentService.GetPagedOfTypes(
                contentTypeIds, pageIndex, 1000, out long totalRecords, null, null).ToArray();
            keys.AddRange(page.Select(c => c.Key));
            pageIndex++;

            if (keys.Count >= totalRecords)
            {
                break;
            }
        }

        return keys.ToArray();
    }
}
