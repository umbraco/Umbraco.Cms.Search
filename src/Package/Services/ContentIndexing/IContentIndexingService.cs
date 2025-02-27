using Package.Models.Indexing;

namespace Package.Services.ContentIndexing;

public interface IContentIndexingService
{
    void Handle(IEnumerable<ContentChange> changes);
}