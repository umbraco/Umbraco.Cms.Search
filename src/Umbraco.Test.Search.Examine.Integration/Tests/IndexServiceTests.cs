using Examine;
using Examine.Lucene.Directories;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Test.Search.Examine.Integration.Tests;


[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class IndexServiceTests : UmbracoIntegrationTest
{
    private Guid RootKey { get; } = Guid.NewGuid();
    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentService ContentService => GetRequiredService<IContentService>();
    private IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
    
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        builder.Services.AddSingleton<IDirectoryFactory, TestInMemoryDirectoryFactory>();
        builder.AddExamineSearchProvider();
        builder.AddSearchCore();
        
        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MediaTreeChangeNotification, MediaTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberSavedNotification, MemberSavedDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberDeletedNotification, MemberDeletedDistributedCacheNotificationHandler>();
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexAnyDocument(bool publish)
    {
        CreateRootDocument(publish);
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);

        var index = ExamineManager.GetIndex(publish ? Cms.Search.Core.Constants.IndexAliases.PublishedContent : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var results = index.Searcher.CreateQuery().All().Execute();
        Assert.That(results, Is.Not.Empty);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexTextProperty(bool publish)
    {
        CreateRootDocument(publish);
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);

        var index = ExamineManager.GetIndex(publish ? Cms.Search.Core.Constants.IndexAliases.PublishedContent : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("title");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo("The root title"));
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexIntegerValues(bool publish)
    {
        CreateRootDocument(publish);
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);

        var index = ExamineManager.GetIndex(publish ? Cms.Search.Core.Constants.IndexAliases.PublishedContent : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("count");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo("12"));
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void CanIndexTags(bool publish)
    {
        CreateRootDocument(publish);
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);

        var index = ExamineManager.GetIndex(publish ? Cms.Search.Core.Constants.IndexAliases.PublishedContent : Cms.Search.Core.Constants.IndexAliases.DraftContent);

        var queryBuilder = index.Searcher.CreateQuery().All();
        queryBuilder.SelectField("tags");
        var results = queryBuilder.Execute();
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First().Values.First().Value, Is.EqualTo("[\"tag1\",\"tag2\"]"));
    }
    
    

    private void CreateRootDocument(bool publish = false)
    {
        var contentType = new ContentTypeBuilder()
            .WithAlias("invariant")
            .AddPropertyType()
            .WithAlias("title")
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .AddPropertyType()
            .WithAlias("count")
            .WithDataTypeId(-51)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Integer)
            .Done()
            .AddPropertyType()
            .WithAlias("tags")
            .WithDataTypeId(Constants.DataTypes.Tags)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Tags)
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
                    count = 12,
                    tags = "[\"tag1\",\"tag2\"]"
                })
            .Build();

        if (publish)
        {
            ContentService.SaveAndPublish(root);
        }
        else
        {
            ContentService.Save(root);
        }
    }
}