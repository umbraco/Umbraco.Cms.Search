using Examine;
using NUnit.Framework;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public partial class InvariantContentTreeTests : IndexTestBase
{
    private const string RootTitle = "The root title";
    private const string ChildTitle = "The child title";
    private const string GrandChildTitle = "The grandchild title";
    
    [Test]
    public void CanIndexPublishedDocumentTree()
    {
        CreateInvariantDocumentTree(true);
        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        var results = publishedIndex.Searcher.CreateQuery().All().Execute();
        Assert.That(results.Count(), Is.EqualTo(3));
    }
    
    [Test]
    public void IndexingPublishedTreeAlsoIndexesDraft()
    {
        CreateInvariantDocumentTree(true);
        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = publishedIndex.Searcher.CreateQuery().All().Execute();
        Assert.That(results.Count(), Is.EqualTo(3));
    }
    
    [Test]
    public void UnpublishingRootWillRemoveDescendants()
    {
        CreateInvariantDocumentTree(true);
        var root = ContentService.GetById(RootKey);
        ContentService.Unpublish(root);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = index.Searcher.Search(RootTitle);
        var publishedResultsChild = index.Searcher.Search(ChildTitle);
        var publishedResultsGrandChild = index.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRoot, Is.Empty);
            Assert.That(publishedResultsChild, Is.Empty);
            Assert.That(publishedResultsGrandChild, Is.Empty);
        });
    }
    
    [Test]
    public void UnpublishingChildWillRemoveDescendants()
    {
        CreateInvariantDocumentTree(true);
        var child = ContentService.GetById(ChildKey);
        ContentService.Unpublish(child);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = publishedIndex.Searcher.Search(RootTitle);
        var publishedResultsChild = publishedIndex.Searcher.Search(ChildTitle);
        var publishedResultsGrandChild = publishedIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRoot, Is.Not.Empty);
            Assert.That(publishedResultsRoot.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo(RootTitle));
            Assert.That(publishedResultsChild, Is.Empty);
            Assert.That(publishedResultsGrandChild, Is.Empty);
        });
    }
    
    [Test]
    public void UnpublishingGrandChildWillRemoveDescendants()
    {
        CreateInvariantDocumentTree(true);
        var grandChild = ContentService.GetById(GrandchildKey);
        ContentService.Unpublish(grandChild);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = publishedIndex.Searcher.Search(RootTitle);
        var publishedResultsChild = publishedIndex.Searcher.Search(ChildTitle);
        var publishedResultsGrandChild = publishedIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRoot, Is.Not.Empty);
            Assert.That(publishedResultsRoot.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo(RootTitle));
            Assert.That(publishedResultsChild, Is.Not.Empty);
            Assert.That(publishedResultsChild.First().Values.First(x => x.Key == "title_texts").Value, Is.EqualTo(ChildTitle));
            Assert.That(publishedResultsGrandChild, Is.Empty);
        });
    }
}