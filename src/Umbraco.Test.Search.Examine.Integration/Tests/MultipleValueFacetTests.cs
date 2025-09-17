using NUnit.Framework;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class MultipleValueFacetTests : SearcherTestBase
{
    [Test]
    [Ignore("Examine does not support multivalued facets, and thus does not index it.")]
    public async Task CanFilterSingleDocumentByIntegerRange()
    {
        SearchResult result = await SearchAsync(
            filters: [new IntegerRangeFilter(FieldMultipleValuesWithFacets, [new IntegerRangeFilterRange(1, 2)], false)]);

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(DocumentIds[1]));
            });
    }
}
