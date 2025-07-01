using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.PropertyValueHandlers.Collection;
using Umbraco.Extensions;
using IndexValue = Umbraco.Cms.Search.Core.Models.Indexing.IndexValue;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public abstract class BlockEditorPropertyValueHandler : IPropertyValueHandler
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IContentTypeService _contentTypeService;
    private readonly PropertyEditorCollection _propertyEditorCollection;
    private readonly PropertyValueHandlerCollection _propertyValueHandlerCollection;
    private readonly ILogger<BlockEditorPropertyValueHandler> _logger;

    protected BlockEditorPropertyValueHandler(
        IJsonSerializer jsonSerializer,
        IContentTypeService contentTypeService,
        PropertyEditorCollection propertyEditorCollection,
        PropertyValueHandlerCollection propertyValueHandlerCollection,
        ILogger<BlockEditorPropertyValueHandler> logger)
    {
        _jsonSerializer = jsonSerializer;
        _contentTypeService = contentTypeService;
        _propertyEditorCollection = propertyEditorCollection;
        _propertyValueHandlerCollection = propertyValueHandlerCollection;
        _logger = logger;
    }

    public abstract bool CanHandle(string propertyEditorAlias);

    public virtual IEnumerable<IndexField> GetIndexFields(IProperty property, string? culture, string? segment, bool published, IContentBase contentContext)
    {
        var blockValue = ParsePropertyValue(property, culture, segment, published);
        if (blockValue is null || blockValue.ContentData.Count == 0)
        {
            return [];
        }

        var blockIndexValues = GetCumulativeIndexValues(blockValue, property, culture, segment, published, contentContext);
        return blockIndexValues.Select(kvp =>
                kvp.Value.TextsR1.Count > 0
                || kvp.Value.TextsR2.Count > 0
                || kvp.Value.TextsR3.Count > 0
                || kvp.Value.Texts.Count > 0
                || kvp.Value.Keywords.Count > 0
                || kvp.Value.Integers.Count > 0
                || kvp.Value.Decimals.Count > 0
                || kvp.Value.DateTimeOffsets.Count > 0
                    ? new IndexField(
                        property.Alias,
                        new IndexValue
                        {
                            TextsR1 = kvp.Value.TextsR1.NullIfEmpty(),
                            TextsR2 = kvp.Value.TextsR2.NullIfEmpty(),
                            TextsR3 = kvp.Value.TextsR3.NullIfEmpty(),
                            Texts = kvp.Value.Texts.NullIfEmpty(),
                            Keywords = kvp.Value.Keywords.NullIfEmpty(),
                            Integers = kvp.Value.Integers.NullIfEmpty(),
                            Decimals = kvp.Value.Decimals.NullIfEmpty(),
                            DateTimeOffsets = kvp.Value.DateTimeOffsets.NullIfEmpty(),
                        },
                        kvp.Key.Culture,
                        kvp.Key.Segment
                    )
                    : null
            )
            .WhereNotNull()
            .ToArray();
    }

    private BlockValue? ParsePropertyValue(IProperty property, string? culture, string? segment, bool published)
    {
        var value = property.GetValue(culture, segment, published) as string;
        return value?.DetectIsJson() is true
            ? _jsonSerializer.Deserialize<BlockValue>(value)
            : null;
    }

    protected Dictionary<(string? Culture, string? Segment), CumulativeIndexValue> GetCumulativeIndexValues(
        BlockValue blockValue,
        IProperty property,
        string? culture,
        string? segment,
        bool published,
        IContentBase contentContext)
    {
        // block level variance can cause invariant culture to expand into multiple concrete cultures
        var propertyCultures = GetPropertyCultures(property.PropertyType, culture, published, contentContext);

        // load all the contained element types up front
        var elementTypesByKey = _contentTypeService
            .GetMany(blockValue.ContentData.Select(cd => cd.ContentTypeKey).Distinct())
            .ToDictionary(c => c.Key);

        // these are the cumulative index values (for all contained blocks) per contained variation
        var cumulativeIndexValuesByVariation = new Dictionary<(string? Culture, string? Segment), CumulativeIndexValue>();

        foreach (var contentData in blockValue.ContentData)
        {
            var propertyTypesByAlias = GetPropertyTypesByAlias(contentData.ContentTypeKey, elementTypesByKey, culture, segment);
            if (propertyTypesByAlias is null)
            {
                continue;
            }

            foreach (var propertyCulture in propertyCultures)
            {
                foreach (var blockPropertyValue in contentData.Values.Where(value => value.Culture.InvariantEquals(propertyCulture)))
                {
                    if (published
                        && propertyCulture is not null
                        && blockValue.Expose.Any(e =>
                            e.ContentKey == contentData.Key &&
                            e.Culture.InvariantEquals(blockPropertyValue.Culture) &&
                            e.Segment.InvariantEquals(blockPropertyValue.Segment)) is false)
                    {
                        // un-exposed blocks should not be included in published indexing
                        continue;
                    }

                    if (propertyTypesByAlias.TryGetValue(blockPropertyValue.Alias, out var propertyType) is false)
                    {
                        // this is to be expected, if the property type has been removed from
                        // the element type after the block creation
                        continue;
                    }

                    var editor = _propertyEditorCollection[propertyType.PropertyEditorAlias];
                    if (editor is null)
                    {
                        _logger.LogDebug(
                            "No property editor found for property editor alias {propertyEditorAlias} - skipped indexing of property value.",
                            property.PropertyType.PropertyEditorAlias);
                        continue;
                    }

                    var blockProperty = new Property(propertyType);
                    if (propertyType.VariesByCulture() && propertyCulture is null)
                    {
                        continue;
                    }

                    blockProperty.SetValue(blockPropertyValue.Value, propertyCulture, segment);
                    if (published)
                    {
                        blockProperty.PublishValues(propertyCulture ?? "*", segment ?? "*");
                    }

                    var blockPropertyValueHandler = _propertyValueHandlerCollection.GetPropertyValueHandler(propertyType);
                    if (blockPropertyValueHandler is null)
                    {
                        _logger.LogDebug(
                            "No property value handler found for property editor alias {propertyEditorAlias} - skipped indexing of property value.",
                            property.PropertyType.PropertyEditorAlias);
                        continue;
                    }

                    var blockPropertyIndexFields = blockPropertyValueHandler
                        .GetIndexFields(blockProperty, propertyCulture, segment, published, contentContext)
                        .ToArray();

                    foreach (var blockPropertyIndexField in blockPropertyIndexFields)
                    {
                        if (cumulativeIndexValuesByVariation.TryGetValue((blockPropertyIndexField.Culture, blockPropertyIndexField.Segment), out var blockIndexValue) is false)
                        {
                            blockIndexValue = new CumulativeIndexValue();
                            cumulativeIndexValuesByVariation.Add((blockPropertyIndexField.Culture, blockPropertyIndexField.Segment), blockIndexValue);
                        }
                        blockIndexValue.TextsR1.AddRange(blockPropertyIndexField.Value.TextsR1.EmptyNull());
                        blockIndexValue.TextsR2.AddRange(blockPropertyIndexField.Value.TextsR2.EmptyNull());
                        blockIndexValue.TextsR3.AddRange(blockPropertyIndexField.Value.TextsR3.EmptyNull());
                        blockIndexValue.Texts.AddRange(blockPropertyIndexField.Value.Texts.EmptyNull());
                        blockIndexValue.Keywords.AddRange(blockPropertyIndexField.Value.Keywords.EmptyNull());
                        blockIndexValue.Integers.AddRange(blockPropertyIndexField.Value.Integers.EmptyNull());
                        blockIndexValue.Decimals.AddRange(blockPropertyIndexField.Value.Decimals.EmptyNull());
                        blockIndexValue.DateTimeOffsets.AddRange(blockPropertyIndexField.Value.DateTimeOffsets.EmptyNull());
                    }
                }
            }
        }

        return cumulativeIndexValuesByVariation;
    }

    private string?[] GetPropertyCultures(IPropertyType propertyType, string? requestedCulture, bool published, IContentBase contentContext)
    {
        // block level variance can cause invariant culture to expand into multiple concrete cultures
        var propertyCultures = propertyType.VariesByCulture()
            ? [requestedCulture]
            : contentContext.ContentType.VariesByCulture()
                ? published
                    ? contentContext.PublishedCultures()
                    : contentContext.AvailableCultures().ToArray()
                : [requestedCulture];
        if (propertyCultures.Contains(null) is false)
        {
            // don't forget the invariant culture
            propertyCultures = propertyCultures.Union([null]).ToArray();
        }

        return propertyCultures;
    }

    private Dictionary<string, IPropertyType>? GetPropertyTypesByAlias(Guid elementTypeKey, Dictionary<Guid, IContentType> elementTypes, string? requestedCulture, string? requestedSegment)
    {
        if (elementTypes.TryGetValue(elementTypeKey, out var elementType) is false)
        {
            return null;
        }

        return elementType
            .CompositionPropertyTypes
            .Select(propertyType =>
            {
                // We want to ensure that the nested properties are set to correct variation if the requested variation is explicit.
                // This is because it's perfectly valid to have a nested property type that's set to invariant even if the parent property varies.
                // For instance in a block list, the list itself can vary, but the elements can be invariant, at the same time.
                if (requestedCulture is not null)
                {
                    propertyType.Variations |= ContentVariation.Culture;
                }

                if (requestedSegment is not null)
                {
                    propertyType.Variations |= ContentVariation.Segment;
                }

                return propertyType;
            })
            .ToDictionary(x => x.Alias);
    }
    
    protected class BlockValue
    {
        public required List<BlockItemData> ContentData { get; init; }

        public required List<BlockItemVariation> Expose { get; init; }
    }

    protected record CumulativeIndexValue
    {
        public List<string> TextsR1 { get; } = [];

        public List<string> TextsR2 { get; } = [];

        public List<string> TextsR3 { get; } = [];

        public List<string> Texts { get; } = [];

        public List<string> Keywords { get; } = [];

        public List<int> Integers { get; } = [];

        public List<decimal> Decimals { get; } = [];

        public List<DateTimeOffset> DateTimeOffsets { get; } = [];
    }
}