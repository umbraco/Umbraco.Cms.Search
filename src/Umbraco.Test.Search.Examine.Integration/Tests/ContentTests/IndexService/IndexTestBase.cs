using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Testing;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class IndexTestBase : TestBase
{
    protected IExamineManager ExamineManager => GetRequiredService<IExamineManager>();

    protected void SaveAndPublish(IContent content)
    {
        ContentService.Save(content);
        ContentService.Publish(content, new []{ "*"});
        Thread.Sleep(3000);
    }
    
}