# Indexing and searching custom data

Umbraco Search provides extensibility points for indexing and searching custom data that goes beyond standard Umbraco content properties. This is useful when you need to:

- Index data types not covered by the built-in `IndexValue` (e.g., GUIDs, custom objects).
- Add custom filtering capabilities to the searcher.
- Transform or enrich data before indexing.

## Creating a custom IndexValue

The `IndexValue` record holds the data to be indexed. Create a subclass to add support for additional data types:

```csharp
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Site.Search;

public record CustomIndexValue : IndexValue
{
    public IEnumerable<Guid>? Guids { get; init; }
}
```

## Creating a custom Indexer

Extend the `Indexer` class to handle your custom `IndexValue` type. Override the `AppendCustomIndexValues` method to add custom fields to the index, and optionally override `MergeIndexValue` to handle merging when the same field appears multiple times:

```csharp
using Examine;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using Umbraco.Cms.Search.Provider.Examine.Services;

namespace Site.Search;

public class CustomIndexer : Indexer
{
    public CustomIndexer(IExamineManager examineManager, IOptions<FieldOptions> fieldOptions)
        : base(examineManager, fieldOptions)
    {
    }

    protected override void AppendCustomIndexValues(
        IndexField field,
        Dictionary<string, IEnumerable<object>> result)
    {
        if (field.Value is CustomIndexValue customValue && customValue.Guids?.Any() == true)
        {
            // Index the Guids as keyword values (unanalyzed) for exact matching
            var fieldName = FieldNameHelper.FieldName(
                $"{field.FieldName}_guids", "keywords", field.Segment);
            result.Add(fieldName, customValue.Guids.Select(g => g.ToString()).ToList());
        }
    }

    protected override IndexValue MergeIndexValue(IndexValue original, IndexValue toMerge)
    {
        IndexValue baseMerged = base.MergeIndexValue(original, toMerge);

        var originalCustom = original as CustomIndexValue;
        var toMergeCustom = toMerge as CustomIndexValue;

        if (originalCustom?.Guids is null && toMergeCustom?.Guids is null)
        {
            return baseMerged;
        }

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
}
```

## Creating a custom Filter

Create a custom filter record that extends `Filter` to enable filtering by your custom data type:

```csharp
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Site.Search;

public record GuidFilter(string FieldName, Guid[] Values, bool Negate)
    : Filter(FieldName, Negate);
```

## Creating a custom Searcher

Extend the `Searcher` class to handle your custom filter. Override the `AddCustomFilter` method to implement the filtering logic:

```csharp
using Examine;
using Examine.Search;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using Umbraco.Cms.Search.Provider.Examine.Services;

namespace Site.Search;

public class CustomSearcher : Searcher
{
    public CustomSearcher(IExamineManager examineManager, IOptions<SearcherOptions> searcherOptions)
        : base(examineManager, searcherOptions)
    {
    }

    protected override void AddCustomFilter(
        IBooleanOperation searchQuery,
        Filter filter,
        string? culture,
        string? segment)
    {
        if (filter is GuidFilter guidFilter)
        {
            var fieldName = FieldNameHelper.FieldName(
                $"{filter.FieldName}_guids", "keywords", segment);
            var guidStrings = guidFilter.Values.Select(g => g.ToString()).ToArray();

            if (guidFilter.Negate)
            {
                searchQuery.Not().GroupedOr([fieldName], guidStrings);
            }
            else
            {
                searchQuery.And().GroupedOr([fieldName], guidStrings);
            }
        }
    }
}
```

## Creating a custom IContentIndexer

Implement `IContentIndexer` to hook into the content indexing pipeline and add custom fields whenever content is indexed:

```csharp
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.Search;

public class RelatedItemsContentIndexer : IContentIndexer
{
    private readonly IRelatedItemsService _relatedItemsService;

    public RelatedItemsContentIndexer(IRelatedItemsService relatedItemsService)
        => _relatedItemsService = relatedItemsService;

    public async Task<IEnumerable<IndexField>> GetIndexFieldsAsync(
        IContentBase content,
        string?[] cultures,
        bool published,
        CancellationToken cancellationToken)
    {
        if (content is not IContent document)
        {
            return [];
        }

        // Fetch related item IDs from your custom service
        Guid[] relatedItemIds = await _relatedItemsService.GetRelatedItemIdsAsync(
            document.Key, cancellationToken);

        if (relatedItemIds.Length == 0)
        {
            return [];
        }

        // Return index fields with custom IndexValue containing Guids
        return
        [
            new IndexField(
                FieldName: "relatedItems",
                Value: new CustomIndexValue { Guids = relatedItemIds },
                Culture: null,
                Segment: null)
        ];
    }
}
```

## Registering custom implementations

Register your custom indexer, searcher, and content indexer in a composer:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Services;

namespace Site.DependencyInjection;

public sealed class CustomSearchComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register the custom content indexer to add fields during content indexing
        builder.Services.AddTransient<IContentIndexer, RelatedItemsContentIndexer>();

        // Register the custom indexer to handle the CustomIndexValue type
        builder.Services.AddTransient<IExamineIndexer, CustomIndexer>();
        builder.Services.AddTransient<IIndexer, CustomIndexer>();

        // Register the custom searcher to handle the GuidFilter type
        builder.Services.AddTransient<IExamineSearcher, CustomSearcher>();
        builder.Services.AddTransient<ISearcher, CustomSearcher>();
    }
}
```

## Using the custom filter

Search using your custom filter:

```csharp
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Services;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Site.Services;

public class MySearchService(ISearcher searcher)
{
    public async Task<SearchResult> SearchByRelatedItemAsync(Guid relatedItemId)
        => await searcher.SearchAsync(
            indexAlias: Constants.IndexAliases.PublishedContent,
            filters: [new GuidFilter("relatedItems", [relatedItemId], Negate: false)]
        );
}
```

## Additional extension points

The `Searcher` class provides additional virtual methods for extending faceting and sorting:

| Method | Purpose |
|--------|---------|
| `AddCustomFacet` | Add custom facet operations to the search query |
| `ExtractCustomFacetResult` | Extract custom facet results from the search results |
| `AddCustomSorter` | Handle custom sorter types |
