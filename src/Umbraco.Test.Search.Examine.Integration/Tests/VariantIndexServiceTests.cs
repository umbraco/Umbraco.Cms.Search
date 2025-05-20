using Examine;
using NUnit.Framework;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class VariantIndexServiceTests : IndexTestBase
{
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
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanRemoveAnyDocument(bool publish)
    {
        CreateInvariantDocument(publish);
        var content = ContentService.GetById(RootKey);
        ContentService.Delete(content);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

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
        CreateVariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"Umb_Name_{culture}_texts");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Value == expectedValue).Value, Is.EqualTo(expectedValue));
    }
    
    [TestCase("title", "updatedTitle", "en-US", true)]
    [TestCase("title", "updatedTitle", "da-DK", true)]
    [TestCase("title", "updatedTitle", "ja-JP", true)]
    public void CanIndexUpdatedProperties(string propertyName, string updatedValue, string culture, bool publish)
    {
        CreateVariantDocument(publish);
        UpdateProperty(propertyName, updatedValue, culture, publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var results = index.Searcher.Search(updatedValue);
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == $"{propertyName}_{culture}_texts").Value, Is.EqualTo(updatedValue));
    }
    
    [TestCase(true, "en-US", "Root")]
    [TestCase(false, "en-US", "Root")]
    [TestCase(true, "da-DK", "Rod")]
    [TestCase(false, "da-DK", "Rod")]
    [TestCase(true, "ja-JP", "ル-ト")]
    [TestCase(false, "ja-JP", "ル-ト")]
    public void CanIndexVariantTextByCulture(bool publish, string culture, string expectedValue)
    {
        CreateVariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"title_{culture}_texts");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == $"title_{culture}_texts").Value, Is.EqualTo(expectedValue));
    }
    
    [TestCase(true, "en-US", "segment-1", "body-segment-1")]
    [TestCase(false, "en-US", "segment-2", "body-segment-2")]
    [TestCase(true, "da-DK","segment-1", "krop-segment-1")]
    [TestCase(false, "da-DK","segment-2", "krop-segment-2")]
    [TestCase(true, "ja-JP", "segment-1", "ボディ-segment-1")]
    [TestCase(false, "ja-JP", "segment-2", "ボディ-segment-2")]
    public void CanIndexVariantTextBySegment(bool publish, string culture, string segment, string expectedValue)
    {
        CreateVariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"body_{culture}_{segment}_texts");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == $"body_{culture}_{segment}_texts").Value, Is.EqualTo(expectedValue));
    }  
    
    private void UpdateProperty(string propertyName, object value, string culture, bool publish)
    {
        var content = ContentService.GetById(RootKey);
        content.SetValue(propertyName, value, culture);

        if (publish)
        {
            ContentService.Save(content);
            ContentService.Publish(content, ["*"]);
        }
        else
        {
            ContentService.Save(content);
        }
    }
}