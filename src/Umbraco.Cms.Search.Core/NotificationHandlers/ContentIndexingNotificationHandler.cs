using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

// TODO: add notification handler for content type changes
// TODO: add notification handler for language changes (need to handle deletion)
internal sealed class ContentIndexingNotificationHandler : IndexingNotificationHandlerBase,
    INotificationHandler<PublishedContentCacheRefresherNotification>,
    INotificationHandler<DraftContentCacheRefresherNotification>,
    INotificationHandler<DraftMediaCacheRefresherNotification>,
    INotificationHandler<MemberCacheRefresherNotification>
{
    private readonly IContentIndexingService _contentIndexingService;
    private readonly IIdKeyMap _idKeyMap;
    private readonly ILogger<ContentIndexingNotificationHandler> _logger;

    public ContentIndexingNotificationHandler(
        ICoreScopeProvider coreScopeProvider,
        IContentIndexingService contentIndexingService,
        IIdKeyMap idKeyMap,
        ILogger<ContentIndexingNotificationHandler> logger)
        : base(coreScopeProvider)
    {
        _contentIndexingService = contentIndexingService;
        _logger = logger;
        _idKeyMap = idKeyMap;
    }

    public void Handle(PublishedContentCacheRefresherNotification notification)
    {
        var payloads = GetNotificationPayloads<PublishedContentCacheRefresher.JsonPayload>(notification);
        
        var changes = PublishedDocumentChanges(
            payloads.Select(payload => (payload.ContentKey, TreeChangeTypes: payload.ChangeTypes))
        );

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));
    }

    public void Handle(DraftContentCacheRefresherNotification notification)
    {
        var payloads = GetNotificationPayloads<DraftContentCacheRefresher.JsonPayload>(notification);

        var changes = DraftDocumentChanges(
            payloads.Select(payload => (payload.ContentKey, payload.ChangeTypes))
        );

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));
    }

    public void Handle(DraftMediaCacheRefresherNotification notification)
    {
        var payloads = GetNotificationPayloads<DraftMediaCacheRefresher.JsonPayload>(notification);

        var changes = MediaChanges(
            payloads.Select(payload => (payload.MediaKey, payload.ChangeTypes))
        );

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));
    }

    public void Handle(MemberCacheRefresherNotification notification)
    {
        var payloads = GetNotificationPayloads<MemberCacheRefresher.JsonPayload>(notification);
        var changes = payloads
            .Select(payload =>
            {
                var attempt = _idKeyMap.GetKeyForId(payload.Id, UmbracoObjectTypes.Member);
                return attempt.Success
                    ? ContentChange.Member(attempt.Result, payload.Removed ? ChangeImpact.Remove : ChangeImpact.Refresh, ContentState.Draft)
                    : null;
            })
            .WhereNotNull()
            .ToArray();

        if (changes.Length != payloads.Length)
        {
            _logger.LogError("One or more member cache refresh notifications did not resolve to a content key. Search indexes might be out of sync.");
        }

        ExecuteDeferred(() => _contentIndexingService.Handle(changes));
    }

    private ContentChange[] PublishedDocumentChanges(IEnumerable<(Guid ContentId, TreeChangeTypes ChangeTypes)> payloads)
        => GetContentChanges(
            payloads,
            (contentKey, changeImpact) => ContentChange.Document(contentKey, changeImpact, ContentState.Published)
        );

    private ContentChange[] DraftDocumentChanges(IEnumerable<(Guid ContentId, TreeChangeTypes ChangeTypes)> payloads)
        => GetContentChanges(
            payloads,
            (contentKey, changeImpact) => ContentChange.Document(contentKey, changeImpact, ContentState.Draft)
        );

    private ContentChange[] MediaChanges(IEnumerable<(Guid ContentId, TreeChangeTypes ChangeTypes)> payloads)
        => GetContentChanges(
            payloads,
            (contentKey, changeImpact) => ContentChange.Media(contentKey, changeImpact, ContentState.Draft)
        );

    private ContentChange[] GetContentChanges(IEnumerable<(Guid ContentId, TreeChangeTypes ChangeTypes)> payloads, Func<Guid, ChangeImpact, ContentChange> contentChangeFactory)
        => payloads
            .Select(payload => payload.ChangeTypes switch
                {
                    TreeChangeTypes.None => null,
                    TreeChangeTypes.RefreshAll => contentChangeFactory(payload.ContentId, ChangeImpact.RefreshWithDescendants),
                    TreeChangeTypes.RefreshNode => contentChangeFactory(payload.ContentId, ChangeImpact.Refresh),
                    TreeChangeTypes.RefreshBranch => contentChangeFactory(payload.ContentId, ChangeImpact.RefreshWithDescendants),
                    TreeChangeTypes.Remove => contentChangeFactory(payload.ContentId, ChangeImpact.Remove),
                    _ => throw new ArgumentOutOfRangeException(nameof(payload), payload.ChangeTypes, "Unexpected tree change type.")
                }
            )
            .WhereNotNull()
            .ToArray();
}