using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Provider.InMemory.NotificationHandlers;

public class RebuildIndexesNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentService _contentService;
    private readonly IContentIndexingService _contentIndexingService;
    private readonly ILogger<RebuildIndexesNotificationHandler> _logger;

    public RebuildIndexesNotificationHandler(IContentService contentService, IContentIndexingService contentIndexingService, ILogger<RebuildIndexesNotificationHandler> logger)
    {
        _contentService = contentService;
        _contentIndexingService = contentIndexingService;
        _logger = logger;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        _logger.LogInformation("Starting index rebuild...");

        // TODO: replace all this when a proper index rebuilding mechanism has been created

        var rootContent = _contentService.GetRootContent();

        var changes = rootContent
            .Select(c => new ContentChange(c.Key, ContentChangeType.RefreshWithDescendants, true))
            .ToArray();
        _contentIndexingService.Handle(changes);

        changes = _contentService
            .GetPagedDescendants(Constants.System.Root, 0, 1000, out _, null, Ordering.By("Path"))
            .Select(c => new ContentChange(c.Key, ContentChangeType.Refresh, false))
            .ToArray();
        _contentIndexingService.Handle(changes);

        _logger.LogInformation("Index rebuild complete.");
    }
}