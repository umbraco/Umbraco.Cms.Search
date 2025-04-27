using Examine;
using Examine.Lucene;
using Examine.Lucene.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.None)]
public class InMemoryIndexTests : UmbracoIntegrationTest
{
    
    public IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddExamine();
        services.AddSingleton<TestInMemoryDirectoryFactory>();
        services.AddExamineLuceneIndex<TestIndex, TestInMemoryDirectoryFactory>(TestIndex.TestIndexName);
    }

    private class TestIndex : LuceneIndex
    {
        public const string TestIndexName = "TestIndex";

        public TestIndex(ILoggerFactory loggerFactory, string name, IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions) : base(loggerFactory, name, indexOptions)
        {
        }
    }

    [Test]
    public async Task CanIndexAnyData()
    {
        var index = GetIndex();
        index.IndexOperationComplete += (_, _) =>
        {
            indexingHandle.Set();
        };
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

        await indexingHandle.WaitOneAsync(millisecondsTimeout: 3000);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.TotalItemCount, Is.EqualTo(1));
        Assert.That(results.First().Id, Is.EqualTo("test"));
    }
    
    [Test]
    [TestCase(3)]
    [TestCase(50)]
    [TestCase(100)]
    public async Task CanIndexData(int count)
    {
        var index = GetIndex();
        index.IndexOperationComplete += (_, _) =>
        {
            indexingHandle.Set();
        };
        IndexData(index, count);
        await indexingHandle.WaitOneAsync(millisecondsTimeout: 3000);
        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.TotalItemCount, Is.EqualTo(count));
    }

    public void IndexData(IIndex index, int count = 3)
    {
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
    
    private AutoResetEvent indexingHandle = new(false);

    private IIndex GetIndex()
    {
        ExamineManager.TryGetIndex("TestIndex", out IIndex index);
        return index;
    }
}