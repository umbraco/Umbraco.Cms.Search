using Examine;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class IndexTestBase : UmbracoIntegrationTest
{
    protected DateTimeOffset CurrentDateTimeOffset = DateTimeOffset.UtcNow;

    protected double DecimalValue = 12.43;
    protected Guid RootKey { get; } = Guid.NewGuid();
    
    protected Guid ChildKey { get; } = Guid.NewGuid();

    protected Guid GrandchildKey { get; } = Guid.NewGuid();
    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    protected IContentService ContentService => GetRequiredService<IContentService>();
    protected IExamineManager ExamineManager => GetRequiredService<IExamineManager>();
    protected IDataTypeService DataTypeService => GetRequiredService<IDataTypeService>();
    protected ILocalizationService LocalizationService => GetRequiredService<ILocalizationService>();
    
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        
        builder.Services.AddExamine();
        builder.Services.AddSingleton<TestInMemoryDirectoryFactory>();
        builder.Services.ConfigureOptions<TestFacetConfigureOptions>();
        builder.Services.ConfigureOptions<ConfigureIndexOptions>();
        builder.Services.AddExamineLuceneIndex<TestIndex, TestInMemoryDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.DraftContent,
            config => { });
        builder.Services.AddExamineLuceneIndex<TestIndex, TestInMemoryDirectoryFactory>(
            Cms.Search.Core.Constants.IndexAliases.PublishedContent,
            config => { });
        builder.Services.AddTransient<IndexService>();
        builder.Services.AddTransient<IIndexService, IndexService>();
        builder.Services.AddTransient<ISearchService, SearchService>();
        
        builder.Services.Configure<Umbraco.Cms.Search.Core.Configuration.IndexOptions>(options =>
        {
            options.RegisterIndex<IndexService, IDraftContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.DraftContent, UmbracoObjectTypes.Document);
            options.RegisterIndex<IndexService, IPublishedContentChangeStrategy>(Cms.Search.Core.Constants.IndexAliases.PublishedContent, UmbracoObjectTypes.Document);
        });

        builder.AddSearchCore();
        
        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MediaTreeChangeNotification, MediaTreeChangeDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberSavedNotification, MemberSavedDistributedCacheNotificationHandler>();
        builder.AddNotificationHandler<MemberDeletedNotification, MemberDeletedDistributedCacheNotificationHandler>();
    }

    protected void CreateVariantDocument(bool publish = false)
    {
        
        var langDk = new LanguageBuilder()
            .WithCultureInfo("da-DK")
            .WithIsDefault(true)
            .Build();
        var langJp = new LanguageBuilder()
            .WithCultureInfo("ja-JP")
            .Build();

        LocalizationService.Save(langDk);
        LocalizationService.Save(langJp);

        var contentType = new ContentTypeBuilder()
            .WithAlias("variant")
            .WithContentVariation(ContentVariation.CultureAndSegment)
            .AddPropertyType()
            .WithAlias("title")
            .WithVariations(ContentVariation.Culture)
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .AddPropertyType()
            .WithAlias("body")
            .WithVariations(ContentVariation.CultureAndSegment)
            .WithDataTypeId(Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Build();
        ContentTypeService.Save(contentType);
        
        var root = new ContentBuilder()
            .WithKey(RootKey)
            .WithContentType(contentType)
            .WithCultureName("en-US", "Name")
            .WithCultureName("da-DK", "Navn")
            .WithCultureName("ja-JP", "名前")
            .Build();
        
        root.SetValue("title", "Root", "en-US");
        root.SetValue("title", "Rod", "da-DK");
        root.SetValue("title", "ル-ト", "ja-JP");
        
        root.SetValue("body", "body-segment-1", "en-US", "segment-1");
        root.SetValue("body", "body-segment-2", "en-US", "segment-2");
        root.SetValue("body", "krop-segment-1", "da-DK", "segment-1");
        root.SetValue("body", "krop-segment-2", "da-DK", "segment-2");
        root.SetValue("body", "ボディ-segment-1", "ja-JP", "segment-1");
        root.SetValue("body", "ボディ-segment-2", "ja-JP", "segment-2");

        if (publish)
        {
            ContentService.Save(root);
            ContentService.Publish(root, new []{ "*"});
        }
        else
        {
            ContentService.Save(root);
        }
        
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);

    }
    
    protected void CreateInvariantDocument(bool publish = false)
    {
        var dataType = new DataTypeBuilder()
            .WithId(0)
            .WithoutIdentity()
            .WithDatabaseType(ValueStorageType.Decimal)
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        
        DataTypeService.Save(dataType);
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
            .WithAlias("datetime")
            .WithDataTypeId(Constants.DataTypes.DateTime)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DateTime)
            .Done()
            .AddPropertyType()
            .WithAlias("decimalproperty")
            .WithDataTypeId(dataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Decimal)
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
                    datetime = CurrentDateTimeOffset.DateTime,
                    decimalproperty = DecimalValue
                })
            .Build();

        SaveOrPublish(root, publish);
        
        var content = ContentService.GetById(RootKey);
        Assert.That(content, Is.Not.Null);
    }
    
    protected void CreateInvariantDocumentTree(bool publish = false)
    {
        var dataType = new DataTypeBuilder()
            .WithId(0)
            .WithoutIdentity()
            .WithDatabaseType(ValueStorageType.Decimal)
            .AddEditor()
            .WithAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        
        DataTypeService.Save(dataType);
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
            .WithAlias("datetime")
            .WithDataTypeId(Constants.DataTypes.DateTime)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.DateTime)
            .Done()
            .AddPropertyType()
            .WithAlias("decimalproperty")
            .WithDataTypeId(dataType.Id)
            .WithPropertyEditorAlias(Constants.PropertyEditors.Aliases.Decimal)
            .Done()
            .Build();
        ContentTypeService.Save(contentType);
        contentType.AllowedContentTypes = [new ContentTypeSort(contentType.Key, 0, contentType.Alias)];
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
                    datetime = CurrentDateTimeOffset.DateTime,
                    decimalproperty = DecimalValue
                })
            .Build();

        SaveOrPublish(root, publish);
        
        var child = new ContentBuilder()
            .WithKey(ChildKey)
            .WithContentType(contentType)
            .WithName("Child")
            .WithParent(root)
            .WithPropertyValues(
                new
                {
                    title = "The child title",
                    count = 12,
                    datetime = CurrentDateTimeOffset.DateTime,
                    decimalproperty = DecimalValue
                })
            .Build();
        
        SaveOrPublish(child, publish);

        var grandchild = new ContentBuilder()
            .WithKey(GrandchildKey)
            .WithContentType(contentType)
            .WithName("Grandchild")
            .WithParent(child)
            .WithPropertyValues(
                new
                {
                    title = "The grandchild title",
                    count = 12,
                    datetime = CurrentDateTimeOffset.DateTime,
                    decimalproperty = DecimalValue
                })
            .Build();
        
        SaveOrPublish(grandchild, publish);
    }

    public void SaveOrPublish(IContent content, bool publish)
    {
        if (publish)
        {
            ContentService.Save(content);
            ContentService.Publish(content, new []{ "*"});
        }
        else
        {
            ContentService.Save(content);
        }
    }
    
}