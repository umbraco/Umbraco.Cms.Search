using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;

namespace Umbraco.Test.Search.Examine.Integration.Tests.NonUmbracoTests;

public abstract class NonUmbracoTestBase : TestBase
{
    protected const string IndexAlias = Constants.IndexAliases.DraftContent;
    protected const string FieldSingleValue = "fieldone";
    protected const string FieldMultipleValue = "fieldtwo";
    
    protected ISearcher Searcher => GetRequiredService<ISearcher>();
    
    [SetUp]
    public async Task Setup()
    {
        await EnsureIndex();

        var indexer = GetRequiredService<IIndexer>();
        
        for (var i = 1; i <= 100; i++)
        {
            var id = Guid.NewGuid();

            await indexer.AddOrUpdateAsync(
                IndexAlias,
                id,
                UmbracoObjectTypes.Document,
                [new Variation(Culture: null, Segment: null)],
                [
                    new IndexField(
                        FieldSingleValue,
                        new IndexValue
                        {
                            // Decimals = [i * 0.01m],
                            Integers = [i],
                            // Keywords = [$"single{i}"],
                            // DateTimeOffsets = [StartDate().AddDays(i)],
                            // Texts = [$"single{i}"]
                        },
                        Culture: null,
                        Segment: null
                    ),
                    new IndexField(
                        FieldMultipleValue,
                        new IndexValue
                        {
                            // Decimals = [i * 0.01m],
                            Integers = [i, i + 1],
                            // Keywords = [$"single{i}"],
                            // DateTimeOffsets = [StartDate().AddDays(i)],
                            // Texts = [$"single{i}"]
                        },
                        Culture: null,
                        Segment: null
                    ),
                ],
                null);
        }
        
        await Task.Delay(3000);
    }
    
    private async Task EnsureIndex()
    {
        await DeleteIndex();
    }
    
    private async Task DeleteIndex()
        => await GetRequiredService<IIndexer>().ResetAsync(IndexAlias);
}