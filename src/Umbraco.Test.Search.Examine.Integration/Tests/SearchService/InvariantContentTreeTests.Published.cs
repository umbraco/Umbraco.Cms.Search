using NUnit.Framework;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public partial class InvariantContentTreeTests : SearcherTestBase
{
    [Test]
    public async Task PublishStructure_WithRootInRecycleBin_YieldsNoDocuments()
    {
        CreateInvariantDocumentTree(true);
        var root = ContentService.GetById(RootKey);
        var result = ContentService.MoveToRecycleBin(root);
        Thread.Sleep(3000);
        
        var indexAlias = GetIndexAlias(true);
        var rootResult = await Searcher.SearchAsync(indexAlias, "Root", null, null, null, null, null, null, 0, 100);
        var childResult = await Searcher.SearchAsync(indexAlias, "Child", null, null, null, null, null, null, 0, 100);
        var grandChildResult = await Searcher.SearchAsync(indexAlias, "Grandchild", null, null, null, null, null, null, 0, 100);
        
        Assert.Multiple(() =>
        {
            Assert.That(rootResult.Total, Is.EqualTo(0));
            Assert.That(childResult.Total, Is.EqualTo(0));
            Assert.That(grandChildResult.Total, Is.EqualTo(0));
        });
    }
    
        
    [Test]
    public async Task PublishStructure_WithChildUnpublished_YieldsNothingBelowRoot()
    {
        CreateInvariantDocumentTree(true);
        var child = ContentService.GetById(ChildKey);
        ContentService.Unpublish(child);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var indexAlias = GetIndexAlias(true);
        var rootResult = await Searcher.SearchAsync(indexAlias, "Root", null, null, null, null, null, null, 0, 100);
        var childResult = await Searcher.SearchAsync(indexAlias, "Child", null, null, null, null, null, null, 0, 100);
        var grandChildResult = await Searcher.SearchAsync(indexAlias, "Grandchild", null, null, null, null, null, null, 0, 100);
        
        Assert.Multiple(() =>
        {
            Assert.That(rootResult.Total, Is.EqualTo(1));
            Assert.That(childResult.Total, Is.EqualTo(0));
            Assert.That(grandChildResult.Total, Is.EqualTo(0));
            Assert.That(rootResult.Documents.First().Id, Is.EqualTo(RootKey));
        });
    }
    
    [Test]
    public async Task PublishStructure_WithGrandchildUnpublished_YieldsNothingBelowChild()
    {
        CreateInvariantDocumentTree(true);
        var grandchild = ContentService.GetById(GrandchildKey);
        ContentService.Unpublish(grandchild);
        
        // TODO: We need to await that the index deleting has completed, for now this is our only option
        Thread.Sleep(3000);
        
        var indexAlias = GetIndexAlias(true);
        var rootResult = await Searcher.SearchAsync(indexAlias, "Root", null, null, null, null, null, null, 0, 100);
        var childResult = await Searcher.SearchAsync(indexAlias, "Child", null, null, null, null, null, null, 0, 100);
        var grandChildResult = await Searcher.SearchAsync(indexAlias, "Grandchild", null, null, null, null, null, null, 0, 100);
        
        Assert.Multiple(() =>
        {
            Assert.That(rootResult.Total, Is.EqualTo(1));
            Assert.That(childResult.Total, Is.EqualTo(1));
            Assert.That(grandChildResult.Total, Is.EqualTo(0));
            Assert.That(rootResult.Documents.First().Id, Is.EqualTo(RootKey));
            Assert.That(childResult.Documents.First().Id, Is.EqualTo(ChildKey));
        });
    }
}