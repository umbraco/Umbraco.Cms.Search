using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public class SearcherTestBase : TestBase
{
    protected ISearcher Searcher => GetRequiredService<ISearcher>();

}