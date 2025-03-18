using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.None)]
public abstract class ContentIndexingServiceTestsBase : UmbracoIntegrationTest
{
    private TestContentChangeStrategy _strategy = new();

    protected TestContentChangeStrategy Strategy => _strategy;

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddSearchCore();
        
        builder.Services.AddUnique<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
    }

    protected class TestContentChangeStrategy : IPublishedContentChangeStrategy, IDraftContentChangeStrategy
    {
        public Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
        {
            HandledIndexInfos.Add(indexInfos.ToList());
            return Task.CompletedTask;
        }

        public List<List<IndexInfo>> HandledIndexInfos { get; } = new();
    }
}