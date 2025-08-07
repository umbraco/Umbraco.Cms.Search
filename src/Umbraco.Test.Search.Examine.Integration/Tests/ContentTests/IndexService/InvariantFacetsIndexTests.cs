using Examine;
using Examine.Lucene;
using Examine.Search;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

public class InvariantFacetsIndexTests : IndexTestBase
{
    private IContentType ContentType { get; set; }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetOneIntFacet(bool publish)
    {
        await CreateCountDocuments([1, 2]);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery()
            .All()
            .WithFacets(facets => facets.FacetLongRange("Umb_otherName_integers", new Int64Range("0-9", 0, true, 9, true)))
            .Execute();

        var facets = results.GetFacets();
        var facet = facets.First().Facet("0-9");
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(facet.Value, Is.EqualTo(2));
        });
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetOneDecimalFacet(bool publish)
    {
        await CreateDecimalDocuments([3.6, 600.4]);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery()
            .All()
            .WithFacets(facets => facets.FacetDoubleRange("Umb_decimalproperty_decimals", new DoubleRange("values", 3.5, true, 654.9, true)))
            .Execute();

        var facets = results.GetFacets();
        var facet = facets.First().Facet("values");
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(facet.Value, Is.EqualTo(2));
        });
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetOneTextFacet(bool publish)
    {
        await CreateTitleDocuments(["Title", "Title", "Another"]);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery()
            .All()
            .WithFacets(facets => facets.FacetString("Umb_title_texts"))
            .Execute();

        var facets = results.GetFacets();
        var facet = facets.First().Facet("Title");
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(facet.Value, Is.EqualTo(2));
        });
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetMultipleIntFacets(bool publish)
    {
        await CreateCountDocuments([1, 2, 99, 101, 170]);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery()
            .All()
            .WithFacets(facets => facets.FacetLongRange("Umb_otherName_integers", new Int64Range("0-9", 0, true, 9, true),  new Int64Range("100-199", 100, true, 199, true)))
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
    
    private async Task CreateTitleDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("title")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }
    
    private async Task CreateTitleDocuments(string[] values)
    {
        await CreateTitleDocType();

        foreach (var stringValue in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{stringValue}")
                .WithPropertyValues(
                    new
                    {
                        title = stringValue
                    })
                .Build();
            
            SaveAndPublish(document);
        }
    }
    
    private async Task CreateDecimalDocType()
    {
        var dataType = new DataTypeBuilder()
            .WithId(0)
            .WithoutIdentity()
            .WithDatabaseType(ValueStorageType.Decimal)
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        
        DataTypeService.Save(dataType);
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("decimalproperty")
            .WithDataTypeId(dataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }
    
    private async Task CreateDecimalDocuments(double[] values)
    {
        await CreateDecimalDocType();

        foreach (var doubleValue in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{doubleValue.ToString()}")
                .WithPropertyValues(
                    new
                    {
                        decimalproperty = doubleValue
                    })
                .Build();
            
            SaveAndPublish(document);
        }
    }

    private async Task CreateCountDocuments(int[] values)
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
            
            SaveAndPublish(document);
        }
    }
}