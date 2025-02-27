using IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Package.Models.Indexing;
using Package.Services;
using Package.Services.ContentIndexing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace IntegrationTests.Tests;

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
