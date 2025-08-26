using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Cache.Content;

internal sealed class PublishedContentNotificationHandler : ContentNotificationHandlerBase,
    IDistributedCacheNotificationHandler<ContentPublishedNotification>,
    IDistributedCacheNotificationHandler<ContentUnpublishedNotification>,
    IDistributedCacheNotificationHandler<ContentMovedNotification>,
    INotificationHandler<ContentMovedToRecycleBinNotification>
{
    private readonly DistributedCache _distributedCache;

    public PublishedContentNotificationHandler(DistributedCache distributedCache)
        => _distributedCache = distributedCache;

    public void Handle(ContentPublishedNotification notification)
    {
        // we sometimes get unpublished entities here... filter those out, we don't need them
        IContent[] publishedEntities = notification.PublishedEntities.Where(entity => entity.Published).ToArray();
        if (publishedEntities.Any() is false)
        {
            return;
        }

        IContent[] topmostEntities = FindTopmostEntities(publishedEntities);
        PublishedContentCacheRefresher.JsonPayload[] payloads = topmostEntities
            .Select(entity =>
            {
                IEnumerable<string> publishedCultures = entity.CultureInfos?.Values
                    .Where(x => entity.WasPropertyDirty($"{ContentBase.ChangeTrackingPrefix.PublishedCulture}{x.Culture}"))
                    .Select(x => x.Culture) ?? [];
                IEnumerable<string> unpublishedCultures = entity.CultureInfos?.Values
                    .Where(x => entity.WasPropertyDirty($"{ContentBase.ChangeTrackingPrefix.UnpublishedCulture}{x.Culture}"))
                    .Select(x => x.Culture) ?? [];
                var wasUnpublished = entity.WasPropertyDirty(nameof(IContent.Published));

                var affectedCultures = publishedCultures.Union(unpublishedCultures).Distinct().ToArray();
                return new PublishedContentCacheRefresher.JsonPayload(entity.Key, wasUnpublished || affectedCultures.Length > 0 ? TreeChangeTypes.RefreshBranch : TreeChangeTypes.RefreshNode, affectedCultures);
            })
            .WhereNotNull()
            .ToArray();

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }

    public void Handle(ContentUnpublishedNotification notification)
    {
        PublishedContentCacheRefresher.JsonPayload[] payloads = notification
            .UnpublishedEntities
            .Select(entity => new PublishedContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.Remove, []))
            .ToArray();

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }

    public void Handle(ContentMovedNotification notification)
        => HandleMove(notification.MoveInfoCollection, TreeChangeTypes.RefreshBranch);

    public void Handle(ContentMovedToRecycleBinNotification notification)
        => HandleMove(notification.MoveInfoCollection, TreeChangeTypes.Remove);

    private void HandleMove(IEnumerable<MoveEventInfoBase<IContent>> moveEventInfo, TreeChangeTypes changeType)
    {
        IContent[] topmostEntities = FindTopmostEntities(moveEventInfo.Select(i => i.Entity));
        PublishedContentCacheRefresher.JsonPayload[] payloads = topmostEntities
            .Select(entity => new PublishedContentCacheRefresher.JsonPayload(entity.Key, changeType, []))
            .ToArray();

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }
}
