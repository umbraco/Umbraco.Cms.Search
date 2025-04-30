using Examine.Lucene.Directories;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

namespace Umbraco.Test.Search.Examine.Integration.Tests;


public class IndexServiceTests : InvariantContentTestBase
{
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AddExamineSearchProvider();
        builder.Services.AddSingleton<IDirectoryFactory, TestInMemoryDirectoryFactory>();
    }

    [Test]
    public async Task CanIndexAnyData()
    {
        var content = ContentService.GetById(RootKey);
        Assert.IsNotNull(content);
    }
}