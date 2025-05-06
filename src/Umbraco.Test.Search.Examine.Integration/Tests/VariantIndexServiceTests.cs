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
        queryBuilder.SelectField($"Umb_Name_{culture}_texts");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Value == expectedValue).Value, Is.EqualTo(expectedValue));
    }
    
    [TestCase("title", "updatedTitle", "en-us", true)]
    [TestCase("title", "updatedTitle", "da-dk", true)]
    [TestCase("title", "updatedTitle", "ja-jp", true)]
    public void CanIndexUpdatedProperties(string propertyName, object updatedValue, string culture, bool publish)
    {
        CreateVariantDocument(publish);
        UpdateProperty(propertyName, updatedValue, culture, publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);
        
        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField($"{propertyName}_{culture}_texts");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == $"{propertyName}_{culture}_texts").Value, Is.EqualTo(updatedValue.ToString()));
    }
    
    [TestCase(true, "en-us", "Root")]
    [TestCase(false, "en-us", "Root")]
    [TestCase(true, "da-dk", "Rod")]
    [TestCase(false, "da-dk", "Rod")]
    [TestCase(true, "ja-jp", "ル-ト")]
    [TestCase(false, "ja-jp", "ル-ト")]
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
    
    [TestCase(true, "en-us", "segment-1", "body-segment-1")]
    [TestCase(false, "en-us", "segment-2", "body-segment-2")]
    [TestCase(true, "da-dk","segment-1", "krop-segment-1")]
    [TestCase(false, "da-dk","segment-2", "krop-segment-2")]
    [TestCase(true, "ja-jp", "segment-1", "ボディ-segment-1")]
    [TestCase(false, "ja-jp", "segment-2", "ボディ-segment-2")]
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
            ContentService.SaveAndPublish(content);
        }
        else
        {
            ContentService.Save(content);

        }
    }
}