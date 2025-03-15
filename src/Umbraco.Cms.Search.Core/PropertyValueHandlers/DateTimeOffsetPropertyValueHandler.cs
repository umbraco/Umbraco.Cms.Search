using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class DateTimeOffsetPropertyValueHandler : IPropertyValueHandler
{
    private readonly IDateTimeOffsetConverter _dateTimeOffsetConverter;

    public DateTimeOffsetPropertyValueHandler(IDateTimeOffsetConverter dateTimeOffsetConverter)
        => _dateTimeOffsetConverter = dateTimeOffsetConverter;

    // TODO: include Umbraco.Plain.Date in V15
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Cms.Core.Constants.PropertyEditors.Aliases.DateTime;

    public IndexValue? GetIndexValue(IContentBase content, IProperty property, string? culture, string? segment, bool published)
    {
        var value = content.GetValue<DateTime?>(property.Alias, culture, segment, published);
        return value.HasValue
            ? new IndexValue
            {
                DateTimeOffsets = [_dateTimeOffsetConverter.ToDateTimeOffset(value.Value)]
            }
            : null;
    }
}