using System.Diagnostics;
using System.Reflection;
using Examine;
using Examine.Lucene.Providers;
using Microsoft.Extensions.DependencyInjection;
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
public class RepositoryContentTests : UmbracoIntegrationTest
{
    private bool _indexingComplete;

    private IScopeProvider _scopeProvider => GetRequiredService<IScopeProvider>();

    private PackageMigrationRunner _packageMigrationRunner => GetRequiredService<PackageMigrationRunner>();

    private IRuntimeState RuntimeState => Services.GetRequiredService<IRuntimeState>();

    private IContentTypeEditingService ContentTypeEditingService => GetRequiredService<IContentTypeEditingService>();

    private IContentEditingService ContentEditingService => GetRequiredService<IContentEditingService>();

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
        await TestSetup();
        using IScope scope = _scopeProvider.CreateScope(autoComplete: true);
        IEnumerable<string> tables = scope.Database.SqlContext.SqlSyntax.GetTablesInSchema(scope.Database);
        var result = tables.Any(x => x.InvariantEquals(Constants.Persistence.DocumentTableName));
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task AddsEntryToDatabaseAfterIndexing()
    {
        await TestSetup();
        Document doc = await DocumentRepository.Get(_rootDocument.Key, Constants.IndexAliases.DraftContent);
        Assert.That(doc, Is.Not.Null);
    }

    public async Task TestSetup()
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
        await WaitForIndexing(Constants.IndexAliases.DraftContent, async () =>
        {
            Attempt<ContentCreateResult, ContentEditingOperationStatus> createRootResult = await ContentEditingService.CreateAsync(rootCreateModel, Umbraco.Cms.Core.Constants.Security.SuperUserKey);
            Assert.That(createRootResult.Success, Is.True);
            _rootDocument = createRootResult.Result.Content!;
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
}
