using Examine;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

public abstract class IndexTestBase : TestBase
{
    protected IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
    protected IIndexer Indexer => GetRequiredService<IIndexer>();
}
