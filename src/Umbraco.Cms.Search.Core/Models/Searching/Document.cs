using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Search.Core.Models.Searching;

public record Document(Guid Id, UmbracoObjectTypes ObjectType)
{
    public string? Name { get; init; }

    public string? Icon { get; init; }
}
