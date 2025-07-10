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
    public async Task CanGetOneIntFacet(bool publish)
    {
        await CreateCountDocuments([1, 2]);
        
        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(indexAlias, null, null, new List<Facet>(){ new IntegerRangeFacet("otherName", new []{ new IntegerRangeFacetRange("Below 100", 0, 100)})}, null, null, null, null, 0, 100);
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