using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Cache.Content;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

// TODO: add notification handler for content type changes
// TODO: add notification handler for language changes (need to handle deletion)
internal sealed class ContentIndexingNotificationHandler : IndexingNotificationHandlerBase, INotificationAsyncHandler<ContentCacheRefresherNotification>
{
    private readonly IContentIndexingService _contentIndexingService;

    public ContentIndexingNotificationHandler(ICoreScopeProvider coreScopeProvider, IContentIndexingService contentIndexingService)
        : base(coreScopeProvider)
        => _contentIndexingService = contentIndexingService;

    public Task HandleAsync(ContentCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        var payloads = GetNotificationPayloads<ContentCacheRefresher.JsonPayload>(notification);

        var changes = payloads
            .Select(payload => payload.TreeChangeTypes switch
                {
                    TreeChangeTypes.None => null,
                    TreeChangeTypes.RefreshAll => new ContentChange(payload.ContentKey, ContentChangeType.RefreshWithDescendants, payload.PublishStateAffected),
                    TreeChangeTypes.RefreshNode => new ContentChange(payload.ContentKey, ContentChangeType.Refresh, payload.PublishStateAffected),
                    TreeChangeTypes.RefreshBranch => new ContentChange(payload.ContentKey, ContentChangeType.RefreshWithDescendants, payload.PublishStateAffected),
                    TreeChangeTypes.Remove => new ContentChange(payload.ContentKey, ContentChangeType.Remove, payload.PublishStateAffected),
                    _ => throw new ArgumentOutOfRangeException()
                }
            )
            .WhereNotNull()
            .ToArray();

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));

        return Task.CompletedTask;
    }
}