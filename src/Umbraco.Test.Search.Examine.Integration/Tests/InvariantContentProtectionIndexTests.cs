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

    [Test]
    public async Task CanIndexContentProtection()
    {
        CreateInvariantDocument();

        var result = await MemberGroupService.CreateAsync(new MemberGroup() {Name = "testGroup"});
        await PublicAccessService.CreateAsync(
            new PublicAccessEntrySlim
            {
                ErrorPageId = RootKey,
                LoginPageId = RootKey,
                ContentId = RootKey,
                MemberGroupNames = ["testGroup"]
            });

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        var indexedAccessKeys = results.First().AllValues.First(x => x.Key == "protection").Value;
        Assert.That(indexedAccessKeys, Has.Count.EqualTo(1));
        Assert.That(indexedAccessKeys, Has.Member(result.Result.Key.ToString()));
    }

    [Test]
    public async Task CanIndexMultipleContentProtection()
    {
        CreateInvariantDocument();

        var group = await MemberGroupService.CreateAsync(new MemberGroup {Name = "testGroup"});
        var group2 = await MemberGroupService.CreateAsync(new MemberGroup {Name = "testGroup 2"});
        var group3 = await MemberGroupService.CreateAsync(new MemberGroup {Name = "testGroup 3"});
        var group4 = await MemberGroupService.CreateAsync(new MemberGroup {Name = "testGroup 4"});
        var group5 = await MemberGroupService.CreateAsync(new MemberGroup {Name = "testGroup 5"});
        await PublicAccessService.CreateAsync(
            new PublicAccessEntrySlim
            {
                ErrorPageId = RootKey,
                LoginPageId = RootKey,
                ContentId = RootKey,
                MemberGroupNames = ["testGroup", "testGroup 2", "testGroup 3", "testGroup 4", "testGroup 5"]
            });

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);
        
        var results = index.Searcher.CreateQuery().All().Execute();
        var indexedAccessKeys = results.First().AllValues.First(x => x.Key == "protection").Value;
        Assert.That(indexedAccessKeys, Has.Count.EqualTo(5));
        Assert.That(indexedAccessKeys, Has.Member(group.Result.Key.ToString()));
        Assert.That(indexedAccessKeys, Has.Member(group2.Result.Key.ToString()));
        Assert.That(indexedAccessKeys, Has.Member(group3.Result.Key.ToString()));
        Assert.That(indexedAccessKeys, Has.Member(group4.Result.Key.ToString()));
        Assert.That(indexedAccessKeys, Has.Member(group5.Result.Key.ToString()));
    }
    
    [Test]
    public void DoesNotIndexContentProtectionIfNoneExists()
    {
        CreateInvariantDocument();

        var index = ExamineManager.GetIndex(Cms.Search.Core.Constants.IndexAliases.PublishedContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results.First().AllValues.SelectMany(x => x.Value), Does.Not.Contain("protection"));
    }

    private void CreateInvariantDocument()
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

        SaveOrPublish(root, true);

        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);
    }
}