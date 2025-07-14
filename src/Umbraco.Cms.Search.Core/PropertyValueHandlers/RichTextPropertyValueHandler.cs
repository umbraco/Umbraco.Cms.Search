using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.PropertyValueHandlers.Collection;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public class RichTextPropertyValueHandler : BlockEditorPropertyValueHandler, ICorePropertyValueHandler
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IHtmlIndexValueParser _htmlIndexValueParser;
    private readonly ILogger<RichTextPropertyValueHandler> _logger;

    public RichTextPropertyValueHandler(
        IJsonSerializer jsonSerializer,
        IContentTypeService contentTypeService,
        PropertyEditorCollection propertyEditorCollection,
        PropertyValueHandlerCollection propertyValueHandlerCollection,
        IHtmlIndexValueParser htmlIndexValueParser,
        ILogger<RichTextPropertyValueHandler> logger)
        : base(jsonSerializer, contentTypeService, propertyEditorCollection, propertyValueHandlerCollection, logger)
    {
        _htmlIndexValueParser = htmlIndexValueParser;
        _logger = logger;
        _jsonSerializer = jsonSerializer;
    }

    public override bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Cms.Core.Constants.PropertyEditors.Aliases.RichText;

    public override IEnumerable<IndexField> GetIndexFields(IProperty property, string? culture, string? segment, bool published, IContentBase contentContext)
    {
        var source = property.GetValue(culture, segment, published);
        if (RichTextPropertyEditorHelper.TryParseRichTextEditorValue(source, _jsonSerializer, _logger, out var richTextEditorValue) is false)
        {
            return [];
        }

        var blockIndexValues = richTextEditorValue.Blocks is not null
            ? GetCumulativeIndexValues(richTextEditorValue.Blocks.ContentData, richTextEditorValue.Blocks.Expose, property, culture, segment, published, contentContext)
            : new ();

        var htmlFieldValue = _htmlIndexValueParser.Parse(richTextEditorValue.Markup);
        if (htmlFieldValue is not null)
        {
            if (blockIndexValues.TryGetValue((culture, segment), out var fieldValue) is false)
            {
                fieldValue = new();
                blockIndexValues[(culture, segment)] = fieldValue;
            }

            AmendCumulativeIndexValue(fieldValue, htmlFieldValue);
        }

        return blockIndexValues.Count > 0
            ? ToIndexFields(blockIndexValues, property.Alias)
            : [];
    }
}
