using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Site.NotificationHandlers;

public class IndexBuildingNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentService _contentService;
    private readonly IContentIndexingService _contentIndexingService;

    public IndexBuildingNotificationHandler(IContentService contentService, IContentIndexingService contentIndexingService)
    {
        _contentService = contentService;
        _contentIndexingService = contentIndexingService;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        var rootContent = _contentService.GetRootContent();
        var changes = rootContent
            .Select(c => new ContentChange(c.Key, ContentChangeType.RefreshWithDescendants, true))
            .ToArray();

        if (changes.Length is 0)
        {
            return;
        }

        _contentIndexingService.Handle(changes);
    }
}