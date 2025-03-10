using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services.Changes;

namespace Umbraco.Cms.Search.Core.Cache.Content;

public class ContentCacheRefresher : PayloadCacheRefresherBase<ContentCacheRefresherNotification, ContentCacheRefresher.JsonPayload>
{
    public static readonly Guid UniqueId = Guid.Parse("6BDC4BA1-5454-436B-80AC-FD13442CD216");

    public ContentCacheRefresher(AppCaches appCaches, IJsonSerializer serializer, IEventAggregator eventAggregator, ICacheRefresherNotificationFactory factory)
        : base(appCaches, serializer, eventAggregator, factory)
    {
    }

    public override Guid RefresherUniqueId => UniqueId;

    public override string Name => "Content Cache Refresher (Search.Core)";

    public record JsonPayload(Guid ContentKey, TreeChangeTypes TreeChangeTypes, bool PublishStateAffected)
    {
    }
}