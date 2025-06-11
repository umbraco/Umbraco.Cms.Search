using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Test.Search.Examine.Integration.Tests.IndexService;

namespace Umbraco.Test.Search.Examine.Integration.Tests.IndexService;

public class MemberIndexServiceTests : IndexTestBase
{
    [Test]
    public async Task CanIndexAnyMember()
    {
        await CreateMemberAsync();

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.DraftMembers);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.TotalItemCount, Is.EqualTo(1));
    }
    
    private async Task CreateMemberAsync()
    {
        var memberType = new MemberTypeBuilder()
            .WithAlias("theMemberType")
            .AddPropertyGroup()
            .AddPropertyType()
            .WithAlias("organization")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Done()
            .Build();
        await GetRequiredService<IMemberTypeService>().CreateAsync(memberType, Constants.Security.SuperUserKey);

        GetRequiredService<IMemberService>().Save(
            new MemberBuilder()
                .WithMemberType(memberType)
                .WithName("The Member")
                .WithEmail("member@local")
                .WithLogin("member@local", "Test123456")
                .AddPropertyData()
                .WithKeyValue("organization", "The Organization")
                .Done()
                .Build()
        );
        
        Thread.Sleep(3000);
    }
}