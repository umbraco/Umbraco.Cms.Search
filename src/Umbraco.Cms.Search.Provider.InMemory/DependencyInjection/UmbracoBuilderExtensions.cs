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

        // register the in-memory searcher and indexer so they can be used explicitly for index registrations
        builder.Services.AddTransient<IInMemoryIndexer, InMemoryIndexer>();
        builder.Services.AddTransient<IInMemorySearcher, InMemorySearcher>();

        // register the in-memory searcher and indexer as the defaults
        builder.Services.AddTransient<IIndexer, InMemoryIndexer>();
        builder.Services.AddTransient<ISearcher, InMemorySearcher>();

        builder.Services.Configure<IndexOptions>(options =>
        {
            // register in-memory indexes for draft and published content
            options.RegisterIndex<IInMemoryIndexer, IInMemorySearcher, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IInMemoryIndexer, IInMemorySearcher, IPublishedContentChangeStrategy>(Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);

            // register in-memory index for media
            options.RegisterIndex<IInMemoryIndexer, IInMemorySearcher, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMedia, UmbracoObjectTypes.Media);

            // register in-memory index for members
            options.RegisterIndex<IInMemoryIndexer, IInMemorySearcher, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMembers, UmbracoObjectTypes.Member);
        });

        return builder;
    }

    public static IUmbracoBuilder RebuildIndexesAfterStartup(this IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, RebuildIndexesNotificationHandler>();
        return builder;
    }
}