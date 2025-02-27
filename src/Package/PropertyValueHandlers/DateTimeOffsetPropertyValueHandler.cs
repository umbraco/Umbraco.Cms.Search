using Package.Helpers;
using Package.Models.Indexing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;

namespace Package.PropertyValueHandlers;

public sealed class DateTimeOffsetPropertyValueHandler : IPropertyValueHandler
{
    private readonly IDateTimeOffsetConverter _dateTimeOffsetConverter;

    public DateTimeOffsetPropertyValueHandler(IDateTimeOffsetConverter dateTimeOffsetConverter)
        => _dateTimeOffsetConverter = dateTimeOffsetConverter;

    // TODO: include Umbraco.Plain.Date in V15
    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Constants.PropertyEditors.Aliases.DateTime;

    public IndexValue? GetIndexValue(IContent content, IProperty property, string? culture, string? segment, bool published)
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