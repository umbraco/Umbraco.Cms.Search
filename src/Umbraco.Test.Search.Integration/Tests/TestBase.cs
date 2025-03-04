using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Cms.Infrastructure.Sync;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Test.Search.Integration.Services;

namespace Umbraco.Test.Search.Integration.Tests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public abstract class TestBase : UmbracoIntegrationTest
{
    protected TestIndexService IndexService { get; } = new ();

    protected IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>(); 

    protected IContentService ContentService => GetRequiredService<IContentService>();
        
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);
        builder.AddComposers();
        builder.Services.AddUnique<IBackgroundTaskQueue, BackgroundTaskQueue>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);
        services.AddTransient<IIndexService>(_ => IndexService);
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
