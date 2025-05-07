using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class VariantContentTreeTests : IndexTestBase
{
    private const string EnglishRootTitle = "Root title";
    private const string DanishRootTitle = "Rod titel";
    private const string JapaneseRootTitle = "ル-トタイタル";
    private const string EnglishChildTitle = "Child title";
    private const string DanishChildTitle = "Barn titel";
    private const string JapaneseChildTitle = "子供タイタル";
    private const string EnglishGrandChildTitle = "Grandchild title";
    private const string DanishGrandChildTitle = "Barnebarn titel";
    private const string JapaneseGrandChildTitle = "孫タイタル";
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDocumentTree(bool publish)
    {
        CreateVariantDocumentTree(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.Count(), Is.EqualTo(3));
    }
    
    [Ignore("Get in memory working with searching")]
    [Test]
    public void UnpublishingRootWillRemoveAncestors_Root_Unpublished()
    {
        CreateVariantDocumentTree(true);

        var root = ContentService.GetById(RootKey);
        ContentService.Unpublish(root);

        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRootEnglish = publishedIndex.Searcher.Search(EnglishRootTitle);
        var publishedResultsRootDanish = publishedIndex.Searcher.Search(DanishRootTitle);
        var publishedResultsRootJapanese = publishedIndex.Searcher.Search(JapaneseRootTitle);           
        var publishedResultsChildEnglish = publishedIndex.Searcher.Search(EnglishChildTitle);
        var publishedResultsChildDanish = publishedIndex.Searcher.Search(DanishChildTitle);
        var publishedResultsChildJapanese = publishedIndex.Searcher.Search(JapaneseChildTitle);        
        var publishedResultsGrandChildEnglish = publishedIndex.Searcher.Search(EnglishGrandChildTitle);
        var publishedResultsGrandChildDanish = publishedIndex.Searcher.Search(DanishGrandChildTitle);
        var publishedResultsGrandChildJapanese = publishedIndex.Searcher.Search(JapaneseGrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRootEnglish, Is.Empty);
            Assert.That(publishedResultsRootDanish, Is.Empty);
            Assert.That(publishedResultsRootJapanese, Is.Empty); 
            Assert.That(publishedResultsChildEnglish, Is.Empty);
            Assert.That(publishedResultsChildDanish, Is.Empty);
            Assert.That(publishedResultsChildJapanese, Is.Empty);         
            Assert.That(publishedResultsGrandChildEnglish, Is.Empty);
            Assert.That(publishedResultsGrandChildDanish, Is.Empty);
            Assert.That(publishedResultsGrandChildJapanese, Is.Empty);
        });

        AssertDraft();
    }
    
    [Ignore("Get in memory working with searching")]
    [Test]
    public void UnpublishingRootWillRemoveAncestors_Child_Unpublished()
    {
        CreateVariantDocumentTree(true);

        var child = ContentService.GetById(ChildKey);
        ContentService.Unpublish(child);

        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRootEnglish = publishedIndex.Searcher.Search(EnglishRootTitle);
        var publishedResultsRootDanish = publishedIndex.Searcher.Search(DanishRootTitle);
        var publishedResultsRootJapanese = publishedIndex.Searcher.Search(JapaneseRootTitle);           
        var publishedResultsChildEnglish = publishedIndex.Searcher.Search(EnglishChildTitle);
        var publishedResultsChildDanish = publishedIndex.Searcher.Search(DanishChildTitle);
        var publishedResultsChildJapanese = publishedIndex.Searcher.Search(JapaneseChildTitle);        
        var publishedResultsGrandChildEnglish = publishedIndex.Searcher.Search(EnglishGrandChildTitle);
        var publishedResultsGrandChildDanish = publishedIndex.Searcher.Search(DanishGrandChildTitle);
        var publishedResultsGrandChildJapanese = publishedIndex.Searcher.Search(JapaneseGrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRootEnglish, Is.Empty);
            Assert.That(publishedResultsRootDanish, Is.Empty);
            Assert.That(publishedResultsRootJapanese, Is.Empty); 
            Assert.That(publishedResultsChildEnglish, Is.Empty);
            Assert.That(publishedResultsChildDanish, Is.Empty);
            Assert.That(publishedResultsChildJapanese, Is.Empty);         
            Assert.That(publishedResultsGrandChildEnglish, Is.Empty);
            Assert.That(publishedResultsGrandChildDanish, Is.Empty);
            Assert.That(publishedResultsGrandChildJapanese, Is.Empty);
        });

        AssertDraft();
    }
    
    [Ignore("Get in memory working with searching")]
    [Test]
    public void UnpublishingRootWillRemoveAncestors_GrandChild_Unpublished()
    {
        CreateVariantDocumentTree(true);

        var grandChild = ContentService.GetById(GrandchildKey);
        ContentService.Unpublish(grandChild);

        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRootEnglish = publishedIndex.Searcher.Search(EnglishRootTitle);
        var publishedResultsRootDanish = publishedIndex.Searcher.Search(DanishRootTitle);
        var publishedResultsRootJapanese = publishedIndex.Searcher.Search(JapaneseRootTitle);           
        var publishedResultsChildEnglish = publishedIndex.Searcher.Search(EnglishChildTitle);
        var publishedResultsChildDanish = publishedIndex.Searcher.Search(DanishChildTitle);
        var publishedResultsChildJapanese = publishedIndex.Searcher.Search(JapaneseChildTitle);        
        var publishedResultsGrandChildEnglish = publishedIndex.Searcher.Search(EnglishGrandChildTitle);
        var publishedResultsGrandChildDanish = publishedIndex.Searcher.Search(DanishGrandChildTitle);
        var publishedResultsGrandChildJapanese = publishedIndex.Searcher.Search(JapaneseGrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRootEnglish, Is.Not.Empty);
            Assert.That(publishedResultsRootEnglish.First().Values.First(x => x.Key == "title_en-us").Value, Is.EqualTo(EnglishRootTitle));
            Assert.That(publishedResultsRootDanish, Is.Not.Empty);
            Assert.That(publishedResultsRootDanish.First().Values.First(x => x.Key == "title_da-dk").Value, Is.EqualTo(DanishRootTitle));
            Assert.That(publishedResultsRootJapanese, Is.Not.Empty);
            Assert.That(publishedResultsRootJapanese.First().Values.First(x => x.Key == "title_ja-jp").Value, Is.EqualTo(JapaneseRootTitle));
            Assert.That(publishedResultsChildEnglish, Is.Not.Empty);
            Assert.That(publishedResultsChildEnglish.First().Values.First(x => x.Key == "title_en-us").Value, Is.EqualTo(EnglishChildTitle));
            Assert.That(publishedResultsChildDanish, Is.Not.Empty);
            Assert.That(publishedResultsChildDanish.First().Values.First(x => x.Key == "title_da-dk").Value, Is.EqualTo(DanishChildTitle));
            Assert.That(publishedResultsChildJapanese, Is.Not.Empty); 
            Assert.That(publishedResultsChildJapanese.First().Values.First(x => x.Key == "title_ja-jp").Value, Is.EqualTo(JapaneseChildTitle));
            Assert.That(publishedResultsGrandChildEnglish, Is.Empty);
            Assert.That(publishedResultsGrandChildDanish, Is.Empty);
            Assert.That(publishedResultsGrandChildJapanese, Is.Empty);
        });

        AssertDraft();
    }
    
    private void AssertDraft()
    {
        var draftIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftContent);
        var draftResultsRootEnglish = draftIndex.Searcher.Search(EnglishRootTitle);
        var draftResultsRootDanish = draftIndex.Searcher.Search(DanishRootTitle);
        var draftResultsRootJapanese = draftIndex.Searcher.Search(JapaneseRootTitle);
        var draftResultsChildEnglish = draftIndex.Searcher.Search(EnglishChildTitle);
        var draftResultsChildDanish = draftIndex.Searcher.Search(DanishChildTitle);
        var draftResultsChildJapanese = draftIndex.Searcher.Search(JapaneseChildTitle);
        var draftResultsGrandChildEnglish = draftIndex.Searcher.Search(EnglishGrandChildTitle);
        var draftResultsGrandChildDanish = draftIndex.Searcher.Search(DanishGrandChildTitle);
        var draftResultsGrandChildJapanese = draftIndex.Searcher.Search(JapaneseGrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(draftResultsRootEnglish, Is.Not.Empty);
            Assert.That(draftResultsRootEnglish.First().Values.First(x => x.Key == "title_en-us").Value, Is.EqualTo(EnglishRootTitle));             
            Assert.That(draftResultsRootDanish, Is.Not.Empty);
            Assert.That(draftResultsRootDanish.First().Values.First(x => x.Key == "title_da-dk").Value, Is.EqualTo(DanishRootTitle));                
            Assert.That(draftResultsRootJapanese, Is.Not.Empty);
            Assert.That(draftResultsRootJapanese.First().Values.First(x => x.Key == "title_ja-jp").Value, Is.EqualTo(JapaneseRootTitle)
            );                   
            Assert.That(draftResultsChildEnglish, Is.Not.Empty);
            Assert.That(draftResultsChildEnglish.First().Values.First(x => x.Key == "title_en-us").Value, Is.EqualTo(EnglishChildTitle));             
            Assert.That(draftResultsChildDanish, Is.Not.Empty);
            Assert.That(draftResultsChildDanish.First().Values.First(x => x.Key == "title_da-dk").Value, Is.EqualTo(DanishChildTitle));                
            Assert.That(draftResultsChildJapanese, Is.Not.Empty);
            Assert.That(draftResultsChildJapanese.First().Values.First(x => x.Key == "title_ja-jp").Value, Is.EqualTo(JapaneseChildTitle));                
            
            Assert.That(draftResultsGrandChildEnglish, Is.Not.Empty);
            Assert.That(draftResultsGrandChildEnglish.First().Values.First(x => x.Key == "title_en-us").Value, Is.EqualTo(EnglishGrandChildTitle));             
            Assert.That(draftResultsGrandChildDanish, Is.Not.Empty);
            Assert.That(draftResultsGrandChildDanish.First().Values.First(x => x.Key == "title_da-dk").Value, Is.EqualTo(DanishGrandChildTitle));                
            Assert.That(draftResultsGrandChildJapanese, Is.Not.Empty);
            Assert.That(draftResultsGrandChildJapanese.First().Values.First(x => x.Key == "title_ja-jp").Value, Is.EqualTo(JapaneseGrandChildTitle));            
        });
    }
    
    private void CreateVariantDocumentTree(bool publish = false)
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
        contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Key, 0, contentType.Alias)];
        ContentTypeService.Save(contentType);

        var root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Root")
            .WithCultureName("da-DK", "Rod")
            .WithCultureName("ja-JP", "ル-ト")
            .Build();
        
        root.SetValue("title", EnglishRootTitle, "en-US");
        root.SetValue("title", DanishRootTitle, "da-DK");
        root.SetValue("title", JapaneseRootTitle, "ja-JP");
        
        root.SetValue("body", "root-body-segment-1", "en-US", "segment-1");
        root.SetValue("body", "root-body-segment-2", "en-US", "segment-2");
        root.SetValue("body", "rod-krop-segment-1", "da-DK", "segment-1");
        root.SetValue("body", "rod-krop-segment-2", "da-DK", "segment-2");
        root.SetValue("body", "ル-ト-ボディ-segment-1", "ja-JP", "segment-1");
        root.SetValue("body", "ル-ト-ボディ-segment-2", "ja-JP", "segment-2");

        SaveOrPublish(root, publish);
        
        var child = new ContentBuilder()
            .WithKey(ChildKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Child")
            .WithCultureName("da-DK", "Barn")
            .WithCultureName("ja-JP", "子供")
            .WithParent(root)
            .Build();
        
        child.SetValue("title", EnglishChildTitle, "en-US");
        child.SetValue("title", DanishChildTitle, "da-DK");
        child.SetValue("title", JapaneseChildTitle, "ja-JP");
        
        child.SetValue("body", "child-body-segment-1", "en-US", "segment-1");
        child.SetValue("body", "child-body-segment-2", "en-US", "segment-2");
        child.SetValue("body", "barn-krop-segment-1", "da-DK", "segment-1");
        child.SetValue("body", "barn-krop-segment-2", "da-DK", "segment-2");
        child.SetValue("body", "子供-ボディ-segment-1", "ja-JP", "segment-1");
        child.SetValue("body", "子供-ボディ-segment-2", "ja-JP", "segment-2");
        
        SaveOrPublish(child, publish);

        var grandchild = new ContentBuilder()
            .WithKey(GrandchildKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Grandchild")
            .WithCultureName("da-DK", "Barn")
            .WithCultureName("ja-JP", "孫")
            .WithParent(child)
            .Build();
        
        grandchild.SetValue("title", EnglishGrandChildTitle, "en-US");
        grandchild.SetValue("title", DanishGrandChildTitle, "da-DK");
        grandchild.SetValue("title", JapaneseGrandChildTitle, "ja-JP");
        
        grandchild.SetValue("body", "grandchild-body-segment-1", "en-US", "segment-1");
        grandchild.SetValue("body", "grandchild-body-segment-2", "en-US", "segment-2");
        grandchild.SetValue("body", "barnebarn-krop-segment-1", "da-DK", "segment-1");
        grandchild.SetValue("body", "barnebarn-krop-segment-2", "da-DK", "segment-2");
        grandchild.SetValue("body", "孫-ボディ-segment-1", "ja-JP", "segment-1");
        grandchild.SetValue("body", "孫-ボディ-segment-2", "ja-JP", "segment-2");
        
        SaveOrPublish(grandchild, publish);
    }
    
}