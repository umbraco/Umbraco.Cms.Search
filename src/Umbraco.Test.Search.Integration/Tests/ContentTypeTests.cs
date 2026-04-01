using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public class ContentTypeTests : ContentBaseTestBase
{
    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private IShortStringHelper ShortStringHelper => GetRequiredService<IShortStringHelper>();

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
        IndexerAndSearcher.Reset();

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
        IReadOnlyList<TestIndexDocument> draftDocuments = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(6));

        IReadOnlyList<TestIndexDocument> publishedDocuments = IndexerAndSearcher.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Has.Count.EqualTo(6));

        await ContentTypeService.DeleteAsync(_contentType1.Key, Constants.Security.SuperUserKey);

        draftDocuments = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(4));

        publishedDocuments = IndexerAndSearcher.Dump(IndexAliases.PublishedContent);
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
        IReadOnlyList<TestIndexDocument> draftDocuments = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(6));

        IReadOnlyList<TestIndexDocument> publishedDocuments = IndexerAndSearcher.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Has.Count.EqualTo(6));

        await ContentTypeService.DeleteAsync(_contentType2.Key, Constants.Security.SuperUserKey);
        await ContentTypeService.DeleteAsync(_contentType3.Key, Constants.Security.SuperUserKey);

        draftDocuments = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(2));

        publishedDocuments = IndexerAndSearcher.Dump(IndexAliases.PublishedContent);
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
    public async Task UpdateComposedContentType_ReindexesComposingTypeContent()
    {
        // Create a content type that will be used as a composition
        IContentType compositionType = new ContentTypeBuilder()
            .WithAlias("composition")
            .AddPropertyType()
                .WithAlias("originalProp")
                .WithDataTypeId(Constants.DataTypes.Textbox)
                .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
                .Done()
            .Build();
        await ContentTypeService.CreateAsync(compositionType, Constants.Security.SuperUserKey);
        compositionType.AllowedAsRoot = true;
        await ContentTypeService.UpdateAsync(compositionType, Constants.Security.SuperUserKey);

        // Create a content type that inherits from (composes) the first
        IContentType composingType = new ContentTypeBuilder()
            .WithAlias("composing")
            .Build();
        composingType.AddContentType(compositionType);
        await ContentTypeService.CreateAsync(composingType, Constants.Security.SuperUserKey);
        composingType.AllowedAsRoot = true;
        await ContentTypeService.UpdateAsync(composingType, Constants.Security.SuperUserKey);

        // Create content of the composition type
        var compositionContentKey = Guid.NewGuid();
        Content compositionContent = new ContentBuilder()
            .WithKey(compositionContentKey)
            .WithContentType(compositionType)
            .Build();
        ContentService.Save(compositionContent);

        // Create content of the composing type
        var composingContentKey = Guid.NewGuid();
        Content composingContent = new ContentBuilder()
            .WithKey(composingContentKey)
            .WithContentType(composingType)
            .Build();
        ContentService.Save(composingContent);

        // Verify initial state: both new content items indexed in draft
        // (plus 6 from the base SetUp = 8 total)
        IReadOnlyList<TestIndexDocument> draftDocuments = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(8));

        // Clear the index to track what gets re-indexed by the content type change
        IndexerAndSearcher.Reset();
        draftDocuments = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Is.Empty);

        // Act: make a structural change to the composition type (add a property)
        compositionType.AddPropertyType(
            new PropertyType(ShortStringHelper, "newProp", ValueStorageType.Ntext)
            {
                Alias = "newProp",
                DataTypeId = Constants.DataTypes.Textbox,
                PropertyEditorAlias = Constants.PropertyEditors.Aliases.TextBox,
                Name = "newProp",
            });
        await ContentTypeService.UpdateAsync(compositionType, Constants.Security.SuperUserKey);

        // Assert: BOTH content items should have been re-indexed,
        // not just the composition type's content
        draftDocuments = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(draftDocuments.Select(d => d.Id), Contains.Item(compositionContentKey));
            Assert.That(draftDocuments.Select(d => d.Id), Contains.Item(composingContentKey));
        });
    }

    [Test]
    public async Task DeleteAllContentTypes()
    {
        IReadOnlyList<TestIndexDocument> draftDocuments = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Has.Count.EqualTo(6));

        IReadOnlyList<TestIndexDocument> publishedDocuments = IndexerAndSearcher.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Has.Count.EqualTo(6));

        await ContentTypeService.DeleteAsync(_contentType1.Key, Constants.Security.SuperUserKey);
        await ContentTypeService.DeleteAsync(_contentType2.Key, Constants.Security.SuperUserKey);
        await ContentTypeService.DeleteAsync(_contentType3.Key, Constants.Security.SuperUserKey);

        draftDocuments = IndexerAndSearcher.Dump(IndexAliases.DraftContent);
        Assert.That(draftDocuments, Is.Empty);

        publishedDocuments = IndexerAndSearcher.Dump(IndexAliases.PublishedContent);
        Assert.That(publishedDocuments, Is.Empty);
    }
}
