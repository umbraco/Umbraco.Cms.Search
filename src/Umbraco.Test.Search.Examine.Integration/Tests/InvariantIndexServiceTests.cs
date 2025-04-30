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
    public void CanIndexTextProperty(bool publish)
    {
        CreateInvariantDocument(publish);

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
        CreateInvariantDocument(publish);

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
    public void CanIndexDecimalValues(bool publish)
    {
        CreateInvariantDocument(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("decimalproperty");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo("12,43"));
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
        queryBuilder.SelectField("datetime");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo(CurrentDateTimeOffset.ToString()));
    }
}