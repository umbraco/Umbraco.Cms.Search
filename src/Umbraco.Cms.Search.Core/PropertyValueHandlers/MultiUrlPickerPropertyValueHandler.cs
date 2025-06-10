using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Extensions;
using IndexValue = Umbraco.Cms.Search.Core.Models.Indexing.IndexValue;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public class MultiUrlPickerPropertyValueHandler : IPropertyValueHandler, ICorePropertyValueHandler
{
    private readonly IJsonSerializer _jsonSerializer;

    public MultiUrlPickerPropertyValueHandler(IJsonSerializer jsonSerializer)
        => _jsonSerializer = jsonSerializer;

    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.MultiUrlPicker;

    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var texts = ParsePropertyValue(content, property, culture, segment, published);
        return texts is not null
            ? new IndexValue
            {
                Texts = texts
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

        try
        {
            var linkDtos = _jsonSerializer.Deserialize<MultiUrlPickerValueEditor.LinkDto[]>(value);
            return linkDtos?.Select(linkDto => linkDto.Name).WhereNotNull().ToArray();
        }
        catch
        {
            // silently fail - this is an invalid property value, expect it to be reported elsewhere
            return null;
        }
    }

}