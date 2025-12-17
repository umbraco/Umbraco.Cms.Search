using NUnit.Framework;
using Umbraco.Cms.Core;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

public abstract class SearcherTestBase : TestBase
{
    protected ISearcher Searcher => GetRequiredService<ISearcher>();

    [SetUp]
    public async Task RunMigrations()
    {
        await PackageMigrationRunner.RunPackageMigrationsIfPendingAsync("Umbraco CMS Search").ConfigureAwait(false);
        Assert.That(RuntimeState.Level, Is.EqualTo(RuntimeLevel.Run));
    }
}
