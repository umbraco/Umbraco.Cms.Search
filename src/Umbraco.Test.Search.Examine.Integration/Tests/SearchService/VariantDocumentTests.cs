using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public class VariantDocumentTests : SearcherTestBase
{
    
    [TestCase(true, "en-US", "Name")]
    [TestCase(false, "en-US", "Name")]
    [TestCase(true, "da-DK", "Navn")]
    [TestCase(false, "da-DK", "Navn")]
    // The japanese characters aren't searchable in examine for now
    [TestCase(true, "ja-JP", "名前")]
    [TestCase(false, "ja-JP", "名前")]
    public async Task CanSearchVariantName(bool publish, string culture, string expectedValue)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, expectedValue, null, null, null, culture, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }
    
    [TestCase(true, "en-US", "Name")]
    [TestCase(false, "en-US", "Name")]
    [TestCase(true, "da-DK", "Navn")]
    [TestCase(false, "da-DK", "Navn")]
    [TestCase(true, "ja-JP", "名前")]
    [TestCase(false, "ja-JP", "名前")]
    public async Task CanNotSearchVariantNameWithInvariantCulture(bool publish, string culture, string expectedValue)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, expectedValue, null, null, null, null, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(0));
    }
    
    [TestCase("title", "updatedTitle", "en-US")]
    [TestCase("title", "updatedTitle", "da-DK")]
    [TestCase("title", "updatedTitle", "ja-JP")]
    public async Task CanSearchUpdatedProperties(string propertyName, string updatedValue, string culture)
    {
        UpdateProperty(propertyName, updatedValue, culture);
        
        var indexAlias = GetIndexAlias(true);

        var results = await Searcher.SearchAsync(indexAlias, updatedValue, null, null, null, culture, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }
    
    [TestCase(true, "en-US", "Root")]
    [TestCase(false, "en-US", "Root")]
    [TestCase(true, "da-DK", "Rod")]
    [TestCase(false, "da-DK", "Rod")]
    // [TestCase(true, "ja-JP", "ル-ト")]
    // [TestCase(false, "ja-JP", "ル-ト")]
    public async Task CanSearchVariantTextByCulture(bool publish, string culture, string expectedValue)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, expectedValue, null, null, null, culture, null, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }
    
    [TestCase(true, "en-US", "segment-1", "body-segment-1")]
    [TestCase(false, "en-US", "segment-2", "body-segment-2")]
    [TestCase(true, "da-DK","segment-1", "krop-segment-1")]
    [TestCase(false, "da-DK","segment-2", "krop-segment-2")]
    [TestCase(true, "ja-JP", "segment-1", "ボディ-segment-1")]
    [TestCase(false, "ja-JP", "segment-2", "ボディ-segment-2")]
    public async Task CanSearchVariantTextBySegment(bool publish, string culture, string segment, string expectedValue)
    {
        var indexAlias = GetIndexAlias(publish);

        var results = await Searcher.SearchAsync(indexAlias, expectedValue, null, null, null, culture, segment, null, 0, 100);
        Assert.That(results.Total, Is.EqualTo(1));
        Assert.That(results.Documents.First().Id, Is.EqualTo(RootKey));
    }
    
    [SetUp]
    public void CreateVariantDocument()
    {
        
        var langDk = new LanguageBuilder()
            .WithCultureInfo("da-DK")
            .WithIsDefault(true)
            .Build();
        var langJp = new LanguageBuilder()
            .WithCultureInfo("ja-JP")
            .Build();

        LocalizationService.Save(langDk);
        LocalizationService.Save(langJp);

        var contentType = new ContentTypeBuilder()
            .WithAlias("variant")
            .WithContentVariation(ContentVariation.CultureAndSegment)
            .AddPropertyType()
            .WithAlias("title")
            .WithVariations(ContentVariation.Culture)
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .AddPropertyType()
            .WithAlias("body")
            .WithVariations(ContentVariation.CultureAndSegment)
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Build();
        ContentTypeService.Save(contentType);
        
        var root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Name")
            .WithCultureName("da-DK", "Navn")
            .WithCultureName("ja-JP", "名前")
            .Build();
        
        root.SetValue("title", "Root", "en-US");
        root.SetValue("title", "Rod", "da-DK");
        root.SetValue("title", "ル-ト", "ja-JP");
        
        root.SetValue("body", "body-segment-1", "en-US", "segment-1");
        root.SetValue("body", "body-segment-2", "en-US", "segment-2");
        root.SetValue("body", "krop-segment-1", "da-DK", "segment-1");
        root.SetValue("body", "krop-segment-2", "da-DK", "segment-2");
        root.SetValue("body", "ボディ-segment-1", "ja-JP", "segment-1");
        root.SetValue("body", "ボディ-segment-2", "ja-JP", "segment-2");

        ContentService.Save(root);
        ContentService.Publish(root, new []{ "*"});
        Thread.Sleep(3000);
        
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);
    }
    
    
    private void UpdateProperty(string propertyName, object value, string culture)
    {
        var content = ContentService.GetById(RootKey);
        content.SetValue(propertyName, value, culture);

        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);
        Thread.Sleep(3000);
    }
}