using Examine;
using NUnit.Framework;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class InvariantContentTreeTests : IndexTestBase
{
    private const string RootTitle = "The root title";
    private const string ChildTitle = "The child title";
    private const string GrandChildTitle = "The grandchild title";
    
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexDocumentTree(bool publish)
    {
        CreateInvariantDocumentTree(publish);

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.Count(), Is.EqualTo(3));
    }
    
    [Test]
    public void UnpublishingRootWillRemoveAncestors_Root_Unpublished()
    {
        CreateInvariantDocumentTree(true);

        var root = ContentService.GetById(RootKey);
        ContentService.Unpublish(root);

        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = publishedIndex.Searcher.Search(RootTitle);
        var publishedResultsChild = publishedIndex.Searcher.Search(ChildTitle);
        var publishedResultsGrandChild = publishedIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRoot, Is.Empty);
            Assert.That(publishedResultsChild, Is.Empty);
            Assert.That(publishedResultsGrandChild, Is.Empty);
        });

        AssertDraft();
    }
    
    [Test]
    public void UnpublishingRootWillRemoveAncestors_Child_Unpublished()
    {
        CreateInvariantDocumentTree(true);

        var child = ContentService.GetById(ChildKey);
        ContentService.Unpublish(child);

        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = publishedIndex.Searcher.Search(RootTitle);
        var publishedResultsChild = publishedIndex.Searcher.Search(ChildTitle);
        var publishedResultsGrandChild = publishedIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRoot, Is.Not.Empty);
            Assert.That(publishedResultsRoot.First().Values.First(x => x.Key == "title").Value, Is.EqualTo(RootTitle));
            Assert.That(publishedResultsChild, Is.Empty);
            Assert.That(publishedResultsGrandChild, Is.Empty);
        });

        AssertDraft();
    }
    
        
    [Test]
    public void UnpublishingRootWillRemoveAncestors_GrandChild_Unpublished()
    {
        CreateInvariantDocumentTree(true);

        var grandChild = ContentService.GetById(GrandchildKey);
        ContentService.Unpublish(grandChild);
        
        var publishedIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        var publishedResultsRoot = publishedIndex.Searcher.Search(RootTitle);
        var publishedResultsChild = publishedIndex.Searcher.Search(ChildTitle);
        var publishedResultsGrandChild = publishedIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(publishedResultsRoot, Is.Not.Empty);
            Assert.That(publishedResultsRoot.First().Values.First(x => x.Key == "title").Value, Is.EqualTo(RootTitle));
            Assert.That(publishedResultsChild, Is.Not.Empty);
            Assert.That(publishedResultsChild.First().Values.First(x => x.Key == "title").Value, Is.EqualTo(ChildTitle));
            Assert.That(publishedResultsGrandChild, Is.Empty);
        });
        
        AssertDraft();
    }
    
    private void AssertDraft()
    {
        var draftIndex = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftContent);
        var draftResultsRoot = draftIndex.Searcher.Search(RootTitle);
        var draftResultsChild = draftIndex.Searcher.Search(ChildTitle);
        var draftResultsGrandChild = draftIndex.Searcher.Search(GrandChildTitle);
        Assert.Multiple(() =>
        {
            Assert.That(draftResultsRoot, Is.Not.Empty);
            Assert.That(draftResultsRoot.First().Values.First(x => x.Key == "title").Value, Is.EqualTo(RootTitle));            
            Assert.That(draftResultsChild, Is.Not.Empty);
            Assert.That(draftResultsChild.First().Values.First(x => x.Key == "title").Value, Is.EqualTo(ChildTitle));    
            Assert.That(draftResultsGrandChild, Is.Not.Empty);
            Assert.That(draftResultsGrandChild.First().Values.First(x => x.Key == "title").Value, Is.EqualTo(GrandChildTitle));
        });
    }
}