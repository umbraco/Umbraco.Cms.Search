using Examine;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

public class InvariantContentProtectionIndexTests : IndexTestBase
{
    public IPublicAccessService PublicAccessService => GetRequiredService<IPublicAccessService>();
    public IMemberGroupService MemberGroupService => GetRequiredService<IMemberGroupService>();
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanIndexAnyDocument(bool publish)
    {
        CreateInvariantDocument(publish);

        var memberResult = await MemberGroupService.CreateAsync(new MemberGroup() { Name = "testGroup" });
        var result = await PublicAccessService.CreateAsync(new PublicAccessEntrySlim(){ ErrorPageId = RootKey, LoginPageId = RootKey, ContentId = RootKey, MemberGroupNames = ["testGroup"] });

        var index = ExamineManager.GetIndex(publish
            ? Cms.Search.Core.Constants.IndexAliases.PublishedContent
            : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Not.Empty);
    }
    
    private void CreateInvariantDocument(bool publish = false)
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

        SaveOrPublish(root, publish);
        
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);
    }
}