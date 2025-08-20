using Umbraco.Cms.Core.Models;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

public abstract class SearcherTestBase : TestBase
{
    protected ISearcher Searcher => GetRequiredService<ISearcher>();
}
