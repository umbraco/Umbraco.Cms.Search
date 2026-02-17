using System.Collections.Concurrent;
using Examine;
using Microsoft.Extensions.Logging;

namespace Umbraco.Cms.Search.Provider.Examine.Services;

internal sealed class ActiveIndexManager : IActiveIndexManager
{
    private readonly IExamineManager _examineManager;
    private readonly ILogger<ActiveIndexManager> _logger;
    private readonly ConcurrentDictionary<string, Index> _indexes = new();

    internal const string SuffixA = "_a";
    internal const string SuffixB = "_b";

    public ActiveIndexManager(IExamineManager examineManager, ILogger<ActiveIndexManager> logger)
    {
        _examineManager = examineManager;
        _logger = logger;
    }

    public string ResolveActiveIndexName(string indexAlias)
    {
        Index slot = GetOrCreateSlot(indexAlias);
        return indexAlias + slot.ActiveSuffix;
    }

    public string ResolveShadowIndexName(string indexAlias)
    {
        Index slot = GetOrCreateSlot(indexAlias);
        return indexAlias + slot.ShadowSuffix;
    }

    public bool IsRebuilding(string indexAlias) => _indexes.TryGetValue(indexAlias, out Index? index) && index.IsRebuilding;

    public void StartRebuilding(string indexAlias) =>
        _indexes.AddOrUpdate(
            indexAlias,
            _ => new Index(SuffixA, true),
            (_, current) =>
            {
                if (current.IsRebuilding)
                {
                    _logger.LogWarning("Rebuild already in progress for {IndexAlias}, ignoring start request.", indexAlias);
                    return current;
                }

                _logger.LogInformation(
                    "Starting rebuild for {IndexAlias}. Active: {Active}, Shadow: {Shadow}",
                    indexAlias,
                    indexAlias + current.ActiveSuffix,
                    indexAlias + current.ShadowSuffix);

                return current with { IsRebuilding = true };
            });

    public void CompleteRebuilding(string indexAlias) =>
        _indexes.AddOrUpdate(
            indexAlias,
            _ => new Index(SuffixA, false),
            (_, current) =>
            {
                if (current.IsRebuilding is false)
                {
                    _logger.LogWarning("No rebuild in progress for {IndexAlias}, ignoring complete request.", indexAlias);
                    return current;
                }

                _logger.LogInformation(
                    "Completing rebuild for {IndexAlias}. Swapping active from {OldActive} to {NewActive}.",
                    indexAlias,
                    indexAlias + current.ActiveSuffix,
                    indexAlias + current.ShadowSuffix);

                return new Index(current.ShadowSuffix, false);
            });

    public void CancelRebuilding(string indexAlias) =>
        _indexes.AddOrUpdate(
            indexAlias,
            _ => new Index(SuffixA, false),
            (_, current) =>
            {
                if (current.IsRebuilding is false)
                {
                    return current;
                }

                _logger.LogWarning("Cancelling rebuild for {IndexAlias}. Active index remains {Active}.", indexAlias, indexAlias + current.ActiveSuffix);
                return current with { IsRebuilding = false };
            });

    private Index GetOrCreateSlot(string indexAlias) => _indexes.GetOrAdd(indexAlias, DetermineInitialSlot);

    private Index DetermineInitialSlot(string indexAlias)
    {
        var aExists = IndexHasData(indexAlias + SuffixA);
        var bExists = IndexHasData(indexAlias + SuffixB);

        if (aExists && bExists)
        {
            var aCount = GetDocumentCount(indexAlias + SuffixA);
            var bCount = GetDocumentCount(indexAlias + SuffixB);

            var activeSuffix = bCount > aCount ? SuffixB : SuffixA;
            _logger.LogInformation("Both indexes exist for {IndexAlias}. Selecting {Active} as active (A: {ACount} docs, B: {BCount} docs).", indexAlias, indexAlias + activeSuffix, aCount, bCount);
            return new Index(activeSuffix, false);
        }

        if (bExists)
        {
            _logger.LogInformation("Only index _b exists for {IndexAlias}. Using _b as active.", indexAlias);
            return new Index(SuffixB, false);
        }

        if (aExists)
        {
            _logger.LogInformation("Only index _a exists for {IndexAlias}. Using _a as active.", indexAlias);
        }

        // Default to _a (either _a exists or neither exists)
        return new Index(SuffixA, false);
    }

    private bool IndexHasData(string physicalIndexName) => _examineManager.TryGetIndex(physicalIndexName, out IIndex? index) && index.IndexExists();

    private long GetDocumentCount(string physicalIndexName)
    {
        if (_examineManager.TryGetIndex(physicalIndexName, out IIndex? index) is false)
        {
            return 0;
        }

        if (index is IIndexStats stats)
        {
            return stats.GetDocumentCount();
        }

        return 0;
    }

    private sealed record Index(string ActiveSuffix, bool IsRebuilding)
    {
        public string ShadowSuffix => ActiveSuffix == SuffixA ? SuffixB : SuffixA;
    }
}
