using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

internal sealed class ContentIndexingNotificationHandler : INotificationAsyncHandler<ContentCacheRefresherNotification>
{
    private readonly ICoreScopeProvider _coreScopeProvider;
    private readonly IContentIndexingService _contentIndexingService;

    public ContentIndexingNotificationHandler(ICoreScopeProvider coreScopeProvider, IContentIndexingService contentIndexingService)
    {
        _coreScopeProvider = coreScopeProvider;
        _contentIndexingService = contentIndexingService;
    }

    // TODO: add the ability to populate different indexes at the same time - e.g. update both published and draft document indexes in one go
    public Task HandleAsync(ContentCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        var payloads = GetNotificationPayloads<ContentCacheRefresher.JsonPayload>(notification);
        if (payloads.Any(payload => payload.Key.HasValue is false))
        {
            throw new InvalidOperationException("Expected Key properties on all content cache refresher payloads.");
        }

        var changes = payloads
            .Select(payload => new ContentChange(payload.Key!.Value, payload.ChangeTypes))
            .ToList();

        Execute(() => _contentIndexingService.Handle(changes));

        return Task.CompletedTask;
    }
    
    private T[] GetNotificationPayloads<T>(CacheRefresherNotification notification)
    {
        if (notification.MessageType != MessageType.RefreshByPayload || notification.MessageObject is not T[] payloads)
        {
            throw new NotSupportedException();
        }

        return payloads;
    }

    private void Execute(Action action)
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