using Microsoft.Extensions.Logging;
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
// TODO: add notification handler for language changes (need to handle deletion)
internal sealed class ContentIndexingNotificationHandler : IndexingNotificationHandlerBase,
    INotificationHandler<PublishedContentCacheRefresherNotification>,
    INotificationHandler<ContentCacheRefresherNotification>,
    INotificationHandler<MediaCacheRefresherNotification>
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
        
        var changes = PublishedDocumentChanges(
            payloads.Select(payload => (payload.ContentKey, TreeChangeTypes: payload.ChangeTypes))
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

        var changes = DraftDocumentChanges(
            payloads
                .Where(payload => payload.Key.HasValue)
                .Select(payload => (payload.Key!.Value, payload.ChangeTypes))
        );

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));
    }

    public void Handle(MediaCacheRefresherNotification notification)
    {
        var payloads = GetNotificationPayloads<MediaCacheRefresher.JsonPayload>(notification);

        if (payloads.Any(payload => payload.Key.HasValue is false))
        {
            _logger.LogError("One or more media cache refresh notifications did not contain a content key. Search indexes might be out of sync.");
        }

        var changes = MediaChanges(
            payloads
                .Where(payload => payload.Key.HasValue)
                .Select(payload => (payload.Key!.Value, payload.ChangeTypes))
        );

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));
    }

    private ContentChange[] PublishedDocumentChanges(IEnumerable<(Guid ContentKey, TreeChangeTypes ChangeTypes)> payloads)
        => GetContentChanges(
            payloads,
            (contentKey, changeImpact) => ContentChange.Document(contentKey, changeImpact, ContentState.Published)
        );

    private ContentChange[] DraftDocumentChanges(IEnumerable<(Guid ContentKey, TreeChangeTypes ChangeTypes)> payloads)
        => GetContentChanges(
            payloads,
            (contentKey, changeImpact) => ContentChange.Document(contentKey, changeImpact, ContentState.Draft)
        );

    private ContentChange[] MediaChanges(IEnumerable<(Guid ContentKey, TreeChangeTypes ChangeTypes)> payloads)
        => GetContentChanges(
            payloads,
            (contentKey, changeImpact) => ContentChange.Media(contentKey, changeImpact, ContentState.Draft)
        );
    
    private ContentChange[] GetContentChanges(IEnumerable<(Guid ContentKey, TreeChangeTypes ChangeTypes)> payloads, Func<Guid, ChangeImpact, ContentChange> contentChangeFactory)
        => payloads
            .Select(payload => payload.ChangeTypes switch
                {
                    TreeChangeTypes.None => null,
                    TreeChangeTypes.RefreshAll => contentChangeFactory(payload.ContentKey, ChangeImpact.RefreshWithDescendants),
                    TreeChangeTypes.RefreshNode => contentChangeFactory(payload.ContentKey, ChangeImpact.Refresh),
                    TreeChangeTypes.RefreshBranch => contentChangeFactory(payload.ContentKey, ChangeImpact.RefreshWithDescendants),
                    TreeChangeTypes.Remove => contentChangeFactory(payload.ContentKey, ChangeImpact.Remove),
                    _ => throw new ArgumentOutOfRangeException(nameof(payload), payload.ChangeTypes, "Unexpected tree change type.")
                }
            )
            .WhereNotNull()
            .ToArray();

}