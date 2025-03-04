using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Cms.Search.Core.Cache;

public class PublicAccessCacheRefresherNotification : CacheRefresherNotification
{
    public PublicAccessCacheRefresherNotification(object messageObject, MessageType messageType)
        : base(messageObject, messageType)
    {
    }
}