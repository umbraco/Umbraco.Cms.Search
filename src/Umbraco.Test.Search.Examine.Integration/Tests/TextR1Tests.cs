using NUnit.Framework;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

// tests specifically related to the IndexValue.TextsR1 collection
// - note that these tests are not exhaustive - see more test cases for IndexValue.Texts
public class TextR1Tests : SearcherTestBase
{
    [Test]
    public async Task CanFilterSingleDocumentBySpecificTextR1()
    {
        SearchResult result = await SearchAsync(
            filters: [new TextFilter(FieldTextRelevance, ["texts_r1_22"], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[22]));
            }
        );
    }

    [Test]
    public async Task CanFilterMultipleDocumentsBySpecificTextR1()
    {
        SearchResult result = await SearchAsync(
            filters: [new TextFilter(FieldTextRelevance, ["texts_r1_21", "texts_r1_22", "texts_r1_23"], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(3));

                var documents = result.Documents.ToList();
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EqualTo(new[] { _documentIds[21], _documentIds[22], _documentIds[23] }).AsCollection
                );
            }
        );
    }

    [Test]
    public async Task CanFilterDocumentsBySpecificTextR1Negated()
    {
        SearchResult result = await SearchAsync(
            filters: [new TextFilter(FieldTextRelevance, ["texts_r1_22"], true)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(99));
                Assert.That(
                    result.Documents.Select(d => d.Id),
                    Is.EqualTo(_documentIds.Values.Except([_documentIds[22]])).AsCollection
                );
            }
        );
    }
}
