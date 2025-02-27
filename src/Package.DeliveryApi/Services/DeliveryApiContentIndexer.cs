using Microsoft.Extensions.Logging;
using Package.Helpers;
using Package.Models.Indexing;
using Package.Services.ContentIndexing;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models;
using IndexField = Package.Models.Indexing.IndexField;

namespace Package.DeliveryApi.Services;

internal sealed class DeliveryApiContentIndexer : IContentIndexer
{
    private readonly ContentIndexHandlerCollection _contentIndexHandlerCollection;
    private readonly IDateTimeOffsetConverter _dateTimeOffsetConverter;
    private readonly ILogger<DeliveryApiContentIndexer> _logger;

    public DeliveryApiContentIndexer(
        ContentIndexHandlerCollection contentIndexHandlerCollection,
        IDateTimeOffsetConverter dateTimeOffsetConverter,
        ILogger<DeliveryApiContentIndexer> logger)
    {
        _contentIndexHandlerCollection = contentIndexHandlerCollection;
        _dateTimeOffsetConverter = dateTimeOffsetConverter;
        _logger = logger;
    }

    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IContent content, string?[] cultures, bool published, CancellationToken cancellationToken)
    {
        var indexFieldsByIdentifier = new Dictionary<string, IndexField>();

        foreach (IContentIndexHandler handler in _contentIndexHandlerCollection)
        {
            // ignore the core handlers, as they've covered by the system fields
            if (handler.GetType().Namespace?.StartsWith("Umbraco.Cms.Api.Delivery") is true)
            {
                continue;
            }

            foreach (var culture in cultures)
            {
                var fields = handler.GetFields().ToArray();
                var fieldValues = handler.GetFieldValues(content, culture).ToArray();
                
                foreach (IndexFieldValue fieldValue in fieldValues)
                {
                    var identifier = $"{fieldValue.FieldName}|{culture}";;
                    if (indexFieldsByIdentifier.ContainsKey(identifier))
                    {
                        _logger.LogWarning(
                            "Duplicate field value found for field name {fieldName} (culture: {culture}) among the index handlers - first one wins.",
                            fieldValue.FieldName,
                            culture ?? "[null]");
                        continue;
                    }

                    var field = fields.FirstOrDefault(f => f.FieldName == fieldValue.FieldName);
                    if (field is null)
                    {
                        _logger.LogWarning("Field name {fieldName}  did not have a corresponding field definition from the index handler {indexHandler}", fieldValue.FieldName, handler.GetType().FullName);
                        continue;
                    }

                    IndexValue? indexValue = null;
                    switch (field.FieldType)
                    {
                        case FieldType.StringAnalyzed:
                        case FieldType.StringRaw:
                        case FieldType.StringSortable:
                            var texts = fieldValue.Values.OfType<string>().ToArray();
                            if (texts.Length > 0)
                            {
                                indexValue = new()
                                {
                                    Texts = texts
                                };
                            }
                            break;
                        case FieldType.Number:
                            var decimals = fieldValue.Values.OfType<decimal>()
                                .Union(fieldValue.Values.OfType<int>().Select(i => (decimal)i))
                                .ToArray();
                            if (decimals.Length > 0)
                            {
                                indexValue = new()
                                {
                                    Decimals = decimals
                                };
                            }
                            break;
                        case FieldType.Date:
                            var dateTimeOffsets = fieldValue.Values
                                .OfType<DateTime>()
                                .Select(_dateTimeOffsetConverter.ToDateTimeOffset)
                                .ToArray();
                            if (dateTimeOffsets.Length > 0)
                            {
                                indexValue = new()
                                {
                                    DateTimeOffsets = dateTimeOffsets
                                };
                            }
                            break;
                    }

                    if (indexValue is null)
                    {
                        continue;
                    }

                    indexFieldsByIdentifier[identifier] = new IndexField(fieldValue.FieldName, indexValue, culture, null);
                }
            }
        }

        return Task.FromResult(indexFieldsByIdentifier.Values.AsEnumerable());
    }
}