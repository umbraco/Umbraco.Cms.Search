using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Integration.Tests;

public class PropertyValueHandlerTests : TestBase
{
    [Test]
    public void AllSupportedEditors_CanBeIndexed()
    {
        var content = new ContentBuilder()
            .WithContentType(GetContentType())
            .WithName("All Supported Editors")
            .WithPropertyValues(
                new
                {
                    textBoxValue = "The TextBox value",
                    textAreaValue = "The TextArea value",
                    integerValue = 1234,
                    decimalValue = 56.78m,
                    dateValue = new DateTime(2001, 02, 03),
                    dateAndTimeValue = new DateTime(2004, 05, 06, 07, 08, 09),
                })
            .Build();

        ContentService.SaveAndPublish(content);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));

        var document = documents.Single();
        Assert.Multiple(() =>
        {
            var textBoxValue = document.Fields.FirstOrDefault(f => f.FieldName == "textBoxValue")?.Value.Texts?.SingleOrDefault();
            Assert.That(textBoxValue, Is.EqualTo("The TextBox value"));

            var textAreaValue = document.Fields.FirstOrDefault(f => f.FieldName == "textAreaValue")?.Value.Texts?.SingleOrDefault();
            Assert.That(textAreaValue, Is.EqualTo("The TextArea value"));

            var integerValue = document.Fields.FirstOrDefault(f => f.FieldName == "integerValue")?.Value.Integers?.SingleOrDefault();
            Assert.That(integerValue, Is.EqualTo(1234));

            var decimalValue = document.Fields.FirstOrDefault(f => f.FieldName == "decimalValue")?.Value.Decimals?.SingleOrDefault();
            Assert.That(decimalValue, Is.EqualTo(56.78m));

            var dateValue = document.Fields.FirstOrDefault(f => f.FieldName == "dateValue")?.Value.DateTimeOffsets?.SingleOrDefault();
            Assert.That(dateValue, Is.EqualTo(new DateTimeOffset(new DateOnly(2001, 02, 03), new TimeOnly(), TimeSpan.Zero)));

            var dateAndTimeValue = document.Fields.FirstOrDefault(f => f.FieldName == "dateAndTimeValue")?.Value.DateTimeOffsets?.SingleOrDefault();
            Assert.That(dateAndTimeValue, Is.EqualTo(new DateTimeOffset(new DateOnly(2004, 05, 06), new TimeOnly(07, 08, 09), TimeSpan.Zero)));
        });
    }
    
    [SetUp]
    public void SetupTest()
    {
        var dateTypeService = GetRequiredService<IDataTypeService>();

        var decimalDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Decimal)
            .WithName("Decimal")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        dateTypeService.Save(decimalDataType);
        
        var contentType = new ContentTypeBuilder()
            .WithAlias("allEditors")
            .AddPropertyType()
            .WithAlias("textBoxValue")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .AddPropertyType()
            .WithAlias("textAreaValue")
            .WithDataTypeId(Constants.DataTypes.Textarea)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextArea)
            .Done()
            .AddPropertyType()
            .WithAlias("integerValue")
            .WithDataTypeId(-51)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .AddPropertyType()
            .WithAlias("decimalValue")
            .WithDataTypeId(decimalDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .AddPropertyType()
            .WithAlias("dateValue")
            .WithDataTypeId(-41)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DateTime)
            .Done()
            .AddPropertyType()
            .WithAlias("dateAndTimeValue")
            .WithDataTypeId(Constants.DataTypes.DateTime)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DateTime)
            .Done()
            .Build();
        ContentTypeService.Save(contentType);
    }

    private IContentType GetContentType() => ContentTypeService.Get("allEditors")
                                             ?? throw new InvalidOperationException("Could not find the content type");
}