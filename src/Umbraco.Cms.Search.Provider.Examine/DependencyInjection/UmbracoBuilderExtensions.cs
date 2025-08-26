using Examine;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineSearchProvider(this IUmbracoBuilder builder)
    {
        builder.Services.AddExamineSearchProviderServices();

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
}
