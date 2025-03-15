namespace Umbraco.Test.Search.Integration.Tests;

public class MediaStructureTests : MediaTestBase
{
    [Test]
    public void FullStructure_YieldsAllDocuments()
    {
        MediaService.Save([RootFolder(), ChildFolder(), RootMedia(), ChildMedia(), GrandchildMedia()]);

        var documents = IndexService.Dump(IndexAliases.Media);
        Assert.That(documents, Has.Count.EqualTo(5));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootFolderKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildFolderKey));
            Assert.That(documents[2].Key, Is.EqualTo(RootMediaKey));
            Assert.That(documents[3].Key, Is.EqualTo(ChildMediaKey));
            Assert.That(documents[4].Key, Is.EqualTo(GrandchildMediaKey));
        });
    }

    [Test]
    public void FullStructure_YieldsNoDraftContentDocuments()
    {
        MediaService.Save([RootFolder(), ChildFolder(), RootMedia(), ChildMedia(), GrandchildMedia()]);

        var documents = IndexService.Dump(IndexAliases.DraftContent);
        Assert.That(documents, Has.Count.EqualTo(0));
    }

    [Test]
    public void RootOnly_YieldsOnlyRoot()
    {
        MediaService.Save(RootFolder());

        var documents = IndexService.Dump(IndexAliases.Media);
        Assert.That(documents, Has.Count.EqualTo(1));
        Assert.That(documents[0].Key, Is.EqualTo(RootFolderKey));
    }

    [Test]
    public void FullStructure_WithChildFolderInRecycleBin_YieldsAllDocuments()
    {
        MediaService.Save([RootFolder(), ChildFolder(), RootMedia(), ChildMedia(), GrandchildMedia()]);
        
        var result = MediaService.MoveToRecycleBin(ChildFolder());
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ChildFolder().Trashed, Is.True);
            Assert.That(GrandchildMedia().Trashed, Is.True);
        });

        var documents = IndexService.Dump(IndexAliases.Media);
        Assert.That(documents, Has.Count.EqualTo(5));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootFolderKey));
            Assert.That(documents[1].Key, Is.EqualTo(ChildFolderKey));
            Assert.That(documents[2].Key, Is.EqualTo(RootMediaKey));
            Assert.That(documents[3].Key, Is.EqualTo(ChildMediaKey));
            Assert.That(documents[4].Key, Is.EqualTo(GrandchildMediaKey));
        });
    }

    [Test]
    public void FullStructure_WithChildFolderDeleted_YieldsNothingBelowRootFolder()
    {
        MediaService.Save([RootFolder(), ChildFolder(), RootMedia(), ChildMedia(), GrandchildMedia()]);
        
        var result = MediaService.Delete(ChildFolder());
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(MediaService.GetById(GrandchildMediaKey), Is.Null);
        });
           
        var documents = IndexService.Dump(IndexAliases.Media);
        Assert.That(documents, Has.Count.EqualTo(3));
           
        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Key, Is.EqualTo(RootFolderKey));
            Assert.That(documents[1].Key, Is.EqualTo(RootMediaKey));
            Assert.That(documents[2].Key, Is.EqualTo(ChildMediaKey));
        });
    }
}