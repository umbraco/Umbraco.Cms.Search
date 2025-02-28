using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

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
            VerifyDocumentStructureValues(documents[0], RootKey, Guid.Empty, RootKey);
            VerifyDocumentStructureValues(documents[1], ChildKey, RootKey, RootKey, ChildKey);
            VerifyDocumentStructureValues(documents[2], GrandchildKey, ChildKey, RootKey, ChildKey, GrandchildKey);
            VerifyDocumentStructureValues(documents[3], GreatGrandchildKey, GrandchildKey, RootKey, ChildKey, GrandchildKey, GreatGrandchildKey);
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
            VerifyDocumentSystemValues(documents[0], Root());
            VerifyDocumentSystemValues(documents[1], Child());
            VerifyDocumentSystemValues(documents[2], Grandchild());
            VerifyDocumentSystemValues(documents[3], GreatGrandchild());
        });
    }

    private void VerifyDocumentPropertyValues(TestIndexDocument document, string title, int count)
        => Assert.Multiple(() =>
        {
            var titleValue = document.Fields.FirstOrDefault(f => f.FieldName == "title")?.Value.Texts?.SingleOrDefault();
            Assert.That(titleValue, Is.EqualTo(title));
            
            var countValue = document.Fields.FirstOrDefault(f => f.FieldName == "count")?.Value.Integers?.SingleOrDefault();
            Assert.That(countValue, Is.EqualTo(count));
        });

    private void VerifyDocumentStructureValues(TestIndexDocument document, Guid id, Guid parentId, params Guid[] pathIds)
        => Assert.Multiple(() =>
        {
            var idValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.Id)?.Value.Keywords?.SingleOrDefault();
            Assert.That(idValue, Is.EqualTo(id.ToString("D")));
            
            var parentIdValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.ParentId)?.Value.Keywords?.SingleOrDefault();
            Assert.That(parentIdValue, Is.EqualTo(parentId.ToString("D")));

            var pathIdsValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.PathIds)?.Value.Keywords?.ToArray();
            Assert.That(pathIdsValue, Is.Not.Null);
            Assert.That(pathIdsValue.Length, Is.EqualTo(pathIds.Length));
            Assert.That(pathIdsValue, Is.EquivalentTo(pathIds.Select(ancestorId => ancestorId.ToString("D"))));
        });

    private void VerifyDocumentSystemValues(TestIndexDocument document, IContent content)
    {
        var dateTimeOffsetConverter = new DateTimeOffsetConverter();
        Assert.Multiple(() =>
        {
            var contentTypeValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.ContentType)?.Value.Keywords?.SingleOrDefault();
            Assert.That(contentTypeValue, Is.EqualTo(content.ContentType.Alias));

            var nameValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.Name)?.Value.Texts?.SingleOrDefault();
            Assert.That(nameValue, Is.EqualTo(content.Name));

            var createDateValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.CreateDate)?.Value.DateTimeOffsets?.SingleOrDefault();
            Assert.That(createDateValue, Is.EqualTo(dateTimeOffsetConverter.ToDateTimeOffset(content.CreateDate)));

            var updateDateValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.UpdateDate)?.Value.DateTimeOffsets?.SingleOrDefault();
            Assert.That(updateDateValue, Is.EqualTo(dateTimeOffsetConverter.ToDateTimeOffset(content.UpdateDate)));

            var levelValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.Level)?.Value.Integers?.SingleOrDefault();
            Assert.That(levelValue, Is.EqualTo(content.Level));

            var sortOrderValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.SortOrder)?.Value.Integers?.SingleOrDefault();
            Assert.That(sortOrderValue, Is.EqualTo(content.SortOrder));
        });
    }
}