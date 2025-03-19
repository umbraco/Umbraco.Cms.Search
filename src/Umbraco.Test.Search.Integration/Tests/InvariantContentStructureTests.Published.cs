using Umbraco.Cms.Core.Models;

namespace Umbraco.Test.Search.Integration.Tests;

public partial class InvariantContentStructureTests
{
    [Test]
    public void PublishedStructure_YieldsAllPublishedDocuments()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Key, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Key, Is.EqualTo(GreatGrandchildKey));

            Assert.That(documents.All(d => d.ObjectType is UmbracoObjectTypes.Document), Is.True);
        });
    }
    
    [Test]
    public void PublishedRoot_YieldsOnlyRootDocument()
    {
        ContentService.SaveAndPublish(Root());

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Key, Is.EqualTo(RootKey));
    }

    [Test]
    public void PublishedStructure_WithUnpublishedRoot_YieldsNoDocuments()
    {
        ContentService.SaveAndPublishBranch(Root(), true);
        
        var result = ContentService.Unpublish(Root());
        Assert.That(result.Success, Is.True);
        Assert.That(Child().Published, Is.True);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Is.Empty);
    }

    [Test]
    public void PublishedStructure_WithUnpublishedGrandchild_YieldsNothingBelowChild()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        var result = ContentService.Unpublish(Grandchild());
        Assert.That(result.Success, Is.True);
        Assert.That(GreatGrandchild().Published, Is.True);
           
        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
        });
    }

    [Test]
    public void PublishedStructure_WithGrandchildInRecycleBin_YieldsNothingBelowChild()
    {
        ContentService.SaveAndPublishBranch(Root(), true);
        
        var result = ContentService.MoveToRecycleBin(Grandchild());
        Assert.That(result.Success, Is.True);
        Assert.That(GreatGrandchild().Trashed, Is.True);
           
        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
        });
    }

    [Test]
    public void PublishedStructure_WithGrandchildDeleted_YieldsNothingBelowChild()
    {
        ContentService.SaveAndPublishBranch(Root(), true);
        
        var result = ContentService.Delete(Grandchild());
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ContentService.GetById(GreatGrandchildKey), Is.Null);
        });
           
        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
        });
    }
}