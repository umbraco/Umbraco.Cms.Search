using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;

namespace Umbraco.Cms.Search.Core.Cache.Content;

internal sealed class DraftContentNotificationHandler : ContentNotificationHandlerBase,
    IDistributedCacheNotificationHandler<ContentSavedNotification>,
    IDistributedCacheNotificationHandler<ContentMovedNotification>,
    IDistributedCacheNotificationHandler<ContentMovedToRecycleBinNotification>,
    IDistributedCacheNotificationHandler<ContentDeletedNotification>
{
    private readonly DistributedCache _distributedCache;

    public DraftContentNotificationHandler(DistributedCache distributedCache)
        => _distributedCache = distributedCache;

    public void Handle(ContentSavedNotification notification)
    {
        DraftContentCacheRefresher.JsonPayload[] payloads = notification
            .SavedEntities
            .Select(entity => new DraftContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.RefreshNode))
            .ToArray();

        _distributedCache.RefreshByPayload(DraftContentCacheRefresher.UniqueId, payloads);
    }

    public void Handle(ContentMovedNotification notification)
        => HandleMove(notification.MoveInfoCollection);

    public void Handle(ContentMovedToRecycleBinNotification notification)
        => HandleMove(notification.MoveInfoCollection);

    public void Handle(ContentDeletedNotification notification)
    {
        DraftContentCacheRefresher.JsonPayload[] payloads = notification
            .DeletedEntities
            .Select(entity => new DraftContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.Remove))
            .ToArray();

        _distributedCache.RefreshByPayload(DraftContentCacheRefresher.UniqueId, payloads);
    }

    private void HandleMove(IEnumerable<MoveEventInfoBase<IContent>> moveEventInfo)
    {
        IContent[] topmostEntities = FindTopmostEntities(moveEventInfo.Select(i => i.Entity));
        DraftContentCacheRefresher.JsonPayload[] payloads = topmostEntities
            .Select(entity => new DraftContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.RefreshBranch))
            .ToArray();

        _distributedCache.RefreshByPayload(DraftContentCacheRefresher.UniqueId, payloads);
    }
}
