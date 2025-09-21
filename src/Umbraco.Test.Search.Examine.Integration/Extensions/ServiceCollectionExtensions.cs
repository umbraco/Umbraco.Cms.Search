using Examine;
using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

namespace Umbraco.Test.Search.Examine.Integration.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExamineSearchProviderServicesForTest<TIndex, TDirectoryFactory>(this IServiceCollection services)
        where TIndex : LuceneIndex
        where TDirectoryFactory : class, IDirectoryFactory
    {

        services.ConfigureOptions<TestIndexConfigureOptions>();
        services.AddSingleton<TDirectoryFactory>();

        // Register indexes with optional custom type and factory
        services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.DraftContent,
            _ => { });

        services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.PublishedContent,
            _ => { });

        services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.DraftMedia,
            _ => { });

        services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.DraftMembers,
            _ => { });

        services.AddExamineSearchProviderServices();


        return services;
    }
}
