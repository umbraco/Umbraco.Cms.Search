# A custom index for non-Umbraco data

> [!NOTE]
> If you only need extra fields on an existing content, media, or member index, use `IContentIndexer` instead. This page is for entirely separate entity types.

This is intended to be comparable to the Docs page on [custom Examine indexes](https://docs.umbraco.com/umbraco-cms/reference/searching/examine/indexing#a-custom-index-for-non-umbraco-data), but using the new Umbraco Search framework instead of coding directly against Examine.

Rather than coding directly against Examine's Lucene APIs, Umbraco Search provides a provider-agnostic abstraction. You interact with `IIndexer` and `ISearcher` regardless of what search technology is underneath.

The examples in this article use a `Book` entity to illustrate the pattern.

---

## 1. Register the Lucene index

Register a Lucene index for the new entity in a composer, giving it a unique alias:

```csharp
builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(
    "Umb_Books", _ => { });
```

> [!TIP]
> Use the `Umb_` prefix to stay consistent with the built-in Umbraco indexes.

---

## 2. Map your entity to index fields

Instead of `ValueSet.FromObject()`, map properties to typed `IndexField` instances. Define a service to wrap the indexing operations:

```csharp
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;

namespace MyProject.Search;

internal sealed class BookIndexingService(IIndexer indexer)
{
    public Task AddOrUpdateAsync(Book book)
    {
        IndexField[] fields =
        [
            // System fields used by the backoffice
            new(Constants.FieldNames.Id,   new IndexValue { Keywords = [book.Id.AsKeyword()] }, Culture: null, Segment: null),
            new(Constants.FieldNames.Name, new IndexValue { TextsR1  = [book.Title] },         Culture: null, Segment: null),
            new(Constants.FieldNames.Icon, new IndexValue { Keywords  = ["icon-book"] },        Culture: null, Segment: null),

            // Custom fields
            new("author",    new IndexValue { Keywords  = [book.Author] },      Culture: null, Segment: null),
            new("published", new IndexValue { Integers  = [book.PublishedYear] }, Culture: null, Segment: null),
        ];

        return indexer.AddOrUpdateAsync(
            indexAlias: "Umb_Books",
            id: book.Id,
            objectType: UmbracoObjectTypes.Unknown,
            variations: [new Variation(null, null)],
            fields: fields,
            protection: null);
    }

    public Task DeleteAsync(Guid bookId)
        => indexer.DeleteAsync("Umb_Books", [bookId]);
}
```

> [!TIP]
> The `Umb_Id`, `Umb_Name`, and `Umb_Icon` system fields are used by the backoffice. For Umbraco content indexes these are indexed automatically by the framework, but for custom entity indexes you must include them yourself. `Umb_Id` is required for the "Show Fields" action to work; `Umb_Name` and `Umb_Icon` control what appears in the search results table.

**Field type guide:**

| Data | `IndexValue` property | Filter type |
|---|---|---|
| Full-text (title, body) | `TextsR1` / `TextsR2` / `TextsR3` / `Texts` | `TextFilter` |
| Exact match, IDs, categories | `Keywords` | `KeywordFilter` |
| Whole numbers | `Integers` | `IntegerRangeFilter` / `IntegerExactFilter` |
| Decimals | `Decimals` | `DecimalRangeFilter` / `DecimalExactFilter` |
| Dates | `DateTimeOffsets` | `DateTimeOffsetRangeFilter` / `DateTimeOffsetExactFilter` |

For entities without culture variants, pass a single `Variation(null, null)`. Pass `UmbracoObjectTypes.Unknown` for entity types not known to Umbraco core.

> [!WARNING]
> Filter type must match field type. Using `KeywordFilter` on a `Texts` field (or vice versa) returns zero results.

---

## 3. Keep the index in sync

Register notification handlers to index and remove documents as entities are saved and deleted:

```csharp
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace MyProject.Search;

internal sealed class BookNotificationHandler(BookIndexingService books)
    : INotificationAsyncHandler<BookSavedNotification>,
      INotificationAsyncHandler<BookDeletedNotification>
{
    public async Task HandleAsync(BookSavedNotification notification, CancellationToken cancellationToken)
    {
        foreach (Book book in notification.SavedEntities)
        {
            await books.AddOrUpdateAsync(book);
        }
    }

    public async Task HandleAsync(BookDeletedNotification notification, CancellationToken cancellationToken)
    {
        foreach (Book book in notification.DeletedEntities)
        {
            await books.DeleteAsync(book.Id);
        }
    }
}
```

---

## 4. Support backoffice rebuild

To enable the **Rebuild** button in the backoffice Search section, implement `IIndexRebuildStrategy`:

```csharp
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services;

namespace MyProject.Search;

internal sealed class BookRebuildStrategy(IBookRepository bookRepository, BookIndexingService books)
    : IIndexRebuildStrategy
{
    public async Task RebuildAsync(IndexInfo indexInfo, CancellationToken cancellationToken)
    {
        foreach (Book book in await bookRepository.GetAllAsync())
        {
            await books.AddOrUpdateAsync(book);
        }
    }
}
```

> [!NOTE]
> `IIndexRebuildStrategy` handles only the rebuild concern. The built-in Umbraco content indexes use `IContentChangeStrategy` (which extends `IIndexRebuildStrategy`) to also track content changes — but for custom entities, `IIndexRebuildStrategy` is all you need.

---

## 5. Register everything

Wire up all the pieces in a composer or builder extension method:

```csharp
using Examine.Lucene.Providers;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Provider.Examine.Services;

namespace MyProject.Search;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddBookSearch(this IUmbracoBuilder builder)
    {
        // Register the Lucene index
        builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(
            "Umb_Books", _ => { });

        // Register with the Search framework for backoffice visibility and rebuild support
        builder.Services.Configure<IndexOptions>(options =>
            options.RegisterIndex<IExamineIndexer, IExamineSearcher, BookRebuildStrategy>(
                "Umb_Books", UmbracoObjectTypes.Unknown));

        // Register services
        builder.Services.AddTransient<BookIndexingService>();
        builder.Services.AddTransient<BookRebuildStrategy>();

        // Notification handlers
        builder.AddNotificationAsyncHandler<BookSavedNotification, BookNotificationHandler>();
        builder.AddNotificationAsyncHandler<BookDeletedNotification, BookNotificationHandler>();

        return builder;
    }
}
```

`RegisterIndex` is what causes the index to appear in the backoffice Search section and makes the **Rebuild** button functional.

---

## Custom field types

If your entity has properties that don't map to the built-in `IndexValue` types (for example, collections of GUIDs), follow the [custom extensibility guide](custom-extensibility.md) to create a custom `IndexValue` subclass and a corresponding `Indexer` subclass to handle it. The rest of this pattern remains unchanged.

