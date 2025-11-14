using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentTypeEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.ContentTypeEditing;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.NotificationHandlers;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.TestHelpers;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Examine.Integration.Extensions;
using Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

namespace Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.SearchService;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class DocumentTypeTests : UmbracoIntegrationTest
{
    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private ISearcher Searcher => GetRequiredService<ISearcher>();

    private IContentTypeEditingService ContentTypeEditingService => GetRequiredService<IContentTypeEditingService>();

    private IContentEditingService ContentEditingService => GetRequiredService<IContentEditingService>();

    private IContentType _parentContentType = null!;
    private IContentType _childContentType = null!;


    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.AddExamineSearchProviderForTest<TestIndex, TestInMemoryDirectoryFactory>();
        builder.AddSearchCore();
        builder.AddNotificationHandler<ContentTypeChangedNotification, RebuildIndexesNotificationHandler>();
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
    }

    [Test]
    public async Task CannotSearchForRemovedProperty()
    {
        await CreateDocuments();
        // Act
        _childContentType.RemovePropertyType("title");
        await ContentTypeService.UpdateAsync(_childContentType, Constants.Security.SuperUserKey);

        await Task.Delay(3000);
        IContentType? contentType = await ContentTypeService.GetAsync(_childContentType.Key);
        // Assert.That(contentType!.PropertyTypes.Any(), Is.False);

        SearchResult finalResults = await Searcher.SearchAsync(
            Cms.Search.Core.Constants.IndexAliases.DraftContent,
            query: "Home Page");

        // We should still find the
        Assert.That(finalResults.Total, Is.EqualTo(1));
    }

    [Test]
    public async Task CannotSearchForRemovedDocument()
    {
        await CreateDocuments();

        // Act
        await ContentTypeService.DeleteAsync(_childContentType.Key, Constants.Security.SuperUserKey);

        await Task.Delay(3000);
        IContentType? contentType = await ContentTypeService.GetAsync(_childContentType.Key);

        SearchResult finalResults = await Searcher.SearchAsync(
            Cms.Search.Core.Constants.IndexAliases.DraftContent,
            query: "Home Page");

        Assert.That(finalResults.Total, Is.EqualTo(1));
        Assert.That(contentType, Is.Null);
    }

    private async Task CreateDocuments()
    {
        ContentTypeCreateModel parentContentTypeCreateModel = ContentTypeEditingBuilder.CreateSimpleContentType(
            "parentType",
            "Parent Type");
        Attempt<IContentType?, ContentTypeOperationStatus> parentContentTypeAttempt = await ContentTypeEditingService.CreateAsync(
            parentContentTypeCreateModel,
            Constants.Security.SuperUserKey);
        Assert.IsTrue(parentContentTypeAttempt.Success);
        _parentContentType = parentContentTypeAttempt.Result!;

        // Create Child ContentType
        ContentTypeCreateModel childContentTypeCreateModel = ContentTypeEditingBuilder.CreateSimpleContentType(
            "childType",
            "Child Type");
        Attempt<IContentType?, ContentTypeOperationStatus> childContentTypeAttempt = await ContentTypeEditingService.CreateAsync(
            childContentTypeCreateModel,
            Constants.Security.SuperUserKey);
        Assert.IsTrue(childContentTypeAttempt.Success);
        _childContentType = childContentTypeAttempt.Result!;

        // Update Parent ContentType to allow Child ContentType
        ContentTypeUpdateModel parentContentTypeUpdateModel = ContentTypeUpdateHelper.CreateContentTypeUpdateModel(_parentContentType);
        parentContentTypeUpdateModel.AllowedContentTypes =
        [
            new ContentTypeSort(_childContentType.Key, 0, childContentTypeCreateModel.Alias)
        ];
        Attempt<IContentType?, ContentTypeOperationStatus> updatedParentResult = await ContentTypeEditingService.UpdateAsync(
            _parentContentType,
            parentContentTypeUpdateModel,
            Constants.Security.SuperUserKey);
        Assert.IsTrue(updatedParentResult.Success);

        // Create Root Document (Parent)
        ContentCreateModel rootCreateModel = ContentEditingBuilder.CreateSimpleContent(_parentContentType.Key, "Root Document");
        Attempt<ContentCreateResult, ContentEditingOperationStatus> createRootResult = await ContentEditingService.CreateAsync(rootCreateModel, Constants.Security.SuperUserKey);
        Assert.IsTrue(createRootResult.Success);
        IContent? rootDocument = createRootResult.Result.Content;

        // Create Child Document under Root
        ContentCreateModel childCreateModel = ContentEditingBuilder.CreateSimpleContent(
            _childContentType.Key,
            "Child Document",
            rootDocument!.Key);
        Attempt<ContentCreateResult, ContentEditingOperationStatus> createChildResult = await ContentEditingService.CreateAsync(childCreateModel, Constants.Security.SuperUserKey);
        Assert.IsTrue(createChildResult.Success);
    }
}
