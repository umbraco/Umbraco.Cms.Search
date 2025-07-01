using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Umbraco.Cms.Search.Core.PropertyValueHandlers;

public sealed class HtmlIndexValueParser : IHtmlIndexValueParser
{
    private readonly ILogger<HtmlIndexValueParser> _logger;

    public HtmlIndexValueParser(ILogger<HtmlIndexValueParser> logger)
        => _logger = logger;

    public IndexValue? Parse(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // all nodes of textual relevance to indexing is at document root level
            var children = doc.DocumentNode.ChildNodes;

            var h1 = children.Where(c => c.Name.InvariantEquals("h1")).ToArray();
            var h2 = children.Where(c => c.Name.InvariantEquals("h2")).ToArray();
            var h3 = children.Where(c => c.Name.InvariantEquals("h3")).ToArray();
            var texts = children.Except(h1.Union(h2).Union(h3)).ToArray();

            var indexValue = new IndexValue
            {
                TextsR1 = ParseNodes(h1).NullIfEmpty(),
                TextsR2 = ParseNodes(h2).NullIfEmpty(),
                TextsR3 = ParseNodes(h3).NullIfEmpty(),
                Texts = ParseNodes(texts).NullIfEmpty()
            };

            return indexValue.TextsR1 is not null
                || indexValue.TextsR2 is not null
                || indexValue.TextsR3 is not null
                || indexValue.Texts is not null
                ? indexValue
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not parse markdown HTML, see exception for details");
            return null;
        }

    }

    private string[] ParseNodes(HtmlNode[] nodes)
        => nodes
            .Select(node => node.InnerText.Replace('\n', ' ').Replace('\r', ' '))
            .Where(text => text.IsNullOrWhiteSpace() is false)
            .ToArray();
}