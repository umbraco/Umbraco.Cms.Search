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
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Persistence;
using Umbraco.Cms.Search.Core.NotificationHandlers;
using Umbraco.Cms.Search.Core.Persistence;
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
public class MemberServiceTests : UmbracoIntegrationTest
{
    private bool _indexingComplete;

    private PackageMigrationRunner PackageMigrationRunner => GetRequiredService<PackageMigrationRunner>();

    private IRuntimeState RuntimeState => Services.GetRequiredService<IRuntimeState>();

    private IMemberTypeService MemberTypeService => GetRequiredService<IMemberTypeService>();

    private IMemberService MemberService => GetRequiredService<IMemberService>();

    private IDocumentRepository DocumentRepository => GetRequiredService<IDocumentRepository>();

    private IMember _member = null!;

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddExamineSearchProviderForTest<TestIndex, TestInMemoryDirectoryFactory>();

        builder.AddSearchCore();

        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        builder.AddNotificationAsyncHandler<LanguageDeletedNotification, RebuildIndexesNotificationHandler>();

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
        using IScope scope = ScopeProvider.CreateScope(autoComplete: true);
        Document? doc = await DocumentRepository.GetAsync(_member.Key, false);
        Assert.That(doc, Is.Not.Null);
    }

    [Test]
    public async Task UpdatesEntryInDatabaseAfterPropertyChange()
    {
        await TestSetup();

        IndexField[] initialFields;
        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            // Verify initial document exists
            Document? initialDoc = await DocumentRepository.GetAsync(_member.Key, false);
            Assert.That(initialDoc, Is.Not.Null);
            initialFields = initialDoc!.Fields;
        }

        // Update the member name
        _member.Name = "Updated Member Name";

        await WaitForIndexing(Constants.IndexAliases.DraftMembers, () =>
        {
            MemberService.Save(_member);
            return Task.CompletedTask;
        });

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            // Verify the document was updated
            Document? updatedDoc = await DocumentRepository.GetAsync(_member.Key, false);
            Assert.That(updatedDoc, Is.Not.Null);
            Assert.That(updatedDoc!.Fields, Is.Not.EqualTo(initialFields));
            Assert.That(FieldsContainText(updatedDoc.Fields, "Updated Member Name"), Is.True);
        }
    }

    [Test]
    [Ignore("This does not work in 16, as the MemberCacheRefresher passes id, and the IdKeyMap is already cleared, this should work in 17 as it uses key.")]
    public async Task RemovesEntryFromDatabaseAfterDeletion()
    {
        await TestSetup();

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            // Verify initial document exists
            Document? initialDoc = await DocumentRepository.GetAsync(_member.Key, false);
            Assert.That(initialDoc, Is.Not.Null);
        }

        // Delete the member
        MemberService.Delete(_member);
        await Task.Delay(4000);

        using (ScopeProvider.CreateScope(autoComplete: true))
        {
            // Verify the document was removed
            Document? deletedDoc = await DocumentRepository.GetAsync(_member.Key, false);
            Assert.That(deletedDoc, Is.Null);
        }
    }

    private async Task TestSetup()
    {
        await PackageMigrationRunner.RunPackageMigrationsIfPendingAsync("Umbraco CMS Search").ConfigureAwait(false);
        Assert.That(RuntimeState.Level, Is.EqualTo(RuntimeLevel.Run));

        IMemberType memberType = new MemberTypeBuilder()
            .WithAlias("testMemberType")
            .AddPropertyGroup()
            .AddPropertyType()
            .WithAlias("organization")
            .WithDataTypeId(Umbraco.Cms.Core.Constants.DataTypes.Textbox)
            .WithPropertyEditorAlias(Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextBox)
            .Done()
            .Done()
            .Build();
        await MemberTypeService.CreateAsync(memberType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        await WaitForIndexing(Constants.IndexAliases.DraftMembers, () =>
        {
            _member = new MemberBuilder()
                .WithMemberType(memberType)
                .WithName("Test Member")
                .WithEmail("testmember@local")
                .WithLogin("testmember@local", "Test123456")
                .AddPropertyData()
                .WithKeyValue("organization", "Test Organization")
                .Done()
                .Build();
            MemberService.Save(_member);
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

    private static bool FieldsContainText(IndexField[] fields, string text)
        => fields.Any(f =>
            (f.Value.Texts?.Any(t => t.Contains(text)) == true) ||
            (f.Value.TextsR1?.Any(t => t.Contains(text)) == true) ||
            (f.Value.TextsR2?.Any(t => t.Contains(text)) == true) ||
            (f.Value.TextsR3?.Any(t => t.Contains(text)) == true) ||
            (f.Value.Keywords?.Any(k => k.Contains(text)) == true));
}
