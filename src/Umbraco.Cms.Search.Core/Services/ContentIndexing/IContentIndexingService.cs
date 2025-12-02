using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Umbraco.Cms.Search.Core.Services.ContentIndexing;

public interface IContentIndexingService
{
    void Handle(IEnumerable<ContentChange> changes);

    void Rebuild(string indexAlias, bool useDatabase = true);
}
