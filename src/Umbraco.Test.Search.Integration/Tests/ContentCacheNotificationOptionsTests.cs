using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Tests.Common.Testing;

namespace Umbraco.Test.Search.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentCacheNotificationOptionsTests : TestBase
{
    [Test]
    public void DistributedCacheIsUsedByDefault()
    {
        IOptions<ContentCacheNotificationOptions> options = GetRequiredService<IOptions<ContentCacheNotificationOptions>>();
        Assert.That(options.Value.UseDistributedCache, Is.True);
    }
}
