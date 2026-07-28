using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Search.Core.Helpers;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

internal sealed class DateTimeOffsetPropertyValueHandler : IPropertyValueHandler, ICorePropertyValueHandler
{
    private readonly IDateTimeOffsetConverter _dateTimeOffsetConverter;
    private readonly IJsonSerializer _jsonSerializer;

    public DateTimeOffsetPropertyValueHandler(IDateTimeOffsetConverter dateTimeOffsetConverter, IJsonSerializer jsonSerializer)
    {
        _dateTimeOffsetConverter = dateTimeOffsetConverter;
        _jsonSerializer = jsonSerializer;
    }

    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Cms.Core.Constants.PropertyEditors.Aliases.DateTime
            or Cms.Core.Constants.PropertyEditors.Aliases.PlainDateTime
            or Cms.Core.Constants.PropertyEditors.Aliases.DateOnly
            or Cms.Core.Constants.PropertyEditors.Aliases.TimeOnly
            or Cms.Core.Constants.PropertyEditors.Aliases.DateTimeWithTimeZone
            or Cms.Core.Constants.PropertyEditors.Aliases.DateTimeUnspecified;

    public IEnumerable<IndexField> GetIndexFields(IProperty property, string? culture, string? segment, bool published, IContentBase contentContext)
    {
        DateTimeOffset? dateTimeOffset = property.GetValue(culture, segment, published) switch
        {
            DateTime dateTime => _dateTimeOffsetConverter.ToDateTimeOffset(dateTime),
            string jsonValue => ParseDateTimeOffset(jsonValue),
            _ => null
        };

        return dateTimeOffset is not null
            ? [new IndexField(property.Alias, new IndexValue { DateTimeOffsets = [dateTimeOffset.Value] }, culture, segment)]
            : [];
    }

    private DateTimeOffset? ParseDateTimeOffset(string jsonValue)
    {
        try
        {
            return _jsonSerializer.Deserialize<DateTimeValueConverterBase.DateTimeDto>(jsonValue)?.Date;
        }
        catch
        {
            // silently fail - this is an invalid property value, expect it to be reported elsewhere
            return null;
        }
    }
}
