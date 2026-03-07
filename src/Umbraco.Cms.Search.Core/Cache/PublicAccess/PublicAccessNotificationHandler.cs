using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Cache.PublicAccess;

internal sealed class PublicAccessNotificationHandler : ContentNotificationHandlerBase<PublicAccessDetailedCacheRefresherNotification, PublicAccessDetailedCacheRefresher.JsonPayload>,
    IDistributedCacheNotificationHandler<PublicAccessEntrySavedNotification>,
    IDistributedCacheNotificationHandler<PublicAccessEntryDeletedNotification>
{
    private readonly IIdKeyMap _idKeyMap;

    protected override Guid CacheRefresherUniqueId => PublicAccessDetailedCacheRefresher.UniqueId;

    protected override PublicAccessDetailedCacheRefresherNotification CreateCacheRefresherNotification(PublicAccessDetailedCacheRefresher.JsonPayload[] payloads)
        => new (payloads, MessageType.RefreshByPayload);

    public PublicAccessNotificationHandler(
        DistributedCache distributedCache,
        IEventAggregator eventAggregator,
        IOptions<ContentCacheNotificationOptions> contentCacheNotificationOptions,
        IIdKeyMap idKeyMap)
        : base(distributedCache, eventAggregator, contentCacheNotificationOptions)
        => _idKeyMap = idKeyMap;

    public void Handle(PublicAccessEntrySavedNotification notification)
        => Handle(notification.SavedEntities);

    public void Handle(PublicAccessEntryDeletedNotification notification)
        => Handle(notification.DeletedEntities);

    private void Handle(IEnumerable<PublicAccessEntry> entities)
    {
        PublicAccessDetailedCacheRefresher.JsonPayload[] payloads = entities.Select(entity =>
            {
                Attempt<Guid> attempt = _idKeyMap.GetKeyForId(entity.ProtectedNodeId, UmbracoObjectTypes.Document);
                return attempt.Success
                    ? new PublicAccessDetailedCacheRefresher.JsonPayload(attempt.Result)
                    : null;
            })
            .WhereNotNull()
            .ToArray();

        HandlePayloads(payloads);
    }
}
