using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Cms.Search.Core.Cache;

internal sealed class PublicAccessEntryDeletedNotificationHandler : DeletedDistributedCacheNotificationHandlerBase<PublicAccessEntry, PublicAccessEntryDeletedNotification>
{
    private readonly DistributedCache _distributedCache;
    private readonly IIdKeyMap _idKeyMap;

    public PublicAccessEntryDeletedNotificationHandler(DistributedCache distributedCache, IIdKeyMap idKeyMap)
    {
        _distributedCache = distributedCache;
        _idKeyMap = idKeyMap;
    }

    protected override void Handle(IEnumerable<PublicAccessEntry> entities)
        // just perform the same handling as when public access entries are saved... the consumer will handle both events identically anyway 
        => PublicAccessEntrySavedNotificationHandler.Handle(entities, _distributedCache, _idKeyMap);
}