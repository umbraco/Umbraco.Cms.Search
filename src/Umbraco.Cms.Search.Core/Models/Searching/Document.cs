using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Search.Core.Models.Searching;

public record Document(Guid Key, UmbracoObjectTypes ObjectType)
{
}