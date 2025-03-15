using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.
namespace Umbraco.Cms.Core.Events;

internal sealed class PublicAccessNotificationHandler :
    IDistributedCacheNotificationHandler<PublicAccessEntrySavedNotification>,
    IDistributedCacheNotificationHandler<PublicAccessEntryDeletedNotification>
{
    private readonly DistributedCache _distributedCache;
    private readonly IIdKeyMap _idKeyMap;

    public PublicAccessNotificationHandler(DistributedCache distributedCache, IIdKeyMap idKeyMap)
    {
        _distributedCache = distributedCache;
        _idKeyMap = idKeyMap;
    }

    public void Handle(PublicAccessEntrySavedNotification notification)
        => Handle(notification.SavedEntities);

    public void Handle(PublicAccessEntryDeletedNotification notification)
        => Handle(notification.DeletedEntities);

    private void Handle(IEnumerable<PublicAccessEntry> entities)
    {
        var payloads = entities.Select(entity =>
            {
                var attempt = _idKeyMap.GetKeyForId(entity.ProtectedNodeId, UmbracoObjectTypes.Document);
                return attempt.Success
                    ? new PublicAccessDetailedCacheRefresher.JsonPayload(attempt.Result)
                    : null;
            })
            .WhereNotNull()
            .ToArray();
        
        _distributedCache.RefreshByPayload(PublicAccessDetailedCacheRefresher.UniqueId, payloads);
    }    
}
