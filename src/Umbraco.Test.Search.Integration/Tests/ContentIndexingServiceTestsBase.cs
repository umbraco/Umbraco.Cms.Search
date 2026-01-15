using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Sync;
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
    protected TestContentChangeStrategy Strategy { get; } = new();

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddSearchCore();

        builder.Services.Replace(Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<IBackgroundTaskQueue, ImmediateBackgroundTaskQueue>());
        builder.Services.Replace(Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<IServerMessenger, LocalServerMessenger>());
    }

    protected class TestContentChangeStrategy : IPublishedContentChangeStrategy, IDraftContentChangeStrategy
    {
        public Task HandleAsync(IEnumerable<IndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
        {
            HandledIndexInfos.Add(indexInfos.ToList());
            return Task.CompletedTask;
        }

        public Task RebuildAsync(IndexInfo indexInfo, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public List<List<IndexInfo>> HandledIndexInfos { get; } = new();
    }
}
