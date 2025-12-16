using Examine;
using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Search.Provider.Examine.Compat;

namespace Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineSearchProvider(this IUmbracoBuilder builder)
    {
        builder.Services.AddExamineLuceneIndex<LuceneIndex, CompatConfigurationEnabledDirectoryFactory>(Core.Constants.IndexAliases.DraftContent, _ => { });

        builder.Services.AddExamineLuceneIndex<LuceneIndex, CompatConfigurationEnabledDirectoryFactory>(Core.Constants.IndexAliases.PublishedContent, _ => { });

        builder.Services.AddExamineLuceneIndex<LuceneIndex, CompatConfigurationEnabledDirectoryFactory>(Core.Constants.IndexAliases.DraftMedia, _ => { });

        builder.Services.AddExamineLuceneIndex<LuceneIndex, CompatConfigurationEnabledDirectoryFactory>(Core.Constants.IndexAliases.DraftMembers, _ => { });

        // This is needed, due to locking of indexes on Azure, to read more on this issue go here: https://github.com/umbraco/Umbraco-CMS/pull/15571
        builder.Services.AddSingleton<UmbracoTempEnvFileSystemDirectoryFactory>();
        builder.Services.RemoveAll<SyncedFileSystemDirectoryFactory>();
        builder.Services.AddSingleton<SyncedFileSystemDirectoryFactory>(
            s =>
            {
                var tempDir = UmbracoTempEnvFileSystemDirectoryFactory.GetTempPath(
                    s.GetRequiredService<IApplicationIdentifier>(), s.GetRequiredService<IHostingEnvironment>());

                return ActivatorUtilities.CreateInstance<SyncedFileSystemDirectoryFactory>(
                    s, new DirectoryInfo(tempDir), s.GetRequiredService<IApplicationRoot>().ApplicationRoot);
            });

        builder.Services.AddExamineSearchProviderServices();

        return builder;
    }
}
