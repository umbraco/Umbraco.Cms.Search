using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Umbraco.Cms.Search.Core.NotificationHandlers;

internal sealed class RebuildIndexesNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>, INotificationHandler<LanguageDeletedNotification>
{
    private readonly IContentIndexingService _contentIndexingService;
    private readonly ILogger<RebuildIndexesNotificationHandler> _logger;
    private readonly IndexOptions _options;

    public RebuildIndexesNotificationHandler(
        IContentIndexingService contentIndexingService,
        ILogger<RebuildIndexesNotificationHandler> logger,
        IOptions<IndexOptions> options)
    {
        _contentIndexingService = contentIndexingService;
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

    public void Handle(LanguageDeletedNotification notification)
    {
        _logger.LogInformation("Rebuilding search indexes after language deletion...");

        foreach (var indexAlias in _options.IncludeRebuildWhenLanguageDeleted)
        {
            _contentIndexingService.Rebuild(indexAlias);
        }
    }
}
