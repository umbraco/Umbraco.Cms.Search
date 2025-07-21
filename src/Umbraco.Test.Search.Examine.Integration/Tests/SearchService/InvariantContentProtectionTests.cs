using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests.SearchService;

public class InvariantContentProtectionTests : SearcherTestBase
{
    public IPublicAccessService PublicAccessService => GetRequiredService<IPublicAccessService>();
    public IMemberGroupService MemberGroupService => GetRequiredService<IMemberGroupService>();
    
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
    
    
    // var member = await _memberManager.GetUserAsync(authenticateResult.Principal);
    //     if (member?.UserName is null)
    // {
    //     return null;
    // }
    //     
    // var memberRoles = _memberService.GetAllRoles(member.UserName).ToArray();
    // var memberGroupKeys = memberRoles.Length > 0
    //         ? _memberService
    //             .GetAllRoles()
    //             .Where(group => memberRoles.InvariantContains(group.Name ?? string.Empty))
    //             .Select(group => group.Key)
    //             .ToArray()
    //         : null;
    //
    //     return new AccessContext(member.Key, memberGroupKeys);
    
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