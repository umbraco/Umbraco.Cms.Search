using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

// tests specifically related to the IndexValue.Keywords collection
public  class KeywordTests : SearcherTestBase
{
    [Test]
    public async Task CanFilterSingleDocumentByKeyword()
    {
        SearchResult result = await SearchAsync(
            filters: [new KeywordFilter(FieldMultipleValues, ["single1"], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[1]));
            }
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanFilterMultipleDocumentsByKeyword(bool even)
    {
        SearchResult result = await SearchAsync(
            filters: [new KeywordFilter(FieldMultipleValues, [even ? "even" : "odd"], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(50));

                var documents = result.Documents.ToList();
                var expectedIds = OddOrEvenIds(even);
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EqualTo(expectedIds.Select(id => _documentIds[id])).AsCollection
                );
            }
        );
    }

    [Test]
    public async Task CanFilterAllDocumentsByKeyword()
    {
        SearchResult result = await SearchAsync(
            filters: [new KeywordFilter(FieldMultipleValues, ["all"], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(100));
                Assert.That(result.Documents.Select(d => d.Id), Is.EqualTo(_documentIds.Values).AsCollection);
            }
        );
    }

    [Test]
    public async Task CanFilterDocumentsByKeywordNegated()
    {
        SearchResult result = await SearchAsync(
            filters: [new KeywordFilter(FieldMultipleValues, ["single1"], true)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(99));
                Assert.That(result.Documents.Select(d => d.Id), Is.EqualTo(_documentIds.Values.Skip(1)).AsCollection);
            }
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanFacetDocumentsByKeyword(bool filtered)
    {
        SearchResult result = await SearchAsync(
            facets: [new KeywordFacet(FieldMultipleValues)],
            filters: filtered
                ? [new KeywordFilter(FieldMultipleValues, ["single10", "single20", "single30"], false)]
                : []
        );

        // expecting the same facets whether filtering is enabled or not, because
        // both faceting and filtering is applied to the same field
        var expectedFacetValues = Enumerable
            .Range(1, 100)
            .SelectMany(i => new[] { "all", i % 2 == 0 ? "even" : "odd", $"single{i}" })
            .GroupBy(i => i)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToArray();

        // expecting
        // - when filtered: 10, 20 and 30
        // - when not filtered: all of them
        Assert.That(result.Total, Is.EqualTo(filtered ? 3 : 100));

        FacetResult[] facets = result.Facets.ToArray();
        Assert.That(facets, Has.Length.EqualTo(1));

        FacetResult facet = facets.First();
        Assert.That(facet.FieldName, Is.EqualTo(FieldMultipleValues));

        KeywordFacetValue[] facetValues = facet.Values.OfType<KeywordFacetValue>().ToArray();
        Assert.That(facetValues, Has.Length.EqualTo(expectedFacetValues.Length));
        foreach (var expectedFacetValue in expectedFacetValues)
        {
            KeywordFacetValue? facetValue = facetValues.FirstOrDefault(f => f.Key == expectedFacetValue.Key);
            Assert.That(facetValue, Is.Not.Null);
            Assert.That(facetValue.Count, Is.EqualTo(expectedFacetValue.Count));
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSortDocumentsByKeyword(bool ascending)
    {
        SearchResult result = await SearchAsync(
            sorters: [new KeywordSorter(FieldSingleValue, ascending ? Direction.Ascending : Direction.Descending)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(100));
                Assert.That(result.Documents.First().Id, Is.EqualTo(ascending ? _documentIds[1] : _documentIds[99]));
            }
        );
    }
}
