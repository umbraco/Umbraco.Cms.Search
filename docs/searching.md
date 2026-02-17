# Searching with Umbraco Search

The `ISearcher` interface is the entrypoint for searching.

`ISearcher` features multiple different approaches to search, all of which can be combined into single queries. Each of these are described below.

Umbraco Search indexes all relevant content properties alongside system fields like the content ID (key), name, type etc.

Different Umbraco property editors yield different index value types; some yield searchable `Text`, some yield filterable `Keyword`, and some yield numeric or date field types. This is important to keep in mind when searching with Umbraco Search, because the way property values are indexed directly affects the search results.

A list of the built-in Umbraco property editors and their corresponding index value types can be found in [Appendix A](#appendix-a-indexed-values-of-built-in-property-editors).

An overview of the indexed system fields can be found in [Appendix B](#appendix-b-system-fields).

## Search by query (full text search)

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

## Search by filtering

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

## Facets in search results

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

## Sorting search results

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

## Search result pagination

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

## Searching specific content variations

By default, Umbraco Search will search only for invariant content. Use `culture` and/or `segment` to include within specific content variations.

> [!TIP]
> Invariant content will automatically be included in the search result when searching for variant content.

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

## Searching for protected content

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
