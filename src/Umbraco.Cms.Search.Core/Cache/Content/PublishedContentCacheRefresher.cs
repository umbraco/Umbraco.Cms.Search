using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services.Changes;

// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.
namespace Umbraco.Cms.Core.Cache;

public class PublishedContentCacheRefresher : PayloadCacheRefresherBase<PublishedContentCacheRefresherNotification, PublishedContentCacheRefresher.JsonPayload>
{
    public static readonly Guid UniqueId = Guid.Parse("6BDC4BA1-5454-436B-80AC-FD13442CD216");

    public PublishedContentCacheRefresher(AppCaches appCaches, IJsonSerializer serializer, IEventAggregator eventAggregator, ICacheRefresherNotificationFactory factory)
        : base(appCaches, serializer, eventAggregator, factory)
    {
    }

    public override Guid RefresherUniqueId => UniqueId;

    public override string Name => "Published Content Cache Refresher";

    public record JsonPayload(Guid ContentKey, TreeChangeTypes ChangeTypes, string[] AffectedCultures)
    {
    }
}