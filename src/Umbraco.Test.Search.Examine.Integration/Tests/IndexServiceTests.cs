using Examine;
using NUnit.Framework;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture)]
public class IndexServiceTests : UmbracoIntegrationTest
{
    
    public IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        builder.Services.AddExamine();
        builder.Services.AddExamineLuceneIndex("TestIndex");
    }
    
    [Test]
    public async Task CanIndexAnyData()
    {
        var index = GetIndex();
        index.IndexItem(new ValueSet(
            "test",
            "Person",
            new Dictionary<string, object>()
            {
                {"FirstName", "Nikolaj" },
                {"LastName", "Geisle" },
                {"Email", "nge@umbraco.dk" },
                {"Age", 30}
            }));

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.TotalItemCount, Is.EqualTo(1));
        Assert.That(results.First().Id, Is.EqualTo("test"));
        
    }
    
    [Test]
    public async Task CanIndexData()
    {
        IndexData();
        var index = GetIndex();
        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.TotalItemCount, Is.EqualTo(3));
        
    }

    public void IndexData(int count = 3)
    {
        var index = GetIndex();
        for (int i = 0; i < count; i++)
        {
            index.IndexItem(new ValueSet(
                $"TestId{i}",
                "Person",
                new Dictionary<string, object>()
                {
                    {"FirstName", $"FirstName{i}" },
                    {"LastName",  $"LastName{i}" },
                    {"Age", i },
                }));
        }
    }

    private IIndex GetIndex()
    {
        ExamineManager.TryGetIndex("TestIndex", out IIndex index);
        return index;
    }
}