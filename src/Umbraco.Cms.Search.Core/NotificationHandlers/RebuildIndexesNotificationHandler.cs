using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

internal sealed class RebuildIndexesNotificationHandler :
    INotificationAsyncHandler<LanguageDeletedNotification>,
    INotificationAsyncHandler<ContentTypeChangedNotification>,
    INotificationAsyncHandler<MemberTypeChangedNotification>,
    INotificationAsyncHandler<MediaTypeChangedNotification>

{
    private readonly IContentIndexingService _contentIndexingService;
    private readonly IIndexDocumentService _indexDocumentService;
    private readonly ILogger<RebuildIndexesNotificationHandler> _logger;
    private readonly IndexOptions _options;

    public RebuildIndexesNotificationHandler(
        IContentIndexingService contentIndexingService,
        IIndexDocumentService indexDocumentService,
        ILogger<RebuildIndexesNotificationHandler> logger,
        IOptions<IndexOptions> options)
    {
        _contentIndexingService = contentIndexingService;
        _indexDocumentService = indexDocumentService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task HandleAsync(LanguageDeletedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rebuilding search indexes after language deletion...");
        await _indexDocumentService.DeleteAllAsync();

        foreach (ContentIndexRegistration indexRegistration in _options.GetContentIndexRegistrations())
        {
            if (indexRegistration.ContainedObjectTypes.Contains(UmbracoObjectTypes.Document))
            {
                _contentIndexingService.Rebuild(indexRegistration.IndexAlias);
            }
        }
    }

    public async Task HandleAsync(ContentTypeChangedNotification notification, CancellationToken cancellationToken)
        => await RebuildByObjectType(notification.Changes, UmbracoObjectTypes.Document);

    public async Task HandleAsync(MemberTypeChangedNotification notification, CancellationToken cancellationToken)
        => await RebuildByObjectType(notification.Changes, UmbracoObjectTypes.Member);

    public async Task HandleAsync(MediaTypeChangedNotification notification, CancellationToken cancellationToken)
        => await RebuildByObjectType(notification.Changes, UmbracoObjectTypes.Media);

    private async Task RebuildByObjectType<T>(IEnumerable<ContentTypeChange<T>> changes, UmbracoObjectTypes objectType)
        where T : class, IContentTypeComposition
    {
        foreach (ContentTypeChange<T> change in changes)
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
                    _contentIndexingService.Rebuild(indexRegistration.IndexAlias);
                }
            }
        }
    }
}
