using Examine;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core;
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
using Umbraco.Cms.Search.Provider.Examine.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class IndexTestBase : UmbracoIntegrationTest
{
    protected DateTimeOffset CurrentDateTimeOffset = DateTimeOffset.UtcNow;

    protected double DecimalValue = 12.43;
    protected Guid RootKey { get; } = Guid.NewGuid();
    
    protected Guid ChildKey { get; } = Guid.NewGuid();

    protected Guid GrandchildKey { get; } = Guid.NewGuid();
    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    protected IContentService ContentService => GetRequiredService<IContentService>();
    protected IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
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
        builder.Services.AddTransient<IndexService>();
        builder.Services.AddTransient<IIndexer, IndexService>();
        builder.Services.AddTransient<ISearcher, SearchService>();
        
        builder.Services.Configure<Umbraco.Cms.Search.Core.Configuration.IndexOptions>(options =>
        {
            options.RegisterIndex<IndexService, SearchService, IDraftContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IndexService, SearchService, IPublishedContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IndexService, SearchService, IDraftContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.DraftMedia, UmbracoObjectTypes.Media);
            options.RegisterIndex<IndexService, SearchService, IDraftContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.DraftMembers, UmbracoObjectTypes.Member);
        });

        builder.AddSearchCore();
        
        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MediaTreeChangeNotification, MediaTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberSavedNotification, MemberSavedDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberDeletedNotification, MemberDeletedDistributedCacheNotificationHandler>();
    }

    protected void SaveAndPublish(IContent content)
    {
        ContentService.Save(content);
        ContentService.Publish(content, new []{ "*"});
        Thread.Sleep(3000);
    }
    
}