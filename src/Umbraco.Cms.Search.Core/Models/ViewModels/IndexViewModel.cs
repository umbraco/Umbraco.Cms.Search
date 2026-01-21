namespace Umbraco.Cms.Search.Core.Models.ViewModels;

public class IndexViewModel
{
    public required string IndexAlias { get; set; }

    public long DocumentCount { get; set; }
}
