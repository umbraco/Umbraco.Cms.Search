using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

public class ProtectedContentTests : InvariantTestBase
{
    private Guid _memberGroupOneKey;
    private Guid _memberGroupTwoKey;

    private IPublicAccessService PublicAccessService => GetRequiredService<IPublicAccessService>();

    private IMemberGroupService MemberGroupService => GetRequiredService<IMemberGroupService>();

    [SetUp]
    public override void SetupTest()
    {
        base.SetupTest();
        var memberGroup = new MemberGroup
        {
            Name = "MemberGroupOne"
        };
        MemberGroupService.Save(memberGroup);
        _memberGroupOneKey = memberGroup.Key;

        memberGroup = memberGroup = new MemberGroup
        {
            Name = "MemberGroupTwo"
        };
        MemberGroupService.Save(memberGroup);
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
        
        ContentService.SaveAndPublishBranch(Root(), true);

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
        ContentService.SaveAndPublishBranch(Root(), true);

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

        ContentService.SaveAndPublishBranch(Root(), true);

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
        Assert.That(document.Protection.AccessKeys, Is.EquivalentTo(expectedAccessKeys));
    }
}