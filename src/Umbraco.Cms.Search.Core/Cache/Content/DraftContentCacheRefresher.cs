using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services.Changes;

// NOTE: the namespace is defined as what it would be, if this was part of Umbraco core.
// NOTE: using "key" instead of "id" here, because that's what Umbraco code would do.
namespace Umbraco.Cms.Core.Cache;

public class DraftContentCacheRefresher : PayloadCacheRefresherBase<DraftContentCacheRefresherNotification, DraftContentCacheRefresher.JsonPayload>
{
    public static readonly Guid UniqueId = Guid.Parse("4DA581BA-07B8-4643-945E-FA9687C14D15");

    public DraftContentCacheRefresher(AppCaches appCaches, IJsonSerializer serializer, IEventAggregator eventAggregator, ICacheRefresherNotificationFactory factory)
        : base(appCaches, serializer, eventAggregator, factory)
    {
    }

    public override Guid RefresherUniqueId => UniqueId;

    public override string Name => "Draft Content Cache Refresher";

    public record JsonPayload(Guid ContentKey, TreeChangeTypes ChangeTypes)
    {
    }
}