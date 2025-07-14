using Examine;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Mapping;
using Umbraco.Cms.Search.Provider.Examine.Services;
using IndexOptions = Umbraco.Cms.Search.Core.Configuration.IndexOptions;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

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
        
        builder.Services.AddTransient<IIndexer, Indexer>();
        builder.Services.AddTransient<ISearcher, Searcher>();
        builder.Services.AddTransient<IExamineMapper, ExamineMapper>();
        
        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IIndexer, ISearcher, IPublishedContentChangeStrategy>(Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMedia, UmbracoObjectTypes.Media);
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(Constants.IndexAliases.DraftMembers, UmbracoObjectTypes.Member);
        });

        return builder;
    }
}