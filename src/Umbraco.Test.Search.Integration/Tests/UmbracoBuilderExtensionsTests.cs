using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Test.Search.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.None)]
public class UmbracoBuilderExtensionsTests : UmbracoIntegrationTest
{
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        // Call AddSearchCore() twice to ensure it's idempotent.
        builder.AddSearchCore();
        builder.AddSearchCore();
    }

    [Test]
    public void Can_Get_SwaggerGenOptions()
        => Assert.DoesNotThrow(() =>
        {
            SwaggerGenOptions swaggerGenOptions = GetRequiredService<IOptions<SwaggerGenOptions>>().Value;
            Assert.That(swaggerGenOptions.SwaggerGeneratorOptions.SwaggerDocs, Contains.Key(Constants.Api.Name));
        });
}
