using System.Diagnostics;
using System.Reflection;
using Examine;
using Examine.Lucene.Providers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Install;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.NotificationHandlers;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Examine.Integration.Attributes;
using Umbraco.Test.Search.Examine.Integration.Extensions;
using Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.PersistenceTests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class MediaServiceTests : UmbracoIntegrationTest
{
    private bool _indexingComplete;

    private PackageMigrationRunner PackageMigrationRunner => GetRequiredService<PackageMigrationRunner>();

    private IRuntimeState RuntimeState => Services.GetRequiredService<IRuntimeState>();

    private IMediaTypeService MediaTypeService => GetRequiredService<IMediaTypeService>();

    private IMediaService MediaService => GetRequiredService<IMediaService>();

    private Umbraco.Cms.Search.Core.Services.ContentIndexing.IDocumentService DocumentService => GetRequiredService<Umbraco.Cms.Search.Core.Services.ContentIndexing.IDocumentService>();

    private IMedia _rootMedia = null!;

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddExamineSearchProviderForTest<TestIndex, TestInMemoryDirectoryFactory>();

        builder.AddSearchCore();

        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        builder.AddNotificationHandler<LanguageDeletedNotification, RebuildIndexesNotificationHandler>();

        // the core ConfigureBuilderAttribute won't execute from other assemblies at the moment, so this is a workaround
        var testType = Type.GetType(TestContext.CurrentContext.Test.ClassName!);
        if (testType is not null)
        {
            MethodInfo? methodInfo = testType.GetMethod(TestContext.CurrentContext.Test.Name);
            if (methodInfo is not null)
            {
                foreach (ConfigureUmbracoBuilderAttribute attribute in methodInfo.GetCustomAttributes(typeof(ConfigureUmbracoBuilderAttribute), true).OfType<ConfigureUmbracoBuilderAttribute>())
                {
                    attribute.Execute(builder, testType);
                }
            }
        }
    }

    [Test]
    public async Task AddsEntryToDatabaseAfterIndexing()
    {
        await TestSetup();
        Document? doc = await DocumentService.GetAsync(_rootMedia.Key, Constants.IndexAliases.DraftMedia);
        Assert.That(doc, Is.Not.Null);
    }

    [Test]
    public async Task UpdatesEntryInDatabaseAfterPropertyChange()
    {
        await TestSetup();

        // Verify initial document exists
        Document? initialDoc = await DocumentService.GetAsync(_rootMedia.Key, Constants.IndexAliases.DraftMedia);
        Assert.That(initialDoc, Is.Not.Null);
        var initialFields = initialDoc!.Fields;

        // Update the media name
        _rootMedia.Name = "Updated Root Media";

        await WaitForIndexing(Constants.IndexAliases.DraftMedia, () =>
        {
            MediaService.Save(_rootMedia);
            return Task.CompletedTask;
        });

        // Verify the document was updated
        Document? updatedDoc = await DocumentService.GetAsync(_rootMedia.Key, Constants.IndexAliases.DraftMedia);
        Assert.That(updatedDoc, Is.Not.Null);
        Assert.That(updatedDoc!.Fields, Is.Not.EqualTo(initialFields));
        Assert.That(updatedDoc.Fields, Does.Contain("Updated Root Media"));
    }

    [Test]
    public async Task RemovesEntryFromDatabaseAfterDeletion()
    {
        await TestSetup();

        // Verify initial document exists
        Document? initialDoc = await DocumentService.GetAsync(_rootMedia.Key, Constants.IndexAliases.DraftMedia);
        Assert.That(initialDoc, Is.Not.Null);

        // Delete the media
        await WaitForIndexing(Constants.IndexAliases.DraftMedia, () =>
        {
            MediaService.Delete(_rootMedia);
            return Task.CompletedTask;
        });

        // Verify the document was removed
        Document? deletedDoc = await DocumentService.GetAsync(_rootMedia.Key, Constants.IndexAliases.DraftMedia);
        Assert.That(deletedDoc, Is.Null);
    }

    private async Task TestSetup()
    {
        await PackageMigrationRunner.RunPackageMigrationsIfPendingAsync("Umbraco CMS Search").ConfigureAwait(false);
        Assert.That(RuntimeState.Level, Is.EqualTo(RuntimeLevel.Run));

        IMediaType mediaType = new MediaTypeBuilder()
            .WithAlias("testMediaType")
            .AddPropertyGroup()
            .AddPropertyType()
            .WithAlias("altText")
            .WithDataTypeId(Umbraco.Cms.Core.Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Done()
            .Build();
        await MediaTypeService.CreateAsync(mediaType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        await WaitForIndexing(Constants.IndexAliases.DraftMedia, () =>
        {
            _rootMedia = new MediaBuilder()
                .WithMediaType(mediaType)
                .WithName("Root Media")
                .WithPropertyValues(new { altText = "The media alt text" })
                .Build();
            MediaService.Save(_rootMedia);
            return Task.CompletedTask;
        });
    }

    private async Task WaitForIndexing(string indexAlias, Func<Task> indexUpdatingAction)
    {
        var index = (LuceneIndex)GetRequiredService<IExamineManager>().GetIndex(indexAlias);
        index.IndexCommitted += IndexCommited;

        var hasDoneAction = false;

        var stopWatch = Stopwatch.StartNew();

        while (_indexingComplete is false)
        {
            if (hasDoneAction is false)
            {
                await indexUpdatingAction();
                hasDoneAction = true;
            }

            if (stopWatch.ElapsedMilliseconds > 5000)
            {
                throw new TimeoutException("Indexing timed out");
            }

            await Task.Delay(250);
        }

        _indexingComplete = false;
        index.IndexCommitted -= IndexCommited;
    }

    private void IndexCommited(object? sender, EventArgs e)
    {
        _indexingComplete = true;
    }
}
