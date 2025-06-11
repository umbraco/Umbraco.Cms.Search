using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Directory = Lucene.Net.Store.Directory;

namespace Umbraco.Test.Search.Examine.Integration.Tests.IndexService;

public class TestInMemoryDirectoryFactory : DirectoryFactoryBase
{
    private RandomIdRAMDirectory _randomIdRAMDirectory;
    
    protected override Directory CreateDirectory(LuceneIndex luceneIndex, bool forceUnlock)
    {
        _randomIdRAMDirectory = new RandomIdRAMDirectory();
        return _randomIdRAMDirectory;
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _randomIdRAMDirectory.Dispose();
    }
}