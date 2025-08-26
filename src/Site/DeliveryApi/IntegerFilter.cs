using System.Text.RegularExpressions;
using Umbraco.Cms.Api.Delivery.Querying.Filters;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models;

namespace Site.DeliveryApi;

public partial class IntegerFilter : ContainsFilterBase, IContentIndexHandler
{
    protected override string FieldName => "myInteger";

    protected override Regex QueryParserRegex => MyIntegerRegex();

    [GeneratedRegex("myInteger(?<operator>[><:]{1,2})(?<value>.*)", RegexOptions.IgnoreCase)]
    private static partial Regex MyIntegerRegex();

    // Indexing
    public IEnumerable<IndexFieldValue> GetFieldValues(IContent content, string? culture)
    {
        var integerValue = content.GetValue<int>("integer");

        if (integerValue == 0)
        {
            return [];
        }

        return
        [
            new IndexFieldValue
            {
                FieldName = FieldName,
                Values = [integerValue]
            }
        ];
    }

    public IEnumerable<IndexField> GetFields() =>
    [
        new IndexField
        {
            FieldName = FieldName,
            FieldType = FieldType.Number,
            VariesByCulture = false
        }
    ];
}
