using Umbraco.Cms.Core.Services.Changes;

namespace Package.Models.Indexing;

public record ContentChange(Guid Id, TreeChangeTypes ChangeTypes)
{
}