using Microsoft.Extensions.Logging;
using Package.Models.Indexing;
using Package.PropertyValueHandlers.Collection;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace Package.Services.ContentIndexing.Indexers;

internal sealed class PropertyValueFieldsContentIndexer : IContentIndexer
{
    private readonly PropertyValueHandlerCollection _propertyValueHandlerCollection;
    private readonly ILogger<PropertyValueFieldsContentIndexer> _logger;

    public PropertyValueFieldsContentIndexer(
        PropertyValueHandlerCollection propertyValueHandlerCollection,
        ILogger<PropertyValueFieldsContentIndexer> logger)
    {
        _propertyValueHandlerCollection = propertyValueHandlerCollection;
        _logger = logger;
    }

    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(IContent content, string?[] cultures, bool published, CancellationToken cancellationToken)
        => Task.FromResult(CollectPropertyValueFields(content, cultures, published));

    private IEnumerable<IndexField> CollectPropertyValueFields(IContent content, string?[] cultures, bool published)
    {
        var fields = new List<IndexField>();

        foreach (var property in content.Properties)
        {
            var handler = _propertyValueHandlerCollection.FirstOrDefault(handler => handler.CanHandle(property.PropertyType.PropertyEditorAlias));
            if (handler is null)
            {
                _logger.LogDebug(
                    "No property value handler found for property editor alias {propertyEditorAlias} - cannot index property value.",
                    property.PropertyType.PropertyEditorAlias);
                continue;
            }

            var propertyCultures = property.PropertyType.VariesByCulture()
                ? cultures
                : [null];

            var propertySegments = property.PropertyType.VariesBySegment()
                ? property.Values.Select(value => value.Segment).Distinct().ToArray()
                : [null];

            foreach (var culture in propertyCultures)
            {
                foreach (var segment in propertySegments)
                {
                    var value = handler.GetIndexValue(content, property, culture, segment, published);
                    if (value is null)
                    {
                        continue;
                    }

                    fields.Add(new IndexField(property.Alias, value, culture, segment));
                }
            }
        }

        return fields;
    }
}