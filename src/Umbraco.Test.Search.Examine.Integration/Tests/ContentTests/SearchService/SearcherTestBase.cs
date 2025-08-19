using Umbraco.Cms.Core.Models;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

public abstract class SearcherTestBase : TestBase
{
    protected ISearcher Searcher => GetRequiredService<ISearcher>();

    protected string GetIndexAlias(bool publish)
    {
        return publish ? Cms.Search.Core.Constants.IndexAliases.PublishedContent : Cms.Search.Core.Constants.IndexAliases.DraftContent;
    }

    protected void SaveAndPublish(IContent content)
    {
        ContentService.Save(content);
        ContentService.Publish(content, new []{ "*"});
        Thread.Sleep(3000);
    }
}
