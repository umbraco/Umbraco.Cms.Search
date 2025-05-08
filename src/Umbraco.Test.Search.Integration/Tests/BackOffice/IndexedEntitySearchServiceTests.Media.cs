using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;

namespace Umbraco.Test.Search.Integration.Tests.BackOffice;

public partial class IndexedEntitySearchServiceTests
{
    [Test]
    public async Task Media_CanFindAll()
    {
        var result = await IndexedEntitySearchService.SearchAsync(
            UmbracoObjectTypes.Media,
            query: string.Empty,
            parentId: null,
            contentTypeIds: null,
            trashed: null
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(33));
            Assert.That(result.Items.Count(), Is.EqualTo(33));
            Assert.That(result.Items.Select(item => item.Key), Is.Unique);
            Assert.That(result.Items.DistinctBy(item => item.Trashed).Count(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Media_CanFindAllRootsByQuery()
    {
        var result = await IndexedEntitySearchService.SearchAsync(
            UmbracoObjectTypes.Media,
            query: "root",
            parentId: null,
            contentTypeIds: null,
            trashed: null
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(3));
            Assert.That(result.Items.Count(), Is.EqualTo(3));
            Assert.That(result.Items.Select(item => item.Key), Is.Unique);
            Assert.That(result.Items.Count(item => item.ParentId == Constants.System.Root), Is.EqualTo(2));
            Assert.That(result.Items.Count(item => item.ParentId == Constants.System.RecycleBinMedia), Is.EqualTo(1));
            Assert.That(result.Items.All(item => item.Name!.Contains("Root")), Is.True);
        });
    }

    [Test]
    public async Task Media_CanFindSpecificRootByQuery()
    {
        var root = MediaService.GetRootMedia().OrderBy(media => media.SortOrder).Skip(1).First();
        var result = await IndexedEntitySearchService.SearchAsync(
            UmbracoObjectTypes.Media,
            query: "single1root",
            parentId: null,
            contentTypeIds: null,
            trashed: null
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(1));
            Assert.That(result.Items.Count(), Is.EqualTo(1));
            Assert.That(result.Items.First().Key, Is.EqualTo(root.Key));
        });
    }

    [Test]
    public async Task Media_CanFindAllChildrenByQuery()
    {
        var result = await IndexedEntitySearchService.SearchAsync(
            UmbracoObjectTypes.Media,
            query: "child",
            parentId: null,
            contentTypeIds: null,
            trashed: null
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(30));
            Assert.That(result.Items.Count(), Is.EqualTo(30));
            Assert.That(result.Items.Select(item => item.Key), Is.Unique);
            Assert.That(result.Items.DistinctBy(item => item.ParentId).Count(), Is.EqualTo(3));
            Assert.That(result.Items.All(item => item.Name!.Contains("Child")), Is.True);
        });
    }

    [Test]
    public async Task Media_CanFindAllChildrenBelowParent()
    {
        var root = MediaService.GetRootMedia().Last();
        var result = await IndexedEntitySearchService.SearchAsync(
            UmbracoObjectTypes.Media,
            query: string.Empty,
            parentId: root.Key,
            contentTypeIds: null,
            trashed: null
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(10));
            Assert.That(result.Items.Count(), Is.EqualTo(10));
            Assert.That(result.Items.Select(item => item.Key), Is.Unique);
            Assert.That(result.Items.All(item => item.Name!.Contains("Child")), Is.True);
            Assert.That(result.Items.DistinctBy(item => item.ParentId).Single().ParentId, Is.EqualTo(root.Id));
        });
    }

    [Test]
    public async Task Media_CanFindChildrenBelowParentByQuery()
    {
        var root = MediaService.GetRootMedia().Last();
        var result = await IndexedEntitySearchService.SearchAsync(
            UmbracoObjectTypes.Media,
            query: "triple2child",
            parentId: root.Key,
            contentTypeIds: null,
            trashed: null
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(3));
            var items = result.Items.OrderBy(item => item.SortOrder).ToArray();
            Assert.That(items[0].Name, Is.EqualTo("Child 6"));
            Assert.That(items[1].Name, Is.EqualTo("Child 7"));
            Assert.That(items[2].Name, Is.EqualTo("Child 8"));
            Assert.That(result.Items.DistinctBy(item => item.ParentId).Single().ParentId, Is.EqualTo(root.Id));
        });
    }

    [TestCase("rootMediaType", 3)]
    [TestCase("childMediaType", 30)]
    public async Task Media_CanFindAllByContentType(string mediaTypeAlias, int expectedTotal)
    {
        var mediaTypeKey = MediaTypeService.Get(mediaTypeAlias)?.Key
                             ?? throw new InvalidOperationException($"Could not find {mediaTypeAlias}.");

        var result = await IndexedEntitySearchService.SearchAsync(
            UmbracoObjectTypes.Media,
            query: string.Empty,
            parentId: null,
            contentTypeIds: [mediaTypeKey],
            trashed: null
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(expectedTotal));
            var items = result.Items.OfType<IMediaEntitySlim>().ToArray();
            Assert.That(items.Length, Is.EqualTo(expectedTotal));
            Assert.That(items.All(item => item.ContentTypeAlias == mediaTypeAlias), Is.True);
        });
    }

    [Test]
    public async Task Media_CanCombineParentAndContentTypeFiltering()
    {
        var root = MediaService.GetRootMedia().Last();
        var mediaTypeKey = MediaTypeService.Get("childMediaType")?.Key
                             ?? throw new InvalidOperationException("Could not find childMediaType");

        var result = await IndexedEntitySearchService.SearchAsync(
            UmbracoObjectTypes.Media,
            query: string.Empty,
            parentId: root.Key,
            contentTypeIds: [mediaTypeKey],
            trashed: null
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(10));
            var items = result.Items.OfType<IMediaEntitySlim>().ToArray();
            Assert.That(items.Length, Is.EqualTo(10));
            Assert.That(items.All(item => item.ContentTypeAlias is "childMediaType"), Is.True);
            Assert.That(items.All(item => item.ParentId == root.Id), Is.True);
        });
    }
}