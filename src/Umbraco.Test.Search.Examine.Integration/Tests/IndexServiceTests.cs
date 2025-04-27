using Examine;
using Examine.Lucene;
using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewEmptyPerTest)]
public class IndexServiceTests : UmbracoIntegrationTest
{
    
    public IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        builder.Services.AddSingleton<LuceneRAMDirectoryFactory>();
        builder.Services.AddExamine();
        builder.Services.AddExamineLuceneIndex<MemoryIndex, LuceneRAMDirectoryFactory>(MemoryIndex.TestIndexName);
    }
    
    private class MemoryIndex : LuceneIndex
    {
        public const string TestIndexName = "TestIndex";

        public MemoryIndex(ILoggerFactory loggerFactory, string name, IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions) : base(loggerFactory, name, indexOptions)
        {
        }
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
        var index = GetIndex();
        IndexData(index);
        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.TotalItemCount, Is.EqualTo(3));
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

    private IIndex GetIndex()
    {
        ExamineManager.TryGetIndex("TestIndex", out IIndex index);
        return index;
    }
}