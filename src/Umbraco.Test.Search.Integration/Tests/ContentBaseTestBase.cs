using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public abstract class ContentBaseTestBase : TestBase
{
    protected void VerifyDocumentStructureValues(TestIndexDocument document, Guid key, Guid parentKey, params Guid[] pathKeys)
        => Assert.Multiple(() =>
        {
            var idValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.Id)?.Value.Keywords?.SingleOrDefault();
            Assert.That(idValue, Is.EqualTo(key.ToString("D")));
            
            var parentIdValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.ParentId)?.Value.Keywords?.SingleOrDefault();
            Assert.That(parentIdValue, Is.EqualTo(parentKey.ToString("D")));

            var pathIdsValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.PathIds)?.Value.Keywords?.ToArray();
            Assert.That(pathIdsValue, Is.Not.Null);
            Assert.That(pathIdsValue.Length, Is.EqualTo(pathKeys.Length));
            Assert.That(pathIdsValue, Is.EquivalentTo(pathKeys.Select(ancestorId => ancestorId.ToString("D"))));
        });

    protected void VerifyDocumentSystemValues(TestIndexDocument document, IContentBase content, params string[] tags)
    {
        var dateTimeOffsetConverter = GetRequiredService<IDateTimeOffsetConverter>();

        Assert.Multiple(() =>
        {
            var contentTypeValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.ContentType)?.Value.Keywords?.SingleOrDefault();
            Assert.That(contentTypeValue, Is.EqualTo(content.ContentType.Alias));

            var nameValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.Name)?.Value.Texts?.SingleOrDefault();
            Assert.That(nameValue, Is.EqualTo(content.Name));

            var createDateValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.CreateDate)?.Value.DateTimeOffsets?.SingleOrDefault();
            Assert.That(createDateValue, Is.EqualTo(dateTimeOffsetConverter.ToDateTimeOffset(content.CreateDate)));

            var updateDateValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.UpdateDate)?.Value.DateTimeOffsets?.SingleOrDefault();
            Assert.That(updateDateValue, Is.EqualTo(dateTimeOffsetConverter.ToDateTimeOffset(content.UpdateDate)));

            var levelValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.Level)?.Value.Integers?.SingleOrDefault();
            Assert.That(levelValue, Is.EqualTo(content.Level));

            var sortOrderValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.SortOrder)?.Value.Integers?.SingleOrDefault();
            Assert.That(sortOrderValue, Is.EqualTo(content.SortOrder));

            var tagsValue = document.Fields.FirstOrDefault(f => f.FieldName == Constants.FieldNames.Tags)?.Value.Keywords;
            Assert.That(tagsValue ?? [], Is.EquivalentTo(tags));

            Assert.That(document.Protection, Is.Null);
        });
    }
}