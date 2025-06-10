using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.PropertyValueHandlers;
using Umbraco.Cms.Search.Core.PropertyValueHandlers.Collection;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Integration.Tests;

public class SimplePropertyValueHandlerTests : ContentTestBase
{
    [Test]
    public void AllSupportedEditors_CanBeIndexed()
    {
        var jsonSerializer = GetRequiredService<IJsonSerializer>();
        
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
                    tagsAsJsonValue = "[\"One\",\"Two\",\"Three\"]",
                    tagsAsCsvValue = "Four,Five,Six",
                    multipleTextstringsValue = "First\nSecond\nThird",
                    contentPickerValue = "udi://document/55bf7f6d-acd2-4f1e-92bd-f0b5c41dbfed",
                    booleanAsBooleanValue = true,
                    booleanAsIntegerValue = 1,
                    booleanAsStringValue = "1",
                    sliderSingleValue = "123.45",
                    sliderRangeValue = "123.45,567.89",
                    multiUrlPickerValue = jsonSerializer.Serialize(new []
                    {
                        new MultiUrlPickerValueEditor.LinkDto
                        {
                            Name = "Link One"
                        },
                        new MultiUrlPickerValueEditor.LinkDto
                        {
                            Name = "Link Two"
                        },
                        new MultiUrlPickerValueEditor.LinkDto
                        {
                            // should be ignored - but make sure we test it all the same
                            Name = null
                        }
                    }),
                })
            .Build();

        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
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

            var tagsAsJsonValue = document.Fields.FirstOrDefault(f => f.FieldName == "tagsAsJsonValue")?.Value.Keywords?.ToArray();
            CollectionAssert.AreEqual(tagsAsJsonValue, new [] {"One", "Two", "Three"});

            var tagsAsCsvValue = document.Fields.FirstOrDefault(f => f.FieldName == "tagsAsCsvValue")?.Value.Keywords?.ToArray();
            CollectionAssert.AreEqual(tagsAsCsvValue, new [] {"Four", "Five", "Six"});

            var allTagsValue = document.Fields.FirstOrDefault(f => f.FieldName == Cms.Search.Core.Constants.FieldNames.Tags)?.Value.Keywords?.ToArray();
            CollectionAssert.AreEquivalent(allTagsValue, new [] {"One", "Two", "Three", "Four", "Five", "Six"});

            var multipleTextstringsValue = document.Fields.FirstOrDefault(f => f.FieldName == "multipleTextstringsValue")?.Value.Texts?.ToArray();
            CollectionAssert.AreEqual(multipleTextstringsValue, new [] {"First", "Second", "Third"});

            var contentPickerValue = document.Fields.FirstOrDefault(f => f.FieldName == "contentPickerValue")?.Value.Keywords?.SingleOrDefault();
            CollectionAssert.AreEqual(contentPickerValue, "55bf7f6d-acd2-4f1e-92bd-f0b5c41dbfed");

            var booleanAsBooleanValue = document.Fields.FirstOrDefault(f => f.FieldName == "booleanAsBooleanValue")?.Value.Integers?.SingleOrDefault();
            Assert.That(booleanAsBooleanValue, Is.EqualTo(1));

            var booleanAsIntegerValue = document.Fields.FirstOrDefault(f => f.FieldName == "booleanAsIntegerValue")?.Value.Integers?.SingleOrDefault();
            Assert.That(booleanAsIntegerValue, Is.EqualTo(1));

            var booleanAsStringValue = document.Fields.FirstOrDefault(f => f.FieldName == "booleanAsStringValue")?.Value.Integers?.SingleOrDefault();
            Assert.That(booleanAsStringValue, Is.EqualTo(1));

            var sliderSingleValue = document.Fields.FirstOrDefault(f => f.FieldName == "sliderSingleValue")?.Value.Decimals?.SingleOrDefault();
            Assert.That(sliderSingleValue, Is.EqualTo(123.45m));

            var sliderRangeValue = document.Fields.FirstOrDefault(f => f.FieldName == "sliderRangeValue")?.Value.Decimals?.ToArray();
            CollectionAssert.AreEqual(sliderRangeValue, new[] { 123.45m, 567.89m });

            var multiUrlPickerValue = document.Fields.FirstOrDefault(f => f.FieldName == "multiUrlPickerValue")?.Value.Texts?.ToArray();
            CollectionAssert.AreEqual(multiUrlPickerValue, new[] { "Link One", "Link Two" });
        });
    }

    [Test]
    public void AllCorePropertyValueHandlers_HaveTheCorePropertyValueHandlerMarkerInterface()
    {
        var handlers = GetRequiredService<PropertyValueHandlerCollection>().ToArray();
        CollectionAssert.IsNotEmpty(handlers);
        Assert.That(handlers.All(handler => handler is ICorePropertyValueHandler), Is.True);
    }
    
    [SetUp]
    public async Task SetupTest()
    {
        var dataTypeService = GetRequiredService<IDataTypeService>();

        var decimalDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Decimal)
            .WithName("Decimal")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        await dataTypeService.CreateAsync(decimalDataType, Constants.Security.SuperUserKey);

        var tagsAsCsvDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Tags as CSV")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Tags)
            .Done()
            .Build();
        tagsAsCsvDataType.ConfigurationData = new Dictionary<string, object>
        {
            {"storageType", TagsStorageType.Csv}
        }; 
        await dataTypeService.CreateAsync(tagsAsCsvDataType, Constants.Security.SuperUserKey);

        var tagsAsJsonDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Tags as JSON")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Tags)
            .Done()
            .Build();
        tagsAsJsonDataType.ConfigurationData = new Dictionary<string, object>
        {
            {"storageType", TagsStorageType.Json}
        }; 
        await dataTypeService.CreateAsync(tagsAsCsvDataType, Constants.Security.SuperUserKey);

        var multipleTextstringsDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Multiple textstrings")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.MultipleTextstring)
            .Done()
            .Build();
        await dataTypeService.CreateAsync(multipleTextstringsDataType, Constants.Security.SuperUserKey);

        var contentPickerDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Content picker")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.ContentPicker)
            .Done()
            .Build();
        await dataTypeService.CreateAsync(contentPickerDataType, Constants.Security.SuperUserKey);

        var sliderSingleDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Slider single")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Slider)
            .Done()
            .Build();
        sliderSingleDataType.ConfigurationData = new Dictionary<string, object>
        {
            { "enableRange", false }
        };
        await dataTypeService.CreateAsync(sliderSingleDataType, Constants.Security.SuperUserKey);

        var sliderRangeDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Slider range")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Slider)
            .Done()
            .Build();
        sliderSingleDataType.ConfigurationData = new Dictionary<string, object>
        {
            { "enableRange", true }
        };
        await dataTypeService.CreateAsync(sliderRangeDataType, Constants.Security.SuperUserKey);

        var multiUrlPickerDataType = new DataTypeBuilder()
            .WithId(0)
            .WithDatabaseType(ValueStorageType.Nvarchar)
            .WithName("Multi URL picker")
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.MultiUrlPicker)
            .Done()
            .Build();
        await dataTypeService.CreateAsync(multiUrlPickerDataType, Constants.Security.SuperUserKey);

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
            .AddPropertyType()
            .WithAlias("tagsAsJsonValue")
            .WithDataTypeId(tagsAsJsonDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Tags)
            .Done()
            .AddPropertyType()
            .WithAlias("tagsAsCsvValue")
            .WithDataTypeId(tagsAsCsvDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Tags)
            .Done()
            .AddPropertyType()
            .WithAlias("multipleTextstringsValue")
            .WithDataTypeId(multipleTextstringsDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.MultipleTextstring)
            .Done()
            .AddPropertyType()
            .WithAlias("contentPickerValue")
            .WithDataTypeId(contentPickerDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.ContentPicker)
            .Done()
            .AddPropertyType()
            .WithAlias("booleanAsBooleanValue")
            .WithDataTypeId(Constants.DataTypes.Boolean)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .AddPropertyType()
            .WithAlias("booleanAsIntegerValue")
            .WithDataTypeId(Constants.DataTypes.Boolean)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .AddPropertyType()
            .WithAlias("booleanAsStringValue")
            .WithDataTypeId(Constants.DataTypes.Boolean)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .AddPropertyType()
            .WithAlias("sliderSingleValue")
            .WithDataTypeId(sliderSingleDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Slider)
            .Done()
            .AddPropertyType()
            .WithAlias("sliderRangeValue")
            .WithDataTypeId(sliderRangeDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Slider)
            .Done()
            .AddPropertyType()
            .WithAlias("multiUrlPickerValue")
            .WithDataTypeId(multiUrlPickerDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.MultiUrlPicker)
            .Done()
            .Build();
        await ContentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);
    }

    private IContentType GetContentType() => ContentTypeService.Get("allEditors")
                                             ?? throw new InvalidOperationException("Could not find the content type");
}
