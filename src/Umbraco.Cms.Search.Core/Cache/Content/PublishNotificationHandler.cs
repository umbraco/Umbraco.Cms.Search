using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Extensions;

// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.
namespace Umbraco.Cms.Core.Events;

public class PublishNotificationHandler :
    IDistributedCacheNotificationHandler<ContentPublishedNotification>,
    IDistributedCacheNotificationHandler<ContentUnpublishedNotification>,
    INotificationHandler<ContentMovedToRecycleBinNotification>
{
    private readonly DistributedCache _distributedCache;

    public PublishNotificationHandler(DistributedCache distributedCache)
        => _distributedCache = distributedCache;

    public void Handle(ContentPublishedNotification notification)
    {
        // we sometimes get unpublished entities here... filter those out, we don't need them
        var publishedEntities = notification.PublishedEntities.Where(entity => entity.Published).ToArray();
        if (publishedEntities.Any() is false)
        {
            return;
        }
        
        var topmostEntities = FindTopmostEntities(publishedEntities);
        var payloads = topmostEntities
            .Select(entity =>
            {
                var publishedCultures = entity.CultureInfos?.Values
                    .Where(x => entity.WasPropertyDirty($"{ContentBase.ChangeTrackingPrefix.PublishedCulture}{x.Culture}"))
                    .Select(x => x.Culture) ?? [];
                var unpublishedCultures = entity.CultureInfos?.Values
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
        var payloads = notification
            .UnpublishedEntities
            .Select(entity => new PublishedContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.Remove, []))
            .ToArray();

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }

    public void Handle(ContentMovedToRecycleBinNotification notification)
    {
        var topmostEntities = FindTopmostEntities(notification.MoveInfoCollection.Select(i => i.Entity));
        var payloads = topmostEntities
            .Select(entity => new PublishedContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.Remove, []))
            .ToArray();

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }

    private IContent[] FindTopmostEntities(IEnumerable<IContent> candidates)
    {
        var candidatesAsArray = candidates as IContent[] ?? candidates.ToArray();
        var ids = candidatesAsArray.Select(entity => entity.Id).ToArray();
        return candidatesAsArray.Where(entity => ids.Contains(entity.ParentId) is false).ToArray();
    }
}