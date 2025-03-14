using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Cache.Content;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

// TODO: add notification handler for content type changes
// TODO: add notification handler for language changes (need to handle deletion)
internal sealed class ContentIndexingNotificationHandler : IndexingNotificationHandlerBase,
    INotificationHandler<PublishedContentCacheRefresherNotification>,
    INotificationHandler<ContentCacheRefresherNotification>
{
    private readonly IContentIndexingService _contentIndexingService;
    private readonly ILogger<ContentIndexingNotificationHandler> _logger;

    public ContentIndexingNotificationHandler(
        ICoreScopeProvider coreScopeProvider,
        IContentIndexingService contentIndexingService,
        ILogger<ContentIndexingNotificationHandler> logger)
        : base(coreScopeProvider)
    {
        _contentIndexingService = contentIndexingService;
        _logger = logger;
    }

    public void Handle(PublishedContentCacheRefresherNotification notification)
    {
        var payloads = GetNotificationPayloads<PublishedContentCacheRefresher.JsonPayload>(notification);

        
        var changes = GetChanges(
            payloads
                .Select(payload => (payload.ContentKey, TreeChangeTypes: payload.ChangeTypes)),
            true
        );

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));
    }

    public void Handle(ContentCacheRefresherNotification notification)
    {
        var payloads = GetNotificationPayloads<ContentCacheRefresher.JsonPayload>(notification);

        if (payloads.Any(payload => payload.Key.HasValue is false))
        {
            _logger.LogError("One or more content cache refresh notifications did not contain a content key. Search indexes might be out of sync.");
        }

        var changes = GetChanges(
            payloads
                .Where(payload => payload.Key.HasValue)
                .Select(payload => (payload.Key!.Value, payload.ChangeTypes)),
            false
        );

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));
    }

    private ContentChange[] GetChanges(IEnumerable<(Guid ContentKey, TreeChangeTypes ChangeTypes)> payloads, bool published)
        => payloads
            .Select(payload => payload.ChangeTypes switch
                {
                    TreeChangeTypes.None => null,
                    TreeChangeTypes.RefreshAll => new ContentChange(payload.ContentKey, ContentChangeType.RefreshWithDescendants, published),
                    TreeChangeTypes.RefreshNode => new ContentChange(payload.ContentKey, ContentChangeType.Refresh, published),
                    TreeChangeTypes.RefreshBranch => new ContentChange(payload.ContentKey, ContentChangeType.RefreshWithDescendants, published),
                    TreeChangeTypes.Remove => new ContentChange(payload.ContentKey, ContentChangeType.Remove, published),
                    _ => throw new ArgumentOutOfRangeException(nameof(payload), payload.ChangeTypes, "Unexpected tree change type.")
                }
            )
            .WhereNotNull()
            .ToArray();
}