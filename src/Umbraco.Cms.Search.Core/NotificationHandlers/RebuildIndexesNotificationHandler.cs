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

internal sealed class RebuildIndexesNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>,
    INotificationAsyncHandler<LanguageDeletedNotification>,
    INotificationAsyncHandler<ContentTypeChangedNotification>,
    INotificationAsyncHandler<MemberTypeChangedNotification>,
    INotificationAsyncHandler<MediaTypeChangedNotification>

{
    private readonly IContentIndexingService _contentIndexingService;
    private readonly IDocumentService _documentService;
    private readonly ILogger<RebuildIndexesNotificationHandler> _logger;
    private readonly IndexOptions _options;

    public RebuildIndexesNotificationHandler(
        IContentIndexingService contentIndexingService,
        IDocumentService documentService,
        ILogger<RebuildIndexesNotificationHandler> logger,
        IOptions<IndexOptions> options)
    {
        _contentIndexingService = contentIndexingService;
        _documentService = documentService;
        _logger = logger;
        _options = options.Value;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        _logger.LogInformation("Rebuilding core search indexes...");
        _contentIndexingService.Rebuild(Constants.IndexAliases.PublishedContent);
        _contentIndexingService.Rebuild(Constants.IndexAliases.DraftContent);
        _contentIndexingService.Rebuild(Constants.IndexAliases.DraftMedia);
        _contentIndexingService.Rebuild(Constants.IndexAliases.DraftMembers);
    }

    public async Task HandleAsync(LanguageDeletedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rebuilding search indexes after language deletion...");
        await _documentService.DeleteAllAsync();

        foreach (IndexRegistration indexRegistration in _options.GetIndexRegistrations())
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

            foreach (IndexRegistration indexRegistration in _options.GetIndexRegistrations())
            {
                if (indexRegistration.ContainedObjectTypes.Contains(objectType))
                {
                    await _documentService.DeleteAllAsync();
                    _contentIndexingService.Rebuild(indexRegistration.IndexAlias);
                }
            }
        }
    }
}
