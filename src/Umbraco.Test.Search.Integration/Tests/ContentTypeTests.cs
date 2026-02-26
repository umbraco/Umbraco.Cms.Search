using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public class ContentTypeTests : ContentBaseTestBase
{
    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IContentType _contentType1 = null!;

    private IContentType _contentType2 = null!;

    private IContentType _contentType3 = null!;

    private readonly Guid _contentType1RootContentKey = Guid.NewGuid();

    private readonly Guid _contentType1ChildContentKey = Guid.NewGuid();

    private readonly Guid _contentType2RootContentKey = Guid.NewGuid();

    private readonly Guid _contentType2ChildContentKey = Guid.NewGuid();

    private readonly Guid _contentType3RootContentKey = Guid.NewGuid();

    private readonly Guid _contentType3ChildContentKey = Guid.NewGuid();

    [SetUp]
    public async Task SetupTest()
    {
        Indexer.Reset();

        _contentType1 = await CreateContentType();
        _contentType2 = await CreateContentType();
        _contentType3 = await CreateContentType();

        CreateContentStructure(_contentType1, _contentType1RootContentKey, _contentType1ChildContentKey);
        CreateContentStructure(_contentType2, _contentType2RootContentKey, _contentType2ChildContentKey);
        CreateContentStructure(_contentType3, _contentType3RootContentKey, _contentType3ChildContentKey);

        return;

        async Task<IContentType> CreateContentType()
        {
            IContentType contentType = new ContentTypeBuilder().Build();
            await ContentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);
            contentType.AllowedAsRoot = true;
            contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Key, 0, contentType.Alias)];
            await ContentTypeService.UpdateAsync(contentType, Constants.Security.SuperUserKey);

            return contentType;
        }

        void CreateContentStructure(IContentType contentType, Guid rootContentKey, Guid childContentKey)
        {
            Content root = new ContentBuilder()
                .WithKey(rootContentKey)
                .WithContentType(contentType)
                .Build();
            ContentService.Save(root);

            Content child = new ContentBuilder()
                .WithKey(childContentKey)
                .WithContentType(contentType)
                .WithParent(root)
                .Build();
            ContentService.Save(child);

            ContentService.PublishBranch(root, PublishBranchFilter.IncludeUnpublished, ["*"]);
        }
    }

    [Test]
    public async Task DeleteContentType1()
    {
        IReadOnlyList<TestIndexDocument> draftDocuments = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(6));

        IReadOnlyList<TestIndexDocument> publishedDocuments = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Has.Count.EqualTo(6));

        await ContentTypeService.DeleteAsync(_contentType1.Key, Constants.Security.SuperUserKey);

        draftDocuments = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(4));

        publishedDocuments = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(draftDocuments[0].Id, Is.EqualTo(_contentType2RootContentKey));
            Assert.That(draftDocuments[1].Id, Is.EqualTo(_contentType2ChildContentKey));
            Assert.That(draftDocuments[2].Id, Is.EqualTo(_contentType3RootContentKey));
            Assert.That(draftDocuments[3].Id, Is.EqualTo(_contentType3ChildContentKey));

            Assert.That(publishedDocuments[0].Id, Is.EqualTo(_contentType2RootContentKey));
            Assert.That(publishedDocuments[1].Id, Is.EqualTo(_contentType2ChildContentKey));
            Assert.That(publishedDocuments[2].Id, Is.EqualTo(_contentType3RootContentKey));
            Assert.That(publishedDocuments[3].Id, Is.EqualTo(_contentType3ChildContentKey));
        });
    }

    [Test]
    public async Task DeleteContentType2And3()
    {
        IReadOnlyList<TestIndexDocument> draftDocuments = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(6));

        IReadOnlyList<TestIndexDocument> publishedDocuments = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Has.Count.EqualTo(6));

        await ContentTypeService.DeleteAsync(_contentType2.Key, Constants.Security.SuperUserKey);
        await ContentTypeService.DeleteAsync(_contentType3.Key, Constants.Security.SuperUserKey);

        draftDocuments = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(2));

        publishedDocuments = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(draftDocuments[0].Id, Is.EqualTo(_contentType1RootContentKey));
            Assert.That(draftDocuments[1].Id, Is.EqualTo(_contentType1ChildContentKey));

            Assert.That(publishedDocuments[0].Id, Is.EqualTo(_contentType1RootContentKey));
            Assert.That(publishedDocuments[1].Id, Is.EqualTo(_contentType1ChildContentKey));
        });
    }

    [Test]
    public async Task DeleteAllContentTypes()
    {
        IReadOnlyList<TestIndexDocument> draftDocuments = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(6));

        IReadOnlyList<TestIndexDocument> publishedDocuments = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Has.Count.EqualTo(6));

        await ContentTypeService.DeleteAsync(_contentType1.Key, Constants.Security.SuperUserKey);
        await ContentTypeService.DeleteAsync(_contentType2.Key, Constants.Security.SuperUserKey);
        await ContentTypeService.DeleteAsync(_contentType3.Key, Constants.Security.SuperUserKey);

        draftDocuments = Indexer.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Is.Empty);

        publishedDocuments = Indexer.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Is.Empty);
    }
}
