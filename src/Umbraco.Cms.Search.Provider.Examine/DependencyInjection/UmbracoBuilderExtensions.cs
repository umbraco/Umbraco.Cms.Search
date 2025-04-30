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
        
        builder.Services.AddExamineLuceneIndex(Constants.IndexAliases.DraftContent, configuration =>
        {
        });     
        
        builder.Services.AddExamineLuceneIndex(Constants.IndexAliases.PublishedContent, configuration =>
        {
        });      
        
        builder.Services.AddExamineLuceneIndex(Constants.IndexAliases.DraftMedia, configuration =>
        {
        });     
        
        builder.Services.AddExamineLuceneIndex(Constants.IndexAliases.DraftMembers, configuration =>
        {
        });
        
        builder.Services.AddTransient<InvariantIndexService>();
        builder.Services.AddTransient<IIndexService, InvariantIndexService>();
        builder.Services.AddTransient<ISearchService, SearchService>();
        
        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<InvariantIndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<InvariantIndexService, IPublishedContentChangeStrategy>(Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<InvariantIndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMedia, UmbracoObjectTypes.Media);
            options.RegisterIndex<InvariantIndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMembers, UmbracoObjectTypes.Member);
        });
        
        
        //
        // // rebuild indexes after start-up
        // builder.AddNotificationHandler<UmbracoApplicationStartedNotification, RebuildIndexesNotificationHandler>();

        return builder;
    }
}