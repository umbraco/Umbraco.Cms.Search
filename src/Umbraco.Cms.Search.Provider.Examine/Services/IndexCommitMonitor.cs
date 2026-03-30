using Examine;
using Examine.Lucene.Providers;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

internal sealed class IndexCommitMonitor : IIndexCommitMonitor
{
    private static readonly TimeSpan CommitTimeout = TimeSpan.FromSeconds(30);

    private readonly IExamineManager _examineManager;

    public IndexCommitMonitor(IExamineManager examineManager)
        => _examineManager = examineManager;

    public async Task<bool> WaitForCommitAsync(string indexAlias, CancellationToken cancellationToken)
    {
        if (_examineManager.TryGetIndex(indexAlias, out IIndex? index) is false || index is not LuceneIndex luceneIndex)
        {
            return false;
        }

        if (index is IIndexStats stats && stats.GetDocumentCount() > 0)
        {
            return false;
        }

        var committed = false;
        EventHandler onCommitted = (_, _) => committed = true;

        try
        {
            luceneIndex.IndexCommitted += onCommitted;

            // Re-check after subscribing to avoid a race where the commit happened
            // between the initial check and subscribing to the event.
            if (index is IIndexStats statsAfterSubscribe && statsAfterSubscribe.GetDocumentCount() > 0)
            {
                return true;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (!committed && stopwatch.Elapsed < CommitTimeout)
            {
                await Task.Delay(CommitTimeout, cancellationToken);
            }

            stopwatch.Stop();
            return committed;
        }
        finally
        {
            luceneIndex.IndexCommitted -= onCommitted;
        }
    }
}
