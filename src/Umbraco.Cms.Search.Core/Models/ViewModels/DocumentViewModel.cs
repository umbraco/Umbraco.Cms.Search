using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Search.Core.Models.ViewModels;

public class DocumentViewModel
{
    public required Guid Id { get; set; }

    public required UmbracoObjectTypes ObjectType { get; set; }
}
