// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;

namespace Umbraco.Cms.Core.Events;

internal sealed class DraftMediaNotificationHandler : ContentNotificationHandlerBase,
    IDistributedCacheNotificationHandler<MediaSavedNotification>,
    IDistributedCacheNotificationHandler<MediaMovedNotification>,
    IDistributedCacheNotificationHandler<MediaMovedToRecycleBinNotification>,
    IDistributedCacheNotificationHandler<MediaDeletedNotification>
{
    private readonly DistributedCache _distributedCache;

    public DraftMediaNotificationHandler(DistributedCache distributedCache)
        => _distributedCache = distributedCache;

    public void Handle(MediaSavedNotification notification)
    {
        var payloads = notification
            .SavedEntities
            .Select(entity => new DraftMediaCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.RefreshNode))
            .ToArray();

        _distributedCache.RefreshByPayload(DraftMediaCacheRefresher.UniqueId, payloads);
    }

    public void Handle(MediaMovedNotification notification)
        => HandleMove(notification.MoveInfoCollection);

    public void Handle(MediaMovedToRecycleBinNotification notification)
        => HandleMove(notification.MoveInfoCollection);

    public void Handle(MediaDeletedNotification notification)
    {
        var payloads = notification
            .DeletedEntities
            .Select(entity => new DraftMediaCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.Remove))
            .ToArray();

        _distributedCache.RefreshByPayload(DraftMediaCacheRefresher.UniqueId, payloads);
    }

    private void HandleMove(IEnumerable<MoveEventInfoBase<IMedia>> moveEventInfo)
    {
        var topmostEntities = FindTopmostEntities(moveEventInfo.Select(i => i.Entity));
        var payloads = topmostEntities
            .Select(entity => new DraftMediaCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.RefreshBranch))
            .ToArray();

        _distributedCache.RefreshByPayload(DraftMediaCacheRefresher.UniqueId, payloads);
    }
}