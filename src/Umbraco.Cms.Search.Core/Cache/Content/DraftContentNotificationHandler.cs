using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Configuration;

namespace Umbraco.Cms.Search.Core.Cache.Content;

internal sealed class DraftContentNotificationHandler : ContentNotificationHandlerBase<DraftContentCacheRefresherNotification, DraftContentCacheRefresher.JsonPayload>,
    IDistributedCacheNotificationHandler<ContentSavedNotification>,
    IDistributedCacheNotificationHandler<ContentMovedNotification>,
    IDistributedCacheNotificationHandler<ContentMovedToRecycleBinNotification>,
    IDistributedCacheNotificationHandler<ContentDeletedNotification>
{
    protected override Guid CacheRefresherUniqueId => DraftContentCacheRefresher.UniqueId;

    protected override DraftContentCacheRefresherNotification CreateCacheRefresherNotification(DraftContentCacheRefresher.JsonPayload[] payloads)
        => new (payloads, MessageType.RefreshByPayload);

    public DraftContentNotificationHandler(
        DistributedCache distributedCache,
        IEventAggregator eventAggregator,
        IOptions<ContentCacheNotificationOptions> contentCacheNotificationOptions)
        : base(distributedCache, eventAggregator, contentCacheNotificationOptions)
    {
    }

    public void Handle(ContentSavedNotification notification)
    {
        DraftContentCacheRefresher.JsonPayload[] payloads = notification
            .SavedEntities
            .Select(entity => new DraftContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.RefreshNode))
            .ToArray();

        HandlePayloads(payloads);
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

        HandlePayloads(payloads);
    }

    private void HandleMove(IEnumerable<MoveEventInfoBase<IContent>> moveEventInfo)
    {
        IContent[] topmostEntities = FindTopmostEntities(moveEventInfo.Select(i => i.Entity));
        DraftContentCacheRefresher.JsonPayload[] payloads = topmostEntities
            .Select(entity => new DraftContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.RefreshBranch))
            .ToArray();

        HandlePayloads(payloads);
    }
}
