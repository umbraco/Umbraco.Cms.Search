using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public class InvariantContentProtectionTests : SearcherTestBase
{
    public IPublicAccessService PublicAccessService => GetRequiredService<IPublicAccessService>();
    public IMemberGroupService MemberGroupService => GetRequiredService<IMemberGroupService>();
    public IMemberService MemberService => GetRequiredService<IMemberService>();
    public IMemberTypeService MemberTypeService => GetRequiredService<IMemberTypeService>();
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CannotGetProtectedContent(bool publish)
    {
        var result = await MemberGroupService.CreateAsync(new MemberGroup() {Name = "testGroup"});
        await PublicAccessService.CreateAsync(
            new PublicAccessEntrySlim
            {
                ErrorPageId = RootKey,
                LoginPageId = RootKey,
                ContentId = RootKey,
                MemberGroupNames = ["testGroup"]
            });
        Thread.Sleep(5000);
        
        var indexAlias = GetIndexAlias(publish);
        var results = await Searcher.SearchAsync(indexAlias, "The root title", null, null, null, null, null, null, 0, 100);
        
        // We should still be able to get draft content, as it is not protected
        Assert.That(results.Total, Is.EqualTo(publish ? 0 : 1));
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetProtectedContent_IfAuthenticated(bool publish)
    {
        var result = await MemberGroupService.CreateAsync(new MemberGroup() {Name = "testGroup"});
        await PublicAccessService.CreateAsync(
            new PublicAccessEntrySlim
            {
                ErrorPageId = RootKey,
                LoginPageId = RootKey,
                ContentId = RootKey,
                MemberGroupNames = ["testGroup"]
            });
        Thread.Sleep(5000);
        
        IMemberType memberType = MemberTypeBuilder.CreateSimpleMemberType();
        MemberTypeService.Save(memberType);

        var customMember = MemberBuilder.CreateSimpleMember(memberType, "hello", "hello@test.com", "hello", "hello");
        MemberService.Save(customMember);

        var accessContext = new AccessContext(customMember.Key, [result.Result.Key]);
        var indexAlias = GetIndexAlias(publish);
        var results = await Searcher.SearchAsync(indexAlias, "The root title", null, null, null, null, null, accessContext, 0, 100);
        
        // We should still be able to get draft content, as it is not protected
        Assert.That(results.Total, Is.EqualTo(1));
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanGetProtectedContent_IfAuthenticatedToWrongGroup(bool publish)
    {
        var rightGroupAttempt = await MemberGroupService.CreateAsync(new MemberGroup() {Name = "rightGroup"});
        var wrongGroupAttempt = await MemberGroupService.CreateAsync(new MemberGroup() {Name = "wrongGroup"});
        await PublicAccessService.CreateAsync(
            new PublicAccessEntrySlim
            {
                ErrorPageId = RootKey,
                LoginPageId = RootKey,
                ContentId = RootKey,
                MemberGroupNames = ["rightGroup"]
            });
        Thread.Sleep(5000);
        
        IMemberType memberType = MemberTypeBuilder.CreateSimpleMemberType();
        MemberTypeService.Save(memberType);

        var customMember = MemberBuilder.CreateSimpleMember(memberType, "hello", "hello@test.com", "hello", "hello");
        MemberService.Save(customMember);

        var accessContext = new AccessContext(customMember.Key, [wrongGroupAttempt.Result.Key]);
        var indexAlias = GetIndexAlias(publish);
        var results = await Searcher.SearchAsync(indexAlias, "The root title", null, null, null, null, null, accessContext, 0, 100);
        
        // We should still be able to get draft content, as it is not protected
        Assert.That(results.Total, Is.EqualTo(publish ? 0 : 1));
    }
    
    [SetUp]
    public void CreateInvariantDocument()
    {
        var contentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("title")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Build();
        ContentTypeService.Save(contentType);

        var root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithName("Root")
            .WithPropertyValues(
                new
                {
                    title = "The root title",
                })
            .Build();

        ContentService.Save(root);
        ContentService.Publish(root, ["*"]);
        Thread.Sleep(3000);

        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);
    }
}