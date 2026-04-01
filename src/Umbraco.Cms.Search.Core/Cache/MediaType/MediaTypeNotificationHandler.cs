using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Cache.MediaType;

internal sealed class MediaTypeNotificationHandler
    : ContentNotificationHandlerBase<MediaTypeCacheRefresher.JsonPayload>,
        IDistributedCacheNotificationHandler<MediaTypeChangedNotification>
{
    private readonly IMediaTypeService _mediaTypeService;
    private readonly IMediaService _mediaService;

    public MediaTypeNotificationHandler(
        DistributedCache distributedCache,
        IOriginProvider originProvider,
        IIndexDocumentService indexDocumentService,
        IMediaTypeService mediaTypeService,
        IMediaService mediaService)
        : base(distributedCache, originProvider, indexDocumentService)
    {
        _mediaTypeService = mediaTypeService;
        _mediaService = mediaService;
    }

    protected override Guid CacheRefresherUniqueId => MediaTypeCacheRefresher.UniqueId;

    public void Handle(MediaTypeChangedNotification notification)
    {
        ContentTypeChange<IMediaType>[] changes = notification.Changes.ToArray();

        MediaTypeCacheRefresher.JsonPayload[] payloads = changes
            .Select(change => new MediaTypeCacheRefresher.JsonPayload(change.Item.Key, change.ChangeTypes))
            .ToArray();

        FlushDocumentIndexCacheForAffectedContent(changes);

        HandlePayloads(payloads);
    }

    private void FlushDocumentIndexCacheForAffectedContent(ContentTypeChange<IMediaType>[] changes)
    {
        int[] directMediaTypeIds = changes
            .Where(change => change.ChangeTypes is not ContentTypeChangeTypes.None)
            .Select(change => change.Item.Id)
            .Distinct()
            .ToArray();

        if (directMediaTypeIds.Length == 0)
        {
            return;
        }

        int[] allMediaTypeIds = ExpandWithDependentMediaTypes(directMediaTypeIds);
        Guid[] mediaKeys = GetMediaKeysOfTypes(allMediaTypeIds);
        FlushDocumentIndexCacheForContentKeys(mediaKeys);
    }

    private int[] ExpandWithDependentMediaTypes(int[] mediaTypeIds)
    {
        var mediaTypeIdSet = new HashSet<int>(mediaTypeIds);
        int[] dependentTypeIds = _mediaTypeService.GetAll()
            .Where(mt => mt.CompositionIds().Any(id => mediaTypeIdSet.Contains(id)))
            .Select(mt => mt.Id)
            .ToArray();

        return mediaTypeIds.Union(dependentTypeIds).ToArray();
    }

    private Guid[] GetMediaKeysOfTypes(int[] mediaTypeIds)
    {
        var keys = new List<Guid>();
        var pageIndex = 0L;

        while (true)
        {
            IMedia[] page = _mediaService.GetPagedOfTypes(
                mediaTypeIds, pageIndex, 1000, out long totalRecords, null, null).ToArray();
            keys.AddRange(page.Select(m => m.Key));
            pageIndex++;

            if (keys.Count >= totalRecords)
            {
                break;
            }
        }

        return keys.ToArray();
    }
}
