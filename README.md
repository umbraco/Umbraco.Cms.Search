# Umbraco Search

This repo contains the "New Search" (henceforth referred to as "Umbraco Search") for [Umbraco CMS](https://github.com/umbraco/Umbraco-CMS).

> [!IMPORTANT]
> This is a work in progress. While we urge interested parties to try it out, things might change at moment's notice.

## Background and motivation

The project was started in an effort to improve the search experience in Umbraco - both for the backoffice and the frontend. It is founded in the Umbraco RFC ["The Future of Search"](https://github.com/umbraco/rfcs/blob/0027-the-future-of-search/cms/0027-the-future-of-search.md), 

Umbraco Search will eventually replace the current search implementation in Umbraco, at the earliest starting from Umbraco v18.

## Intended audience

At this time, Umbraco Search is intended strictly for developers wanting to experiment with it and help shape its future.

As we progress with the project, it will eventually be released as an official and production ready add-on for Umbraco v16+.

## Installation

> [!IMPORTANT]
> Umbraco Search is compatible with Umbraco v16 and beyond.

To get started, install Umbraco Search and the Examine search provider from NuGet:

```bash
dotnet add package Umbraco.Cms.Search.Core
dotnet add package Umbraco.Cms.Search.Provider.Examine
```

With these packages installed, enable Umbraco Search using a composer:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

namespace Site.DependencyInjection;

public sealed class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder
            // add core services for search abstractions
            .AddSearchCore()
            // add the Examine search provider
            .AddExamineSearchProvider();

        // force rebuild indexes after startup (awaiting a better solution from Core)
        builder.RebuildIndexesAfterStartup();
    }
}
```

> [!TIP]
> The invocation of `RebuildIndexesAfterStartup()` is a temporary means to an end. We'll use it for now, because:
> 
> 1. Umbraco Search does not yet pack a UI for managing (rebuilding) indexes, so it ensures index population at first install.
> 2. Any subsequent breaking changes that affect the index structure will be automatically propagated into the index at startup.

Umbraco Search covers three different aspects of search in Umbraco:

- The frontend search.
- The backoffice search.
- The Delivery API querying endpoint.

At this time, the backoffice and the Delivery API parts are built as individual NuGet packages, so they can be added/removed independently in case they misbehave.

### The backoffice search

To include the backoffice search, run:

```bash
dotnet add package Umbraco.Cms.Search.BackOffice
```

And add this to the composer:

```csharp
// use Umbraco Search for backoffice search
builder.AddBackOfficeSearch();
```

### The Delivery API querying

To include the Delivery API querying, run:

```bash
dotnet add package Umbraco.Cms.Search.DeliveryApi
```

And add this to the composer:

```csharp
// use Umbraco Search for the Delivery API querying
builder.AddDeliveryApiSearch();
```

## Searching with Umbraco Search

The `ISearcher` interface is the entrypoint for searching.

`ISearcher` features multiple different approaches to search, all of which can be combined into single queries. Each of these are described below.

Umbraco Search indexes all relevant content properties alongside system fields like the content ID (key), name, type etc.

Different Umbraco property editors yield different index value types; some yield searchable `Text`, some yield filterable `Keyword`, and some yield numeric or date field types. This is important to keep in mind when searching with Umbraco Search, because the way property values are indexed directly affects the search results.

A list of the built-in Umbraco property editors and their corresponding index value types can be found in [Appendix A](#appendix-a-indexed-values-of-built-in-property-editors).

An overview of the indexed system fields can be found in [Appendix B](#appendix-b-system-fields).

### Search by query (full text search)

Searching by query yields results where one or more fields indexed as `Text` contains the search query.

```csharp
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Services;

namespace Site.Services;

public class MySearchConsumer(ISearcher searcher)
{
    public async Task<SearchResult> SearchByQueryAsync()
        => await searcher.SearchAsync(
            indexAlias: Constants.IndexAliases.PublishedContent,
            query: "pink"
        );
}
```

### Search by filtering

Multiple filters can be applied in a single query, and multiple values can be defined for each filter. Umbraco Search performs an `AND` search between filters, and an `OR` search between filter values.

When searching by means of filtering, one must pay close attention to the expected index value types of the fields targeted for filtering. Mismatched combinations of filters and value types will most likely yield zero results.

For example, use a `TextFilter` when filtering `Text` value types, and a `KeywordFilter` for `Keyword` value types.

```csharp
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Services;

namespace Site.Services;

public class MySearchConsumer(ISearcher searcher)
{
    public async Task<SearchResult> FilterByKeywordAndIntegerAsync()
        => await searcher.SearchAsync(
            indexAlias: Constants.IndexAliases.PublishedContent,
            // "genre" must be either "rock" or "pop", and "releaseYear" must be either 1984, 1985 or 1986
            filters:
            [
                new KeywordFilter(
                    FieldName: "genre",
                    Values: ["rock", "pop"],
                    Negate: false
                ),
                new IntegerExactFilter(
                    FieldName: "releaseYear",
                    Values: [1984, 1985, 1986],
                    Negate: false
                )
            ]
        );
}
````

Filters can be negated, in which case Umbraco Search will perform an `AND NOT`:

```csharp
// "genre" must be either "rock" or "pop", and "releaseYear" must NOT be any of 1984, 1985 or 1986
filters:
[
    new KeywordFilter(
        FieldName: "genre",
        Values: ["rock", "pop"],
        Negate: false
    ),
    new IntegerExactFilter(
        FieldName: "releaseYear",
        Values: [1984, 1985, 1986],
        Negate: true
    )
]
```

Numeric and date filters also exist in a range version - for example, the `IntegerRangeFilter`:

```csharp
// "genre" must be either "rock" or "pop", and "releaseYear" must either be in the range [1950,1960) or [1980,1990)
filters:
[
    new KeywordFilter(
        FieldName: "genre",
        Values: ["rock", "pop"],
        Negate: false
    ),
    new IntegerRangeFilter(
        FieldName: "releaseYear",
        Ranges:
        [
            new(
                MinValue: 1950,
                MaxValue: 1960
            ),
            new(
                MinValue: 1980,
                MaxValue: 1990
            )
        ],
        Negate: false
    )
]
```

> [!TIP]
> Ranges include the lower interval and excludes the upper. The example above translates into: _"releaseYear" either between 1950 and 1959 (both inclusive) or between 1980 and 1989 (both inclusive)_.

### Facets in search results

Umbraco Search can create facets for fields indexed as type `Keyword`, `Integer`, `Decimal` or `DateTimeOffset`.

Once again, one must pay attention to the expected field value type when defining facets. Mismatched combinations of facets and value types will most likely yield zero facet results.

```csharp
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Services;

namespace Site.Services;

public class MySearchConsumer(ISearcher searcher)
{
    public async Task<SearchResult> FacetByKeywordAndIntegerAsync()
        => await searcher.SearchAsync(
            indexAlias: Constants.IndexAliases.PublishedContent,
            // include facets for "genre" and "releaseYear" in the search result
            facets:
            [
                new KeywordFacet(
                    FieldName: "genre"
                ),
                new IntegerExactFacet(
                    FieldName: "releaseYear"
                )
            ]
        );
}
```

Numeric and date facets also exist in a range version - for example, the `IntegerRangeFacet`:

```csharp
facets:
[
    new KeywordFacet(
        FieldName: "genre"
    ),
    new IntegerRangeFacet(
        FieldName: "releaseYear",
        Ranges: 
        [
            new (
                Key: "Rocking 50s",
                MinValue: 1950,
                MaxValue: 1960
            ),
            new (
                Key: "Glamorous 80s",
                MinValue: 1980,
                MaxValue: 1990
            )
        ]
    )
]
```

### Sorting search results

All fields can be used for sorting (ordering) search results, and sorting can be performed across multiple fields in a single query. 

Sorting by field is also tied explicitly to the field value type. Mismatched combinations of sorters and value types will yield incorrectly sorted search results.

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;
using Umbraco.Cms.Search.Core.Services;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Site.Services;

public class MySearchConsumer(ISearcher searcher)
{
    public async Task<SearchResult> SortByKeywordAndIntegerAsync()
        => await searcher.SearchAsync(
            indexAlias: Constants.IndexAliases.PublishedContent,
            query: "pink",
            // sort the search results by "releaseYear" decsending, then by "genre" ascending
            sorters:
            [
                new IntegerSorter(
                    FieldName: "releaseYear",
                    Direction: Direction.Descending
                ),
                new KeywordSorter(
                    FieldName: "genre",
                    Direction: Direction.Ascending
                )
            ]
        );
}
```

> [!TIP]
> Use the `ScoreSorter` to sort by search result score (relevance).

### Search result pagination

Search results are paginated by using `skip` and `take`.

```csharp
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Services;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Site.Services;

public class MySearchConsumer(ISearcher searcher)
{
    public async Task<SearchResult> SearchWithPaginationAsync()
        => await searcher.SearchAsync(
            indexAlias: Constants.IndexAliases.PublishedContent,
            query: "pink",
            // skip the first 20 results and return the next 10 results
            skip: 20,
            take: 10
        );
}
```

### Searching specific content variations

By default, Umbraco Search will search only for invariant content. Use `culture` and/or `segment` to include within specific content variations.

> [!TIP]
> Invariant content will automatically be included in the search result when searching for variant content.

> [!IMPORTANT]  
> At this time, segment variant search might produce incorrect search results. See the [known limitations](#known-limitations) section for details.

```csharp
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Services;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Site.Services;

public class MySearchConsumer(ISearcher searcher)
{
    public async Task<SearchResult> SearchSpecificCultureAndSegmentAsync()
        => await searcher.SearchAsync(
            indexAlias: Constants.IndexAliases.PublishedContent,
            query: "rosa",
            culture: "es-ES",
            segment: "rock-n-rollers"
        );
}
```

### Searching for protected content

Use the `AccessContext` to include protected content (that is, content with public access restrictions applied) in search results.

The `AccessContext` requires the ID (key) of the currently logged-in [member](https://docs.umbraco.com/umbraco-cms/fundamentals/data/members), and accepts an optional collection of group IDs.

> [!IMPORTANT]
> Umbraco Search has no knowledge of members. If public access rules are defined based on member groups, make sure to pass the group IDs alongside the member ID in `AccessContext`.

```csharp
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Services;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Site.Services;

public class MySearchConsumer(ISearcher searcher)
{
    public async Task<SearchResult> SearchProtectedContent(Guid principalId, Guid[]? groupIds)
    {
        return await searcher.SearchAsync(
            indexAlias: Constants.IndexAliases.PublishedContent,
            query: "pink",
            accessContext: new AccessContext(
                PrincipalId: principalId,
                GroupIds: groupIds
            )
        );
    }
}
```

## The Examine search provider

Umbraco Search uses a provider based approach to the underlying search technology. By default, Umbraco Search is powered by [Examine](https://github.com/Shazwazza/Examine), which likely will require a bit of configuration to function as intended.

> [!NOTE]  
> This section _only_ applies to the default Examine search provider. Alternative search providers might be available, and they might require a different configuration.

### Configuring fields for faceting and/or sorting

Fields that will be used for faceting and/or sorting must be explicitly configured for the Examine search provider, _before_ anything is added to the indexes. This is done by configuring the `FieldOptions` using the options pattern.

The field configuration is essentially a mapping between the Umbraco property aliases that hold the values, and the expected field index type of those properties. For example, the "genre" and "releaseYear" fields used throughout this article should be configured like this: 

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Site.DependencyInjection;

public class FieldOptionsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.Configure<FieldOptions>(options
            => options.Fields =
            [
                // configure faceting and sorting for the "genre" property
                new FieldOptions.Field
                {
                    PropertyName = "genre",
                    FieldValues = FieldValues.Keywords,
                    Facetable = true,
                    Sortable = true
                },
                // configure faceting and sorting for the "releaseYear" property
                new FieldOptions.Field
                {
                    PropertyName = "releaseYear",
                    FieldValues = FieldValues.Integers,
                    Facetable = true,
                    Sortable = true
                }
            ]
        );
}
```
> [!IMPORTANT]
> Since the field configurations must be known at index time, any changes made to this configuration will only take effect after a rebuild of all indexes.

### Configuring the search behavior

The `SearcherOptions` allow for configuring various aspects of how a search is executed. It is configured using the options pattern:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;

namespace Site.DependencyInjection;

public class SearcherOptionsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.Configure<SearcherOptions>(options =>
        {
            // configure searcher options here
        });
}
```

#### Boost levels

Certain Umbraco properties yield different textual relevance values (see [Appendix A](#appendix-a-indexed-values-of-built-in-property-editors)). The Examine search provider automatically performs relevance boosting accordingly, but the boost levels can be tweaked if required. Use:

- `SearcherOptions.BoostFactorTextR1` to control the relevance of highest relevance text (e.g. document names and H1 tags).
- `SearcherOptions.BoostFactorTextR2` to control the relevance of second-highest relevance text (e.g. H2 tags).
- `SearcherOptions.BoostFactorTextR3` to control the relevance of third-highest relevance text (e.g. H3 tags).

#### Facet result behavior

The available facet values are grouped by the `FieldName` passed to the facet definition when searching. In the examples above, this would be "genre" and "releaseYear". 

When an end user picks a facet value from a search result, the subsequent search should contain a filter for the picked value - for example, the `KeywordFilter` in the examples above. 

If a facet value has been picked and is applied as a filter, the default behavior for facet results is to exclude the facet values that are _not_ picked within the facet group.

If all (applicable) facet values should be included for all groups in the search result, configure `SearcherOptions.ExpandFacetValues` as `true`.

> [!CAUTION]
> Expanding the facet values incurs a performance penalty, which is more or less linear to the number of facet groups in the search.

#### Max facet values

The Examine search provider limits the number of resulting facet values within a facet group to 100. This limit can be changed using `SearcherOptions.MaxFacetValues`.

### Known limitations

The Examine search provider has a few known limitations you should be aware of.

#### Segment support

Segment variant content, that has _not_ been created in the targeted segment, will not be part of the search result. This is a bug which will be fixed as soon as possible.

## Appendix A: Indexed values of built-in property editors

The following list shows how the built-in Umbraco property editors are indexed for Umbraco Search.

Some property editors have deliberately been left out (e.g. color pickers and media pickers), because it was deemed that they would generate more noise than value in the index.

| Property editor               | Indexed as       | Notes                                                                                                                                                                                         |
|-------------------------------|------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Umbraco.BlockGrid`           | (see below)      |
| `Umbraco.BlockList`           | (see below)      |
| `Umbraco.CheckBoxList`        | `Keyword`        |
| `Umbraco.ContentPicker`       | `Keyword`        | Indexes the ID (key) of the picked content.                                                                                                                                                   |
| `Umbraco.DateTime`            | `DateTimeOffset` |
| `Umbraco.Decimal`             | `Decimal`        |
| `Umbraco.DropDown.Flexible`   | `Keyword`        |
| `Umbraco.Integer`             | `Integer`        |
| `Umbraco.Label`               | (see below)      |
| `Umbraco.MarkdownEditor`      | `Text`           | Same as `Umbraco.RichText`                                                                                                                                                                    | 
| `Umbraco.MultiNodeTreePicker` | `Keyword`        | Indexes the IDs (keys) of the picked content. Does not index values when configured to pick media or members.                                                                                 |
| `Umbraco.MultipleTextstring`  | `Text`           |
| `Umbraco.MultiUrlPicker`      | `Text`           | Indexes the names (titles) of the picked links.                                                                                                                                               |
| `Umbraco.Plain.String`        | `Text`           |
| `Umbraco.Plain.Decimal`       | `Decimal`        |
| `Umbraco.Plain.Integer`       | `Integer`        |
| `Umbraco.Plain.DateTime`      | `DateTimeOffset` |
| `Umbraco.RadioButtonList`     | `Keyword`        |
| `Umbraco.RichText`            | `Text`           | Headings (H1, H2, H3) are indexed with individual relevance, all other tags as lowest relevance text. If the property contains blocks, they are indexed in the same way as the block editors. |          
| `Umbraco.Slider`              | `Decimal`        | For range slides, both the lower and upper bounds are indexed.                                                                                                                                |
| `Umbraco.Tags`                | `Keyword`        | Also note that all tags for all properties are accumulated into a dedicated system field (see Appendix B).                                                                                    |
| `Umbraco.TextArea`            | `Text`           | Indexed as lowest relevance text.                                                                                                                                                             |
| `Umbraco.TextBox`             | `Text`           | Indexed as lowest relevance text.                                                                                                                                                             |
| `Umbraco.TrueFalse`           | `Integer`        | Indexed as 1 for `true`, 0 for `false`.                                                                                                                                                       |

### Special case: `Umbraco.BlockGrid` and `Umbraco.BlockList` 

Block editors contain other property editors. These will iterate their contained properties and aggregate their index values. As such, a single block editor property value can potentially index as all value types. 

### Special case: `Umbraco.Label`

The label editor indexes as either `Integer`, `Decimal`, `DateTimeOffset` or `Text`, depending on the data type configuration (the property editor value type). 

## Appendix B: System fields

The following fields are explicitly indexed for all content.

| Field name          | Indexed as       | Notes                                                                        |
|---------------------|------------------|------------------------------------------------------------------------------|
| `Umb_ContentTypeId` | `Keyword`        | The ID (key) of the content type.                                            |
| `Umb_CreateDate`    | `DateTimeOffset` | The creation date of the content.                                            |
| `Umb_Id`            | `Keyword`        | The content ID (key).                                                        |
| `Umb_Level`         | `Integer`        | The content level in the tree.                                               |
| `Umb_Name`          | `Text`           | The name of the content. Indexed as highest relevance text.                  |
| `Umb_ObjectType`    | `Keyword`        | The content object type (i.e. "Document").                                   |
| `Umb_ParentId`      | `Keyword`        | The ID (key) of the parent (if any).                                         |
| `Umb_PathIds`       | `Keyword`        | The IDs (keys) of all ancestors and the content itself.                      |
| `Umb_SortOrder`     | `Integer`        | The sort order of the content.                                               |
| `Umb_Tags`          | `Keyword`        | Accumulated collection of tags from all properties contained in the content. |
| `Umb_UpdateDate`    | `DateTimeOffset` | The last update date of the content.                                         |

System fields can be used like any other fields for searching. The system field names are defined in [`Constants.FieldNames`](https://github.com/umbraco/Umbraco.Cms.Search/blob/main/src/Umbraco.Cms.Search.Core/Constants.cs).

```csharp
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Services;
using Constants = Umbraco.Cms.Search.Core.Constants;

namespace Site.Services;

public class MySearchConsumer(ISearcher searcher)
{
    public async Task<SearchResult> FilterByNameAsync()
    {
        return await searcher.SearchAsync(
            indexAlias: Constants.IndexAliases.PublishedContent,
            filters:
            [
                new TextFilter(
                    FieldName: Constants.FieldNames.Name,
                    Values: ["pink"],
                    Negate: false
                )
            ]
        );
    }
}
```
