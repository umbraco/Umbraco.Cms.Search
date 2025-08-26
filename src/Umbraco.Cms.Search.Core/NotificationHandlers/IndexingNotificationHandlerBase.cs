using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

internal abstract class IndexingNotificationHandlerBase
{
    private readonly ICoreScopeProvider _coreScopeProvider;

    protected IndexingNotificationHandlerBase(ICoreScopeProvider coreScopeProvider)
        => _coreScopeProvider = coreScopeProvider;

    protected T[] GetNotificationPayloads<T>(CacheRefresherNotification notification)
    {
        if (notification.MessageType != MessageType.RefreshByPayload || notification.MessageObject is not T[] payloads)
        {
            throw new NotSupportedException();
        }

        return payloads;
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
