using Umbraco.Cms.Core.Services.Changes;

namespace Umbraco.Cms.Search.Core.Models.Indexing;

public record ContentChange(Guid Id, TreeChangeTypes ChangeTypes)
{
}