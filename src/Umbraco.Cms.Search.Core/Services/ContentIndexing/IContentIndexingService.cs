using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public interface IContentIndexingService
{
    void Handle(IEnumerable<ContentChange> changes, string origin);

    // TODO: add origin here as well
    void Rebuild(string indexAlias);
}
