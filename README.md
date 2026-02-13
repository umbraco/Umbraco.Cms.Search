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
    }
}
```

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

## Documentation

- [Searching with Umbraco Search](docs/searching.md) - The `ISearcher` API: filtering, faceting, sorting, pagination, content variations, and protected content.
- [The Examine search provider](docs/examine-provider.md) - Configuring the Examine/Lucene provider: directory factory, field options, searcher options.
- [Indexing and searching custom data](docs/custom-extensibility.md) - Custom `IndexValue`, `Indexer`, `Filter`, `Searcher`, and `IContentIndexer` implementations.
- [The backoffice](docs/backoffice.md) - User guide for interacting with search indexes in the Umbraco backoffice.
- [Extending the search backoffice](docs/backoffice-extensions.md) - Developer guide for adding detail boxes, entity actions, workspace views, and routable modals.
