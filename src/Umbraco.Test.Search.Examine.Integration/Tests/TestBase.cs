using Examine;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Mapping;
using Umbraco.Cms.Search.Provider.Examine.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Examine.Integration.Tests.IndexService;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class TestBase : UmbracoIntegrationTest
{
    protected static readonly Guid RootKey = Guid.Parse("D9EBF985-C65C-4341-955F-FFADA160F6D9");
    protected static readonly Guid ChildKey = Guid.Parse("C84E91B2-3351-4BA9-9906-09C2260D77EC");
    protected static readonly Guid GrandchildKey = Guid.Parse("201858C2-5AC2-4505-AC2E-E4BF38F39AC4");
    
    protected DateTime CurrentDateTime { get; set; }
    
    protected DateTimeOffset CurrentDateTimeOffset = DateTimeOffset.Now;

    protected decimal DecimalValue = 12.431167165486823626216m;
    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    protected IContentService ContentService => GetRequiredService<IContentService>();
    protected IDataTypeService DataTypeService => GetRequiredService<IDataTypeService>();
    protected ILocalizationService LocalizationService => GetRequiredService<ILocalizationService>();
    
        protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        
        builder.Services.AddExamine();
        builder.Services.AddSingleton<TestInMemoryDirectoryFactory>();
        builder.Services.ConfigureOptions<TestIndexConfigureOptions>();
        builder.Services.ConfigureOptions<ConfigureIndexOptions>();
        builder.Services.AddExamineLuceneIndex<TestIndex, TestInMemoryDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.DraftContent,
            config => { });
        builder.Services.AddExamineLuceneIndex<TestIndex, TestInMemoryDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.PublishedContent,
            config => { });        
        builder.Services.AddExamineLuceneIndex<TestIndex, TestInMemoryDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.DraftMedia,
            config => { });    
        builder.Services.AddExamineLuceneIndex<TestIndex, TestInMemoryDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.DraftMembers,
            config => { });
        
        builder.Services.AddTransient<IIndexer, Indexer>();
        builder.Services.AddTransient<ISearcher, Searcher>();
        builder.Services.AddTransient<IExamineMapper, ExamineMapper>();
        
        builder.Services.Configure<Umbraco.Cms.Search.Core.Configuration.IndexOptions>(options =>
        {
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IIndexer, ISearcher, IPublishedContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.DraftMedia, UmbracoObjectTypes.Media);
            options.RegisterIndex<IIndexer, ISearcher, IDraftContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.DraftMembers, UmbracoObjectTypes.Member);
        });

        builder.AddSearchCore();
        
        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MediaTreeChangeNotification, MediaTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberSavedNotification, MemberSavedDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberDeletedNotification, MemberDeletedDistributedCacheNotificationHandler>();
    }
}