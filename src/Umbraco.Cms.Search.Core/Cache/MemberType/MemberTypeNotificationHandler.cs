using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Cache.MemberType;

internal sealed class MemberTypeNotificationHandler : ContentNotificationHandlerBase<MemberTypeCacheRefresher.JsonPayload>,
        IDistributedCacheNotificationHandler<MemberTypeChangedNotification>
{
    private readonly IMemberTypeService _memberTypeService;
    private readonly IMemberService _memberService;

    public MemberTypeNotificationHandler(
        DistributedCache distributedCache,
        IOriginProvider originProvider,
        IIndexDocumentService indexDocumentService,
        IMemberTypeService memberTypeService,
        IMemberService memberService)
        : base(distributedCache, originProvider, indexDocumentService)
    {
        _memberTypeService = memberTypeService;
        _memberService = memberService;
    }

    protected override Guid CacheRefresherUniqueId => MemberTypeCacheRefresher.UniqueId;

    public void Handle(MemberTypeChangedNotification notification)
    {
        ContentTypeChange<IMemberType>[] changes = notification.Changes.ToArray();

        MemberTypeCacheRefresher.JsonPayload[] payloads = changes
            .Select(change => new MemberTypeCacheRefresher.JsonPayload(change.Item.Key, change.ChangeTypes))
            .ToArray();

        FlushDocumentIndexCacheForAffectedContent(changes);

        HandlePayloads(payloads);
    }

    private void FlushDocumentIndexCacheForAffectedContent(ContentTypeChange<IMemberType>[] changes)
    {
        int[] directMemberTypeIds = changes
            .Where(change => change.ChangeTypes is not ContentTypeChangeTypes.None)
            .Select(change => change.Item.Id)
            .Distinct()
            .ToArray();

        if (directMemberTypeIds.Length == 0)
        {
            return;
        }

        int[] allMemberTypeIds = ExpandWithDependentMemberTypes(directMemberTypeIds);
        Guid[] memberKeys = GetMemberKeysOfTypes(allMemberTypeIds);
        FlushDocumentIndexCacheForContentKeys(memberKeys);
    }

    private int[] ExpandWithDependentMemberTypes(int[] memberTypeIds)
    {
        var memberTypeIdSet = new HashSet<int>(memberTypeIds);
        int[] dependentTypeIds = _memberTypeService.GetAll()
            .Where(mt => mt.CompositionIds().Any(id => memberTypeIdSet.Contains(id)))
            .Select(mt => mt.Id)
            .ToArray();

        return memberTypeIds.Union(dependentTypeIds).ToArray();
    }

    private Guid[] GetMemberKeysOfTypes(int[] memberTypeIds)
    {
        var keys = new List<Guid>();
        foreach (int memberTypeId in memberTypeIds)
        {
            IEnumerable<IMember> members = _memberService.GetMembersByMemberType(memberTypeId);
            keys.AddRange(members.Select(m => m.Key));
        }

        return keys.ToArray();
    }
}
