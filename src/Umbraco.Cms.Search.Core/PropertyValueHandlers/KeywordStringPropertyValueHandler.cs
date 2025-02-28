using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class KeywordStringPropertyValueHandler : IPropertyValueHandler
{
    private readonly IJsonSerializer _jsonSerializer;

    public KeywordStringPropertyValueHandler(IJsonSerializer jsonSerializer)
        => _jsonSerializer = jsonSerializer;

    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Constants.PropertyEditors.Aliases.DropDownListFlexible or Constants.PropertyEditors.Aliases.RadioButtonList or Constants.PropertyEditors.Aliases.CheckBoxList;

    public IndexValue? GetIndexValue(IContent content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = content.GetValue<string>(property.Alias, culture, segment, published);
        if (value.IsNullOrWhiteSpace())
        {
            return null;
        }

        var keywords = _jsonSerializer.Deserialize<string[]>(value);
        return keywords?.Length > 0
            ? new IndexValue
            {
                Keywords = keywords
            }
            : null;
    }
}