using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.ServerEvents;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.Persistence;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public abstract class TestBase : UmbracoIntegrationTest
{
    internal static class IndexAliases
    {
        public const string PublishedContent = "TestPublishedContentIndex";
        public const string DraftContent = "TestDraftContentIndex";
        public const string Media = "TestMediaIndex";
        public const string Member = "TestMemberIndex";
    }

    protected TestIndexerAndSearcher IndexerAndSearcher { get; } = new();

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddSearchCore();

        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        builder.Services.AddUnique<IServerEventRouter, NoOpServerEventRouter>();
        builder.Services.AddUnique<IIndexDocumentRepository, NoopIndexDocumentRepository>();

        builder.Services.AddTransient<IIndexer>(_ => IndexerAndSearcher);
        builder.Services.AddTransient<ISearcher>(_ => IndexerAndSearcher);

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterContentIndex<IIndexer, ISearcher, IPublishedContentChangeStrategy>(IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterContentIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterContentIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(IndexAliases.Media, UmbracoObjectTypes.Media);
            options.RegisterContentIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(IndexAliases.Member, UmbracoObjectTypes.Member);
        });

        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MediaTreeChangeNotification, MediaTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberSavedNotification, MemberSavedDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberDeletedNotification, MemberDeletedDistributedCacheNotificationHandler>();
    }

    // these tests are not concerned with DB cached index data, so this is a no-op implementation
    // of the document repository.
    private class NoopIndexDocumentRepository : IIndexDocumentRepository
    {
        public Task AddAsync(IndexDocument indexDocument) => Task.CompletedTask;

        public Task<IndexDocument?> GetAsync(Guid id, bool published) => Task.FromResult<IndexDocument?>(null);

        public Task DeleteAsync(Guid[] ids, bool published) => Task.CompletedTask;

        public Task DeleteAllAsync() => Task.CompletedTask;
    }
}
