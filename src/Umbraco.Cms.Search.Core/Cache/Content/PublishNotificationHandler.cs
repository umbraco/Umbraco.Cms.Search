using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Extensions;

// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.
namespace Umbraco.Cms.Core.Events;

public class PublishNotificationHandler : INotificationHandler<ContentPublishingNotification>,
        IDistributedCacheNotificationHandler<ContentPublishedNotification>,
        IDistributedCacheNotificationHandler<ContentUnpublishedNotification>,
        INotificationHandler<ContentMovedToRecycleBinNotification>
{
    private const string PublishNotificationStateKey = "Umbraco.Cms.Search.Core.PublishNotificationState";
    private readonly DistributedCache _distributedCache;

    public PublishNotificationHandler(DistributedCache distributedCache)
        => _distributedCache = distributedCache;

    public void Handle(ContentPublishingNotification notification)
        => notification.State[PublishNotificationStateKey] = notification.PublishedEntities.ToDictionary(
            entity => entity.Key,
            entity => entity.Published
                ? entity.PublishedCultures.Union(entity.GetCulturesUnpublishing() ?? []).Distinct().ToArray()
                : null
        );

    public void Handle(ContentPublishedNotification notification)
    {
        if (notification.State.TryGetValue(PublishNotificationStateKey, out var stateObject) is false
            || stateObject is not Dictionary<Guid, string[]?> stateByKey)
        {
            // throw for now; we need to know if this happens at any time, because at this point we can't handle it
            throw new InvalidOperationException($"Expected publish state in state key: {PublishNotificationStateKey}");
        }

        var payloads = notification
            .PublishedEntities
            .Select(entity =>
            {
                if (stateByKey.TryGetValue(entity.Key, out var culturesBefore) is false)
                {
                    // this happens - for example when publishing with descendants
                    return null;
                }

                var culturesChanged = culturesBefore.UnsortedSequenceEqual(entity.PublishedCultures) is false;
                return new PublishedContentCacheRefresher.JsonPayload(entity.Key, culturesChanged ? TreeChangeTypes.RefreshBranch : TreeChangeTypes.RefreshNode);
            })
            .WhereNotNull()
            .ToArray();

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }

    public void Handle(ContentUnpublishedNotification notification)
    {
        var payloads = notification
            .UnpublishedEntities
            .Select(entity => new PublishedContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.Remove))
            .ToArray();

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }

    public void Handle(ContentMovedToRecycleBinNotification notification)
    {
        var payloads = notification
            .MoveInfoCollection
            .Select(info => new PublishedContentCacheRefresher.JsonPayload(info.Entity.Key, TreeChangeTypes.Remove))
            .ToArray();

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }
}