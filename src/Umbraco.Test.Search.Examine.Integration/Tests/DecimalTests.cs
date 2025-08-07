using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

// tests specifically related to the IndexValue.Decimals collection
public class DecimalTests : SearcherTestBase
{
    [Test]
    public async Task CanFilterSingleDocumentByDecimalExact()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalExactFilter(FieldMultipleValues, [1.5m], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[1]));
            }
        );
    }

    [Test]
    public async Task CanFilterSingleDocumentByNegativeDecimalExact()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalExactFilter(FieldMultipleValues, [-1.5m], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[1]));
            }
        );
    }

    [Test]
    public async Task CanFilterSingleDocumentByDecimalRange()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalRangeFilter(FieldMultipleValues, [new FilterRange<decimal?>(1m, 2m)], false)]
            // filters: [new DecimalRangeFilter(FieldMultipleValues, [new DecimalRangeFilterRange(1m, 2m)], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[1]));
            }
        );
    }

    [Test]
    public async Task CanFilterSingleDocumentByNegativeDecimalRange()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalRangeFilter(FieldMultipleValues, [new FilterRange<decimal?>(-1.9m, -1.1m)], false)]
            // filters: [new DecimalRangeFilter(FieldMultipleValues, [new DecimalRangeFilterRange(-1.9m, -1.1m)], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[1]));
            }
        );
    }

    [Test]
    public async Task CanFilterMultipleDocumentsByDecimalExact()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalExactFilter(FieldMultipleValues, [15m, 30m, 42m], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(6));

                var documents = result.Documents.ToList();
                // expecting 10 (15), 15 (15), 20 (30), 28 (42), 30 (30) and 42 (42)
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EqualTo(
                        new[]
                        {
                            _documentIds[10],
                            _documentIds[15],
                            _documentIds[20],
                            _documentIds[28],
                            _documentIds[30],
                            _documentIds[42]
                        }
                    ).AsCollection
                );
            }
        );
    }

    [Test]
    public async Task CanFilterMultipleDocumentsByDecimalRange()
    {
        SearchResult result = await SearchAsync(
            filters:
            [
                new DecimalRangeFilter(
                    FieldMultipleValues,
                    [
                        new FilterRange<decimal?>(1m, 5m),
                        new FilterRange<decimal?>(20m, 25m),
                        new FilterRange<decimal?>(100m, 101m)
                        // new DecimalRangeFilterRange(1m, 5m),
                        // new DecimalRangeFilterRange(20m, 25m),
                        // new DecimalRangeFilterRange(100m, 101m)
                    ],
                    false
                )
            ]
        );

        Assert.Multiple(
            () =>
            {
                // expecting
                // - first range: 1, 2, 3, 4
                // - second range: 14 (21), 15 (22.5), 16 (24), 20, 21, 22, 23, 24
                // - third range: 67 (100.5), 100
                Assert.That(result.Total, Is.EqualTo(14));

                var documents = result.Documents.ToList();
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EquivalentTo(
                        new[]
                        {
                            _documentIds[1],
                            _documentIds[2],
                            _documentIds[3],
                            _documentIds[4],
                            _documentIds[14],
                            _documentIds[15],
                            _documentIds[16],
                            _documentIds[20],
                            _documentIds[21],
                            _documentIds[22],
                            _documentIds[23],
                            _documentIds[24],
                            _documentIds[67],
                            _documentIds[100],
                        }
                    )
                );
            }
        );
    }

    [Test]
    public async Task CanFilterDocumentsByDecimalExactNegated()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalExactFilter(FieldMultipleValues, [1.5m], true)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(99));
                Assert.That(result.Documents.Select(d => d.Id), Is.EqualTo(_documentIds.Values.Skip(1)).AsCollection);
            }
        );
    }

    [Test]
    public async Task CanFilterDocumentsByDecimalRangeNegated()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalRangeFilter(FieldMultipleValues, [new FilterRange<decimal?>(1m, 2m)], true)]
            // filters: [new DecimalRangeFilter(FieldMultipleValues, [new DecimalRangeFilterRange(1m, 2m)], true)]
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
    public async Task CanFacetDocumentsByDecimalExact(bool filtered)
    {
        SearchResult result = await SearchAsync(
            facets: [new DecimalExactFacet(FieldMultipleValues)],
            filters: filtered ? [new DecimalExactFilter(FieldMultipleValues, [1m, 2m, 3m], false)] : []
        );

        // expecting the same facets whether filtering is enabled or not, because
        // both faceting and filtering is applied to the same field
        var expectedFacetValues = Enumerable
            .Range(1, 100)
            .SelectMany(i => new[] { i, i * 1.5m, i * -1m, i * -1.5m })
            .GroupBy(i => i)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToArray();

        // expecting
        // - when filtered: 1, 2 and 3
        // - when not filtered: all of them
        Assert.That(result.Total, Is.EqualTo(filtered ? 3 : 100));

        FacetResult[] facets = result.Facets.ToArray();
        Assert.That(facets, Has.Length.EqualTo(1));

        FacetResult facet = facets.First();
        Assert.That(facet.FieldName, Is.EqualTo(FieldMultipleValues));

        DecimalExactFacetValue[] facetValues = facet.Values.OfType<DecimalExactFacetValue>().ToArray();
        Assert.That(facetValues, Has.Length.EqualTo(expectedFacetValues.Length));
        foreach (var expectedFacetValue in expectedFacetValues)
        {
            DecimalExactFacetValue? facetValue = facetValues.FirstOrDefault(f => f.Key == expectedFacetValue.Key);
            Assert.That(facetValue, Is.Not.Null);
            Assert.That(facetValue.Count, Is.EqualTo(expectedFacetValue.Count));
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanFacetDocumentsByDecimalRange(bool filtered)
    {
        SearchResult result = await SearchAsync(
            facets:
            [
                new DecimalRangeFacet(
                    FieldMultipleValues,
                    [
                        new DecimalRangeFacetRange("One", 1m, 25m),
                        new DecimalRangeFacetRange("Two", 25m, 50m),
                        new DecimalRangeFacetRange("Three", 50m, 75m),
                        new DecimalRangeFacetRange("Four", 75m, 100m)
                    ]
                )
            ],
            filters: filtered ? [new DecimalExactFilter(FieldMultipleValues, [1m, 2m, 3m], false)] : []
        );

        // expecting the same facets whether filtering is enabled or not, because
        // both faceting and filtering is applied to the same field
        var expectedFacetValues = Enumerable
            .Range(1, 100)
            .SelectMany(
                i => new[] { i, i * 1.5m }
                    .Select(
                        value => value switch
                        {
                            < 25m => "One",
                            < 50m => "Two",
                            < 75m => "Three",
                            < 100m => "Four",
                            _ => null
                        }
                    )
                    .WhereNotNull()
                    .Distinct()
            )
            .GroupBy(key => key)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .WhereNotNull()
            .ToArray();

        // expecting
        // - when filtered: 1, 2 and 3
        // - when not filtered: all of them
        Assert.That(result.Total, Is.EqualTo(filtered ? 3 : 100));

        FacetResult[] facets = result.Facets.ToArray();
        Assert.That(facets, Has.Length.EqualTo(1));

        FacetResult facet = facets.First();
        Assert.That(facet.FieldName, Is.EqualTo(FieldMultipleValues));

        DecimalRangeFacetValue[] facetValues = facet.Values.OfType<DecimalRangeFacetValue>().ToArray();
        Assert.That(facetValues, Has.Length.EqualTo(expectedFacetValues.Length));
        foreach (var expectedFacetValue in expectedFacetValues)
        {
            DecimalRangeFacetValue? facetValue = facetValues.FirstOrDefault(f => f.Key == expectedFacetValue.Key);
            Assert.That(facetValue, Is.Not.Null);
            Assert.That(facetValue.Count, Is.EqualTo(expectedFacetValue.Count));
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSortDocumentsByDecimal(bool ascending)
    {
        SearchResult result = await SearchAsync(
            sorters: [new DecimalSorter(FieldSingleValue, ascending ? Direction.Ascending : Direction.Descending)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(100));
                Assert.That(result.Documents.First().Id, Is.EqualTo(ascending ? _documentIds[1] : _documentIds[100]));
            }
        );
    }
}
