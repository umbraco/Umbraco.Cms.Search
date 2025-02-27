using IntegrationTests.Services;
using Package;
using Package.Models.Indexing;
using Umbraco.Cms.Core.Services.Changes;

namespace IntegrationTests.Tests;

public class InvariantContentTests : InvariantTestBase
{
    [Test]
    public async Task PublishedStructure_YieldsAllPublishedDocuments()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));

        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Key, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Key, Is.EqualTo(GreatGrandchildKey));
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
    public async Task PublishedStructure_CanRefreshChild()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));
        
        var child = Child();
        child.SetValue("title", "The updated child title");
        child.SetValue("count", 123456);
        ContentService.SaveAndPublish(child);

        await HandleContentChangeAsync(new ContentChange(ChildKey, TreeChangeTypes.RefreshNode));

        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Key, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Key, Is.EqualTo(GreatGrandchildKey));
        });

        VerifyDocumentPropertyValues(documents[1], "The updated child title", 123456);
    }

    [Test]
    public async Task PublishedStructure_YieldsStructuralFields()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));

        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Key, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Key, Is.EqualTo(GreatGrandchildKey));
        });

        Assert.Multiple(() =>
        {
            VerifyDocumentStructureValues(documents[0], RootKey, Guid.Empty);
            VerifyDocumentStructureValues(documents[1], ChildKey, RootKey, RootKey);
            VerifyDocumentStructureValues(documents[2], GrandchildKey, ChildKey, RootKey, ChildKey);
            VerifyDocumentStructureValues(documents[3], GreatGrandchildKey, GrandchildKey, RootKey, ChildKey, GrandchildKey);
        });
    }

    [Test]
    public async Task PublishedStructure_YieldsSystemFields()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));

        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
            Assert.That(documents[2].Key, Is.EqualTo(GrandchildKey));
            Assert.That(documents[3].Key, Is.EqualTo(GreatGrandchildKey));
        });

        Assert.Multiple(() =>
        {
            VerifyDocumentSystemValues(documents[0], "Root");
            VerifyDocumentSystemValues(documents[1], "Child");
            VerifyDocumentSystemValues(documents[2], "Grandchild");
            VerifyDocumentSystemValues(documents[3], "Great Grandchild");
        });

        Assert.Inconclusive("Validate create and update dates in this test when they're supported");
    }

    private void VerifyDocumentPropertyValues(TestIndexDocument document, string title, int count)
        => Assert.Multiple(() =>
        {
            var titleValue = document.Fields.FirstOrDefault(f => f.Alias == "title")?.Value.Texts?.SingleOrDefault();
            Assert.That(titleValue, Is.EqualTo(title));
            
            var countValue = document.Fields.FirstOrDefault(f => f.Alias == "count")?.Value.Integers?.SingleOrDefault();
            Assert.That(countValue, Is.EqualTo(count));
        });

    private void VerifyDocumentStructureValues(TestIndexDocument document, Guid id, Guid parentId, params Guid[] ancestorIds)
        => Assert.Multiple(() =>
        {
            var idValue = document.Fields.FirstOrDefault(f => f.Alias == IndexConstants.Aliases.Id)?.Value.Keywords?.SingleOrDefault();
            Assert.That(idValue, Is.EqualTo(id.ToString("D")));
            
            var parentIdValue = document.Fields.FirstOrDefault(f => f.Alias == IndexConstants.Aliases.ParentId)?.Value.Keywords?.SingleOrDefault();
            Assert.That(parentIdValue, Is.EqualTo(parentId.ToString("D")));

            var ancestorIdValues = document.Fields.FirstOrDefault(f => f.Alias == IndexConstants.Aliases.AncestorIds)?.Value.Keywords?.ToArray();
            Assert.That(ancestorIdValues, Is.Not.Null);
            Assert.That(ancestorIdValues.Length, Is.EqualTo(ancestorIds.Length));
            Assert.That(ancestorIdValues, Is.EquivalentTo(ancestorIds.Select(ancestorId => ancestorId.ToString("D"))));
        });

    private void VerifyDocumentSystemValues(TestIndexDocument document, string name)
    {
        Assert.Multiple(() =>
        {
            var contentTypeValue = document.Fields.FirstOrDefault(f => f.Alias == IndexConstants.Aliases.ContentType)?.Value.Keywords?.SingleOrDefault();
            Assert.That(contentTypeValue, Is.EqualTo("invariant"));

            var nameValue = document.Fields.FirstOrDefault(f => f.Alias == IndexConstants.Aliases.Name)?.Value.Texts?.SingleOrDefault();
            Assert.That(nameValue, Is.EqualTo(name));
        });
    }
}