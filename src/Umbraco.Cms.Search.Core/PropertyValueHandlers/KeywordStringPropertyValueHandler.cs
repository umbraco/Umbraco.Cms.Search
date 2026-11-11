using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

internal sealed class KeywordStringPropertyValueHandler : IPropertyValueHandler, ICorePropertyValueHandler
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILogger<KeywordStringPropertyValueHandler> _logger;

    public KeywordStringPropertyValueHandler(IJsonSerializer jsonSerializer, ILogger<KeywordStringPropertyValueHandler> logger)
    {
        _jsonSerializer = jsonSerializer;
        _logger = logger;
    }

    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Cms.Core.Constants.PropertyEditors.Aliases.DropDownListFlexible or Cms.Core.Constants.PropertyEditors.Aliases.RadioButtonList or Cms.Core.Constants.PropertyEditors.Aliases.CheckBoxList;

    public bool CanHandle(IPropertyType propertyType)
        => CanHandle(propertyType.PropertyEditorAlias);

    public IEnumerable<IndexField> GetIndexFields(IProperty property, string? culture, string? segment, bool published, IContentBase contentContext)
    {
        var value = property.GetValue(culture, segment, published) as string;
        if (value.IsNullOrWhiteSpace())
        {
            return [];
        }

        var keywords = value.DetectIsJson()
            ? ParseJsonValue(value, property, culture, segment, contentContext)
            : [value];
        return keywords?.Length > 0
            ? [new IndexField(property.Alias, new IndexValue { Keywords = keywords }, culture, segment)]
            : [];
    }

    private string[]? ParseJsonValue(string value, IProperty property, string? culture, string? segment, IContentBase contentContext)
    {
        try
        {
            return _jsonSerializer.Deserialize<string[]>(value);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to deserialize keyword value for property {PropertyAlias} of content {ContentKey} (Culture: {Culture}, Segment: {Segment}) - skipped indexing of property value.",
                property.Alias,
                contentContext.Key,
                culture,
                segment);
            return null;
        }
    }
}
