using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Cache;

internal abstract class ContentNotificationHandlerBase<TPayload>
{
    private readonly DistributedCache _distributedCache;
    private readonly IOriginProvider _originProvider;

    protected ContentNotificationHandlerBase(DistributedCache distributedCache, IOriginProvider originProvider)
    {
        _distributedCache = distributedCache;
        _originProvider = originProvider;
    }

    protected abstract Guid CacheRefresherUniqueId { get; }

    protected T[] FindTopmostEntities<T>(IEnumerable<T> candidates)
        where T : IContentBase
    {
        T[] candidatesAsArray = candidates as T[] ?? candidates.ToArray();
        var ids = candidatesAsArray.Select(entity => entity.Id).ToArray();
        return candidatesAsArray.Where(entity => ids.Contains(entity.ParentId) is false).ToArray();
    }

    protected void HandlePayloads(TPayload[] payloads)
    {
        var payload = new ContentCacheRefresherNotificationPayload<TPayload>(payloads, _originProvider.GetCurrent());
        _distributedCache.RefreshByPayload(CacheRefresherUniqueId, [payload]);
    }
}
