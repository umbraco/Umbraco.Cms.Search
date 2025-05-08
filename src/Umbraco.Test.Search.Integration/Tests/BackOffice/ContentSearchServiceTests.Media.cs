﻿using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Extensions;

namespace Umbraco.Test.Search.Integration.Tests.BackOffice;

public partial class ContentSearchServiceTests
{
    [Test]
    public async Task Media_CanFindAllRootsWithoutQuery()
    {
        var mediaAtRoot = MediaService.GetRootMedia().OrderBy(media => media.SortOrder).ToArray();
        var result = await MediaSearchService.SearchChildrenAsync(null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(3));
            Assert.That(result.Items.Count(), Is.EqualTo(3));

            var items = result.Items.OrderBy(item => item.SortOrder).ToArray();
            Assert.That(items[0].Key, Is.EqualTo(mediaAtRoot[0].Key));
            Assert.That(items[1].Key, Is.EqualTo(mediaAtRoot[1].Key));
            Assert.That(items[2].Key, Is.EqualTo(mediaAtRoot[2].Key));
        });
    }

    [Test]
    public async Task Media_CanFindAllChildrenWithoutQuery()
    {
        var root = MediaService.GetRootMedia().Last();
        var result = await MediaSearchService.SearchChildrenAsync(null, root.Key, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(10));
            Assert.That(result.Items.Count(), Is.EqualTo(10));
            Assert.That(result.Items.Select(item => item.Key), Is.Unique);
            Assert.That(result.Items.DistinctBy(item => item.ParentId).Single().ParentId, Is.EqualTo(root.Id));
        });
    }

    [Test]
    public async Task Media_CanFindAllRootsByNonDistinctQuery()
    {
        var mediaAtRoot = MediaService.GetRootMedia().OrderBy(media => media.SortOrder).ToArray();
        var result = await MediaSearchService.SearchChildrenAsync("title", null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(3));
            Assert.That(result.Items.Count(), Is.EqualTo(3));

            var items = result.Items.OrderBy(item => item.SortOrder).ToArray();
            Assert.That(items[0].Key, Is.EqualTo(mediaAtRoot[0].Key));
            Assert.That(items[1].Key, Is.EqualTo(mediaAtRoot[1].Key));
            Assert.That(items[2].Key, Is.EqualTo(mediaAtRoot[2].Key));
        });
    }

    [Test]
    public async Task Media_CanFindAllChildrenByQuery()
    {
        var root = MediaService.GetRootMedia().Last();
        var result = await MediaSearchService.SearchChildrenAsync("title", root.Key, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(10));
            Assert.That(result.Items.Count(), Is.EqualTo(10));
            Assert.That(result.Items.Select(item => item.Key), Is.Unique);
            Assert.That(result.Items.DistinctBy(item => item.ParentId).Single().ParentId, Is.EqualTo(root.Id));
        });
    }

    [Test]
    public async Task Media_CanFindAllRootsByDistinctQuery()
    {
        var result = await MediaSearchService.SearchChildrenAsync("root", null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(3));
            var items = result.Items.OrderBy(item => item.SortOrder).ToArray();
            Assert.That(items[0].Name, Is.EqualTo("Root 0"));
            Assert.That(items[1].Name, Is.EqualTo("Root 1"));
            Assert.That(items[2].Name, Is.EqualTo("Root 2"));
        });
    }

    [Test]
    public async Task Media_CanFindSingleRootByQuery()
    {
        var result = await MediaSearchService.SearchChildrenAsync("single1root", null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(1));
            Assert.That(result.Items.First().Name, Is.EqualTo("Root 1"));
        });
    }

    [Test]
    public async Task Media_CanFindSingleChildByQuery()
    {
        var root = MediaService.GetRootMedia().Last();
        var result = await MediaSearchService.SearchChildrenAsync("single3child", root.Key, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(1));
            Assert.That(result.Items.Single().Name, Is.EqualTo("Child 3"));
            Assert.That(result.Items.Single().ParentId, Is.EqualTo(root.Id));
        });
    }

    [Test]
    public async Task Media_CanFindMultipleChildrenByQuery()
    {
        var root = MediaService.GetRootMedia().Last();
        var result = await MediaSearchService.SearchChildrenAsync("triple2child", root.Key, null);

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

    [Test]
    public async Task Media_CanFindRootByIdQuery()
    {
        var root = MediaService.GetRootMedia().First();
        var result = await MediaSearchService.SearchChildrenAsync(root.Key.AsKeyword(), null, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(1));
            Assert.That(result.Items.First().Key, Is.EqualTo(root.Key));
        });
    }

    [Test]
    public async Task Media_CanFindChildByIdQuery()
    {
        var root = MediaService.GetRootMedia().First();
        var child = MediaService.GetPagedChildren(root.Id, 0, 10, out _).First();
        var result = await MediaSearchService.SearchChildrenAsync(child.Key.AsKeyword(), root.Key, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(1));
            Assert.That(result.Items.First().Key, Is.EqualTo(child.Key));
        });
    }
    
    [TestCase(Direction.Ascending)]
    [TestCase(Direction.Descending)]
    public async Task Media_CanSortAllChildrenByNameWithoutQuery(Direction direction)
    {
        var root = MediaService.GetRootMedia().Last();
        var children = MediaService.GetPagedChildren(root.Id, 0, 10, out _);
        var expectedChildrenKeys = (direction is Direction.Ascending
                ? children.OrderBy(child => child.Name)
                : children.OrderByDescending(child => child.Name)
            ).Select(child => child.Key).ToArray();

        var result = await MediaSearchService.SearchChildrenAsync(null, root.Key, Ordering.By("name", direction));

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(10));
            Assert.That(result.Items.Count(), Is.EqualTo(10));
            CollectionAssert.AreEqual(result.Items.Select(item => item.Key), expectedChildrenKeys);
        });
    }
    
    [TestCase(Direction.Ascending)]
    [TestCase(Direction.Descending)]
    public async Task Media_CanSortAllChildrenByUpdateDateWithoutQuery(Direction direction)
    {
        var root = MediaService.GetRootMedia().Last();
        var children = MediaService.GetPagedChildren(root.Id, 0, 10, out _);
        var expectedChildrenKeys = (direction is Direction.Ascending
                ? children.OrderBy(child => child.UpdateDate)
                : children.OrderByDescending(child => child.UpdateDate)
            ).Select(child => child.Key).ToArray();

        var result = await MediaSearchService.SearchChildrenAsync(null, root.Key, Ordering.By("updateDate", direction));

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(10));
            Assert.That(result.Items.Count(), Is.EqualTo(10));
            CollectionAssert.AreEqual(result.Items.Select(item => item.Key), expectedChildrenKeys);
        });
    }

    [TestCase(Direction.Ascending)]
    [TestCase(Direction.Descending)]
    public async Task Media_CanSortChildrenByNameWithQuery(Direction direction)
    {
        var root = MediaService.GetRootMedia().Last();
        var expectedChildrenOrder = new[] { "Child 1", "Child 3", "Child 5", "Child 7", "Child 9" };
        if (direction is Direction.Descending)
        {
            expectedChildrenOrder = expectedChildrenOrder.Reverse().ToArray();
        }

        var result = await MediaSearchService.SearchChildrenAsync("oddeven1child", root.Key, Ordering.By("name", direction));

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(expectedChildrenOrder.Length));
            Assert.That(result.Items.Count(), Is.EqualTo(expectedChildrenOrder.Length));
            CollectionAssert.AreEqual(result.Items.Select(item => item.Name), expectedChildrenOrder);
        });
    }

    [TestCase(Direction.Ascending)]
    [TestCase(Direction.Descending)]
    public async Task Media_CanSortChildrenByUpdateDateWithQuery(Direction direction)
    {
        var root = MediaService.GetRootMedia().Last();
        var expectedChildrenOrder = new[] { "Child 0", "Child 2", "Child 4", "Child 6", "Child 8" };
        if (direction is Direction.Descending)
        {
            expectedChildrenOrder = expectedChildrenOrder.Reverse().ToArray();
        }

        var result = await MediaSearchService.SearchChildrenAsync("oddeven0child", root.Key, Ordering.By("updateDate", direction));

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(expectedChildrenOrder.Length));
            Assert.That(result.Items.Count(), Is.EqualTo(expectedChildrenOrder.Length));
            CollectionAssert.AreEqual(result.Items.Select(item => item.Name), expectedChildrenOrder);
        });
    }

    [Test]
    public async Task Media_CannotFindChildrenWithoutParent()
    {
        var result = await MediaSearchService.SearchChildrenAsync("triple2child", null, null);
        Assert.That(result.Total, Is.EqualTo(0));
    }
}