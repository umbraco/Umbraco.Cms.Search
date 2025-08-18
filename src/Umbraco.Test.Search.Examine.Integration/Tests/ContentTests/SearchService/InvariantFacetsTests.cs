using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Provider.Examine.Extensions;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

public class InvariantFacetsTests : SearcherTestBase
{
    private IContentType ContentType { get; set; } = null!;


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
        var firstFacetValues = (IntegerExactFacetValue) facets.First().Values.First();
        var secondFacetValues = (IntegerExactFacetValue) facets.First().Values.Last();
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(firstFacetValues.Key, Is.EqualTo(1));
            Assert.That(firstFacetValues.Count, Is.EqualTo(2));
            Assert.That(secondFacetValues.Key, Is.EqualTo(2));
            Assert.That(secondFacetValues.Count, Is.EqualTo(1));
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
        await CreateDecimalDocuments([1.55, 1.55, 1.56]);

        var indexAlias = GetIndexAlias(publish);
        var facets = (await Searcher.SearchAsync(indexAlias, null, null, new List<Facet>(){ new DecimalExactFacet("decimalproperty")}, null, null, null, null, 0, 100)).Facets;
        var firstFacetValues = (DecimalExactFacetValue) facets.First().Values.First();
        var secondFacetValues = (DecimalExactFacetValue) facets.First().Values.Last();
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(firstFacetValues.Key, Is.EqualTo(1.55));
            Assert.That(firstFacetValues.Count, Is.EqualTo(2));
            Assert.That(secondFacetValues.Key, Is.EqualTo(1.56));
            Assert.That(secondFacetValues.Count, Is.EqualTo(1));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchKeywordFacet(bool publish)
    {
        await CreateDropDownDocuments(["one", "one", "two"]);

        var indexAlias = GetIndexAlias(publish);
        var facets = (await Searcher.SearchAsync(indexAlias, null, null, new List<Facet> { new KeywordFacet("dropDown")}, null, null, null, null, 0, 100)).Facets;
        var firstFacetValues = (KeywordFacetValue) facets.First().Values.First();
        var secondFacetValues = (KeywordFacetValue) facets.First().Values.Last();
        Assert.Multiple(() =>
        {
            Assert.That(facets, Is.Not.Empty);
            Assert.That(firstFacetValues.Key, Is.EqualTo("one"));
            Assert.That(firstFacetValues.Count, Is.EqualTo(2));
            Assert.That(secondFacetValues.Key, Is.EqualTo("two"));
            Assert.That(secondFacetValues.Count, Is.EqualTo(1));
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

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSearchDatetimeExactFacet(bool publish)
    {
        var firstDateTime = new DateTime(2025, 06, 06);
        var secondDateTime = new DateTime(2025, 06, 06);
        var thirdDateTime = new DateTime(2025, 06, 01);
        await CreateDatetimeDocuments([firstDateTime, secondDateTime, thirdDateTime]);

        var indexAlias = GetIndexAlias(publish);
        var result = await Searcher.SearchAsync(indexAlias, null, null, new List<Facet>(){ new DateTimeOffsetExactFacet("datetime")}, null, null, null, null, 0, 100);
        var firstFacetValues = (DateTimeOffsetExactFacetValue) result.Facets.First().Values.First();
        var secondFacetValues = (DateTimeOffsetExactFacetValue) result.Facets.First().Values.Last();
        Assert.Multiple(() =>
        {
            Assert.That(result.Facets, Is.Not.Empty);
            Assert.That(firstFacetValues.Count, Is.EqualTo(1));
            Assert.That(firstFacetValues.Key.DateTime, Is.EqualTo(thirdDateTime));
            Assert.That(secondFacetValues.Count, Is.EqualTo(2));
            Assert.That(secondFacetValues.Key.DateTime, Is.EqualTo(firstDateTime));
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

    private async Task CreateDropDownDocType()
    {
        ContentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("dropDown")
            .WithDataTypeId(Constants.DataTypes.DropDownSingle)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DropDownListFlexible)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(ContentType, Constants.Security.SuperUserKey);
    }

    private async Task CreateDropDownDocuments(string[] values)
    {
        await CreateDropDownDocType();

        foreach (var stringValue in values)
        {
            var document = new ContentBuilder()
                .WithContentType(ContentType)
                .WithName($"document-{stringValue}")
                .WithPropertyValues(
                    new
                    {
                        dropDown = $"[\"{stringValue}\"]"
                    })
                .Build();

            SaveAndPublish(document);
        }
    }
}
