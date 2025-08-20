using Examine;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

public abstract class IndexTestBase : TestBase
{
    protected IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
}
