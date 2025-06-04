using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Integration.Tests;

public class NoopPropertyValueHandlerTests : ContentTestBase
{
    [Test]
    public void AllNoopEditors_YieldNoValues()
    {
        var jsonSerializer = GetRequiredService<IJsonSerializer>();

        var content = new ContentBuilder()
            .WithContentType(GetContentType())
            .WithName("All Supported Editors")
            .WithPropertyValues(
                new
                {
                    emailValue = "some@email.com",
                    colorPickerWithLabelsValue = jsonSerializer.Serialize(new ColorPickerValueConverter.PickedColor("123456", "test")),
                    colorPickerWithoutLabelsValue = jsonSerializer.Serialize(new ColorPickerValueConverter.PickedColor("123456", "test")),
                    colorPickerEyeDropperValue = "123456"
                })
            .Build();

        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));

        var document = documents.Single();
        Assert.That(document.Fields.Any(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(document.Fields.Any(f => f.FieldName == "emailValue"), Is.False);
            Assert.That(document.Fields.Any(f => f.FieldName == "colorPickerWithLabelsValue"), Is.False);
            Assert.That(document.Fields.Any(f => f.FieldName == "colorPickerWithoutLabelsValue"), Is.False);
            Assert.That(document.Fields.Any(f => f.FieldName == "colorPickerEyeDropperValue"), Is.False);
        });

        // cross-check that the input values actually yielded the expected published values
        var publishedContent = GetRequiredService<IPublishedContentCache>().GetById(content.Key);
        Assert.That(publishedContent, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(publishedContent.Value<string>("emailValue"), Is.EqualTo("some@email.com"));
            Assert.That(publishedContent.Value<ColorPickerValueConverter.PickedColor>("colorPickerWithLabelsValue")?.Color, Is.EqualTo("123456"));
            Assert.That(publishedContent.Value<string>("colorPickerWithoutLabelsValue"), Is.EqualTo("123456"));
            Assert.That(publishedContent.Value<string>("colorPickerEyeDropperValue"), Is.EqualTo("123456"));
        });
    }

    [SetUp]
    public async Task SetupTest()
    {
        var dataTypeService = GetRequiredService<IDataTypeService>();

        var emailDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Email")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.EmailAddress)
            .Done()
            .Build();
        await dataTypeService.CreateAsync(emailDataType, Constants.Security.SuperUserKey);

        var colorPickerWithLabelsDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Color Picker (with labels)")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.ColorPicker)
            .Done()
            .Build();
        colorPickerWithLabelsDataType.ConfigurationData = new Dictionary<string, object>
        {
            { "useLabel", true },
            { "items", new [] { new { value = "123456", label = "test" } } }
        };
        await dataTypeService.CreateAsync(colorPickerWithLabelsDataType, Constants.Security.SuperUserKey);

        var colorPickerWithoutLabelsDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Color Picker (without labels)")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.ColorPicker)
            .Done()
            .Build();
        colorPickerWithoutLabelsDataType.ConfigurationData = new Dictionary<string, object>
        {
            { "useLabel", false },
            { "items", new [] { new { value = "123456", label = "test" } } }
        };
        await dataTypeService.CreateAsync(colorPickerWithoutLabelsDataType, Constants.Security.SuperUserKey);

        var colorPickerEyeDropperDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Color Picker Eye Dropper")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.ColorPickerEyeDropper)
            .Done()
            .Build();
        await dataTypeService.CreateAsync(colorPickerEyeDropperDataType, Constants.Security.SuperUserKey);

        var contentType = new ContentTypeBuilder()
            .WithAlias("allEditors")
            .AddPropertyType()
            .WithAlias("emailValue")
            .WithDataTypeId(emailDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.EmailAddress)
            .Done()
            .AddPropertyType()
            .WithAlias("colorPickerWithLabelsValue")
            .WithDataTypeId(colorPickerWithLabelsDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.ColorPicker)
            .Done()
            .AddPropertyType()
            .WithAlias("colorPickerWithoutLabelsValue")
            .WithDataTypeId(colorPickerWithoutLabelsDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.ColorPicker)
            .Done()
            .AddPropertyType()
            .WithAlias("colorPickerEyeDropperValue")
            .WithDataTypeId(colorPickerEyeDropperDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.ColorPickerEyeDropper)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);
    }

    private IContentType GetContentType() => ContentTypeService.Get("allEditors")
                                             ?? throw new InvalidOperationException("Could not find the content type");
}