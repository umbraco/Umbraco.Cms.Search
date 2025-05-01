using Examine;
using NUnit.Framework;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class VariantIndexServiceTests : IndexTestBase
{
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexAnyDocument(bool publish)
    {
        CreateVariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Not.Empty);
    }
    
    [Test]
    [TestCase(true, "en-us", "Name")]
    [TestCase(false, "en-us", "Name")]
    [TestCase(true, "da-dk", "Navn")]
    [TestCase(false, "da-dk", "Navn")]
    [TestCase(true, "ja-jp", "名前")]
    [TestCase(false, "ja-jp", "名前")]
    public void CanIndexVariantName(bool publish, string culture, string expectedValue)
    {
        CreateVariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"Umb_Name_{culture}");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo(expectedValue));
    }
    
    [Test]
    [TestCase(true, "en-us", "Root")]
    [TestCase(false, "en-us", "Root")]
    [TestCase(true, "da-dk", "Rod")]
    [TestCase(false, "da-dk", "Rod")]
    [TestCase(true, "ja-jp", "ル-ト")]
    [TestCase(false, "ja-jp", "ル-ト")]
    public void CanIndexVariantText(bool publish, string culture, string expectedValue)
    {
        CreateVariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"title_{culture}");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo(expectedValue));
    }
}