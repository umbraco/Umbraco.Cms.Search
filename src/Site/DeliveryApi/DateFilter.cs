using System.Text.RegularExpressions;
using Umbraco.Cms.Api.Delivery.Querying.Filters;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models;

namespace Site.DeliveryApi;

public partial class DateFilter : ContainsFilterBase, IContentIndexHandler
{
    protected override string FieldName => "myDate";

    protected override Regex QueryParserRegex => MyDateRegex();

    [GeneratedRegex("myDate(?<operator>[><:]{1,2})(?<value>.*)", RegexOptions.IgnoreCase)]
    private static partial Regex MyDateRegex();

    // Indexing
    public IEnumerable<IndexFieldValue> GetFieldValues(IContent content, string? culture)
    {
        var dateTimeValue = content.GetValue<DateTime>("date");

        if (dateTimeValue == DateTime.UnixEpoch)
        {
            return Array.Empty<IndexFieldValue>();
        }

        return
        [
            new IndexFieldValue
            {
                FieldName = FieldName,
                Values = [dateTimeValue]
            }
        ];
    }

    public IEnumerable<IndexField> GetFields() =>
    [
        new IndexField
        {
            FieldName = FieldName,
            FieldType = FieldType.Date,
            VariesByCulture = false
        }
    ];
}
