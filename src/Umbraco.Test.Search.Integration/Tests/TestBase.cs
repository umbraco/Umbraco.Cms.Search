using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class TestBase : UmbracoIntegrationTest
{
    protected TestIndexService IndexService { get; } = new ();

    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>(); 

    protected IContentService ContentService => GetRequiredService<IContentService>();
        
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        builder.AddComposers();
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);
        services.AddTransient<IIndexService>(_ => IndexService);
    }

    protected async Task HandleContentChangeAsync(ContentChange contentChange)
    {
        var indexingService = (ContentIndexingService)GetRequiredService<IContentIndexingService>();
        await indexingService.HandleAsync([contentChange], CancellationToken.None);
    }
}
