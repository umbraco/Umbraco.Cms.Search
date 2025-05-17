using Examine;
using Examine.Lucene;
using Examine.Search;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class InvariantFacetsIndexTests : IndexTestBase
{
    private IContentType ContentType { get; set; }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetOneFacet(bool publish)
    {
        await CreateCountDocuments([1, 2], publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery()
            .All()
            .WithFacets(facets => facets.FacetLongRange("otherName_integers", new Int64Range("0-9", 0, true, 9, true)))
            .Execute();

        var facets = results.GetFacets();
        var facet = facets.First().Facet("0-9");
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(facet.Value, Is.EqualTo(2));
        });
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetMultipleFacets(bool publish)
    {
        await CreateCountDocuments([1, 2, 99, 101, 170], publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery()
            .All()
            .WithFacets(facets => facets.FacetLongRange("otherName_integers", new Int64Range("0-9", 0, true, 9, true),  new Int64Range("100-199", 100, true, 199, true)))
            .Execute();

        var facets = results.GetFacets();
        var firstFacet = facets.First().Facet("0-9");
        var secondFacet = facets.First().Facet("100-199");
        Assert.Multiple(() =>
        {
            Assert.That(firstFacet.Value, Is.EqualTo(2));
            Assert.That(secondFacet.Value, Is.EqualTo(2));
        });
    }


    private async Task CreateCountDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("otherName")
            .WithDataTypeId(-51)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }

    private async Task CreateCountDocuments(int[] values, bool publish)
    {
        await CreateCountDocType();

        foreach (var countValue in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{countValue}")
                .WithPropertyValues(
                    new
                    {
                        otherName = countValue,
                    })
                .Build();
            
            SaveOrPublish(document, publish);
        }


    }
}