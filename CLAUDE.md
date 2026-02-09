# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Umbraco Search is a new search abstraction for Umbraco CMS v16+ that will eventually replace the current search implementation. It provides three main capabilities:
1. Frontend search via the `ISearcher` interface
2. Backoffice search
3. Delivery API querying

The project uses a **provider-based architecture** where search technology implementations (currently Examine/Lucene) plug into core abstractions.

## Build & Test Commands

### Building the Solution

```bash
# Build entire solution
dotnet build src/Umbraco.Cms.Search.sln

# Build specific project
dotnet build src/Umbraco.Cms.Search.Core/Umbraco.Cms.Search.Core.csproj

# Build in Release mode
dotnet build src/Umbraco.Cms.Search.sln -c Release
```

### Running Tests

```bash
# Run all tests
dotnet test src/Umbraco.Cms.Search.sln

# Run unit tests only
dotnet test src/Umbraco.Test.Search.Unit/Umbraco.Test.Search.Unit.csproj

# Run integration tests (requires Examine provider)
dotnet test src/Umbraco.Test.Search.Integration/Umbraco.Test.Search.Integration.csproj
dotnet test src/Umbraco.Test.Search.Examine.Integration/Umbraco.Test.Search.Examine.Integration.csproj

# Run specific test by filter
dotnet test --filter "FullyQualifiedName~ContentExtensionsTests"
```

### Client Development (Backoffice UI)

The client code uses an **npm workspaces monorepo** rooted at `src/`. Two workspaces:
- **Core Client**: `src/Umbraco.Cms.Search.Core.Client/Client/` - Main backoffice UI (TypeScript + Vite, 3-bundle code-splitting)
- **Examine Client**: `src/Umbraco.Cms.Search.Provider.Examine/Client/` - Examine provider UI (TypeScript + Vite, single bundle)

Shared config lives in `src/`:
- `src/package.json` - Workspace root with aggregate scripts
- `src/tsconfig.base.json` - Shared TypeScript compiler options (both workspaces extend this)
- `src/.nvmrc` - Node.js version (24)
- `src/.prettierrc.json` - Shared Prettier config

```bash
cd src

# Install all workspaces
npm install

# Build all workspaces
npm run build

# Watch all workspaces
npm run watch

# Lint all workspaces (errors only)
npm run lint:errors-only

# Build a single workspace
npm run build --workspace=Umbraco.Cms.Search.Core.Client/Client
npm run build --workspace=Umbraco.Cms.Search.Provider.Examine/Client

# Generate OpenAPI client (Core Client only, requires test site at https://localhost:44324)
npm run generate-client --workspace=Umbraco.Cms.Search.Core.Client/Client
```

Requires **Node.js 24** (see `src/.nvmrc`).

### Test Site

Run the test site to manually test integration:

```bash
cd src/Umbraco.Web.TestSite.V17
dotnet run
```

The test site runs on .NET 10.0 and includes Umbraco CMS with Search packages pre-configured.

## Architecture

### Core Abstractions (Umbraco.Cms.Search.Core)

The core provides **provider-agnostic abstractions**:

**Key Interfaces:**
- `ISearcher` - Search operations (filtering, faceting, sorting, pagination)
- `IIndexer` - Index management (add/update/delete documents, reset indexes)
- `ISearcherResolver` - Resolves the correct searcher implementation for an index alias
- `IContentIndexingService` - Orchestrates content indexing workflows
- `IContentIndexer` - Indexes system fields and property values into search documents

**System Architecture:**
```
ContentIndexingService (orchestration)
    ├─> IContentIndexingDataCollectionService (gathers data from Umbraco)
    ├─> ISystemFieldsContentIndexer (indexes system fields like Id, Name, Path)
    ├─> PropertyValueFieldsContentIndexer (indexes property values)
    │       └─> IPropertyValueHandler collection (type-specific value handlers)
    └─> IIndexer (writes to underlying provider)
```

**Index Aliases** (see `Constants.IndexAliases`):
- `Umb_PublishedContent` - Published content index
- `Umb_Content` - Draft content index
- `Umb_Media` - Media index
- `Umb_Members` - Members index

**System Field Names** (see `Constants.FieldNames`):
- All system fields are prefixed with `Umb_`
- Examples: `Umb_Id`, `Umb_Name`, `Umb_ContentTypeId`, `Umb_PathIds`, `Umb_Level`, `Umb_CreateDate`, `Umb_UpdateDate`

### Provider Pattern (Umbraco.Cms.Search.Provider.Examine)

The Examine provider implements the core abstractions using Examine/Lucene:

**Key Classes:**
- `Searcher` - Implements `IExamineSearcher : ISearcher`
- `Indexer` - Implements `IExamineIndexer : IIndexer`
- `ConfigureIndexOptions` - Configures Lucene index settings (field options for faceting/sorting)
- `SearcherOptions` - Configures search behavior (boost factors, facet limits)
- `FieldOptions` - Maps property aliases to index field types (Keywords, Integers, Decimals, DateTimeOffsets)

**Important:** Fields used for faceting/sorting must be configured in `FieldOptions` **before** indexing. Changes require a full index rebuild.

### Property Value Handlers (Umbraco.Cms.Search.Core/PropertyValueHandlers)

Property values are indexed based on property editor type. Each handler knows how to extract and transform values:

- `ContentPickerPropertyValueHandler` - Extracts content IDs (Keywords)
- `DateTimePropertyValueHandler` - Indexes dates as DateTimeOffset
- `NumericPropertyValueHandler` - Indexes integers/decimals
- `RichTextPropertyValueHandler` - Extracts text with relevance levels (H1=R1, H2=R2, H3=R3, body=R4)
- `TagsPropertyValueHandler` - Accumulates tags into `Umb_Tags` system field
- `BlockPropertyValueHandler` - Recursively indexes nested block content

See README.md Appendix A for complete property editor mapping.

### Change Tracking Strategies

Content changes are tracked via notification handlers that trigger indexing:

- `IContentChangeStrategy` - Base interface for tracking content state changes
- `IPublishedContentChangeStrategy` - Tracks published content changes (for `Umb_PublishedContent` index)
- `IDraftContentChangeStrategy` - Tracks draft content changes (for `Umb_Content` index)

Implementations use **MessagePack caching** to detect actual changes (not just saves).

### Backoffice Integration (Umbraco.Cms.Search.BackOffice)

Provides backoffice search using the Search API. Registers a backoffice search provider that queries the `Umb_Content` index.

### Delivery API Integration (Umbraco.Cms.Search.DeliveryApi)

Replaces the default Delivery API querying with Search-based querying. Queries the `Umb_PublishedContent` index.

### Client Architecture (npm Workspaces Monorepo)

The backoffice clients are an **npm workspaces monorepo** rooted at `src/`, with shared TypeScript, Vite, ESLint, and Prettier configuration.

#### Core Client (Umbraco.Cms.Search.Core.Client)

Uses **code-splitting with importmap pattern** for optimal loading:

**Three-Bundle Strategy:**
- `search-bundle.js` (~3kb) - Manifest metadata, loaded upfront
- `search-global.js` (~1.5kb) - Global contexts for SignalR event subscriptions, loaded upfront
- `search-settings.js` (~22kb) - Core implementation, lazy-loaded on demand

**Logical Import Pattern:**
- Code imports `@umbraco-cms/search/settings` and `@umbraco-cms/search/global`
- TypeScript resolves via `tsconfig.json` paths for type-checking
- Vite marks as external (not bundled)
- Browser resolves via importmap in `umbraco-package.json` at runtime

**Two-Workspace Architecture:**
- **Root Workspace** (`Umbraco.Search.Workspace.Root`) - Collection view of all search indexes
- **Detail Workspace** (`Umbraco.Search.Workspace`) - Detail view for a single index with extensible boxes

**Custom Extension Type:**
- `searchIndexDetailBox` - Allows adding custom UI boxes to the index detail view via extension slot

**See [Core Client CLAUDE.md](src/Umbraco.Cms.Search.Core.Client/CLAUDE.md) for detailed client architecture, manifest patterns, and development workflow.**

#### Examine Client (Umbraco.Cms.Search.Provider.Examine)

A simpler **single-bundle** workspace (`examine-bundle.js` ~11kb) that provides:
- `UmbSearchExamineProviderRepository` - Fetches search document fields from the Examine API
- `UmbSearchExamineShowFieldsEntityAction` - Entity action to view document fields
- `UmbSearchExamineShowFieldsModal` - Modal displaying indexed fields with filtering, expand/collapse, and copy

Output goes to `wwwroot/App_Plugins/UmbracoSearchExamine/` (gitignored, built by Vite).

**See [Examine Client CLAUDE.md](src/Umbraco.Cms.Search.Provider.Examine/CLAUDE.md) for detailed Examine client architecture and development workflow.**

#### Monorepo Dependency Management

All shared dependencies are hoisted to the root `src/package.json`:
- **`@umbraco-cms/backoffice`** (runtime dependency) - Umbraco backoffice SDK
- **Dev dependencies**: `typescript`, `vite`, `eslint`, `prettier`, and related plugins
- **Script utilities**: `chalk`, `node-fetch`, `cross-env` (used by shared `generate-openapi.js`)

Workspace `package.json` files (`Core.Client/Client/package.json`, `Provider.Examine/Client/package.json`) contain **scripts only** - no dependency declarations. npm workspaces resolves all imports from the hoisted root `node_modules/`.

A shared OpenAPI generation script lives at `src/scripts/generate-openapi.js`. Both workspaces call it with different swagger URLs and output directories via their `generate-client` npm scripts.

## Key Concepts

### Index Field Types

Fields are typed based on how they're queried:

- **Text** - Full-text searchable, analyzed, used with `TextFilter`
- **Keyword** - Exact-match filterable, used for IDs and selections, used with `KeywordFilter`
- **Integer** - Numeric exact or range filtering, used with `IntegerExactFilter` or `IntegerRangeFilter`
- **Decimal** - Decimal exact or range filtering, used with `DecimalExactFilter` or `DecimalRangeFilter`
- **DateTimeOffset** - Date exact or range filtering, used with `DateTimeOffsetExactFilter` or `DateTimeOffsetRangeFilter`

**Mismatched filter types and field types will yield zero results.**

### Search Parameters

`ISearcher.SearchAsync` accepts:
- `query` - Full-text search query (searches Text fields)
- `filters` - AND between filters, OR between values within a filter
- `facets` - Generate facet results for fields
- `sorters` - Multi-field sorting (first sorter is primary)
- `culture` / `segment` - Variant content filtering (invariant always included)
- `accessContext` - Protected content access (requires member ID and optional group IDs)
- `skip` / `take` - Pagination

### Variation Handling

Content variations (culture/segment) are indexed as separate documents with variation-specific field naming:
- Invariant fields: `propertyAlias`
- Culture variant: `propertyAlias_cultureName`
- Segment variant: `propertyAlias__segmentName`
- Both: `propertyAlias_cultureName_segmentName`

When searching with `culture`/`segment`, both invariant and variant fields are queried.

### Protected Content

Content with public access restrictions is indexed with `ContentProtection` metadata:
- `AllowedMemberIds` - Specific members with access
- `AllowedMemberGroupIds` - Member groups with access

Pass `AccessContext` to `SearchAsync` to include protected content in results.

## Development Patterns

### Adding a New Property Value Handler

1. Create handler in `src/Umbraco.Cms.Search.Core/PropertyValueHandlers/`
2. Implement `IPropertyValueHandler` interface
3. Register in `PropertyValueHandlerCollection` via composer
4. Define which property editor aliases it handles via `PropertyEditorAlias` attribute

### Adding a New Filter Type

1. Create filter model in `src/Umbraco.Cms.Search.Core/Models/Searching/Filtering/`
2. Inherit from `Filter` base class
3. Create provider-specific implementation in `src/Umbraco.Cms.Search.Provider.Examine/Models/Searching/Filtering/`
4. Update `Searcher` to handle the new filter type

### Adding a New Facet Type

1. Create facet model in `src/Umbraco.Cms.Search.Core/Models/Searching/Faceting/`
2. Inherit from `Facet` base class
3. Create provider-specific implementation in provider project
4. Update `Searcher` to handle the new facet type

### Modifying Index Structure

1. Update `IIndexer.AddOrUpdateAsync` signature if needed
2. Update provider implementations (`Indexer` class)
3. **Important:** Document that existing indexes must be rebuilt
4. Update `FieldOptions` configuration if adding facetable/sortable fields
5. Add migration if persisted index metadata changes

### Adding a New Repository (Client)

Repositories abstract API calls and provide clean interfaces for UI components. Follow this pattern:

1. **Define Domain Types** in `src/settings/types.ts`:
   - Create request/response types that abstract away API-generated types
   - Example: `UmbSearchRequest`, `UmbSearchResult`

2. **Create Server Data Source** (e.g., `search-query.server.data-source.ts`):
   - Implements data fetching and type mapping
   - Maps domain types → API types (for requests)
   - Maps API types → domain types (for responses)
   - Uses `tryExecute()` for error handling

3. **Create Repository** (e.g., `search-query.repository.ts`):
   - Extends `UmbRepositoryBase`
   - Orchestrates data source calls
   - Provides clean API for consumers
   - Example: `async search(request: UmbSearchRequest) { return this.#dataSource.search(request); }`

4. **Register Repository**:
   - Add constant in `src/global/constants.ts`: `export const UMB_SEARCH_QUERY_REPOSITORY_ALIAS = '...'`
   - Export from `src/settings/repositories/index.ts`
   - Add manifest in `src/bundle/repositories.manifests.ts`

5. **Use in Components**:
   - Import repository class directly: `import { UmbSearchQueryRepository } from '../repositories/search-query.repository.js'`
   - Instantiate: `#repository = new UmbSearchQueryRepository(this)`
   - Call methods: `const { data, error } = await this.#repository.search(request)`

**Benefits**: Separation of concerns, testability, type safety, consistency, reusability.

## Testing Strategy

### Unit Tests (Umbraco.Test.Search.Unit)

- Test extensions, helpers, and models in isolation
- Use Moq for dependencies
- Focus on business logic without infrastructure dependencies

### Integration Tests (Umbraco.Test.Search.Integration)

- Test core services with real Umbraco infrastructure
- Use `Umbraco.Cms.Tests.Integration` base classes
- Test content indexing workflows end-to-end

### Provider Integration Tests (Umbraco.Test.Search.Examine.Integration)

- Test Examine-specific implementations
- Verify Lucene index behavior
- Test query translation and result mapping

## Common Gotchas

1. **Faceting/Sorting Fields Must Be Pre-Configured**: Fields used for faceting or sorting must be defined in `FieldOptions` before indexing. Changes require full index rebuild.

2. **Filter Type Must Match Field Type**: Using `KeywordFilter` on a `Text` field (or vice versa) returns zero results. Same for numeric and date filters.

3. **Variation Field Naming**: When querying variant content, ensure field names include culture/segment suffixes where appropriate.

4. **Global Contexts Must Load Upfront**: Client global contexts (e.g., notification listeners) must be in `search-global.js`, not lazy-loaded in `search-settings.js`.

5. **Client Import Paths**: Always use logical imports (`@umbraco-cms/search/settings` not `./path/to/file`) to leverage the importmap pattern.

6. **Segment Variant Search**: Known limitation - segment variant content not created in the targeted segment may be excluded from results. This is a bug being addressed.

7. **Entity Actions vs Workspace Actions**: Entity actions automatically appear in workspace header dropdowns. Don't create duplicate workspace actions for the same functionality. Ensure the workspace's `entityType` matches the entity action's registered entity type.

8. **Enum JSON Serialization**: C# enums used in ViewModels should use `[JsonConverter(typeof(JsonStringEnumConverter))]` to serialize as strings instead of numbers. This prevents confusion in the UI where enum values would appear as numbers.

9. **State Management Race Conditions**: When setting loading states for async operations, set the state BEFORE making the API call, not after. This ensures immediate UI feedback and prevents race conditions where the operation completes before the loading state is set.

10. **Server-Driven State**: UI state should be derived from server health status (e.g., `healthStatus: 'Rebuilding'` → `state: 'loading'`). This keeps the UI synchronized with actual server state after reloads.

## Coding Conventions

- Follow Umbraco CMS coding standards (StyleCop, .editorconfig)
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Use C# 13 features (latest language version)
- Use `IUmbracoBuilder` extension methods for service registration
- Follow async/await patterns consistently
- Use primary constructors for dependency injection
- Prefix all system fields with `Umb_`
- Use strong types for index aliases (`Constants.IndexAliases`)

## Version & Dependencies

- **Target Framework**: .NET 10.0
- **Umbraco CMS**: v17+
- **Examine**: Search provider implementation
- **Node.js**: 24 (for client build)
- **Versioning**: Uses Nerdbank.GitVersioning (see `version.json`)

## References

- RFC: ["The Future of Search"](https://github.com/umbraco/rfcs/blob/0027-the-future-of-search/cms/0027-the-future-of-search.md)
- Main CMS Repo: [Umbraco-CMS](https://github.com/umbraco/Umbraco-CMS)
- Examine: [Shazwazza/Examine](https://github.com/Shazwazza/Examine)
