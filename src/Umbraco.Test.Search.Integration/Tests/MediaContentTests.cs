using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public class MediaContentTests : MediaTestBase
{
    [Test]
    public void FullStructure_YieldsAllDocuments()
    {
        MediaService.Save([RootFolder(), ChildFolder(), RootMedia(), ChildMedia(), GrandchildMedia()]);

        var documents = IndexService.Dump(IndexAliases.Media);
        Assert.That(documents, Has.Count.EqualTo(5));

        Assert.Multiple(() =>
        {
            VerifyDocumentPropertyValues(documents[0], null, null);
            VerifyDocumentPropertyValues(documents[1], null, null);
            VerifyDocumentPropertyValues(documents[2], "The root alt text", 1234);
            VerifyDocumentPropertyValues(documents[3], "The child alt text", 5678);
            VerifyDocumentPropertyValues(documents[4], "The grandchild alt text", 9012);
        });
    }
    
    [Test]
    public void FullStructure_YieldsStructuralFields()
    {
        MediaService.Save([RootFolder(), ChildFolder(), RootMedia(), ChildMedia(), GrandchildMedia()]);

        var documents = IndexService.Dump(IndexAliases.Media);
        Assert.That(documents, Has.Count.EqualTo(5));

        Assert.Multiple(() =>
        {
            VerifyDocumentStructureValues(documents[0], RootFolderKey, Guid.Empty, RootFolderKey);
            VerifyDocumentStructureValues(documents[1], ChildFolderKey, RootFolderKey, RootFolderKey, ChildFolderKey);
            VerifyDocumentStructureValues(documents[2], RootMediaKey, Guid.Empty, RootMediaKey);
            VerifyDocumentStructureValues(documents[3], ChildMediaKey, RootFolderKey, RootFolderKey, ChildMediaKey);
            VerifyDocumentStructureValues(documents[4], GrandchildMediaKey, ChildFolderKey, RootFolderKey, ChildFolderKey, GrandchildMediaKey);
        });
    }

    [Test]
    public void FullStructure_YieldsSystemFields()
    {
        MediaService.Save([RootFolder(), ChildFolder(), RootMedia(), ChildMedia(), GrandchildMedia()]);

        var documents = IndexService.Dump(IndexAliases.Media);
        Assert.That(documents, Has.Count.EqualTo(5));

        Assert.Multiple(() =>
        {
            VerifyDocumentSystemValues(documents[0], RootFolder(), []);
            VerifyDocumentSystemValues(documents[1], ChildFolder(), []);
            VerifyDocumentSystemValues(documents[2], RootMedia(), ["tag1", "tag2"]);
            VerifyDocumentSystemValues(documents[3], ChildMedia(), ["tag3", "tag4"]);
            VerifyDocumentSystemValues(documents[4], GrandchildMedia(), ["tag5", "tag6"]);
        });
    }

    private void VerifyDocumentPropertyValues(TestIndexDocument document, string? altText, int? bytes)
        => Assert.Multiple(() =>
        {
            var altTextValue = document.Fields.FirstOrDefault(f => f.FieldName == "altText")?.Value.Texts?.SingleOrDefault();
            Assert.That(altTextValue, Is.EqualTo(altText));
            
            var bytesValue = document.Fields.FirstOrDefault(f => f.FieldName == "bytes")?.Value.Integers?.SingleOrDefault();
            Assert.That(bytesValue, Is.EqualTo(bytes));
        });
}