using Umbraco.Cms.Infrastructure.HostedServices;

namespace Umbraco.Test.Search.Integration.Tests;

internal class ImmediateBackgroundTaskQueue : IBackgroundTaskQueue
{
    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        => workItem(CancellationToken.None).GetAwaiter().GetResult();

    public Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException($"${nameof(ImmediateBackgroundTaskQueue)} should execute background jobs immediately, so {nameof(DequeueAsync)} is not implemented.");
}