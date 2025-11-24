namespace Umbraco.Cms.Search.Core.Models.Persistence;

public class Document
{
    public required Guid DocumentKey { get; set; }

    public required string Index { get; set; }

    public required string Fields { get; set; }
}
