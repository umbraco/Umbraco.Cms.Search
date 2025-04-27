namespace Umbraco.Test.Search.Integration.Tests;

public partial class InvariantContentTests
{
    [Test]
    public void PublishedStructure_YieldsAllPublishedDocuments()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Id, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Id, Is.EqualTo(GreatGrandchildKey));
        });

        Assert.Multiple(() =>
        {
            VerifyDocumentPropertyValues(documents[0], "The root title", 12);
            VerifyDocumentPropertyValues(documents[1], "The child title", 34);
            VerifyDocumentPropertyValues(documents[2], "The grandchild title", 56);
            VerifyDocumentPropertyValues(documents[3], "The great grandchild title", 78);
        });
    }

    [Test]
    public void PublishedStructure_CanRefreshChild()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        var child = Child();
        child.SetValue("title", "The updated child title");
        child.SetValue("count", 123456);
        ContentService.SaveAndPublish(child);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Id, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Id, Is.EqualTo(GreatGrandchildKey));
        });

        VerifyDocumentPropertyValues(documents[1], "The updated child title", 123456);
    }

    [Test]
    public void PublishedStructure_YieldsStructuralFields()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Id, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Id, Is.EqualTo(GreatGrandchildKey));
        });

        Assert.Multiple(() =>
        {
            VerifyDocumentStructureValues(documents[0], RootKey, Guid.Empty, RootKey);
            VerifyDocumentStructureValues(documents[1], ChildKey, RootKey, RootKey, ChildKey);
            VerifyDocumentStructureValues(documents[2], GrandchildKey, ChildKey, RootKey, ChildKey, GrandchildKey);
            VerifyDocumentStructureValues(documents[3], GreatGrandchildKey, GrandchildKey, RootKey, ChildKey, GrandchildKey, GreatGrandchildKey);
        });
    }

    [Test]
    public void PublishedStructure_YieldsSystemFields()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Id, Is.EqualTo(RootKey));
            Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Id, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Id, Is.EqualTo(GreatGrandchildKey));
        });

        Assert.Multiple(() =>
        {
            VerifyDocumentSystemValues(documents[0], Root(), "tag1", "tag2");
            VerifyDocumentSystemValues(documents[1], Child(), "tag3", "tag4");
            VerifyDocumentSystemValues(documents[2], Grandchild(), "tag5", "tag6");
            VerifyDocumentSystemValues(documents[3], GreatGrandchild(), "tag7", "tag8");
        });
    }

    [Test]
    public void PublishedStructure_CanUpdateEditableSystemFields()
    {
        ContentService.SaveAndPublishBranch(Root(), true);
        
        var child = Child();
        child.Name = "The updated child name";
        child.SetValue("tags", "[\"updated-tag1\",\"updated-tag2\",\"updated-tag3\"]");
        ContentService.SaveAndPublish(child);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.That(documents[1].Id, Is.EqualTo(ChildKey));
        VerifyDocumentSystemValues(documents[1], Child(), "updated-tag1", "updated-tag2", "updated-tag3");
    }
}