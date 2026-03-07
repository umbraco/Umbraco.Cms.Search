using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Cache;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

internal abstract class IndexingNotificationHandlerBase
{
    private readonly ICoreScopeProvider _coreScopeProvider;

    protected IndexingNotificationHandlerBase(ICoreScopeProvider coreScopeProvider)
        => _coreScopeProvider = coreScopeProvider;

    protected T[] GetNotificationPayloads<T>(CacheRefresherNotification notification, out string origin)
    {
        if (notification.MessageType != MessageType.RefreshByPayload
            || notification.MessageObject is not ContentCacheRefresherNotificationPayload<T>[] payloads
            || payloads.Length != 1)
        {
            throw new NotSupportedException();
        }

        origin = payloads[0].Origin;
        return payloads[0].Payloads;
    }

    protected void ExecuteDeferred(Action action)
    {
        var deferredActions = DeferredActions.Get(_coreScopeProvider);
        if (deferredActions != null)
        {
            deferredActions.Add(action);
        }
        else
        {
            action.Invoke();
        }
    }
}
