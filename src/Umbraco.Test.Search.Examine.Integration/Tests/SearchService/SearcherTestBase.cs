using Examine;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public abstract class SearcherTestBase : TestBase
{
    protected ISearcher Searcher => GetRequiredService<ISearcher>();
    
    protected IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
    
    protected string GetIndexAlias(bool publish)
    {
        return publish ? Cms.Search.Core.Constants.IndexAliases.PublishedContent : Cms.Search.Core.Constants.IndexAliases.DraftContent;
    }
}