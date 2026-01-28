using Examine;
using Examine.Lucene.Directories;
using Examine.Lucene.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using Umbraco.Cms.Search.Provider.Examine.Services;
using Umbraco.Test.Search.Examine.Integration.Tests.ContentTests.IndexService;

namespace Umbraco.Test.Search.Examine.Integration.Tests;

/// <summary>
/// Tests to verify that custom <see cref="IndexValue"/> subclasses can be indexed
/// using a custom <see cref="Indexer"/> implementation.
/// </summary>
[TestFixture]
public class CustomIndexValueTests
{
    private ServiceProvider _serviceProvider = null!;
    private const string IndexAlias = Constants.IndexAliases.PublishedContent;
    private const string CustomGuidFieldName = "customGuids";

    private Dictionary<int, Guid> DocumentIds { get; } = [];
    private Dictionary<int, Guid[]> DocumentCustomGuids { get; } = [];

    [OneTimeSetUp]
    protected async Task PerformOneTimeSetUpAsync()
    {
        var serviceCollection = new ServiceCollection();

        // Use custom service registration that includes our custom indexer
        serviceCollection.AddCustomIndexerServicesForTest<TestIndex, TestInMemoryDirectoryFactory>();
        serviceCollection.AddLogging();

        _serviceProvider = serviceCollection.BuildServiceProvider();

        await EnsureIndex();

        IIndexer indexer = GetRequiredService<IIndexer>();

        // Create test documents with custom IndexValue containing Guids
        for (var i = 1; i <= 10; i++)
        {
            var id = Guid.NewGuid();
            DocumentIds[i] = id;

            // Create some custom Guids for each document
            Guid[] customGuids = new[] { Guid.NewGuid(), Guid.NewGuid() };
            DocumentCustomGuids[i] = customGuids;

            await indexer.AddOrUpdateAsync(
                IndexAlias,
                id,
                UmbracoObjectTypes.Document,
                [new Variation(Culture: null, Segment: null)],
                [
                    new IndexField(
                        Constants.FieldNames.PathIds,
                        new IndexValue { Keywords = [id.AsKeyword()] },
                        Culture: null,
                        Segment: null),
                    // Use our custom IndexValue with Guids property
                    new IndexField(
                        CustomGuidFieldName,
                        new CustomIndexValue
                        {
                            Keywords = [$"doc{i}"],
                            Guids = customGuids
                        },
                        Culture: null,
                        Segment: null),
                ],
                null);
        }

        await Task.Delay(3000);
    }

    [OneTimeTearDown]
    protected async Task PerformOneTimeTearDownAsync()
    {
        await DeleteIndex();

        if (_serviceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }

    [Test]
    public void CustomIndexValueCanBeCreated()
    {
        // Verify that custom IndexValue can be created with additional properties
        var customValue = new CustomIndexValue
        {
            Keywords = ["test"],
            Guids = [Guid.NewGuid(), Guid.NewGuid()]
        };

        Assert.Multiple(() =>
        {
            Assert.That(customValue.Keywords, Has.Exactly(1).Items);
            Assert.That(customValue.Guids, Has.Exactly(2).Items);
        });
    }

    [Test]
    public void CustomIndexerIsResolved()
    {
        // Verify that our custom indexer is being used
        IIndexer indexer = GetRequiredService<IIndexer>();
        Assert.That(indexer, Is.InstanceOf<CustomIndexer>(), $"Expected CustomIndexer but got {indexer.GetType().FullName}");
    }

    [Test]
    public void CustomIndexValueInheritsFromIndexValue()
    {
        var customValue = new CustomIndexValue
        {
            Keywords = ["test"],
            Texts = ["some text"],
            Guids = [Guid.NewGuid()]
        };

        // Verify inheritance - can be assigned to IndexValue
        IndexValue baseValue = customValue;

        Assert.Multiple(() =>
        {
            Assert.That(baseValue.Keywords, Is.EqualTo(customValue.Keywords));
            Assert.That(baseValue.Texts, Is.EqualTo(customValue.Texts));
        });
    }

    [Test]
    public void CustomGuidsAreIndexed()
    {
        // Get the Examine index directly to verify custom fields were indexed
        IExamineManager examineManager = GetRequiredService<IExamineManager>();
        IIndex index = examineManager.GetIndex(IndexAlias);

        // Search for a document by its custom guid
        Guid targetGuid = DocumentCustomGuids[1][0];
        var fieldName = $"Field_{CustomGuidFieldName}_guids_keywords";

        ISearchResults results = index.Searcher
            .CreateQuery()
            .Field(fieldName, targetGuid.ToString())
            .Execute();

        Assert.That(results.TotalItemCount, Is.EqualTo(1), $"Expected to find 1 document with guid {targetGuid}");
    }

    [Test]
    public void CustomGuidsFieldIsIndexed()
    {
        // Verify that the custom guids field exists in the index
        IExamineManager examineManager = GetRequiredService<IExamineManager>();
        IIndex index = examineManager.GetIndex(IndexAlias);

        // Get all documents
        ISearchResults allResults = index.Searcher.CreateQuery().All().Execute();

        Assert.That(allResults.TotalItemCount, Is.GreaterThan(0), "Index should have documents");

        // Get first document and check for the custom guids field
        ISearchResult firstDoc = allResults.First();
        var fieldNames = firstDoc.AllValues.Keys.ToList();

        var expectedFieldName = $"Field_{CustomGuidFieldName}_guids_keywords";
        Assert.That(fieldNames, Does.Contain(expectedFieldName), $"Expected field '{expectedFieldName}' not found. Available fields: {string.Join(", ", fieldNames)}");
    }

    [Test]
    public void AllDocumentsHaveCustomGuidFieldIndexed()
    {
        IExamineManager examineManager = GetRequiredService<IExamineManager>();
        IIndex index = examineManager.GetIndex(IndexAlias);

        // Verify each document has its custom guids indexed
        foreach (KeyValuePair<int, Guid[]> kvp in DocumentCustomGuids)
        {
            var docNumber = kvp.Key;
            Guid[] expectedGuids = kvp.Value;

            foreach (Guid expectedGuid in expectedGuids)
            {
                var fieldName = $"Field_{CustomGuidFieldName}_guids_keywords";
                ISearchResults results = index.Searcher
                    .CreateQuery()
                    .Field(fieldName, expectedGuid.ToString())
                    .Execute();

                Assert.That(
                    results.TotalItemCount,
                    Is.EqualTo(1),
                    $"Document {docNumber} should have guid {expectedGuid} indexed");
            }
        }
    }

    [Test]
    public void CanSearchForDocumentByCustomGuid()
    {
        IExamineManager examineManager = GetRequiredService<IExamineManager>();
        IIndex index = examineManager.GetIndex(IndexAlias);

        // Pick a random document and search for it by its custom guid
        var targetDocNumber = 5;
        Guid targetGuid = DocumentCustomGuids[targetDocNumber][0];
        Guid expectedDocId = DocumentIds[targetDocNumber];

        var fieldName = $"Field_{CustomGuidFieldName}_guids_keywords";
        ISearchResults results = index.Searcher
            .CreateQuery()
            .Field(fieldName, targetGuid.ToString())
            .Execute();

        Assert.Multiple(() =>
        {
            Assert.That(results.TotalItemCount, Is.EqualTo(1));

            ISearchResult result = results.First();
            // The document ID in Examine is the guid (lowercase)
            Assert.That(result.Id, Is.EqualTo(expectedDocId.ToString().ToLowerInvariant()));
        });
    }

    private async Task EnsureIndex() => await DeleteIndex();

    private async Task DeleteIndex()
        => await GetRequiredService<IIndexer>().ResetAsync(IndexAlias);

    private T GetRequiredService<T>() where T : notnull
        => _serviceProvider.GetRequiredService<T>();
}

/// <summary>
/// A custom IndexValue subclass that adds support for indexing Guids.
/// This demonstrates the extensibility of the IndexValue record.
/// </summary>
public record CustomIndexValue : IndexValue
{
    /// <summary>
    /// Collection of Guids to be indexed.
    /// </summary>
    public IEnumerable<Guid>? Guids { get; init; }
}

/// <summary>
/// A custom Indexer that handles <see cref="CustomIndexValue"/> by indexing the Guids property.
/// </summary>
public class CustomIndexer : Indexer
{
    // Use "keywords" suffix so the guids are indexed as RAW (unanalyzed) for exact matching
    private const string GuidsFieldSuffix = "keywords";

    public CustomIndexer(IExamineManager examineManager, IOptions<FieldOptions> fieldOptions)
        : base(examineManager, fieldOptions)
    {
    }

    protected override IndexValue MergeIndexValue(IndexValue original, IndexValue toMerge)
    {
        // First merge the base properties
        IndexValue baseMerged = base.MergeIndexValue(original, toMerge);

        // Then handle our custom Guids property
        var originalCustom = original as CustomIndexValue;
        var toMergeCustom = toMerge as CustomIndexValue;

        // If neither has custom guids, return the base merge result
        if (originalCustom?.Guids is null && toMergeCustom?.Guids is null)
        {
            return baseMerged;
        }

        // Create a CustomIndexValue with merged guids
        return new CustomIndexValue
        {
            Keywords = baseMerged.Keywords,
            Integers = baseMerged.Integers,
            Decimals = baseMerged.Decimals,
            DateTimeOffsets = baseMerged.DateTimeOffsets,
            Texts = baseMerged.Texts,
            TextsR1 = baseMerged.TextsR1,
            TextsR2 = baseMerged.TextsR2,
            TextsR3 = baseMerged.TextsR3,
            Guids = MergeValues(originalCustom?.Guids, toMergeCustom?.Guids)
        };
    }

    protected override void HandleCustomIndexValues(IndexField field, Dictionary<string, IEnumerable<object>> result)
    {
        if (field.Value is CustomIndexValue customValue && customValue.Guids?.Any() == true)
        {
            // Index the Guids as keyword values (RAW/unanalyzed) for exact matching
            // We use a distinct field name to avoid conflicts with regular Keywords
            var fieldName = FieldNameHelper.FieldName($"{field.FieldName}_guids", GuidsFieldSuffix, field.Segment);
            result.Add(fieldName, customValue.Guids.Select(g => g.ToString()).ToList());
        }
    }
}

/// <summary>
/// Extension methods for setting up test services with the custom indexer.
/// </summary>
internal static class CustomIndexerServiceCollectionExtensions
{
    public static IServiceCollection AddCustomIndexerServicesForTest<TIndex, TDirectoryFactory>(
        this IServiceCollection services)
        where TIndex : LuceneIndex
        where TDirectoryFactory : class, IDirectoryFactory
    {
        // Configure base test options plus our custom field
        services.ConfigureOptions<TestIndexConfigureOptions>();
        services.ConfigureOptions<CustomIndexValueFieldOptions>();
        services.Configure<SearcherOptions>(options => options.MaxFacetValues = 250);
        services.AddSingleton<TDirectoryFactory>();

        // Register indexes
        services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Constants.IndexAliases.DraftContent,
            _ => { });

        services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Constants.IndexAliases.PublishedContent,
            _ => { });

        services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Constants.IndexAliases.DraftMedia,
            _ => { });

        services.AddExamineLuceneIndex<TIndex, TDirectoryFactory>(
            Constants.IndexAliases.DraftMembers,
            _ => { });

        // Add base Examine services first
        services.AddExamineSearchProviderServices();

        // Override with our custom indexer
        services.AddTransient<IExamineIndexer, CustomIndexer>();
        services.AddTransient<IIndexer, CustomIndexer>();

        return services;
    }
}

/// <summary>
/// Configures field options for the custom guids field.
/// </summary>
internal class CustomIndexValueFieldOptions : IConfigureOptions<FieldOptions>
{
    public void Configure(FieldOptions fieldOptions)
    {
        // Add our custom guids field configuration
        // The field name is "customGuids_guids" with Keywords field type
        var existingFields = fieldOptions.Fields.ToList();
        existingFields.Add(new FieldOptions.Field
        {
            PropertyName = "customGuids_guids",
            FieldValues = FieldValues.Keywords,
        });
        fieldOptions.Fields = existingFields.ToArray();
    }
}
