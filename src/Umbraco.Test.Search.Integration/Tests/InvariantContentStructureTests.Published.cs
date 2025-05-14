using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Test.Search.Integration.Tests;

public partial class InvariantContentStructureTests
{
    [Test]
    public void PublishedStructure_YieldsAllPublishedDocuments()
    {
        ContentService.Save(Root());
        ContentService.PublishBranch(Root(), PublishBranchFilter.IncludeUnpublished, ["*"]);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Id, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Id, Is.EqualTo(GreatGrandchildKey));

            Assert.That(documents.All(d => d.ObjectType is UmbracoObjectTypes.Document), Is.True);
        });
    }
    
    [Test]
    public void PublishedRoot_YieldsOnlyRootDocument()
    {
        ContentService.Save(Root());
        ContentService.Publish(Root(), ["*"]);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Id, Is.EqualTo(RootKey));
    }

    [Test]
    public void PublishedStructure_WithUnpublishedRoot_YieldsNoDocuments()
    {
        ContentService.Save(Root());
        ContentService.PublishBranch(Root(), PublishBranchFilter.IncludeUnpublished, ["*"]);
        
        var result = ContentService.Unpublish(Root());
        Assert.That(result.Success, Is.True);
        Assert.That(Child().Published, Is.True);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Is.Empty);
    }

    [Test]
    public void PublishedStructure_WithUnpublishedGrandchild_YieldsNothingBelowChild()
    {
        ContentService.Save(Root());
        ContentService.PublishBranch(Root(), PublishBranchFilter.IncludeUnpublished, ["*"]);

        var result = ContentService.Unpublish(Grandchild());
        Assert.That(result.Success, Is.True);
        Assert.That(GreatGrandchild().Published, Is.True);
           
        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
        });
    }

    [Test]
    public void PublishedStructure_WithGrandchildInRecycleBin_YieldsNothingBelowChild()
    {
        ContentService.Save(Root());
        ContentService.PublishBranch(Root(), PublishBranchFilter.IncludeUnpublished, ["*"]);
        
        var result = ContentService.MoveToRecycleBin(Grandchild());
        Assert.That(result.Success, Is.True);
        Assert.That(GreatGrandchild().Trashed, Is.True);
           
        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
        });
    }

    [Test]
    public void PublishedStructure_WithGrandchildDeleted_YieldsNothingBelowChild()
    {
        ContentService.Save(Root());
        ContentService.PublishBranch(Root(), PublishBranchFilter.IncludeUnpublished, ["*"]);
        
        var result = ContentService.Delete(Grandchild());
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ContentService.GetById(GreatGrandchildKey), Is.Null);
        });
           
        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
        });
    }
}