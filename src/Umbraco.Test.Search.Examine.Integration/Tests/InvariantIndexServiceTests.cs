using Examine;
using NUnit.Framework;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class InvariantIndexServiceTests : IndexTestBase
{
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexAnyDocument(bool publish)
    {
        CreateInvariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Not.Empty);
    }
    
    [Test]
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
    
    [Test]
    public void CanRemoveUnpublishedDocument()
    {
        CreateInvariantDocument(true);
        var content = ContentService.GetById(RootKey);
        ContentService.Unpublish(content);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Empty);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexTextProperty(bool publish)
    {
        CreateInvariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("title_texts");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo("The root title"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexIntegerValues(bool publish)
    {
        CreateInvariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("count_integers");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == "count_integers").Value, Is.EqualTo("12"));
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDecimalValues(bool publish)
    {
        CreateInvariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("decimalproperty_decimals");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == "decimalproperty_decimals").Value, Is.EqualTo(DecimalValue.ToString()));
    }    
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDateTimeValues(bool publish)
    {
        CreateInvariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("datetime_datetimeoffsets");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Key == "datetime_datetimeoffsets").Value, Is.EqualTo(CurrentDateTimeOffset.ToString()));
    }
    
        
    [Test]
    [TestCase("title", "updated title", false)]
    [TestCase("title", "updated title", true)]
    [TestCase("count", 12, false)]
    [TestCase("count", 12, true)]
    [TestCase("decimalproperty", 1.45, false)]
    [TestCase("decimalproperty", 1.45, true)]
    public void CanIndexUpdatedProperties(string propertyName, object updatedValue, bool publish)
    {
        CreateInvariantDocument(publish);

        UpdateProperty(propertyName, updatedValue, publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.Search(updatedValue.ToString());
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First(x => x.Value == updatedValue.ToString()).Value, Is.EqualTo(updatedValue.ToString()));
    }
    
    private void UpdateProperty(string propertyName, object value, bool publish)
    {
        var content = ContentService.GetById(RootKey);
        content.SetValue(propertyName, value);

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