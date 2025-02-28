using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

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
            VerifyDocumentSystemValues(documents[0], Root());
            VerifyDocumentSystemValues(documents[1], Child());
            VerifyDocumentSystemValues(documents[2], Grandchild());
            VerifyDocumentSystemValues(documents[3], GreatGrandchild());
        });
    }

    private void VerifyDocumentPropertyValues(TestIndexDocument document, string title, int count)
        => VerifyDocumentPropertyValues(document, $"{title} in English", $"{title} in Danish", count);

    private void VerifyDocumentPropertyValues(TestIndexDocument document, string englishTitle, string danishTitle, int count)
        => Assert.Multiple(() =>
        {
            var titleFields = document.Fields.Where(f => f.FieldName == "title").ToArray();
            Assert.That(titleFields.Length, Is.EqualTo(2));
            Assert.That(titleFields.SingleOrDefault(f => f.Culture.InvariantEquals("en-US"))?.Value.Texts?.SingleOrDefault(), Is.EqualTo(englishTitle));
            Assert.That(titleFields.SingleOrDefault(f => f.Culture.InvariantEquals("da-DK"))?.Value.Texts?.SingleOrDefault(), Is.EqualTo(danishTitle));
            
            var countValue = document.Fields.FirstOrDefault(f => f.FieldName == "count")?.Value.Integers?.SingleOrDefault();
            Assert.That(countValue, Is.EqualTo(count));
        });

    private void VerifyDocumentSystemValues(TestIndexDocument document, IContent content)
    {
        var dateTimeOffsetConverter = new DateTimeOffsetConverter();

        Assert.Multiple(() =>
        {
            var contentTypeValue = document.Fields.FirstOrDefault(f => f.FieldName == IndexConstants.FieldNames.ContentType)?.Value.Keywords?.SingleOrDefault();
            Assert.That(contentTypeValue, Is.EqualTo(content.ContentType.Alias));

            var nameFields = document.Fields.Where(f => f.FieldName == IndexConstants.FieldNames.Name).ToArray();
            Assert.That(nameFields.Length, Is.EqualTo(2));
            Assert.That(nameFields.SingleOrDefault(f => f.Culture.InvariantEquals("en-US"))?.Value.Texts?.SingleOrDefault(), Is.EqualTo(content.GetCultureName("en-US")));
            Assert.That(nameFields.SingleOrDefault(f => f.Culture.InvariantEquals("da-DK"))?.Value.Texts?.SingleOrDefault(), Is.EqualTo(content.GetCultureName("da-DK")));

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