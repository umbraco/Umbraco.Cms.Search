using NUnit.Framework;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

public class RebuildTests : SearcherTestBase
{
    protected string GetIndexPath(string indexName)
    {
        var root = TestContext.CurrentContext.TestDirectory.Split("Umbraco.Tests.Integration")[0];
        return Path.Combine(root, "Umbraco.Tests.Integration", "umbraco", "Data", "TEMP", "ExamineIndexes", indexName);
    }
}
