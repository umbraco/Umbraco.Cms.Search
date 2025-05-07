using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Serialization;

// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.
// NOTE: using "key" instead of "id" here, because that's what Umbraco code would do.
namespace Umbraco.Cms.Core.Cache;

public sealed class PublicAccessDetailedCacheRefresher : PayloadCacheRefresherBase<PublicAccessDetailedCacheRefresherNotification, PublicAccessDetailedCacheRefresher.JsonPayload>
{
    public static readonly Guid UniqueId = Guid.Parse("81CF9AC4-B257-4997-BDCA-2826A90FBA0D");

    public PublicAccessDetailedCacheRefresher(AppCaches appCaches, IJsonSerializer serializer, IEventAggregator eventAggregator, ICacheRefresherNotificationFactory factory)
        : base(appCaches, serializer, eventAggregator, factory)
    {
    }

    public override Guid RefresherUniqueId => UniqueId;

    public override string Name => "Public Access Cache Refresher (Search.Core)";

    public record JsonPayload(Guid ProtectedContentKey)
    {
    }
}