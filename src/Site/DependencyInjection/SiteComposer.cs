using Package.Services;
using Site.Segments;
using Site.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;

namespace Site.DependencyInjection;

public sealed class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddTransient<IIndexService, InMemoryIndexAndSearchService>();
        builder.Services.AddTransient<ISearchService, InMemoryIndexAndSearchService>();

        builder
            .AddNotificationHandler<ServerVariablesParsingNotification, ServerVariablesParsingNotificationHandler>()
            .AddNotificationHandler<SendingContentNotification, CreateSegmentsHandler>();
    }
}
