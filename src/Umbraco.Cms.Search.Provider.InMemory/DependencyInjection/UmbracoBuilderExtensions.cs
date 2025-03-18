using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.InMemory.NotificationHandlers;
using Umbraco.Cms.Search.Provider.InMemory.Services;

namespace Umbraco.Cms.Search.Provider.InMemory.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddInMemorySearchProvider(this IUmbracoBuilder builder)
    {
        // the in-memory datastore is a singleton
        builder.Services.AddSingleton<DataStore>();

        // add the search and index services so they can be used as explicit dependencies
        // when mixing and matching different providers
        builder.Services.AddTransient<InMemoryIndexService>();
        builder.Services.AddTransient<InMemorySearchService>();

        // register the search and index services as the default services
        builder.Services.AddTransient<IIndexService, InMemoryIndexService>();
        builder.Services.AddTransient<ISearchService, InMemorySearchService>();

        builder.Services.Configure<IndexOptions>(options =>
        {
            // register in-memory indexes for draft and published content
            options.RegisterIndex<InMemoryIndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<InMemoryIndexService, IPublishedContentChangeStrategy>(Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);

            // register in-memory index for media
            options.RegisterIndex<InMemoryIndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMedia, UmbracoObjectTypes.Media);

            // register in-memory index for members
            options.RegisterIndex<InMemoryIndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMembers, UmbracoObjectTypes.Member);
        });

        // rebuild in-memory indexes after start-up
        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, RebuildIndexesNotificationHandler>();

        return builder;
    }
}