using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Configuration;

namespace Umbraco.Cms.Search.Core.Cache;

internal abstract class ContentNotificationHandlerBase<TCacheRefresherNotification, TPayload>
    where TCacheRefresherNotification : CacheRefresherNotification
    where TPayload : class
{
    private readonly DistributedCache _distributedCache;
    private readonly IEventAggregator _eventAggregator;
    private readonly ContentCacheNotificationOptions _contentCacheNotificationOptions;

    protected ContentNotificationHandlerBase(
        DistributedCache distributedCache,
        IEventAggregator eventAggregator,
        IOptions<ContentCacheNotificationOptions> contentCacheNotificationOptions)
    {
        _distributedCache = distributedCache;
        _eventAggregator = eventAggregator;
        _contentCacheNotificationOptions = contentCacheNotificationOptions.Value;
    }

    protected abstract Guid CacheRefresherUniqueId { get; }

    protected abstract TCacheRefresherNotification CreateCacheRefresherNotification(TPayload[] payloads);

    protected T[] FindTopmostEntities<T>(IEnumerable<T> candidates)
        where T : IContentBase
    {
        T[] candidatesAsArray = candidates as T[] ?? candidates.ToArray();
        var ids = candidatesAsArray.Select(entity => entity.Id).ToArray();
        return candidatesAsArray.Where(entity => ids.Contains(entity.ParentId) is false).ToArray();
    }

    protected void HandlePayloads(TPayload[] payloads)
    {
        if (_contentCacheNotificationOptions.UseDistributedCache)
        {
            _distributedCache.RefreshByPayload(CacheRefresherUniqueId, payloads);
        }
        else
        {
            _eventAggregator.Publish(CreateCacheRefresherNotification(payloads));
        }
    }
}
