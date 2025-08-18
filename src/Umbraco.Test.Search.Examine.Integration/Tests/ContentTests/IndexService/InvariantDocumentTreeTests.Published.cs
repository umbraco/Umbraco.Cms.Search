using Examine;
using NUnit.Framework;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

public partial class InvariantDocumentTreeTests : IndexTestBase
{
    [Test]
    public void PublishedStructure_YieldsAllPublishedDocuments()
    {
        CreateInvariantDocumentTree(true);
        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        var results = index.Searcher.CreateQuery().All().Execute().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(3));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
            Assert.That(results[1].Id, Is.EqualTo(ChildKey.ToString()));
            Assert.That(results[2].Id, Is.EqualTo(GrandchildKey.ToString()));
        });
    }

    [Test]
    public void PublishedStructure_AlsoIndexesDraftStructure()
    {
        CreateInvariantDocumentTree(true);
        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(3));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
            Assert.That(results[1].Id, Is.EqualTo(ChildKey.ToString()));
            Assert.That(results[2].Id, Is.EqualTo(GrandchildKey.ToString()));
        });
    }

    [Test]
    public void PublishedStructure_WithUnpublishedRoot_YieldsNoDocuments()
    {
        CreateInvariantDocumentTree(true);
        var root = ContentService.GetById(RootKey)!;
        ContentService.Unpublish(root);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = index.Searcher.CreateQuery().All().Execute();
        Assert.That(publishedResultsRoot.TotalItemCount, Is.EqualTo(0));
    }

    [Test]
    public void PublishedStructure_WithUnpublishedChild_YieldsNothingBelowRoot()
    {
        CreateInvariantDocumentTree(true);
        var child = ContentService.GetById(ChildKey)!;
        ContentService.Unpublish(child);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var results = index.Searcher.CreateQuery().All().Execute().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
        });
    }

    [Test]
    public void PublishedStructure_WithUnpublishedGrandchild_YieldsNothingBelowChild()
    {
        CreateInvariantDocumentTree(true);
        var grandChild = ContentService.GetById(GrandchildKey)!;
        ContentService.Unpublish(grandChild);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var results = index.Searcher.CreateQuery().All().Execute().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
            Assert.That(results[1].Id, Is.EqualTo(ChildKey.ToString()));
        });
    }

    [Test]
    public void PublishedStructure_WithRootInRecycleBin_YieldsNoDocuments()
    {
        CreateInvariantDocumentTree(true);
        var root = ContentService.GetById(RootKey)!;
        ContentService.MoveToRecycleBin(root);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = index.Searcher.CreateQuery().All().Execute();
        Assert.That(publishedResultsRoot.TotalItemCount, Is.EqualTo(0));
    }

    [Test]
    public void PublishedStructure_WithChildInRecycleBin_YieldsNothingBelowRoot()
    {
        CreateInvariantDocumentTree(true);
        var child = ContentService.GetById(ChildKey)!;
        ContentService.MoveToRecycleBin(child);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var results = index.Searcher.CreateQuery().All().Execute().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
        });
    }

    [Test]
    public void PublishedStructure_WithUGrandchildInRecycleBin_YieldsNothingBelowChild()
    {
        CreateInvariantDocumentTree(true);
        var grandChild = ContentService.GetById(GrandchildKey)!;
        ContentService.MoveToRecycleBin(grandChild);

        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var results = index.Searcher.CreateQuery().All().Execute().ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results[0].Id, Is.EqualTo(RootKey.ToString()));
            Assert.That(results[1].Id, Is.EqualTo(ChildKey.ToString()));
        });
    }
}
