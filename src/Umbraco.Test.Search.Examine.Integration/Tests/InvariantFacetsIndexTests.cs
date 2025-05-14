using Examine;
using Examine.Lucene;
using Examine.Search;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class InvariantFacetsIndexTests : IndexTestBase
{
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanIndexAnyDocument(bool publish)
    {
        await CreateFacetableDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery()
            .All()
            .WithFacets(facets => facets.FacetLongRange("count_integers", new Int64Range("0-9", 0, true, 9, true)))
            .Execute();

        var facets = results.GetFacets();
        var facet = facets.First().Facet("0-9");
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(facet.Value, Is.EqualTo(2));
        });
    }
    
    
       private async Task CreateFacetableDocument(bool publish = false)
    {
        var contentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("count")
            .WithDataTypeId(-51)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);

        var root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithName("Root")
            .WithPropertyValues(
                new
                {
                    count = 1,
                })
            .Build();

        SaveOrPublish(root, publish);
        
        var anotherRoot = new ContentBuilder()
            .WithContentType(contentType)
            .WithName("AnotherRoot")
            .WithPropertyValues(
                new
                {
                    count = 2,
                })
            .Build();

        SaveOrPublish(anotherRoot, publish);
        
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);
    }
}