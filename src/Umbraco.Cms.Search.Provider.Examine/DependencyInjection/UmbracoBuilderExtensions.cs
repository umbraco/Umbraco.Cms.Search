using Examine;
using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Services;
using IndexOptions = Umbraco.Cms.Search.Core.Configuration.IndexOptions;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineSearchProvider(this IUmbracoBuilder builder)
    {
        AddServices(builder);

        builder.Services.AddExamineLuceneIndex(Search.Core.Constants.IndexAliases.DraftContent, configuration =>
        {
        });

        builder.Services.AddExamineLuceneIndex(Search.Core.Constants.IndexAliases.PublishedContent, configuration =>
        {
        });

        builder.Services.AddExamineLuceneIndex(Search.Core.Constants.IndexAliases.DraftMedia, configuration =>
        {
        });

        builder.Services.AddExamineLuceneIndex(Search.Core.Constants.IndexAliases.DraftMembers, configuration =>
        {
        });


        return builder;
    }

    internal static IUmbracoBuilder AddExamineSearchProviderForTest<TIndex, TDirectoryFactory>(this IUmbracoBuilder builder)
        where TIndex : LuceneIndex
        where TDirectoryFactory : class, IDirectoryFactory
    {
        AddServices(builder);

        builder.Services.AddSingleton<TDirectoryFactory>();

        // Register indexes with optional custom type and factory
        builder.Services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Search.Core.Constants.IndexAliases.DraftContent,
            config => { });

        builder.Services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Search.Core.Constants.IndexAliases.PublishedContent,
            config => { });

        builder.Services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Search.Core.Constants.IndexAliases.DraftMedia,
            config => { });

        builder.Services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Search.Core.Constants.IndexAliases.DraftMembers,
            config => { });

        return builder;
    }

    private static void AddServices(IUmbracoBuilder builder)
    {
        builder.Services.AddExamine();
        builder.Services.ConfigureOptions<ConfigureIndexOptions>();

        // register the in-memory searcher and indexer so they can be used explicitly for index registrations
        builder.Services.AddTransient<IExamineIndexer, Indexer>();
        builder.Services.AddTransient<IExamineSearcher, Searcher>();

        builder.Services.AddTransient<IIndexer, Indexer>();
        builder.Services.AddTransient<ISearcher, Searcher>();

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<IExamineIndexer, IExamineSearcher, IDraftContentChangeStrategy>(Search.Core.Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IExamineIndexer, IExamineSearcher, IPublishedContentChangeStrategy>(Search.Core.Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IExamineIndexer, IExamineSearcher, IDraftContentChangeStrategy>(Search.Core.Constants.IndexAliases.DraftMedia, UmbracoObjectTypes.Media);
            options.RegisterIndex<IExamineIndexer, IExamineSearcher, IDraftContentChangeStrategy>(Search.Core.Constants.IndexAliases.DraftMembers, UmbracoObjectTypes.Member);
        });


        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MediaTreeChangeNotification, MediaTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberSavedNotification, MemberSavedDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberDeletedNotification, MemberDeletedDistributedCacheNotificationHandler>();
    }
}
