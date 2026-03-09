using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Cache.ContentType;
using Umbraco.Cms.Search.Core.Cache.Language;
using Umbraco.Cms.Search.Core.Cache.MediaType;
using Umbraco.Cms.Search.Core.Cache.MemberType;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

internal sealed class RebuildIndexesNotificationHandler  : IndexingNotificationHandlerBase,
    INotificationAsyncHandler<LanguageCacheRefresherNotification>,
    INotificationAsyncHandler<ContentTypeCacheRefresherNotification>,
    INotificationAsyncHandler<MemberTypeCacheRefresherNotification>,
    INotificationAsyncHandler<MediaTypeCacheRefresherNotification>
{
    private readonly IContentIndexingService _contentIndexingService;
    private readonly IIndexDocumentService _indexDocumentService;
    private readonly ILogger<RebuildIndexesNotificationHandler> _logger;
    private readonly IndexOptions _options;

    public RebuildIndexesNotificationHandler(
        IContentIndexingService contentIndexingService,
        IIndexDocumentService indexDocumentService,
        ILogger<RebuildIndexesNotificationHandler> logger,
        IOptions<IndexOptions> options,
        ICoreScopeProvider coreScopeProvider)
        : base(coreScopeProvider)
    {
        _contentIndexingService = contentIndexingService;
        _indexDocumentService = indexDocumentService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task HandleAsync(LanguageCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        LanguageCacheRefresher.JsonPayload[] payloads = GetNotificationPayloads<LanguageCacheRefresher.JsonPayload>(notification, out var origin);
        if (payloads.Any(payload => payload.ChangeTypes is LanguageChangeTypes.Delete) is false)
        {
            return;
        }

        await _indexDocumentService.DeleteAllAsync();

        foreach (ContentIndexRegistration indexRegistration in _options.GetContentIndexRegistrations())
        {
            if (indexRegistration.ContainedObjectTypes.Contains(UmbracoObjectTypes.Document))
            {
                _contentIndexingService.Rebuild(indexRegistration.IndexAlias, origin);
            }
        }
    }

    public async Task HandleAsync(ContentTypeCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        ContentTypeCacheRefresher.JsonPayload[] payloads = GetNotificationPayloads<ContentTypeCacheRefresher.JsonPayload>(notification, out var origin);

        await HandleContentTypeChangesAsync(payloads.Select(payload => (payload.ContentTypeKey, payload.ChangeTypes)), UmbracoObjectTypes.Document, origin);
    }

    public async Task HandleAsync(MemberTypeCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        MemberTypeCacheRefresher.JsonPayload[] payloads = GetNotificationPayloads<MemberTypeCacheRefresher.JsonPayload>(notification, out var origin);

        await HandleContentTypeChangesAsync(payloads.Select(payload => (payload.MemberTypeKey, payload.ChangeTypes)), UmbracoObjectTypes.Member, origin);
    }

    public async Task HandleAsync(MediaTypeCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        MediaTypeCacheRefresher.JsonPayload[] payloads = GetNotificationPayloads<MediaTypeCacheRefresher.JsonPayload>(notification, out var origin);

        await HandleContentTypeChangesAsync(payloads.Select(payload => (payload.MediaTypeKey, payload.ChangeTypes)), UmbracoObjectTypes.Media, origin);
    }

    private async Task HandleContentTypeChangesAsync(IEnumerable<(Guid ContentTypeKey, ContentTypeChangeTypes ChangeTypes)> changes, UmbracoObjectTypes objectType, string origin)
    {
        // TODO: rewrite this logic, it will potentially rebuild multiple times
        foreach ((Guid ContentTypeKey, ContentTypeChangeTypes ChangeTypes) change in changes)
        {
            if (change.ChangeTypes is not (ContentTypeChangeTypes.RefreshMain or ContentTypeChangeTypes.Remove))
            {
                continue;
            }

            foreach (ContentIndexRegistration indexRegistration in _options.GetContentIndexRegistrations())
            {
                if (indexRegistration.ContainedObjectTypes.Contains(objectType))
                {
                    await _indexDocumentService.DeleteAllAsync();
                    _contentIndexingService.Rebuild(indexRegistration.IndexAlias, origin);
                }
            }
        }
    }
}
