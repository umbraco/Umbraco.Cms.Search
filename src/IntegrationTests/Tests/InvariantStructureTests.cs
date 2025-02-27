using Package.Models.Indexing;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Tests.Common.Testing;

namespace IntegrationTests.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class InvariantStructureTests : InvariantTestBase
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
    }
    
    [Test]
    public async Task PublishedRoot_YieldsOnlyRootDocument()
    {
        ContentService.SaveAndPublish(Root());

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));

        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Key, Is.EqualTo(RootKey));
    }

    [Test]
    public async Task PublishedStructure_WithUnpublishedRoot_YieldsNoDocuments()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));
        
        var result = ContentService.Unpublish(Root());
        Assert.That(result.Success, Is.True);
        Assert.That(Child().Published, Is.True);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));

        var documents = IndexService.Dump();
        Assert.That(documents, Is.Empty);
    }

    [Test]
    public async Task PublishedStructure_WithUnpublishedGrandchild_YieldsNothingBelowChild()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));
        
        var result = ContentService.Unpublish(Grandchild());
        Assert.That(result.Success, Is.True);
        Assert.That(GreatGrandchild().Published, Is.True);

        await HandleContentChangeAsync(new ContentChange(GrandchildKey, TreeChangeTypes.RefreshNode));
           
        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
        });
    }

    [Test]
    public async Task PublishedStructure_WithGrandchildInRecycleBin_YieldsNothingBelowChild()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));
        
        var result = ContentService.MoveToRecycleBin(Grandchild());
        Assert.That(result.Success, Is.True);
        Assert.That(GreatGrandchild().Trashed, Is.True);

        await HandleContentChangeAsync(new ContentChange(GrandchildKey, TreeChangeTypes.RefreshNode));
           
        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
        });
    }

    [Test]
    public async Task PublishedStructure_WithGrandchildDeleted_YieldsNothingBelowChild()
    {
        ContentService.SaveAndPublishBranch(Root(), true);

        await HandleContentChangeAsync(new ContentChange(RootKey, TreeChangeTypes.RefreshNode));
        
        var result = ContentService.Delete(Grandchild());
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ContentService.GetById(GreatGrandchildKey), Is.Null);
        });

        await HandleContentChangeAsync(new ContentChange(GrandchildKey, TreeChangeTypes.Remove));
           
        var documents = IndexService.Dump();
        Assert.That(documents, Has.Count.EqualTo(2));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildKey));
        });
    }
}