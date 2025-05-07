﻿using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public class ProtectedContentTests : InvariantContentTestBase
{
    private Guid _memberGroupOneKey;
    private Guid _memberGroupTwoKey;

    private IPublicAccessService PublicAccessService => GetRequiredService<IPublicAccessService>();

    private IMemberGroupService MemberGroupService => GetRequiredService<IMemberGroupService>();

    [SetUp]
    public override async Task SetupTest()
    {
        await base.SetupTest();
        var memberGroup = new MemberGroup
        {
            Name = "MemberGroupOne"
        };
        await MemberGroupService.CreateAsync(memberGroup);
        _memberGroupOneKey = memberGroup.Key;

        memberGroup = new MemberGroup
        {
            Name = "MemberGroupTwo"
        };
        await MemberGroupService.CreateAsync(memberGroup);
        _memberGroupTwoKey = memberGroup.Key;
    }

    [Test]
    public void PublishedStructure_IncludesExistingContentProtectionOnIndexUpdate()
    {
        var root = Root();
        var entryResult = PublicAccessService.Save(
            new PublicAccessEntry(root, root, root, [
                new PublicAccessRule
                {
                    RuleType = Constants.Conventions.PublicAccess.MemberRoleRuleType,
                    RuleValue = "MemberGroupOne"
                },
                new PublicAccessRule
                {
                    RuleType = Constants.Conventions.PublicAccess.MemberRoleRuleType,
                    RuleValue = "MemberGroupTwo"
                }
            ])
        );
        Assert.That(entryResult.Success, Is.True);
        
        ContentService.Save(Root());
        ContentService.PublishBranch(Root(), PublishBranchFilter.IncludeUnpublished, ["*"]);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            VerifyProtection(documents[0], [_memberGroupOneKey, _memberGroupTwoKey]);
            VerifyProtection(documents[1], [_memberGroupOneKey, _memberGroupTwoKey]);
            VerifyProtection(documents[2], [_memberGroupOneKey, _memberGroupTwoKey]);
            VerifyProtection(documents[3], [_memberGroupOneKey, _memberGroupTwoKey]);
        });
    }

    [Test]
    public void PublishedStructure_CanAddContentProtectionWithoutRepublishing()
    {
        ContentService.Save(Root());
        ContentService.PublishBranch(Root(), PublishBranchFilter.IncludeUnpublished, ["*"]);

        var root = Root();
        var entryResult = PublicAccessService.Save(
            new PublicAccessEntry(root, root, root, [
                new PublicAccessRule
                {
                    RuleType = Constants.Conventions.PublicAccess.MemberRoleRuleType,
                    RuleValue = "MemberGroupOne"
                },
                new PublicAccessRule
                {
                    RuleType = Constants.Conventions.PublicAccess.MemberRoleRuleType,
                    RuleValue = "MemberGroupTwo"
                }
            ])
        );
        Assert.That(entryResult.Success, Is.True);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            VerifyProtection(documents[0], [_memberGroupOneKey, _memberGroupTwoKey]);
            VerifyProtection(documents[1], [_memberGroupOneKey, _memberGroupTwoKey]);
            VerifyProtection(documents[2], [_memberGroupOneKey, _memberGroupTwoKey]);
            VerifyProtection(documents[3], [_memberGroupOneKey, _memberGroupTwoKey]);
        });
    }

    [Test]
    public void PublishedStructure_CanRemoveContentProtectionWithoutRepublishing()
    {
        var root = Root();
        var entryResult = PublicAccessService.Save(
            new PublicAccessEntry(root, root, root, [
                new PublicAccessRule
                {
                    RuleType = Constants.Conventions.PublicAccess.MemberRoleRuleType,
                    RuleValue = "MemberGroupOne"
                },
                new PublicAccessRule
                {
                    RuleType = Constants.Conventions.PublicAccess.MemberRoleRuleType,
                    RuleValue = "MemberGroupTwo"
                }
            ])
        );
        Assert.That(entryResult.Success, Is.True);

        ContentService.Save(Root());
        ContentService.PublishBranch(Root(), PublishBranchFilter.IncludeUnpublished, ["*"]);

        var entry = PublicAccessService.GetEntryForContent(root);
        Assert.That(entry, Is.Not.Null);

        entryResult = PublicAccessService.Delete(entry);
        Assert.That(entryResult.Success, Is.True);

        var documents = IndexService.Dump(IndexAliases.PublishedContent);
        Assert.That(documents, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(documents[0].Protection, Is.Null);
            Assert.That(documents[1].Protection, Is.Null);
            Assert.That(documents[2].Protection, Is.Null);
            Assert.That(documents[3].Protection, Is.Null);
        });
    }

    private void VerifyProtection(TestIndexDocument document, Guid[] expectedAccessKeys)
    {
        Assert.That(document.Protection, Is.Not.Null);
        Assert.That(document.Protection.AccessIds, Is.EquivalentTo(expectedAccessKeys));
    }
}