using Examine;
using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Search.Core.Notifications;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.NotificationHandlers;
using Umbraco.Cms.Search.Provider.Examine.Services;

namespace Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddExamineSearchProvider(this IUmbracoBuilder builder)
    {
        IConfigurationSection section = builder.Config.GetSection(ExamineSearchProviderSettings.SectionName);
        builder.Services.Configure<ExamineSearchProviderSettings>(section);

        ExamineSearchProviderSettings settings = section.Get<ExamineSearchProviderSettings>() ?? new ExamineSearchProviderSettings();

        if (settings.ZeroDowntimeIndexing)
        {
            // Register dual indexes (_a and _b) per logical alias for zero-downtime reindexing (blue/green).
            builder.AddActiveAndShadowIndex(Core.Constants.IndexAliases.DraftContent);
            builder.AddActiveAndShadowIndex(Core.Constants.IndexAliases.PublishedContent);
            builder.AddActiveAndShadowIndex(Core.Constants.IndexAliases.DraftMedia);
            builder.AddActiveAndShadowIndex(Core.Constants.IndexAliases.DraftMembers);

            builder.Services.AddSingleton<IActiveIndexManager, ActiveIndexManager>();

            builder.AddNotificationAsyncHandler<IndexRebuildStartingNotification, ZeroDowntimeRebuildNotificationHandler>();
            builder.AddNotificationAsyncHandler<IndexRebuildCompletedNotification, ZeroDowntimeRebuildNotificationHandler>();
        }
        else
        {
            // Register a single index per logical alias (default, no zero-downtime reindexing).
            builder.AddSingleIndex(Core.Constants.IndexAliases.DraftContent);
            builder.AddSingleIndex(Core.Constants.IndexAliases.PublishedContent);
            builder.AddSingleIndex(Core.Constants.IndexAliases.DraftMedia);
            builder.AddSingleIndex(Core.Constants.IndexAliases.DraftMembers);
        }

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

        builder.Services.AddExamineSearchProviderServices();

        return builder;
    }

    private static void AddActiveAndShadowIndex(this IUmbracoBuilder builder, string alias)
    {
        builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(alias + ActiveIndexManager.SuffixA, _ => { });
        builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(alias + ActiveIndexManager.SuffixB, _ => { });
    }

    private static void AddSingleIndex(this IUmbracoBuilder builder, string alias)
    {
        builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(alias, _ => { });
    }
}
