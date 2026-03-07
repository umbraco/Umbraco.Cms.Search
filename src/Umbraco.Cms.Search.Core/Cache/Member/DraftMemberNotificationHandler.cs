using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Configuration;

namespace Umbraco.Cms.Search.Core.Cache.Member;

internal sealed class DraftMemberNotificationHandler : ContentNotificationHandlerBase<DraftMemberCacheRefresherNotification, DraftMemberCacheRefresher.JsonPayload>,
    IDistributedCacheNotificationHandler<MemberSavedNotification>,
    IDistributedCacheNotificationHandler<MemberDeletedNotification>
{
    protected override Guid CacheRefresherUniqueId => DraftMemberCacheRefresher.UniqueId;

    protected override DraftMemberCacheRefresherNotification CreateCacheRefresherNotification(DraftMemberCacheRefresher.JsonPayload[] payloads)
        => new (payloads, MessageType.RefreshByPayload);

    public DraftMemberNotificationHandler(
        DistributedCache distributedCache,
        IEventAggregator eventAggregator,
        IOptions<ContentCacheNotificationOptions> contentCacheNotificationOptions)
        : base(distributedCache, eventAggregator, contentCacheNotificationOptions)
    {
    }

    public void Handle(MemberSavedNotification notification)
    {
        DraftMemberCacheRefresher.JsonPayload[] payloads = notification
            .SavedEntities
            .Select(entity => new DraftMemberCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.RefreshNode))
            .ToArray();

        HandlePayloads(payloads);
    }

    public void Handle(MemberDeletedNotification notification)
    {
        DraftMemberCacheRefresher.JsonPayload[] payloads = notification
            .DeletedEntities
            .Select(entity => new DraftMemberCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.Remove))
            .ToArray();

        HandlePayloads(payloads);
    }
}
