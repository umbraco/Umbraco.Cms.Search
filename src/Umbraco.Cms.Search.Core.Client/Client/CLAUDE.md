# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the backoffice client code in this directory.

**See also:** [Main repository CLAUDE.md](../../../CLAUDE.md) for server-side architecture and build commands.

## Overview

The Umbraco Search backoffice client uses a **three-bundle code-splitting pattern with importmap** for optimal performance:

- **search-bundle.js** (~3kb) - Manifest metadata, loaded upfront
- **search-global.js** (~1.5kb) - Global contexts for SignalR events, loaded upfront
- **search-settings.js** (~22kb) - Core implementations, lazy-loaded on demand

**Key principle:** Logical imports (`@umbraco-cms/search/settings`) resolve via importmap at runtime, not physical file paths.

## Build Commands

```bash
# Install dependencies
npm install

# Build for production
npm run build

# Watch mode for development
npm run watch

# Generate OpenAPI client (requires test site at https://localhost:44324)
npm run generate-client
```

**Requirements:** Node.js 24 (see `.nvmrc`)

## Architecture Pattern

### Bundle Structure

```
src/
├── bundle/                          → search-bundle.js (manifests only)
│   ├── search-bundle.ts             (Entry point, aggregates all manifests)
│   ├── bundle.manifests.ts          (Aggregates all manifest files)
│   ├── collectionActions.manifests.ts
│   ├── collectionViews.manifests.ts
│   ├── entityActions.manifests.ts
│   ├── globalContexts.manifests.ts
│   ├── repositories.manifests.ts
│   ├── search-root-workspace.manifests.ts  (Root collection workspace)
│   ├── search-workspace.manifests.ts       (Detail workspace)
│   ├── indexDetailBoxes.manifests.ts       (Index detail box extensions)
│   ├── indexDetailBoxKind.manifests.ts     (Custom extension kind)
│   ├── types.ts                     (Extension type definitions)
│   └── lang/                        (Localization strings)
│       ├── en.ts, da.ts
│       └── manifests.ts
│
├── global/                          → search-global.js (loaded upfront)
│   ├── search-global.ts             (Entry point)
│   ├── search.global-context.ts     (UmbSearchContext for notifications)
│   ├── constants.ts                 (Entity types, aliases, event types)
│   └── index.ts                     (Exports)
│
└── settings/                        → search-settings.js (lazy-loaded)
    ├── search-settings.ts           (Entry point)
    ├── index.ts                     (Library exports)
    ├── types.ts                     (Domain types: UmbSearchIndex, etc.)
    ├── api/                         (Generated OpenAPI client)
    ├── collection/
    │   ├── search-collection.context.ts
    │   ├── search-root-collection-view.element.ts
    │   ├── reload.collection-action.ts
    │   └── rebuild-index.action.ts  (Entity action for collection items)
    ├── repositories/
    │   ├── search-collection.repository.ts
    │   ├── search-collection.server.data-source.ts
    │   ├── search-detail.repository.ts
    │   ├── search-detail.server.data-source.ts
    │   ├── search-detail.store.ts
    │   └── search-detail.store.context-token.ts
    ├── workspace/
    │   ├── paths.ts
    │   └── search/
    │       ├── search-workspace.context.ts
    │       ├── search-workspace.context-token.ts
    │       ├── search-workspace-editor.element.ts
    │       └── views/
    │           ├── search-details-view.element.ts   (Container with extension slot)
    │           ├── search-index-stats-box.element.ts
    │           └── search-index-search-box.element.ts
    └── workspaceActions/
        └── rebuild-index.workspace-action.ts
```

### Two-Workspace Architecture

The Search UI uses **two separate workspaces**:

1. **Root Workspace** (`Umbraco.Search.Workspace.Root`)
   - Displays table/collection view of all search indexes
   - Components: `UmbSearchRootCollectionView` with columns for Alias, Health, Doc Count
   - Entity actions on each row (e.g., Rebuild Index)

2. **Detail Workspace** (`Umbraco.Search.Workspace`)
   - Detailed view of a single search index
   - Route: `/edit/:unique` where `unique` is the index alias
   - Uses `UmbSearchWorkspaceContext` extending `UmbEntityNamedDetailWorkspaceContextBase`
   - Workspace actions (e.g., Rebuild Index button)

### Custom Extension Type: searchIndexDetailBox

The detail view uses an **extensible box pattern** for composable UI:

```typescript
// In search-details-view.element.ts
<umb-extension-slot type="searchIndexDetailBox"></umb-extension-slot>
```

**Built-in boxes:**
- **Stats Box** (weight: 100) - Displays index alias, document count, health status with color coding
- **Search Box** (weight: 200) - Test search functionality within the workspace

**Extension type definition** (`bundle/types.ts`):
```typescript
interface ManifestSearchIndexDetailBox extends ManifestElement, ManifestWithDynamicConditions {
  type: 'searchIndexDetailBox';
  meta?: MetaSearchIndexDetailBox;
}
```

**Kind registration** (`bundle/indexDetailBoxKind.manifests.ts`):
```typescript
{
  type: 'kind',
  alias: 'Umb.Kind.SearchIndexDetailBox',
  matchKind: 'default',
  matchType: 'searchIndexDetailBox',
}
```

### The Importmap Pattern

Instead of importing files directly, use logical module identifiers that resolve differently in each context:

**What you write:**
```typescript
import { UmbSearchRepository } from '@umbraco-cms/search/settings';
```

**Development (TypeScript):**
- `tsconfig.json` paths resolve to `./src/settings/index.ts`
- Full IntelliSense and type safety

**Build (Vite):**
- Marked as external in `rollupOptions`
- Not bundled, left as import statement

**Runtime (Browser):**
- `umbraco-package.json` importmap resolves to `/App_Plugins/UmbracoSearch/search-settings.js`
- Lazy-loaded when first used

## Manifest Patterns

### For `api` Properties (Instantiating Classes)

```typescript
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'repository',
    alias: 'Umb.Repository.SearchCollection',
    api: () => import('@umbraco-cms/search/settings')
      .then(m => ({ default: m.UmbSearchCollectionRepository })),
  }
];
```

**Why the wrapper?** Umbraco expects `{ default: Class }` or `{ api: Class }`.

### For `element` Properties (Web Components)

```typescript
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'collectionView',
    alias: 'Umb.CollectionView.SearchRoot',
    element: '@umbraco-cms/search/settings',
    elementName: 'umb-search-root-collection-view',
  }
];
```

**Why simpler?** The module just needs to load (registers custom element). Umbraco finds it by `elementName`.

### For Global Contexts (Upfront Loading)

```typescript
import { UMB_SEARCH_NOTIFICATION_CONTEXT } from '@umbraco-cms/search/global';

// In your component
this.consumeContext(UMB_SEARCH_NOTIFICATION_CONTEXT, (instance) => {
  instance.setUserWaitingForIndexUpdate('myIndex', true);
});
```

**Critical:** Global contexts must be in `search-global.js` (not `search-settings.js`) because they subscribe to SignalR events immediately.

## Configuration Files

### vite.config.ts

Three entry points create three bundles:

```typescript
export default defineConfig({
  build: {
    lib: {
      entry: {
        "search-bundle": "src/bundle/search-bundle.ts",
        "search-global": "src/global/search-global.ts",
        "search-settings": "src/settings/search-settings.ts"
      },
      formats: ["es"],
    },
    rollupOptions: {
      external: [/^@umbraco/]  // Don't bundle @umbraco imports
    },
  },
});
```

### tsconfig.json

Maps logical imports to physical files for type-checking:

```json
{
  "compilerOptions": {
    "paths": {
      "@umbraco-cms/search/global": ["./src/global/index.ts"],
      "@umbraco-cms/search/settings": ["./src/settings/index.ts"]
    }
  }
}
```

**Note:** Maps to `index.ts` (exports), not entry files (`search-*.ts`).

### public/umbraco-package.json

Provides runtime resolution via importmap:

```json
{
  "id": "Umbraco.Cms.Search.Core",
  "name": "@umbraco-cms/search",
  "importmap": {
    "imports": {
      "@umbraco-cms/search/global": "/App_Plugins/UmbracoSearch/search-global.js",
      "@umbraco-cms/search/settings": "/App_Plugins/UmbracoSearch/search-settings.js"
    }
  }
}
```

## Adding New Features

### Adding a New Collection Action

1. Create implementation in `src/settings/collectionActions/`
2. Export from `src/settings/index.ts`
3. Add manifest in `src/bundle/collectionActions.manifests.ts`
4. Use `api: () => import('@umbraco-cms/search/settings').then(m => ({ default: m.YourAction }))`

### Adding a New Global Context

1. Create context in `src/global/`
2. Create context token in same file or separate file
3. Export both from `src/global/index.ts`
4. **Important:** Global contexts load upfront, so keep them lightweight

### Adding a New Index Detail Box

1. Create element in `src/settings/workspace/search/views/`
2. Extend `UmbLitElement` and consume `UMB_SEARCH_WORKSPACE_CONTEXT`
3. Export from `src/settings/index.ts`
4. Add manifest in `src/bundle/indexDetailBoxes.manifests.ts`:

```typescript
{
  type: 'searchIndexDetailBox',
  alias: 'Umb.Search.IndexDetailBox.MyBox',
  name: 'My Custom Box',
  weight: 150,
  element: '@umbraco-cms/search/settings',
  elementName: 'umb-search-index-my-box',
}
```

### Adding a New API Endpoint

1. Run `npm run generate-client` after adding endpoint to server
2. Generated types appear in `src/settings/api/` (should be moved to new module if consumed by other parts of the Backoffice in the future)
3. Use generated services in repositories or contexts

## Global Constants

All entity types, aliases, and event types are centralized in `src/global/constants.ts`:

```typescript
// Entity types
UMB_SEARCH_ROOT_ENTITY_TYPE = 'search-root'
UMB_SEARCH_ENTITY_TYPE = 'search'
UMB_SEARCH_INDEX_ENTITY_TYPE = 'Umb.Search.Index'

// Repository/Store aliases
UMB_SEARCH_COLLECTION_REPOSITORY_ALIAS = 'UMB_SEARCH_COLLECTION_REPOSITORY'
UMB_SEARCH_DETAIL_REPOSITORY_ALIAS = 'UmbSearchDetailRepository'
UMB_SEARCH_DETAIL_STORE_ALIAS = 'UmbSearchStore'

// Workspace aliases
UMB_SEARCH_ROOT_WORKSPACE_ALIAS = 'Umbraco.Search.Workspace.Root'
UMB_SEARCH_WORKSPACE_ALIAS = 'Umbraco.Search.Workspace'

// Server events
UMB_SEARCH_SERVER_EVENT_TYPE = 'IndexRebuildCompleted'
```

## Common Patterns

### Workspace Context Token with Type Guard

```typescript
// search-workspace.context-token.ts
export const UMB_SEARCH_WORKSPACE_CONTEXT = new UmbContextToken<
  UmbWorkspaceContext,
  UmbSearchWorkspaceContext
>(
  'UmbWorkspaceContext',
  undefined,
  (context): context is UmbSearchWorkspaceContext =>
    context.getEntityType?.() === UMB_SEARCH_ENTITY_TYPE,
);
```

### Observing Workspace Context Properties

```typescript
// In an element consuming the workspace context
this.consumeContext(UMB_SEARCH_WORKSPACE_CONTEXT, (context) => {
  this.observe(context.documentCount, (count) => {
    this._documentCount = count;
  });
  this.observe(context.healthStatus, (status) => {
    this._healthStatus = status;
  });
});
```

### Consuming Global Search Context

```typescript
import { UMB_SEARCH_CONTEXT } from '@umbraco-cms/search/global';

export class MyElement extends UmbLitElement {
  constructor() {
    super();
    this.consumeContext(UMB_SEARCH_CONTEXT, (context) => {
      // Track user waiting for rebuild notification
      context.setUserWaitingForIndexUpdate('indexAlias', true);
    });
  }
}
```

### Observing Server Events (SignalR)

```typescript
// Pattern used in workspace/collection contexts
this.observe(
  this.#serverEventContext?.byEventSource(UMB_SEARCH_SERVER_EVENT_TYPE),
  async (args) => {
    if (args?.eventSource !== this.getUnique()) return;
    await this.reload();
  },
  'observer-rebuild-event'
);
```

### Lazy Loading Core Services

```typescript
// In a manifest
api: () => import('@umbraco-cms/search/settings')
  .then(m => ({ default: m.UmbSearchRepository }))

// Or in code
const { UmbSearchRepository } = await import('@umbraco-cms/search/settings');
```

### Exporting Classes with Default Export

```typescript
// src/settings/repositories/search.repository.ts
export class UmbSearchRepository { }

// src/settings/index.ts
export * from './repositories/search.repository.js';
export { UmbSearchRepository } from './repositories/search.repository.js';
```

## Why This Pattern?

1. **Performance**: Only 4.5kb loaded upfront vs 26.5kb
2. **Shared Code**: Core bundle loaded once, shared across all manifests
3. **SignalR Ready**: Global contexts can subscribe to events immediately
4. **Type Safe**: Full IntelliSense via TypeScript path mapping
5. **Umbraco Convention**: Matches core pattern (`@umbraco-cms/backoffice/*`)
6. **Extensible**: Third parties can import `@umbraco-cms/search/*` in their packages

## Common Gotchas

1. **Global vs Settings**: SignalR listeners must be in `search-global.js`, not `search-settings.js`
2. **Global Has No Dependencies**: The `bundle` and `global` bundles must NOT import from anything else - they load upfront and must stay lightweight with no lazy-loaded dependencies
3. **Default Exports**: Classes used in manifests need both named and default exports
4. **Path Mapping**: TypeScript paths point to `index.ts`, not entry files
5. **Importmap Scope**: Only `settings` and `global` are in importmap; `bundle` loads via the `umbraco-package.json` declaration file
6. **External Dependencies**: All `@umbraco-cms/backoffice/*` imports must be external
7. **Workspace Context Token**: Use type guard function to narrow generic `UmbWorkspaceContext` to specific type
8. **Extension Slot Types**: Custom extension types (like `searchIndexDetailBox`) need both kind manifest and TypeScript interface with global declaration

## Testing

The client is tested through the test site. To run the test site:

```bash
cd ../../../Umbraco.Web.TestSite.V17
dotnet run
```

For client-side debugging:
1. Run test site (command above)
2. Run watch mode: `npm run watch`
3. Changes auto-rebuild and hot-reload in browser

See [main CLAUDE.md](../../../CLAUDE.md) for full test site details and server-side testing.
