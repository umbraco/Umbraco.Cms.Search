using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Cache.Media;

internal sealed class DraftMediaNotificationHandler : ContentNotificationHandlerBase<DraftMediaCacheRefresher.JsonPayload>,
    IDistributedCacheNotificationHandler<MediaSavedNotification>,
    IDistributedCacheNotificationHandler<MediaMovedNotification>,
    IDistributedCacheNotificationHandler<MediaMovedToRecycleBinNotification>,
    IDistributedCacheNotificationHandler<MediaDeletedNotification>
{
    protected override Guid CacheRefresherUniqueId => DraftMediaCacheRefresher.UniqueId;

    public DraftMediaNotificationHandler(DistributedCache distributedCache, IOriginProvider originProvider)
        : base(distributedCache, originProvider)
    {
    }

    public void Handle(MediaSavedNotification notification)
    {
        DraftMediaCacheRefresher.JsonPayload[] payloads = notification
            .SavedEntities
            .Select(entity => new DraftMediaCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.RefreshNode))
            .ToArray();

        HandlePayloads(payloads);
    }

    public void Handle(MediaMovedNotification notification)
        => HandleMove(notification.MoveInfoCollection);

    public void Handle(MediaMovedToRecycleBinNotification notification)
        => HandleMove(notification.MoveInfoCollection);

    public void Handle(MediaDeletedNotification notification)
    {
        DraftMediaCacheRefresher.JsonPayload[] payloads = notification
            .DeletedEntities
            .Select(entity => new DraftMediaCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.Remove))
            .ToArray();

        HandlePayloads(payloads);
    }

    private void HandleMove(IEnumerable<MoveEventInfoBase<IMedia>> moveEventInfo)
    {
        IMedia[] topmostEntities = FindTopmostEntities(moveEventInfo.Select(i => i.Entity));
        DraftMediaCacheRefresher.JsonPayload[] payloads = topmostEntities
            .Select(entity => new DraftMediaCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.RefreshBranch))
            .ToArray();

        HandlePayloads(payloads);
    }
}
