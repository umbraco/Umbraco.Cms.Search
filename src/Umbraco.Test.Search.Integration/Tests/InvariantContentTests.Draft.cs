using Umbraco.Cms.Core.Models;

namespace Umbraco.Test.Search.Integration.Tests;

public partial class InvariantContentTests
{
    [Test]
    public void DraftStructure_YieldsAllDocuments()
    {
        SetupDraftContent();
        ContentService.Save([Root(), Child(), Grandchild(), GreatGrandchild()]);

        var documents = IndexService.Dump(IndexAliases.DraftContent);
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
            VerifyDocumentPropertyValues(documents[0], "The root title (draft)", 13);
            VerifyDocumentPropertyValues(documents[1], "The child title (draft)", 35);
            VerifyDocumentPropertyValues(documents[2], "The grandchild title (draft)", 57);
            VerifyDocumentPropertyValues(documents[3], "The great grandchild title (draft)", 79);
        });
    }
    
    [Test]
    public void DraftStructure_YieldsStructuralFields()
    {
        SetupDraftContent();
        ContentService.Save([Root(), Child(), Grandchild(), GreatGrandchild()]);

        var documents = IndexService.Dump(IndexAliases.DraftContent);
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
    public void DraftStructure_YieldsSystemFields()
    {
        SetupDraftContent();
        ContentService.Save([Root(), Child(), Grandchild(), GreatGrandchild()]);

        var documents = IndexService.Dump(IndexAliases.DraftContent);
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
            // NOTE: unpublished content does not have any tags - tags are collected from published content only
            VerifyDocumentSystemValues(documents[0], Root(), []);
            VerifyDocumentSystemValues(documents[1], Child(), []);
            VerifyDocumentSystemValues(documents[2], Grandchild(), []);
            VerifyDocumentSystemValues(documents[3], GreatGrandchild(), []);
        });
    }

    [Test]
    public void PublishedDraftStructure_YieldsSystemFieldsWithTags()
    {
        ContentService.Save(Root());
        ContentService.PublishBranch(Root(), PublishBranchFilter.IncludeUnpublished, ["*"]);

        SetupDraftContent();
        ContentService.Save([Root(), Child(), Grandchild(), GreatGrandchild()]);

        var documents = IndexService.Dump(IndexAliases.DraftContent);
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
            // NOTE: tags are collected from published content only, so the "draft" tag added
            //       by SetupDraftContent() will not be listed here
            VerifyDocumentSystemValues(documents[0], Root(), "tag1", "tag2");
            VerifyDocumentSystemValues(documents[1], Child(), "tag3", "tag4");
            VerifyDocumentSystemValues(documents[2], Grandchild(), "tag5", "tag6");
            VerifyDocumentSystemValues(documents[3], GreatGrandchild(), "tag7", "tag8");
        });
    }    
}