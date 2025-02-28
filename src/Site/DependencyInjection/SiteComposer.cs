using Site.NotificationHandlers;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;

namespace Site.DependencyInjection;

public sealed class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder
            .AddNotificationHandler<ServerVariablesParsingNotification, EnableSegmentsNotificationHandler>()
            .AddNotificationHandler<SendingContentNotification, CreateSegmentsNotificationHandler>()
            .AddNotificationHandler<UmbracoApplicationStartedNotification, IndexBuildingNotificationHandler>();
}

