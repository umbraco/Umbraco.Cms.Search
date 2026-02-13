using Examine;
using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Cms.Search.Provider.Examine.NotificationHandlers;
using Umbraco.Cms.Search.Provider.Examine.Services;

namespace Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineSearchProvider(this IUmbracoBuilder builder)
    {
        // Register two physical indexes per logical alias for zero-downtime reindexing
        builder.AddActiveAndShadowIndex(Core.Constants.IndexAliases.DraftContent);
        builder.AddActiveAndShadowIndex(Core.Constants.IndexAliases.PublishedContent);
        builder.AddActiveAndShadowIndex(Core.Constants.IndexAliases.DraftMedia);
        builder.AddActiveAndShadowIndex(Core.Constants.IndexAliases.DraftMembers);

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

        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, RebuildNotificationHandler>();
        builder.AddNotificationAsyncHandler<IndexRebuildStartingNotification, ZeroDowntimeRebuildNotificationHandler>();
        builder.AddNotificationAsyncHandler<IndexRebuildCompletedNotification, ZeroDowntimeRebuildNotificationHandler>();

        builder.Services.AddExamineSearchProviderServices();

        return builder;
    }

    private static void AddActiveAndShadowIndex(this IUmbracoBuilder builder, string alias)
    {
        builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(alias + ActiveIndexManager.SuffixA, _ => { });
        builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(alias + ActiveIndexManager.SuffixB, _ => { });
    }
}
