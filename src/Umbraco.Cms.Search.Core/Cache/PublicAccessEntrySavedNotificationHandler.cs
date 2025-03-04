using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.Cache;

internal sealed class PublicAccessEntrySavedNotificationHandler : SavedDistributedCacheNotificationHandlerBase<PublicAccessEntry, PublicAccessEntrySavedNotification>
{
    private readonly DistributedCache _distributedCache;
    private readonly IIdKeyMap _idKeyMap;

    public PublicAccessEntrySavedNotificationHandler(DistributedCache distributedCache, IIdKeyMap idKeyMap)
    {
        _distributedCache = distributedCache;
        _idKeyMap = idKeyMap;
    }

    protected override void Handle(IEnumerable<PublicAccessEntry> entities)
        => Handle(entities, _distributedCache, _idKeyMap);

    internal static void Handle(IEnumerable<PublicAccessEntry> entities, DistributedCache distributedCache, IIdKeyMap idKeyMap)
    {
        var payloads = entities.Select(entity =>
            {
                var attempt = idKeyMap.GetKeyForId(entity.ProtectedNodeId, UmbracoObjectTypes.Document);
                return attempt.Success
                    ? new PublicAccessCacheRefresher.JsonPayload(attempt.Result)
                    : null;
            })
            .WhereNotNull()
            .ToArray();
        
        distributedCache.RefreshByPayload(PublicAccessCacheRefresher.UniqueId, payloads);
    }
}