using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

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
    public void VariantStructure_YieldsAllDocuments()
    {
        PublishEntireStructure();
        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        // 3 roots with 3 children with 3 grandchildren
        Assert.That(results.Count(), Is.EqualTo(3 * 3 * 3));
    }

    [Test]
    public void VariantStructure_WithRootUnpublished_YieldsNoDocuments()
    {
        PublishEntireStructure();
        var root = ContentService.GetById(RootKey)!;
        ContentService.Unpublish(root);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

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
    }

    [Test]
    public void VariantStructure_WithChildUnpublished_YieldsNoDocumentsBelowRoot()
    {
        PublishEntireStructure();
        var child = ContentService.GetById(ChildKey)!;
        ContentService.Unpublish(child);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

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
            Assert.That(publishedResultsRootDanish, Is.Not.Empty);
            Assert.That(publishedResultsRootJapanese, Is.Not.Empty);
            Assert.That(publishedResultsChildEnglish, Is.Empty);
            Assert.That(publishedResultsChildDanish, Is.Empty);
            Assert.That(publishedResultsChildJapanese, Is.Empty);
            Assert.That(publishedResultsGrandChildEnglish, Is.Empty);
            Assert.That(publishedResultsGrandChildDanish, Is.Empty);
            Assert.That(publishedResultsGrandChildJapanese, Is.Empty);
        });
    }

    [Test]
    public void VariantStructure_WithGrandChildUnpublished_YieldsNoDocumentsBelowChild()
    {
        PublishEntireStructure();
        var grandChild = ContentService.GetById(GrandchildKey)!;
        ContentService.Unpublish(grandChild);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

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
            Assert.That(publishedResultsRootDanish, Is.Not.Empty);
            Assert.That(publishedResultsRootJapanese, Is.Not.Empty);
            Assert.That(publishedResultsChildEnglish, Is.Not.Empty);
            Assert.That(publishedResultsChildDanish, Is.Not.Empty);
            Assert.That(publishedResultsChildJapanese, Is.Not.Empty);
            Assert.That(publishedResultsGrandChildEnglish, Is.Empty);
            Assert.That(publishedResultsGrandChildDanish, Is.Empty);
            Assert.That(publishedResultsGrandChildJapanese, Is.Empty);
        });
    }

    [TestCase("en-US")]
    [TestCase("da-DK")]
    [TestCase("ja-JP")]
    public void PublishedStructureSingleCulture_YieldsAllPublishedDocumentsInOneCultures(string culture)
    {
        var root = ContentService.GetById(RootKey)!;
        ContentService.PublishBranch(root, PublishBranchFilter.IncludeUnpublished, [culture]);
        Thread.Sleep(3000);
        VerifyVariance([culture]);
    }


    [TestCase("en-US", "da-DK", "ja-JP")]
    [TestCase("da-DK", "en-US", "ja-JP")]
    [TestCase("ja-JP", "en-US", "da-DK")]
    public void PublishedStructureInAllCultures_WithUnpublishedRootInSingleCulture_YieldsAllDocumentInPublishedRootCulture(string cultureToUnpublish, string expectedCulture, string otherExpectedCulture)
    {
        PublishEntireStructure();
        var root = ContentService.GetById(RootKey)!;

        var result = ContentService.Unpublish(root, cultureToUnpublish);
        Assert.That(result.Success, Is.True);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        VerifyVariance([expectedCulture, otherExpectedCulture]);
    }

    private void VerifyVariance(IEnumerable<string> expectedExistingCultures)
    {
        // Dictionary to map culture to expected root and child titles
        var rootTitles = new Dictionary<string, string>
        {
            { "en-US", EnglishRootTitle },
            { "da-DK", DanishRootTitle },
            { "ja-JP", JapaneseRootTitle }
        };

        var childTitles = new Dictionary<string, string>
        {
            { "en-US", EnglishChildTitle },
            { "da-DK", DanishChildTitle },
            { "ja-JP", JapaneseChildTitle }
        };

        var grandChildTitles = new Dictionary<string, string>
        {
            { "en-US", EnglishGrandChildTitle },
            { "da-DK", DanishGrandChildTitle },
            { "ja-JP", JapaneseGrandChildTitle }
        };

        var allCultures = new[] { "en-US", "da-DK", "ja-JP" };
        var expectedSet = new HashSet<string>(expectedExistingCultures);

        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        Assert.Multiple(() =>
        {
            foreach (var currentCulture in allCultures)
            {
                var rootResults = publishedIndex.Searcher.Search(rootTitles[currentCulture]);
                var childResults = publishedIndex.Searcher.Search(childTitles[currentCulture]);
                var grandChildResults = publishedIndex.Searcher.Search(grandChildTitles[currentCulture]);

                if (expectedSet.Contains(currentCulture))
                {
                    Assert.That(rootResults, Is.Not.Empty);
                    Assert.That(childResults, Is.Not.Empty);
                    Assert.That(grandChildResults, Is.Not.Empty);
                }
                else
                {
                    Assert.That(rootResults, Is.Empty, $"Expected no root results for culture '{currentCulture}'");
                    Assert.That(childResults, Is.Empty, $"Expected no child results for culture '{currentCulture}'");
                    Assert.That(grandChildResults, Is.Empty, $"Expected no grandchild results for culture '{currentCulture}'");
                }
            }
        });
    }

    [SetUp]
    public void CreateVariantDocumentTree()
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

        ContentService.Save(root);

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

        ContentService.Save(child);

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

        ContentService.Save(grandchild);
    }

    private void PublishEntireStructure()
    {
        var root = ContentService.GetById(RootKey)!;
        ContentService.PublishBranch(root, PublishBranchFilter.IncludeUnpublished, ["*"]);
        Thread.Sleep(3000);
    }

}
