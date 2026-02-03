namespace Umbraco.Cms.Search.Provider.Examine.Models.ViewModels;

public class DocumentViewModel
{
    public required Guid Key { get; set; }

    public string? Culture { get; set; }

    public required IReadOnlyDictionary<string, IReadOnlyCollection<string>> Fields { get; set; }
}
