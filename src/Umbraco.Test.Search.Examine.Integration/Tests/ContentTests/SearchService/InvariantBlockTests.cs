using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

public class InvariantBlockTests : SearcherTestBase
{
    private IJsonSerializer JsonSerializer => GetRequiredService<IJsonSerializer>();

    private PropertyEditorCollection PropertyEditorCollection => GetRequiredService<PropertyEditorCollection>();

    private IConfigurationEditorJsonSerializer ConfigurationEditorJsonSerializer => GetRequiredService<IConfigurationEditorJsonSerializer>();


    [Test]
    public async Task Can_Search_TextBox_In_Block()
    {
        var indexAlias = GetIndexAlias(false);
        await CreateBlockContent();

        SearchResult results = await Searcher.SearchAsync(indexAlias, "testOne", null, null, null, null, null, null, 0, 100);

        Assert.That(results.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task Can_Search_TextArea_In_Block()
    {
        var indexAlias = GetIndexAlias(false);
        await CreateBlockContent();

        SearchResult results = await Searcher.SearchAsync(indexAlias, "testTwo", null, null, null, null, null, null, 0, 100);

        Assert.That(results.Total, Is.EqualTo(1));
    }

    private async Task CreateBlockContent()
    {
        ContentType elementType = ContentTypeBuilder.CreateAllTypesContentType("myElementType", "My Element Type");
        elementType.IsElement = true;
        ContentTypeService.Save(elementType);

        IContentType blockListContentType = await CreateBlockListContentType(elementType);

        var contentElementKey = Guid.NewGuid();
        var blockListValue = new BlockListValue
        {
            Layout = new Dictionary<string, IEnumerable<IBlockLayoutItem>>
            {
                {
                    Constants.PropertyEditors.Aliases.BlockList, [new BlockListLayoutItem { ContentKey = contentElementKey }]
                },
            },
            ContentData =
            [
                new BlockItemData
                {
                    Key = contentElementKey,
                    ContentTypeAlias = elementType.Alias,
                    ContentTypeKey = elementType.Key,
                    Values =
                    [
                        new BlockPropertyValue { Alias = "singleLineText", Value = "testOne" },
                        new BlockPropertyValue { Alias = "multilineText", Value = "testTwo" },
                    ],
                }
            ],
            Expose =
            [
                new BlockItemVariation(contentElementKey, null, null)
            ],
        };
        var blocksPropertyValue = JsonSerializer.Serialize(blockListValue);

        Content content = new ContentBuilder()
            .WithContentType(blockListContentType)
            .WithName("My Blocks")
            .WithPropertyValues(new {blocks = blocksPropertyValue})
            .Build();

        var indexAlias = GetIndexAlias(false);
        await WaitForIndexing(indexAlias, () =>
        {
            ContentService.Save(content);
            return Task.CompletedTask;
        });
    }

    private async Task<IContentType> CreateBlockListContentType(IContentType elementType)
    {
        var blockListDataType = new DataType(PropertyEditorCollection[Constants.PropertyEditors.Aliases.BlockList], ConfigurationEditorJsonSerializer)
        {
            ConfigurationData = new Dictionary<string, object>
            {
                {
                    "blocks",
                    new BlockListConfiguration.BlockConfiguration[]
                    {
                        new() { ContentElementTypeKey = elementType.Key }
                    }
                }
            },
            Name = "My Block List",
            DatabaseType = ValueStorageType.Ntext,
            ParentId = Constants.System.Root,
            CreateDate = DateTime.UtcNow
        };

        await DataTypeService.CreateAsync(blockListDataType, Constants.Security.SuperUserKey);

        var contentType = new ContentTypeBuilder()
            .WithAlias("myPage")
            .WithName("My Page")
            .AddPropertyType()
            .WithAlias("blocks")
            .WithName("Blocks")
            .WithDataTypeId(blockListDataType.Id)
            .Done()
            .Build();
        ContentTypeService.Save(contentType);

        // re-fetch to wire up all key bindings (particularly to the datatype)
        return await ContentTypeService.GetAsync(contentType.Key) ?? null!;
    }
}
