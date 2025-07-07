using Examine;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public class SearcherTestBase : TestBase
{
    protected ISearcher Searcher => GetRequiredService<ISearcher>();
    
    protected IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
}