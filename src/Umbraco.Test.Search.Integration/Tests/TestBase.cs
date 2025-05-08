using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public abstract class TestBase : UmbracoIntegrationTest
{
    private readonly TestIndexService _testIndexService = new();
    
    internal static class IndexAliases
    {
        public const string PublishedContent = "TestPublishedContentIndex";
        public const string DraftContent = "TestDraftContentIndex";
        public const string Media = "TestMediaIndex";
        public const string Member = "TestMemberIndex";
    }

    protected TestIndexService IndexService => _testIndexService;
        
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddSearchCore();
        
        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();

        builder.Services.AddTransient<IIndexService>(_ => IndexService);
        builder.Services.AddTransient<ISearchService>(_ => IndexService);

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<IIndexService, IPublishedContentChangeStrategy>(IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IIndexService, IDraftContentChangeStrategy>(IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IIndexService, IDraftContentChangeStrategy>(IndexAliases.Media, UmbracoObjectTypes.Media);
            options.RegisterIndex<IIndexService, IDraftContentChangeStrategy>(IndexAliases.Member, UmbracoObjectTypes.Member);
        });

        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MediaTreeChangeNotification, MediaTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberSavedNotification, MemberSavedDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberDeletedNotification, MemberDeletedDistributedCacheNotificationHandler>();
    }
}