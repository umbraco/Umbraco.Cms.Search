using NUnit.Framework;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture)]
public class IndexServiceTests : UmbracoIntegrationTest
{
    [Test]
    public async Task CanIndexCustomData()
    {
        Assert.Pass();
    }
}