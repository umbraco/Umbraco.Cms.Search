using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

internal sealed class RebuildIndexesNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentIndexingService _contentIndexingService;
    private readonly ILogger<RebuildIndexesNotificationHandler> _logger;

    public RebuildIndexesNotificationHandler(
        IContentIndexingService contentIndexingService,
        ILogger<RebuildIndexesNotificationHandler> logger)
    {
        _contentIndexingService = contentIndexingService;
        _logger = logger;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        _logger.LogInformation("Rebuilding core search indexes...");
        _contentIndexingService.Rebuild(Core.Constants.IndexAliases.PublishedContent);
        _contentIndexingService.Rebuild(Core.Constants.IndexAliases.DraftContent);
        _contentIndexingService.Rebuild(Core.Constants.IndexAliases.DraftMedia);
        _contentIndexingService.Rebuild(Core.Constants.IndexAliases.DraftMembers);
    }
}