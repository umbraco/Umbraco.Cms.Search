using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Directory = Lucene.Net.Store.Directory;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

public class TestInMemoryDirectoryFactory : IDirectoryFactory
{
    private RandomIdRAMDirectory? _mainDir;
    private RandomIdRAMDirectory? _taxonomyDir;

    public Directory CreateDirectory(LuceneIndex luceneIndex, bool forceUnlock)
    {
        _mainDir ??= new RandomIdRAMDirectory();
        return _mainDir;
    }

    public Directory CreateTaxonomyDirectory(LuceneIndex luceneIndex, bool forceUnlock)
    {
        _taxonomyDir ??= new RandomIdRAMDirectory();
        return _taxonomyDir;
    }

    public void Dispose()
    {
        _mainDir?.Dispose();
        _taxonomyDir?.Dispose();
    }
}
