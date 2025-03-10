using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Cms.Infrastructure.Sync;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class TestBase : UmbracoIntegrationTest
{
    protected static class IndexAliases
    {
        public const string PublishedContent = "TestPublishedContentIndex";
        public const string DraftContent = "TestDraftContentIndex";
    }
    
    protected TestIndexService IndexService { get; } = new ();

    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>(); 

    protected IContentService ContentService => GetRequiredService<IContentService>();
        
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.AddSearchCore();
        
        builder.Services.AddUnique<IBackgroundTaskQueue, BackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();

        builder.Services.AddUnique<TestIndexService>(_ => IndexService);
        builder.Services.AddUnique<IIndexService>(_ => IndexService);

        builder.Services.Configure<IndexOptions>(options =>
        {
            // TODO: need to test both permutations of options - explicit and implicit
            options.RegisterIndex<IIndexService, IPublishedContentChangeStrategy>(IndexAliases.PublishedContent);
            options.RegisterIndex<IIndexService, IDraftContentChangeStrategy>(IndexAliases.DraftContent);
            // options.RegisterIndex<TestIndexService, PublishedContentChangeStrategy>(IndexAliases.PublishedContent);
            // options.RegisterIndex<TestIndexService, DraftContentChangeStrategy>(IndexAliases.DraftContent);
        });
    }
    
    private class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
            => workItem(CancellationToken.None).GetAwaiter().GetResult();

        public Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException($"${nameof(BackgroundTaskQueue)} should execute background jobs immediately, so {nameof(DequeueAsync)} is not implemented.");
    }
    
    private class LocalServerMessenger : ServerMessengerBase
    {
        public LocalServerMessenger()
            : base(false)
        {
        }

        public override void SendMessages()
        {
        }

        public override void Sync()
        {
        }

        protected override void DeliverRemote(ICacheRefresher refresher, MessageType messageType, IEnumerable<object>? ids = null, string? json = null)
        {
        }
    }
}
