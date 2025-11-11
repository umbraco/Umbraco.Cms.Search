namespace Umbraco.Cms.Search.Core.Configuration;

public class IndexSettings
{
    public required string[] IncludeRebuildWhenLanguageDeleted { get; set; } = [Constants.IndexAliases.DraftContent, Constants.IndexAliases.PublishedContent];
}
