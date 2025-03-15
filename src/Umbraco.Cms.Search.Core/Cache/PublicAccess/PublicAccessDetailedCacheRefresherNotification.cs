using Umbraco.Cms.Core.Sync;

// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.
namespace Umbraco.Cms.Core.Notifications;

public class PublicAccessDetailedCacheRefresherNotification : CacheRefresherNotification
{
    public PublicAccessDetailedCacheRefresherNotification(object messageObject, MessageType messageType)
        : base(messageObject, messageType)
    {
    }
}