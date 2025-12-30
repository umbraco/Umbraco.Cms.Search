using System.Diagnostics;
using System.Reflection;
using Examine;
using Examine.Lucene.Providers;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentTypeEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.ContentTypeEditing;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Install;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.NotificationHandlers;
using Umbraco.Cms.Search.Core.Persistence;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Examine.Integration.Attributes;
using Umbraco.Test.Search.Examine.Integration.Extensions;
using Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.PersistenceTests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class DocumentServiceTests : UmbracoIntegrationTest
{
    private bool _indexingComplete;

    private PackageMigrationRunner _packageMigrationRunner => GetRequiredService<PackageMigrationRunner>();

    private IRuntimeState RuntimeState => GetRequiredService<IRuntimeState>();

    private IContentTypeEditingService ContentTypeEditingService => GetRequiredService<IContentTypeEditingService>();

    private IContentEditingService ContentEditingService => GetRequiredService<IContentEditingService>();

    private IContentService ContentService => GetRequiredService<IContentService>();

    private Umbraco.Cms.Search.Core.Services.ContentIndexing.IDocumentService DocumentService => GetRequiredService<Umbraco.Cms.Search.Core.Services.ContentIndexing.IDocumentService>();

    private IDocumentRepository DocumentRepository => GetRequiredService<IDocumentRepository>();

    private IContent _rootDocument = null!;

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
    public async Task CanRunMigration()
    {
        await TestSetup(false);
        using IScope scope = ScopeProvider.CreateScope(autoComplete: true);
        IEnumerable<string> tables = scope.Database.SqlContext.SqlSyntax.GetTablesInSchema(scope.Database);
        var result = tables.Any(x => x.InvariantEquals(Constants.Persistence.DocumentTableName));
        Assert.That(result, Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task AddsEntryToDatabaseAfterIndexing(bool publish)
    {
        await TestSetup(publish);
        var changeStrategy = GetStrategy(publish);
        using IScope scope = ScopeProvider.CreateScope(autoComplete: true);
        Document? doc = await DocumentRepository.GetAsync(_rootDocument.Key, publish);
        Assert.That(doc, Is.Not.Null);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task UpdatesEntryInDatabaseAfterPropertyChange(bool publish)
    {
        await TestSetup(publish);
        var indexAlias = GetIndexAlias(publish);
        var changeStrategy = GetStrategy(publish);

        IndexField[] initialFields;
        // Verify initial document exists
        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            Document? initialDoc = await DocumentRepository.GetAsync(_rootDocument.Key, publish);
            Assert.That(initialDoc, Is.Not.Null);
            initialFields = initialDoc!.Fields;
        }


        // Update the content name
        _rootDocument.Name = "Updated Root Document";

        await WaitForIndexing(indexAlias, () =>
        {
            ContentService.Save(_rootDocument);
            if (publish)
            {
                ContentService.Publish(_rootDocument, ["*"]);
            }

            return Task.CompletedTask;
        });

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            // Verify the document was updated
            Document? updatedDoc = await DocumentRepository.GetAsync(_rootDocument.Key, publish);
            Assert.That(updatedDoc, Is.Not.Null);
            Assert.That(updatedDoc!.Fields, Is.Not.EqualTo(initialFields));
            Assert.That(FieldsContainText(updatedDoc.Fields, "Updated Root Document"), Is.True);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task RemovesEntryFromDatabaseAfterDeletion(bool publish)
    {
        await TestSetup(publish);
        var indexAlias = GetIndexAlias(publish);
        var changeStrategy = GetStrategy(publish);

        // Verify initial document exists
        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            Document? initialDoc = await DocumentRepository.GetAsync(_rootDocument.Key, publish);
            Assert.That(initialDoc, Is.Not.Null);
        }

        // Delete the content
        await WaitForIndexing(indexAlias, () =>
        {
            ContentService.Delete(_rootDocument);
            return Task.CompletedTask;
        });

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            // Verify the document was removed
            Document? deletedDoc = await DocumentRepository.GetAsync(_rootDocument.Key, publish);
            Assert.That(deletedDoc, Is.Null);
        }
    }

    private string GetIndexAlias(bool publish) => publish ? Constants.IndexAliases.PublishedContent : Constants.IndexAliases.DraftContent;

    private string GetStrategy(bool publish) => publish ? "PublishedContentChangeStrategy" : "DraftContentChangeStrategy";

    public async Task TestSetup(bool publish)
    {
        await _packageMigrationRunner.RunPackageMigrationsIfPendingAsync("Umbraco CMS Search").ConfigureAwait(false);
        Assert.That(RuntimeState.Level, Is.EqualTo(RuntimeLevel.Run));

        ContentTypeCreateModel contentTypeCreateModel = ContentTypeEditingBuilder.CreateSimpleContentType(
            "parentType",
            "Parent Type");
        Attempt<IContentType?, ContentTypeOperationStatus> contentTypeAttempt = await ContentTypeEditingService.CreateAsync(
            contentTypeCreateModel,
            Umbraco.Cms.Core.Constants.Security.SuperUserKey);
        Assert.That(contentTypeAttempt.Success, Is.True);
        IContentType contentType = contentTypeAttempt.Result!;
        ContentCreateModel rootCreateModel = ContentEditingBuilder.CreateSimpleContent(contentType.Key, "Root Document");

        var indexAlias = GetIndexAlias(publish);
        await WaitForIndexing(indexAlias, async () =>
        {
            Attempt<ContentCreateResult, ContentEditingOperationStatus> createRootResult = await ContentEditingService.CreateAsync(rootCreateModel, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
            Assert.That(createRootResult.Success, Is.True);
            _rootDocument = createRootResult.Result.Content!;

            if (publish)
            {
                ContentService.Publish(_rootDocument, ["*"]);
            }
        });
    }

    protected async Task WaitForIndexing(string indexAlias, Func<Task> indexUpdatingAction)
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

    private static bool FieldsContainText(IndexField[] fields, string text)
        => fields.Any(f =>
            (f.Value.Texts?.Any(t => t.Contains(text)) == true) ||
            (f.Value.TextsR1?.Any(t => t.Contains(text)) == true) ||
            (f.Value.TextsR2?.Any(t => t.Contains(text)) == true) ||
            (f.Value.TextsR3?.Any(t => t.Contains(text)) == true) ||
            (f.Value.Keywords?.Any(k => k.Contains(text)) == true));
}
