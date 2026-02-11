namespace Umbraco.Cms.Search.Provider.Examine.Models.ViewModels;

public class DocumentViewModel
{
    public required Guid Key { get; set; }

    public string? Culture { get; set; }

    public required IReadOnlyCollection<FieldViewModel> Fields { get; set; }
}
