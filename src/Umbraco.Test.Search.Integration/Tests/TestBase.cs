using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
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

    protected TestIndexer Indexer { get; } = new();

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddSearchCore();

        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        builder.Services.AddUnique<IDocumentService, DocumentService>();
        builder.Services.AddUnique<IDocumentRepository, DocumentRepository>();

        builder.Services.AddTransient<IIndexer>(_ => Indexer);
        builder.Services.AddTransient<ISearcher>(_ => Indexer);

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<IIndexer, ISearcher, IPublishedContentChangeStrategy>(IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(IndexAliases.Media, UmbracoObjectTypes.Media);
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(IndexAliases.Member, UmbracoObjectTypes.Member);
        });

        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MediaTreeChangeNotification, MediaTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberSavedNotification, MemberSavedDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberDeletedNotification, MemberDeletedDistributedCacheNotificationHandler>();
    }

    private class DocumentRepository : IDocumentRepository
    {
        public Task AddAsync(Document document) => Task.CompletedTask;

        public Task<Document?> GetAsync(Guid id, bool published) => Task.FromResult<Document?>(null);

        public Task DeleteAsync(Guid[] ids, bool published) => Task.CompletedTask;
    }
}
