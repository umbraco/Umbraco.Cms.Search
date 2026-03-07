using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Cache.Member;

internal sealed class DraftMemberNotificationHandler : ContentNotificationHandlerBase<DraftMemberCacheRefresher.JsonPayload>,
    IDistributedCacheNotificationHandler<MemberSavedNotification>,
    IDistributedCacheNotificationHandler<MemberDeletedNotification>
{
    protected override Guid CacheRefresherUniqueId => DraftMemberCacheRefresher.UniqueId;

    public DraftMemberNotificationHandler(DistributedCache distributedCache, IOriginProvider originProvider)
        : base(distributedCache, originProvider)
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
