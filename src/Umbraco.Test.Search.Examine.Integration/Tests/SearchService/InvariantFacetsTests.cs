using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public class InvariantFacetsTests : SearcherTestBase
{
    private IContentType ContentType { get; set; }
    
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchOneIntegerRangeFacet(bool publish)
    {
        await CreateCountDocuments([1, 2, 101]);
        
        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(indexAlias, null, null, new List<Facet>(){ new IntegerRangeFacet("count", new []{ new IntegerRangeFacetRange("Below 100", 0, 100)})}, null, null, null, null, 0, 100);
        Assert.Multiple(() =>
        {
            Assert.That(result.Facets, Is.Not.Empty);
            Assert.That(result.Facets.First().Values.First().Count, Is.EqualTo(2));
        });
    }    
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchIntegerExactFacet(bool publish)
    {
        await CreateCountDocuments([1, 1, 2]);
        
        var indexAlias = GetIndexAlias(publish);
        var facets = (await Searcher.SearchAsync(indexAlias, null, null, new List<Facet>(){ new IntegerExactFacet("count")}, null, null, null, null, 0, 100)).Facets;
        var exactFacetValues = facets.Select(x => (IntegerExactFacetValue)x.Values.First()).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(exactFacetValues[0].Key, Is.EqualTo(1));
            Assert.That(exactFacetValues[0].Count, Is.EqualTo(2));
            Assert.That(exactFacetValues[1].Key, Is.EqualTo(2));
            Assert.That(exactFacetValues[1].Count, Is.EqualTo(1));
        });
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchOneDecimalRangeFacet(bool publish)
    {
        await CreateDecimalDocuments([1.5, 2.5, 100.5]);
        
        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(indexAlias, null, null, new List<Facet>(){ new DecimalRangeFacet("decimalproperty", new []{ new DecimalRangeFacetRange("Below 100", 0, 100)})}, null, null, null, null, 0, 100);
        Assert.Multiple(() =>
        {
            Assert.That(result.Facets, Is.Not.Empty);
            Assert.That(result.Facets.First().Values.First().Count, Is.EqualTo(2));
        });
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchDecimalExactFacet(bool publish)
    {
        await CreateDecimalDocuments([1.55, 1.55, 1.55]);
        
        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(indexAlias, null, null, new List<Facet>(){ new DecimalExactFacet("decimalproperty")}, null, null, null, null, 0, 100);
        Assert.Multiple(() =>
        {
            Assert.That(result.Facets, Is.Not.Empty);
            Assert.That(result.Facets.First().Values.First().Count, Is.EqualTo(3));
        });
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchKeywordFacet(bool publish)
    {
        await CreateTitleDocuments(["one", "one", "two"]);
        
        var indexAlias = GetIndexAlias(publish);
        var facets = (await Searcher.SearchAsync(indexAlias, null, null, new List<Facet> { new KeywordFacet("title")}, null, null, null, null, 0, 100)).Facets;
        var keyWordFacets = facets.Select(x => (KeywordFacetValue)x.Values.First()).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(keyWordFacets[0].Key, Is.EqualTo("one"));
            Assert.That(keyWordFacets[0].Count, Is.EqualTo(2));
            Assert.That(keyWordFacets[1].Key, Is.EqualTo("two"));
            Assert.That(keyWordFacets[1].Count, Is.EqualTo(1));
        });
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchDatetimeRangeFacet(bool publish)
    {
        await CreateDatetimeDocuments([new DateTime(2025, 06, 06), new DateTime(2025, 02, 01), new DateTime(2024, 01, 01)]);
        
        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(indexAlias, null, null, new List<Facet>(){ new DateTimeOffsetRangeFacet("datetime", new []{ new DateTimeOffsetRangeFacetRange("Below 100", new DateTime(2025, 01, 01), null)})}, null, null, null, null, 0, 100);
        Assert.Multiple(() =>
        {
            Assert.That(result.Facets, Is.Not.Empty);
            Assert.That(result.Facets.First().Values.First().Count, Is.EqualTo(2));
        });
    }
    
        private async Task CreateCountDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("count")
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
    
    private async Task CreateDatetimeDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("datetime")
            .WithDataTypeId(Constants.DataTypes.DateTime)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DateTime)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }
    
    private async Task CreateDatetimeDocuments(DateTimeOffset[] values)
    {
        await CreateDatetimeDocType();

        foreach (var dateTimeOffset in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{dateTimeOffset.ToString()}")
                .WithPropertyValues(
                    new
                    {
                        datetime = dateTimeOffset
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
                        count = countValue,
                    })
                .Build();
            
            SaveAndPublish(document);
        }
    }
}