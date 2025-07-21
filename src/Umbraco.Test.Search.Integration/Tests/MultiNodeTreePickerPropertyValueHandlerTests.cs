﻿using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Integration.Tests;

public class MultiNodeTreePickerPropertyValueHandlerTests : ContentTestBase
{
    private IContentType _contentType;

    [Test]
    public void ExplicitDocumentPicker_CanBeIndexed()
    {
        var content = new ContentBuilder()
            .WithContentType(_contentType)
            .WithName("MultiNode Tree Picker")
            .WithPropertyValues(
                new
                {
                    explicitPickerValue = "umb://document/7c7ad126bdbc46c18cc1c281bf575d97,umb://document/b9cc8a2e9a024bbeb0ca38e197316517"
                })
            .Build();

        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));

        var document = documents.Single();
        var explicitPickerValue = document.Fields.FirstOrDefault(f => f.FieldName == "explicitPickerValue")?.Value.Keywords;
        Assert.That(explicitPickerValue, Is.EqualTo(new [] {"7c7ad126-bdbc-46c1-8cc1-c281bf575d97", "b9cc8a2e-9a02-4bbe-b0ca-38e197316517"}).AsCollection);
    }

    [Test]
    public void ImplicitDocumentPicker_CanBeIndexed()
    {
        var content = new ContentBuilder()
            .WithContentType(_contentType)
            .WithName("MultiNode Tree Picker")
            .WithPropertyValues(
                new
                {
                    implicitPickerValue = "umb://document/7c7ad126bdbc46c18cc1c281bf575d97,umb://document/b9cc8a2e9a024bbeb0ca38e197316517"
                })
            .Build();

        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));

        var document = documents.Single();
        var implicitPickerValue = document.Fields.FirstOrDefault(f => f.FieldName == "implicitPickerValue")?.Value.Keywords;
        Assert.That(implicitPickerValue, Is.EqualTo(new [] {"7c7ad126-bdbc-46c1-8cc1-c281bf575d97", "b9cc8a2e-9a02-4bbe-b0ca-38e197316517"}).AsCollection);
    }

    [Test]
    public void NonDocumentPicker_IsIgnored()
    {
        var content = new ContentBuilder()
            .WithContentType(_contentType)
            .WithName("MultiNode Tree Picker")
            .WithPropertyValues(
                new
                {
                    mediaPickerValue = "umb://media/7c7ad126bdbc46c18cc1c281bf575d97,umb://media/b9cc8a2e9a024bbeb0ca38e197316517",
                    memberPickerValue = "umb://member/7c7ad126bdbc46c18cc1c281bf575d97,umb://member/b9cc8a2e9a024bbeb0ca38e197316517"
                })
            .Build();

        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));

        var document = documents.Single();
        var mediaPickerValue = document.Fields.FirstOrDefault(f => f.FieldName == "mediaPickerValue");
        Assert.That(mediaPickerValue, Is.Null);
        var memberPickerValue = document.Fields.FirstOrDefault(f => f.FieldName == "memberPickerValue");
        Assert.That(memberPickerValue, Is.Null);
    }

    [Test]
    public void ExplicitDocumentPicker_DisregardsInvalidValues()
    {
        var content = new ContentBuilder()
            .WithContentType(_contentType)
            .WithName("MultiNode Tree Picker")
            .WithPropertyValues(
                new
                {
                    explicitPickerValue = "umb://media/7c7ad126bdbc46c18cc1c281bf575d97,umb://document/b9cc8a2e9a024bbeb0ca38e197316517"
                })
            .Build();

        ContentService.Save(content);
        ContentService.Publish(content, ["*"]);

        var documents = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(1));

        var document = documents.Single();
        var explicitPickerValue = document.Fields.FirstOrDefault(f => f.FieldName == "explicitPickerValue")?.Value.Keywords;
        Assert.That(explicitPickerValue, Is.EqualTo(new [] {"b9cc8a2e-9a02-4bbe-b0ca-38e197316517"}).AsCollection);
    }

    [SetUp]
    protected async Task CreateAllLabelEditorsContentType()
    {
        var dataTypeService = GetRequiredService<IDataTypeService>();
        var propertyEditorCollection = GetRequiredService<PropertyEditorCollection>();
        var configurationEditorJsonSerializer = GetRequiredService<IConfigurationEditorJsonSerializer>();

        var implicitPickerDataType = new DataType(propertyEditorCollection[Constants.PropertyEditors.Aliases.MultiNodeTreePicker], configurationEditorJsonSerializer)
        {
            Name = "Document picker (implicit)",
            DatabaseType = ValueStorageType.Ntext,
            ParentId = Constants.System.Root,
            CreateDate = DateTime.UtcNow
        };
        await dataTypeService.CreateAsync(implicitPickerDataType, Constants.Security.SuperUserKey);

        var explicitPickerDataType = new DataType(propertyEditorCollection[Constants.PropertyEditors.Aliases.MultiNodeTreePicker], configurationEditorJsonSerializer)
        {
            ConfigurationData = new Dictionary<string, object>
            {
                {
                    "startNode",
                    new MultiNodePickerConfigurationTreeSource
                    {
                        ObjectType = Constants.ObjectTypes.Strings.Document
                    }
                }
            },
            Name = "Document picker (explicit)",
            DatabaseType = ValueStorageType.Ntext,
            ParentId = Constants.System.Root,
            CreateDate = DateTime.UtcNow
        };
        await dataTypeService.CreateAsync(explicitPickerDataType, Constants.Security.SuperUserKey);

        var mediaPickerDataType = new DataType(propertyEditorCollection[Constants.PropertyEditors.Aliases.MultiNodeTreePicker], configurationEditorJsonSerializer)
        {
            ConfigurationData = new Dictionary<string, object>
            {
                {
                    "startNode",
                    new MultiNodePickerConfigurationTreeSource
                    {
                        ObjectType = Constants.ObjectTypes.Strings.Media
                    }
                }
            },
            Name = "Media picker",
            DatabaseType = ValueStorageType.Ntext,
            ParentId = Constants.System.Root,
            CreateDate = DateTime.UtcNow
        };
        await dataTypeService.CreateAsync(explicitPickerDataType, Constants.Security.SuperUserKey);

        var memberPickerDataType = new DataType(propertyEditorCollection[Constants.PropertyEditors.Aliases.MultiNodeTreePicker], configurationEditorJsonSerializer)
        {
            ConfigurationData = new Dictionary<string, object>
            {
                {
                    "startNode",
                    new MultiNodePickerConfigurationTreeSource
                    {
                        ObjectType = Constants.ObjectTypes.Strings.Member
                    }
                }
            },
            Name = "Member picker",
            DatabaseType = ValueStorageType.Ntext,
            ParentId = Constants.System.Root,
            CreateDate = DateTime.UtcNow
        };
        await dataTypeService.CreateAsync(explicitPickerDataType, Constants.Security.SuperUserKey);

        _contentType = new ContentTypeBuilder()
            .WithAlias("allMultiNodeTreePickerEditors")
            .AddPropertyType()
            .WithAlias("implicitPickerValue")
            .WithDataTypeId(implicitPickerDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.MultiNodeTreePicker)
            .Done()
            .AddPropertyType()
            .WithAlias("explicitPickerValue")
            .WithDataTypeId(explicitPickerDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.MultiNodeTreePicker)
            .Done()
            .AddPropertyType()
            .WithAlias("mediaPickerValue")
            .WithDataTypeId(mediaPickerDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.MultiNodeTreePicker)
            .Done()
            .AddPropertyType()
            .WithAlias("memberPickerValue")
            .WithDataTypeId(memberPickerDataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.MultiNodeTreePicker)
            .Done()
            .Build();

        await ContentTypeService.CreateAsync(_contentType, Constants.Security.SuperUserKey);

        Indexer.Reset();
    }
}