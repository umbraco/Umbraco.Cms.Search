namespace Umbraco.Cms.Search.Core.Cache;

public record ContentCacheRefresherNotificationPayload<TPayload>(TPayload[] Payloads, string Origin)
{
}
