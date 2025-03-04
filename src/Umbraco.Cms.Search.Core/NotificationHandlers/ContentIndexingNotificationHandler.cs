using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

// TODO: add notification handler for content type changes
internal sealed class ContentIndexingNotificationHandler : IndexingNotificationHandlerBase, INotificationAsyncHandler<ContentCacheRefresherNotification>
{
    private readonly IContentIndexingService _contentIndexingService;

    public ContentIndexingNotificationHandler(ICoreScopeProvider coreScopeProvider, IContentIndexingService contentIndexingService)
        : base(coreScopeProvider)
        => _contentIndexingService = contentIndexingService;

    // TODO: add the ability to populate different indexes at the same time - e.g. update both published and draft document indexes in one go
    public Task HandleAsync(ContentCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        var payloads = GetNotificationPayloads<ContentCacheRefresher.JsonPayload>(notification);
        if (payloads.Any(payload => payload.Key.HasValue is false))
        {
            throw new InvalidOperationException("Expected Key properties on all content cache refresher payloads.");
        }

        var changes = payloads
            .Select(payload =>
                payload.ChangeTypes.HasType(TreeChangeTypes.Remove)
                    ? new ContentChange(payload.Key!.Value, ContentChangeType.Remove)
                    : payload.ChangeTypes.HasType(TreeChangeTypes.RefreshNode) || payload.ChangeTypes.HasType(TreeChangeTypes.RefreshBranch)
                        ? new ContentChange(payload.Key!.Value, ContentChangeType.Refresh)
                        : null
            )
            .WhereNotNull()
            .ToList();

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));

        return Task.CompletedTask;
    }
}