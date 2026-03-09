using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.Cache.Language;

internal sealed class LanguageNotificationHandler
    : ContentNotificationHandlerBase<LanguageCacheRefresher.JsonPayload>,
        IDistributedCacheNotificationHandler<LanguageDeletedNotification>
{
    public LanguageNotificationHandler(DistributedCache distributedCache, IOriginProvider originProvider)
        : base(distributedCache, originProvider)
    {
    }

    protected override Guid CacheRefresherUniqueId => LanguageCacheRefresher.UniqueId;

    public void Handle(LanguageDeletedNotification notification)
    {
        LanguageCacheRefresher.JsonPayload[] payloads = notification
            .DeletedEntities
            .Select(language => new LanguageCacheRefresher.JsonPayload(language.Key, language.IsoCode, LanguageChangeTypes.Delete))
            .ToArray();

        HandlePayloads(payloads);
    }
}
