using Examine;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Services;
using IndexOptions = Umbraco.Cms.Search.Core.Configuration.IndexOptions;

namespace Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineSearchProvider(this IUmbracoBuilder builder)
    {
        builder.Services.AddExamine();
        
        builder.Services.ConfigureOptions<ConfigureIndexOptions>();
        
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
        
        builder.Services.AddTransient<IndexService>();
        builder.Services.AddTransient<IIndexService, IndexService>();
        builder.Services.AddTransient<ISearchService, SearchService>();
        
        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<IndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IndexService, IPublishedContentChangeStrategy>(Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMedia, UmbracoObjectTypes.Media);
            options.RegisterIndex<IndexService, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMembers, UmbracoObjectTypes.Member);
        });

        return builder;
    }
}