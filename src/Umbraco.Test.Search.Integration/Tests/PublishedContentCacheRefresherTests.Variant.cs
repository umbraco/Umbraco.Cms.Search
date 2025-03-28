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
    public void Variant_PublishRoot_MultipleCultures(bool publishDescendants)
    {
        var setup = SetupVariantContentTest();
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
            Assert.That(payloads[0].AffectedCultures, Is.EquivalentTo(new[] { "en-us", "da-dk" }));
        });
    }

    [TestCase("en-US", true)]
    [TestCase("en-US", false)]
    [TestCase("da-DK", true)]
    [TestCase("da-DK", false)]
    public void Variant_PublishRoot_SingleCulture(string cultureToPublish, bool publishDescendants)
    {
        var setup = SetupVariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true, [cultureToPublish]);

        // the result must be same no matter if descendants are included or not, because the root was unpublished to begin with
        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.RefreshBranch));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.RootKey));
            Assert.That(payloads[0].AffectedCultures, Is.EquivalentTo(new [] {cultureToPublish.ToLowerInvariant()}));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Variant_PublishRoot_CultureByCulture(bool publishDescendants)
    {
        var setup = SetupVariantContentTest();
        if (publishDescendants)
        {
            ContentService.SaveAndPublishBranch(Get(setup.RootKey), true, ["en-US"]);
            ContentService.SaveAndPublishBranch(Get(setup.RootKey), true, ["da-DK"]);
        }
        else
        {
            ContentService.SaveAndPublish(Get(setup.RootKey), ["en-US"]);
            ContentService.SaveAndPublish(Get(setup.RootKey), ["da-DK"]);
        }

        // the result must be same no matter if descendants are included or not, because the root was unpublished to begin with
        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.RefreshBranch));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.RootKey));
            Assert.That(payloads[0].AffectedCultures, Is.EquivalentTo(new[] { "en-us" }));
            
            Assert.That(payloads[1].ChangeTypes, Is.EqualTo(TreeChangeTypes.RefreshBranch));
            Assert.That(payloads[1].ContentKey, Is.EqualTo(setup.RootKey));
            Assert.That(payloads[1].AffectedCultures, Is.EquivalentTo(new[] { "da-dk" }));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Variant_RepublishChild_MultipleCultures(bool publishDescendants)
    {
        var setup = SetupVariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ResetNotificationPayloads();

        if (publishDescendants)
        {
            // we need to change something, otherwise the branch publish will detect "no changes" and no notifications will be invoked
            var content = Get(setup.ChildKey);
            content.SetCultureName("Updated EN", "en-US");
            content.SetCultureName("Updated DA", "da-DK");
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

    [TestCase("en-US", true)]
    [TestCase("en-US", false)]
    [TestCase("da-DK", true)]
    [TestCase("da-DK", false)]
    public void Variant_RepublishChild_SingleCultures(string cultureToPublish, bool publishDescendants)
    {
        var setup = SetupVariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ResetNotificationPayloads();

        if (publishDescendants)
        {
            // we need to change something, otherwise the branch publish will detect "no changes" and no notifications will be invoked
            var content = Get(setup.ChildKey);
            content.SetCultureName("Updated EN", "en-US");
            content.SetCultureName("Updated DA", "da-DK");
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
    public void Variant_UnpublishRoot_AllCultures(bool publishDescendants)
    {
        var setup = SetupVariantContentTest();
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
    public void Variant_UnpublishChild_AllCultures()
    {
        var setup = SetupVariantContentTest();
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
    
    [TestCase(true)]
    [TestCase(false)]
    public void Variant_UnpublishRoot_CultureByCulture(bool publishDescendants)
    {
        var setup = SetupVariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), publishDescendants);
        ResetNotificationPayloads();

        ContentService.Unpublish(Get(setup.RootKey), "da-DK");
        ContentService.Unpublish(Get(setup.RootKey), "en-US");

        // the result must be same no matter if descendants are included or not, because unpublish explicitly affects the whole branch
        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            // the first payload is "refresh branch" because the content is still published in one culture
            Assert.That(payloads[0].ChangeTypes, Is.EqualTo(TreeChangeTypes.RefreshBranch));
            Assert.That(payloads[0].ContentKey, Is.EqualTo(setup.RootKey));
            Assert.That(payloads[0].AffectedCultures, Is.EquivalentTo(new[] { "da-dk" }));
            
            // the second payload is "remove" because the content is completely unpublshed
            Assert.That(payloads[1].ChangeTypes, Is.EqualTo(TreeChangeTypes.Remove));
            Assert.That(payloads[1].ContentKey, Is.EqualTo(setup.RootKey));
            Assert.That(payloads[1].AffectedCultures, Is.Empty);
        });
    }
    
    [Test]
    public void Variant_MoveRootToRecycleBin()
    {
        var setup = SetupVariantContentTest();
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
    public void Variant_MoveChildToRecycleBin()
    {
        var setup = SetupVariantContentTest();
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
    public void Variant_DeletePublishedRoot()
    {
        var setup = SetupVariantContentTest();
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
    public void Variant_DeletePublishedChild()
    {
        var setup = SetupVariantContentTest();
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
    public void Variant_DeleteRootFromRecycleBin()
    {
        var setup = SetupVariantContentTest();
        ContentService.SaveAndPublishBranch(Get(setup.RootKey), true);
        ContentService.MoveToRecycleBin(Get(setup.RootKey));
        ResetNotificationPayloads();

        ContentService.Delete(Get(setup.RootKey));

        // no payload expected; it should've already been handled when moving the content to the recycle bin
        var payloads = GetNotificationPayloads();
        Assert.That(payloads, Has.Count.EqualTo(0));
    }

    private (Guid RootKey, Guid ChildKey, Guid GrandchildKey) SetupVariantContentTest()
    {
        GetRequiredService<ILocalizationService>().Save(
            new LanguageBuilder().WithCultureInfo("da-DK").Build()
        );
        
        var contentType = new ContentTypeBuilder()
            .WithAlias("variant")
            .WithContentVariation(ContentVariation.CultureAndSegment)
            .Build();
        ContentTypeService.Save(contentType);
        contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Id, 0)];
        GetRequiredService<IContentTypeService>().Save(contentType);

        var root = new ContentBuilder()
            .WithContentType(contentType)
            .WithCultureName("en-US", "Root EN")
            .WithCultureName("da-DK", "Root DA")
            .Build();
        ContentService.Save(root);

        var child = new ContentBuilder()
            .WithContentType(contentType)
            .WithCultureName("en-US", "Child EN")
            .WithCultureName("da-DK", "Child DA")
            .WithParent(root)
            .Build();
        ContentService.Save(child);

        var grandchild = new ContentBuilder()
            .WithContentType(contentType)
            .WithCultureName("en-US", "Grandchild EN")
            .WithCultureName("da-DK", "Grandchild DA")
            .WithParent(child)
            .Build();
        ContentService.Save(grandchild);

        ResetNotificationPayloads();

        return (root.Key, child.Key, grandchild.Key);
    }
}