using Examine;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineSearchProvider(this IUmbracoBuilder builder)
    {
        builder.Services.AddExamineSearchProviderServices();

        builder.Services.AddExamineLuceneIndex(Core.Constants.IndexAliases.DraftContent, _ => { });

        builder.Services.AddExamineLuceneIndex(Core.Constants.IndexAliases.PublishedContent, _ => { });

        builder.Services.AddExamineLuceneIndex(Core.Constants.IndexAliases.DraftMedia, _ => { });

        builder.Services.AddExamineLuceneIndex(Core.Constants.IndexAliases.DraftMembers, _ => { });

        return builder;
    }
}
