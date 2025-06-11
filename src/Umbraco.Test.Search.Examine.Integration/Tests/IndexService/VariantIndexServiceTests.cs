using Examine;
using Examine.Search;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Test.Search.Examine.Integration.Tests.IndexService;

namespace Umbraco.Test.Search.Examine.Integration.Tests.IndexService;

public class VariantIndexServiceTests : IndexTestBase
{
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexAnyDocument(bool publish)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Not.Empty);
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanRemoveAnyDocument(bool publish)
    {
        var content = ContentService.GetById(RootKey);
        ContentService.Delete(content);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Empty);
    }
    
    [TestCase(true, "en-US", "Name")]
    [TestCase(false, "en-US", "Name")]
    [TestCase(true, "da-DK", "Navn")]
    [TestCase(false, "da-DK", "Navn")]
    [TestCase(true, "ja-JP", "名前")]
    [TestCase(false, "ja-JP", "名前")]
    public void CanIndexVariantName(bool publish, string culture, string expectedValue)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("Umb_Name_texts");
        var results = queryBuilder.Execute();
        var result = results
            .SelectMany(x => x.Values.Values)
            .First(x => x == expectedValue);
        Assert.That(results, Is.Not.Empty);
        Assert.That(result, Is.EqualTo(expectedValue));
    }
    
    [TestCase("title", "updatedTitle", "en-US")]
    [TestCase("title", "updatedTitle", "da-DK")]
    [TestCase("title", "updatedTitle", "ja-JP")]
    public void CanIndexUpdatedProperties(string propertyName, string updatedValue, string culture)
    {
        UpdateProperty(propertyName, updatedValue, culture);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        
        var results = index.Searcher.Search(updatedValue);
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == $"{propertyName}_texts").Value, Is.EqualTo(updatedValue));
    }
    
    [TestCase(true, "en-US", "Root")]
    [TestCase(false, "en-US", "Root")]
    [TestCase(true, "da-DK", "Rod")]
    [TestCase(false, "da-DK", "Rod")]
    [TestCase(true, "ja-JP", "ル-ト")]
    [TestCase(false, "ja-JP", "ル-ト")]
    public void CanIndexVariantTextByCulture(bool publish, string culture, string expectedValue)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("title_texts");
        var results = queryBuilder.Execute();
        
        var result = results
            .SelectMany(x => x.Values.Values)
            .First(x => x == expectedValue);
        Assert.That(results, Is.Not.Empty);
        Assert.That(result, Is.EqualTo(expectedValue));
    }
    
    [TestCase(true, "en-US", "segment-1", "body-segment-1")]
    [TestCase(false, "en-US", "segment-2", "body-segment-2")]
    [TestCase(true, "da-DK","segment-1", "krop-segment-1")]
    [TestCase(false, "da-DK","segment-2", "krop-segment-2")]
    [TestCase(true, "ja-JP", "segment-1", "ボディ-segment-1")]
    [TestCase(false, "ja-JP", "segment-2", "ボディ-segment-2")]
    public void CanIndexVariantTextBySegment(bool publish, string culture, string segment, string expectedValue)
    {
        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var results = index.Searcher.Search(expectedValue);
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == "body_texts").Value, Is.EqualTo(expectedValue));
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