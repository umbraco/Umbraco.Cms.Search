using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Provider.Examine.Extensions;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

public class VariantDocumentTests : IndexTestBase
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
        var content = ContentService.GetById(RootKey)!;
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
        queryBuilder.SelectField("Umb_Name_textsr1");
        var results = queryBuilder.Execute();
        var result = results
            .SelectMany(x => x.Values.Values)
            .First(x => x == expectedValue);
        Assert.That(results, Is.Not.Empty);
        Assert.That(result, Is.EqualTo(expectedValue));
    }

    [TestCase("Umb_invarianttitle_texts", "Invariant", "en-US")]
    [TestCase("Umb_invarianttitle_texts", "Invariant", "da-DK")]
    [TestCase("Umb_invarianttitle_texts", "Invariant", "ja-JP")]
    [TestCase("Umb_invariantcount_integers", 12, "en-US")]
    [TestCase("Umb_invariantcount_integers", 12, "da-DK")]
    [TestCase("Umb_invariantcount_integers", 12, "ja-JP")]
    [TestCase("Umb_invariantdecimalproperty_decimals", 12.4552, "en-US")]
    [TestCase("Umb_invariantdecimalproperty_decimals", 12.4552, "da-DK")]
    [TestCase("Umb_invariantdecimalproperty_decimals", 12.4552, "ja-JP")]
    public void CanIndexInvariantProperty(string field, object value, string culture)
    {
        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField(field);
        var results = queryBuilder.Execute();
        var result = results
            .SelectMany(x => x.Values.Values)
            .First(x => x == value.ToString());
        Assert.That(results, Is.Not.Empty);
        Assert.That(result, Is.EqualTo(value.ToString()));
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
        Assert.That(results.First().Values.First(x => x.Key == $"Umb_{propertyName}_texts").Value, Is.EqualTo(updatedValue));
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
        queryBuilder.SelectField("Umb_title_texts");
        var results = queryBuilder.Execute();

        var result = results
            .SelectMany(x => x.Values.Values)
            .First(x => x == expectedValue.TransformDashes());
        Assert.That(results, Is.Not.Empty);
        Assert.That(result, Is.EqualTo(expectedValue.TransformDashes()));
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
        Assert.That(results.First().Values.First(x => x.Key == "Umb_body_texts").Value, Is.EqualTo(expectedValue.TransformDashes()));
    }

    [SetUp]
    public void CreateVariantDocument()
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
            .WithAlias("invarianttitle")
            .WithVariations(ContentVariation.Nothing)
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .AddPropertyType()
            .WithAlias("invariantcount")
            .WithVariations(ContentVariation.Nothing)
            .WithDataTypeId(-51)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .AddPropertyType()
            .WithAlias("invariantdecimalproperty")
            .WithVariations(ContentVariation.Nothing)
            .WithDataTypeId(dataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Decimal)
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

        root.SetValue("invarianttitle", "Invariant");
        root.SetValue("invariantcount", 12);
        root.SetValue("invariantdecimalproperty", 12.4552);
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
        var content = ContentService.GetById(RootKey)!;
        content.SetValue(propertyName, value, culture);

        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);
        Thread.Sleep(3000);
    }
}
