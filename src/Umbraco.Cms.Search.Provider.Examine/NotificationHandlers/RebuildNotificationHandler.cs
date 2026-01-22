using Examine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using IndexOptions = Umbraco.Cms.Search.Core.Configuration.IndexOptions;

namespace Umbraco.Cms.Search.Provider.Examine.NotificationHandlers;

public class RebuildNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private readonly IExamineManager _examineManager;
    private readonly IContentIndexingService _contentIndexingService;
    private readonly ILogger<RebuildNotificationHandler> _logger;
    private readonly IndexOptions _options;


    public RebuildNotificationHandler(
        IExamineManager examineManager,
        IContentIndexingService contentIndexingService,
        IOptions<IndexOptions> options,
        ILogger<RebuildNotificationHandler> logger)
    {
        _examineManager = examineManager;
        _contentIndexingService = contentIndexingService;
        _logger = logger;
        _options = options.Value;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        _logger.LogInformation("Boot detected, determining indexes to rebuild");
        foreach (IndexRegistration indexRegistration in _options.GetIndexRegistrations())
        {

            if (_examineManager.TryGetIndex(indexRegistration.IndexAlias, out IIndex? index))
            {
                // Check if index exists, if it does AND it has items in, we can skip rebuilding
                if (index.IndexExists())
                {
                    continue;
                }
            }
            else
            {
                // Not a registered examine index, don't rebuild from here.
                continue;
            }

            _logger.LogInformation("Rebuilding index {IndexRegistrationIndexAlias}", indexRegistration.IndexAlias);
            _contentIndexingService.Rebuild(indexRegistration.IndexAlias);
        }
    }
}
