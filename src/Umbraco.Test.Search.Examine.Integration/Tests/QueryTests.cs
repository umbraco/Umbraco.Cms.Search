using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

// tests specifically related to free text querying
public class QueryTests : SearcherTestBase
{
    [TestCase(null)]
    [TestCase("R1")]
    [TestCase("R2")]
    [TestCase("R3")]
    public async Task CanQuerySingleDocument(string? relevanceLevel)
    {
        var query = $"texts{(relevanceLevel is not null ? $"_{relevanceLevel.ToLowerInvariant()}" : null)}_12";
        SearchResult result = await SearchAsync(
            query: query
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[12]));
            }
        );
    }

    [Test]
    public async Task CanQueryMultipleDocuments()
    {
        SearchResult result = await SearchAsync(
            query: "single1"
        );

        Assert.Multiple(
            () =>
            {
                // expected: 1, 10-19, 100
                Assert.That(result.Total, Is.EqualTo(12));

                var documents = result.Documents.ToList();
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EqualTo(
                        new[]
                        {
                            _documentIds[1],
                            _documentIds[10],
                            _documentIds[11],
                            _documentIds[12],
                            _documentIds[13],
                            _documentIds[14],
                            _documentIds[15],
                            _documentIds[16],
                            _documentIds[17],
                            _documentIds[18],
                            _documentIds[19],
                            _documentIds[100],
                        }
                    ).AsCollection
                );
            }
        );
    }

    [Test]
    public async Task CanQuerySingleDocumentByPhrase()
    {
        SearchResult result = await SearchAsync(
            query: "phrase search single12"
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[12]));
            }
        );
    }

    [Test]
    public async Task CanQuerySingleDocumentByPhraseInverted()
    {
        SearchResult result = await SearchAsync(
            query: "single12 search phrase"
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[12]));
            }
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanQueryMultipleDocumentsByCommonWord(bool even)
    {
        SearchResult result = await SearchAsync(
            query: even ? "even" : "odd"
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

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanQueryDocumentsByTextualRelevance(bool ascending)
    {
        SearchResult result = await SearchAsync(
            query: "special",
            sorters: [new ScoreSorter(ascending ? Direction.Ascending : Direction.Descending)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(4));

                Guid[] expectedDocumentIdsByOrderOfRelevance =
                [
                    _documentIds[30], // TextsR1
                    _documentIds[20], // TextsR2
                    _documentIds[40], // TextsR3
                    _documentIds[10] // Texts
                ];
                if (ascending)
                {
                    expectedDocumentIdsByOrderOfRelevance = expectedDocumentIdsByOrderOfRelevance.Reverse().ToArray();
                }

                Assert.That(
                    result.Documents.Select(d => d.Id),
                    Is.EqualTo(expectedDocumentIdsByOrderOfRelevance).AsCollection
                );
            }
        );
    }
}
