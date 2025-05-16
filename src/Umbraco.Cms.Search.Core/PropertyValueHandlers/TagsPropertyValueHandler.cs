using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Extensions;
using IndexValue = Umbraco.Cms.Search.Core.Models.Indexing.IndexValue;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class TagsPropertyValueHandler : IPropertyValueHandler, ICorePropertyValueHandler
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IDataTypeConfigurationCache _dataTypeConfigurationCache;

    public TagsPropertyValueHandler(IJsonSerializer jsonSerializer, IDataTypeConfigurationCache dataTypeConfigurationCache)
    {
        _jsonSerializer = jsonSerializer;
        _dataTypeConfigurationCache = dataTypeConfigurationCache;
    }

    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.Tags;

    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var values = ParsePropertyValue(content, property, culture, segment, published);
        return values?.Any() is true
            ? new IndexValue
            {
                Keywords = values
            }
            : null;
    }

    private string[]? ParsePropertyValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = content.GetValue<string>(property.Alias, culture, segment, published);
        if (value.IsNullOrWhiteSpace())
        {
            return null;
        }

        var tagConfiguration = _dataTypeConfigurationCache.GetConfigurationAs<TagConfiguration>(property.PropertyType.DataTypeKey)
                               ?? new TagConfiguration();
        tagConfiguration.Delimiter = tagConfiguration.Delimiter == default ? ',' : tagConfiguration.Delimiter;

        return tagConfiguration.StorageType switch
        {
            TagsStorageType.Json when value.DetectIsJson() => _jsonSerializer.Deserialize<string[]>(value),
            TagsStorageType.Csv => value.Split(tagConfiguration.Delimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            _ => null
        };
    }
}