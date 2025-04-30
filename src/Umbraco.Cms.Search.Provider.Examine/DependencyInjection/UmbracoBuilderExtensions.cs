using Examine;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Services;
using IndexOptions = Umbraco.Cms.Search.Core.Configuration.IndexOptions;

namespace Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineSearchProvider(this IUmbracoBuilder builder)
    {
        builder.Services.AddExamine();
        builder.Services.AddExamineLuceneIndex("ExamineIndex", configuration =>
        {
        });
        
        builder.Services.AddTransient<IIndexService, IndexService>();
        
        // // register the search and index services as the default services
        // builder.Services.AddTransient<IIndexService, InMemoryIndexService>();
        //
        builder.Services.Configure<IndexOptions>(options =>
        {
            // register in-memory indexes for draft and published content
            options.RegisterIndex<IndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IndexService, IPublishedContentChangeStrategy>(Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
        
            // register in-memory index for media
            options.RegisterIndex<IndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMedia, UmbracoObjectTypes.Media);
        
            // register in-memory index for members
            options.RegisterIndex<IndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMembers, UmbracoObjectTypes.Member);
        });
        //
        // // rebuild in-memory indexes after start-up
        // builder.AddNotificationHandler<UmbracoApplicationStartedNotification, RebuildIndexesNotificationHandler>();

        return builder;
    }
}