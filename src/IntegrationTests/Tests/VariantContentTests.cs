using IntegrationTests.Services;
using Package;
using Package.Models.Indexing;
using Umbraco.Cms.Core.Services.Changes;

namespace IntegrationTests.Tests;

public class VariantContentTests : VariantTestBase
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
    public async Task PublishedStructure_CanRefreshChild_InSingleCulture()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));
        
        var child = Child();
        child.SetValue("title", "The updated child title in English", "en-US");
        ContentService.SaveAndPublish(child);

        await HandleContentChangeAsync(new ContentChange(ChildKey, TreeChangeTypes.RefreshNode));

        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(4));

        VerifyDocumentPropertyValues(documents[1], "The updated child title in English", "The child title in Danish", 34);
    }
    
    [Test]
    public async Task PublishedStructure_CanRefreshChild_InMultipleCultures()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));
        
        var child = Child();
        child.SetValue("title", "The updated child title in English", "en-US");
        child.SetValue("title", "The updated child title in Danish", "da-DK");
        ContentService.SaveAndPublish(child);

        await HandleContentChangeAsync(new ContentChange(ChildKey, TreeChangeTypes.RefreshNode));

        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(4));

        VerifyDocumentPropertyValues(documents[1], "The updated child title in English", "The updated child title in Danish", 34);
    }

    [Test]
    public async Task PublishedStructure_CanRefreshChild_InvariantCulture()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));
        
        var child = Child();
        child.SetValue("count", 123456);
        ContentService.SaveAndPublish(child);

        await HandleContentChangeAsync(new ContentChange(ChildKey, TreeChangeTypes.RefreshNode));

        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(4));

        VerifyDocumentPropertyValues(documents[1], "The child title in English", "The child title in Danish", 123456);
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
        => VerifyDocumentPropertyValues(document, $"{title} in English", $"{title} in Danish", count);

    private void VerifyDocumentPropertyValues(TestIndexDocument document, string englishTitle, string danishTitle, int count)
        => Assert.Multiple(() =>
        {
            var titleFields = document.Fields.Where(f => f.Alias == "title").ToArray();
            Assert.That(titleFields.Length, Is.EqualTo(2));
            Assert.That(titleFields.SingleOrDefault(f => f.Culture.InvariantEquals("en-US"))?.Value.Texts?.SingleOrDefault(), Is.EqualTo(englishTitle));
            Assert.That(titleFields.SingleOrDefault(f => f.Culture.InvariantEquals("da-DK"))?.Value.Texts?.SingleOrDefault(), Is.EqualTo(danishTitle));
            
            var countValue = document.Fields.FirstOrDefault(f => f.Alias == "count")?.Value.Integers?.SingleOrDefault();
            Assert.That(countValue, Is.EqualTo(count));
        });

    private void VerifyDocumentSystemValues(TestIndexDocument document, string name)
    {
        Assert.Multiple(() =>
        {
            var contentTypeValue = document.Fields.FirstOrDefault(f => f.Alias == IndexConstants.Aliases.ContentType)?.Value.Keywords?.SingleOrDefault();
            Assert.That(contentTypeValue, Is.EqualTo("variant"));

            var nameFields = document.Fields.Where(f => f.Alias == IndexConstants.Aliases.Name).ToArray();
            Assert.That(nameFields.Length, Is.EqualTo(2));
            Assert.That(nameFields.SingleOrDefault(f => f.Culture.InvariantEquals("en-US"))?.Value.Texts?.SingleOrDefault(), Is.EqualTo($"{name} EN"));
            Assert.That(nameFields.SingleOrDefault(f => f.Culture.InvariantEquals("da-DK"))?.Value.Texts?.SingleOrDefault(), Is.EqualTo($"{name} DA"));
        });
    }
}