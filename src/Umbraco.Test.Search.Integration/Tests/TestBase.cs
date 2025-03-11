using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class TestBase : UmbracoIntegrationTest
{
    private readonly TestIndexService _testIndexService = new();
    
    protected static class IndexAliases
    {
        public const string PublishedContent = "TestPublishedContentIndex";
        public const string DraftContent = "TestDraftContentIndex";
    }

    protected TestIndexService IndexService => _testIndexService;

    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>(); 

    protected IContentService ContentService => GetRequiredService<IContentService>();
        
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        _testIndexService.Reset();
        
        builder.AddSearchCore();
        
        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();

        builder.Services.AddTransient<IIndexService>(_ => IndexService);

        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterIndex<IIndexService, IPublishedContentChangeStrategy>(IndexAliases.PublishedContent);
            options.RegisterIndex<IIndexService, IDraftContentChangeStrategy>(IndexAliases.DraftContent);
        });
    }
}