using Examine;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.NotificationHandlers;
using Umbraco.Cms.Search.Provider.Examine.Services;
using IndexOptions = Umbraco.Cms.Search.Core.Configuration.IndexOptions;
using ISearcher = Umbraco.Cms.Search.Core.Services.ISearcher;

namespace Umbraco.Test.Search.Unit.Tests;

public class RebuildNotificationHandlerTests
{
    [Test]
    public void Handle_WhenAnIndexCannotBeOpened_SkipsItAndChecksTheRemainingIndexes()
    {
        const string lockedIndexAlias = "LockedIndex";
        const string missingIndexAlias = "MissingIndex";

        var indexOptions = new IndexOptions();
        indexOptions.RegisterContentIndex<IIndexer, ISearcher, IContentChangeStrategy>(lockedIndexAlias, UmbracoObjectTypes.Document);
        indexOptions.RegisterContentIndex<IIndexer, ISearcher, IContentChangeStrategy>(missingIndexAlias, UmbracoObjectTypes.Document);

        var lockedIndexMock = new Mock<IIndex>();
        lockedIndexMock
            .Setup(m => m.IndexExists())
            .Throws(new IOException("The process cannot access the file because it is being used by another process."));

        var missingIndexMock = new Mock<IIndex>();
        missingIndexMock.Setup(m => m.IndexExists()).Returns(false);

        var examineManagerMock = new Mock<IExamineManager>();
        IIndex? lockedIndex = lockedIndexMock.Object;
        IIndex? missingIndex = missingIndexMock.Object;
        examineManagerMock.Setup(m => m.TryGetIndex(lockedIndexAlias, out lockedIndex)).Returns(true);
        examineManagerMock.Setup(m => m.TryGetIndex(missingIndexAlias, out missingIndex)).Returns(true);

        var activeIndexManagerMock = new Mock<IActiveIndexManager>();
        activeIndexManagerMock.Setup(m => m.ResolveActiveIndexName(It.IsAny<string>())).Returns<string>(alias => alias);

        var originProviderMock = new Mock<IOriginProvider>();
        originProviderMock.Setup(m => m.GetCurrent()).Returns("origin");

        var contentIndexingServiceMock = new Mock<IContentIndexingService>();

        var handler = new RebuildNotificationHandler(
            examineManagerMock.Object,
            activeIndexManagerMock.Object,
            contentIndexingServiceMock.Object,
            Options.Create(indexOptions),
            NullLogger<RebuildNotificationHandler>.Instance,
            originProviderMock.Object);

        Assert.DoesNotThrow(() => handler.Handle(new UmbracoApplicationStartedNotification(false)));

        contentIndexingServiceMock.Verify(m => m.Rebuild(missingIndexAlias, "origin"), Times.Once);
        contentIndexingServiceMock.Verify(m => m.Rebuild(lockedIndexAlias, It.IsAny<string>()), Times.Never);
    }
}
