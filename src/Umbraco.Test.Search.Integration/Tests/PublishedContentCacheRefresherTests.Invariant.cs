using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Integration.Tests;

public partial class PublishedContentCacheRefresherTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Invariant_PublishRoot(bool publishDescendants)
    {
        var setup = SetupInvariantContentTest();
        if (publishDescendants)
        {
            ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        }
        else
        {
            ContentService.SaveAndPublish(Get(setup.RootKey));
        }

        // the result must be same no matter if descendants are included or not, because the root was unpublished to begin with
        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.RefreshBranch));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.RootKey));
            Assert.That(payloads[0].AffectedCultures, Is.Empty);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Invariant_RepublishChild(bool publishDescendants)
    {
        var setup = SetupInvariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ResetNotificationPayloads();

        if (publishDescendants)
        {
            // we need to change something, otherwise the branch publish will detect "no changes" and no notifications will be invoked
            var content = Get(setup.ChildKey);
            content.Name = "Updated";
            ContentService.Save(content);

            ContentService.SaveAndPublishBranch(Get(setup.ChildKey), true);
        }
        else
        {
            ContentService.SaveAndPublish(Get(setup.ChildKey));
        }

        // the result must be same no matter if descendants are included or not, because the child was already published
        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.RefreshNode));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.ChildKey));
            Assert.That(payloads[0].AffectedCultures, Is.Empty);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Invariant_UnpublishRoot(bool publishDescendants)
    {
        var setup = SetupInvariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), publishDescendants);
        ResetNotificationPayloads();

        ContentService.Unpublish(Get(setup.RootKey));

        // the result must be same no matter if descendants are included or not, because unpublish explicitly affects the whole branch
        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.Remove));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.RootKey));
            Assert.That(payloads[0].AffectedCultures, Is.Empty);
        });
    }

    [Test]
    public void Invariant_UnpublishChild()
    {
        var setup = SetupInvariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ResetNotificationPayloads();

        ContentService.Unpublish(Get(setup.ChildKey));

        // the result must be same no matter if descendants are included or not, because unpublish explicitly affects the whole branch
        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.Remove));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.ChildKey));
            Assert.That(payloads[0].AffectedCultures, Is.Empty);
        });
    }
    
    [Test]
    public void Invariant_MoveRootToRecycleBin()
    {
        var setup = SetupInvariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ResetNotificationPayloads();

        ContentService.MoveToRecycleBin(Get(setup.RootKey));

        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.Remove));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.RootKey));
            Assert.That(payloads[0].AffectedCultures, Is.Empty);
        });
    }

    [Test]
    public void Invariant_MoveChildToRecycleBin()
    {
        var setup = SetupInvariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ResetNotificationPayloads();

        ContentService.MoveToRecycleBin(Get(setup.ChildKey));

        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.Remove));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.ChildKey));
            Assert.That(payloads[0].AffectedCultures, Is.Empty);
        });
    }
    
    [Test]
    public void Invariant_DeletePublishedRoot()
    {
        var setup = SetupInvariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ResetNotificationPayloads();

        ContentService.Delete(Get(setup.RootKey));

        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.Remove));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.RootKey));
            Assert.That(payloads[0].AffectedCultures, Is.Empty);
        });
    }
    
    [Test]
    public void Invariant_DeletePublishedChild()
    {
        var setup = SetupInvariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ResetNotificationPayloads();

        ContentService.Delete(Get(setup.ChildKey));

        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.Remove));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.ChildKey));
            Assert.That(payloads[0].AffectedCultures, Is.Empty);
        });
    }

    [Test]
    public void Invariant_DeleteRootFromRecycleBin()
    {
        var setup = SetupInvariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ContentService.MoveToRecycleBin(Get(setup.RootKey));
        ResetNotificationPayloads();

        ContentService.Delete(Get(setup.RootKey));

        // no payload expected; it should've already been handled when moving the content to the recycle bin
        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(0));
    }
    
    private (Guid RootKey, Guid ChildKey, Guid GrandchildKey) SetupInvariantContentTest()
    {
        var contentType = new ContentTypeBuilder()
            .WithAlias("variant")
            .WithContentVariation(ContentVariation.Nothing)
            .Build();
        ContentTypeService.Save(contentType);
        contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Id, 0)];
        GetRequiredService<IContentTypeService>().Save(contentType);

        var root = new ContentBuilder()
            .WithContentType(contentType)
            .WithName("Root")
            .Build();
        ContentService.Save(root);

        var child = new ContentBuilder()
            .WithContentType(contentType)
            .WithName("Child")
            .WithParent(root)
            .Build();
        ContentService.Save(child);

        var grandchild = new ContentBuilder()
            .WithContentType(contentType)
            .WithName("Grandchild")
            .WithParent(child)
            .Build();
        ContentService.Save(grandchild);

        ResetNotificationPayloads();

        return (root.Key, child.Key, grandchild.Key);
    }
}