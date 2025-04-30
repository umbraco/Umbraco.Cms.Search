using Examine;
using NUnit.Framework;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class IndexServiceTests : IndexTestBase
{
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexAnyDocument(bool publish)
    {
        CreateInvariantRootDocument(publish);
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Not.Empty);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexTextProperty(bool publish)
    {
        CreateInvariantRootDocument(publish);
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("title");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo("The root title"));
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexIntegerValues(bool publish)
    {
        CreateInvariantRootDocument(publish);
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("count");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo("12"));
    }    
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDateTimeValues(bool publish)
    {
        CreateInvariantRootDocument(publish);
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("datetime");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo(CurrentDateTimeOffset.ToString()));
    }
}