# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the backoffice client code in this directory.

**See also:** [Main repository CLAUDE.md](../../../CLAUDE.md) for server-side architecture and build commands.

## Overview

The Umbraco Search backoffice client uses a **three-bundle code-splitting pattern with importmap** for optimal performance:

- **search-bundle.js** (~3kb) - Manifest metadata, loaded upfront
- **search-global.js** (~1.5kb) - Global contexts for SignalR events, loaded upfront
- **search-core.js** (~22kb) - Core implementations, lazy-loaded on demand

**Key principle:** Logical imports (`@umbraco-cms/search/core`) resolve via importmap at runtime, not physical file paths.

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
├── bundle/          → search-bundle.js (manifests only)
│   ├── search-bundle.ts
│   └── *.manifests.ts
├── global/          → search-global.js (loaded upfront)
│   ├── search-global.ts
│   ├── index.ts
│   └── globalContexts/
├── core/            → search-core.js (lazy-loaded)
│   ├── search-core.ts
│   ├── index.ts
│   ├── collectionActions/
│   ├── collectionViews/
│   ├── entityActions/
│   ├── repositories/
│   └── api/
└── lang/            → Localization (not code-split)
```

### The Importmap Pattern

Instead of importing files directly, use logical module identifiers that resolve differently in each context:

**What you write:**
```typescript
import { UmbSearchRepository } from '@umbraco-cms/search/core';
```

**Development (TypeScript):**
- `tsconfig.json` paths resolve to `./src/core/index.ts`
- Full IntelliSense and type safety

**Build (Vite):**
- Marked as external in `rollupOptions`
- Not bundled, left as import statement

**Runtime (Browser):**
- `umbraco-package.json` importmap resolves to `/App_Plugins/UmbracoSearch/search-core.js`
- Lazy-loaded when first used

## Manifest Patterns

### For `api` Properties (Instantiating Classes)

```typescript
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'repository',
    alias: 'Umb.Repository.SearchCollection',
    api: () => import('@umbraco-cms/search/core')
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
    element: '@umbraco-cms/search/core',
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

**Critical:** Global contexts must be in `search-global.js` (not `search-core.js`) because they subscribe to SignalR events immediately.

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
        "search-core": "src/core/search-core.ts"
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
      "@umbraco-cms/search/core": ["./src/core/index.ts"]
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
      "@umbraco-cms/search/core": "/App_Plugins/UmbracoSearch/search-core.js"
    }
  }
}
```

## Adding New Features

### Adding a New Collection Action

1. Create implementation in `src/core/collectionActions/`
2. Export from `src/core/index.ts`
3. Add manifest in `src/bundle/collectionActions.manifests.ts`
4. Use `api: () => import('@umbraco-cms/search/core').then(m => ({ default: m.YourAction }))`

### Adding a New Global Context

1. Create context in `src/global/globalContexts/`
2. Create context token in same directory
3. Export both from `src/global/index.ts`
4. **Important:** Global contexts load upfront, so keep them lightweight

### Adding a New API Endpoint

1. Run `npm run generate-client` after adding endpoint to server
2. Generated types appear in `src/core/api/`
3. Use generated services in repositories or contexts

## Common Patterns

### Consuming Global Contexts

```typescript
import { UMB_SEARCH_NOTIFICATION_CONTEXT } from '@umbraco-cms/search/global';

export class MyElement extends UmbElementMixin(LitElement) {
  constructor() {
    super();
    this.consumeContext(UMB_SEARCH_NOTIFICATION_CONTEXT, (context) => {
      this._notificationContext = context;
    });
  }
}
```

### Lazy Loading Core Services

```typescript
// In a manifest
api: () => import('@umbraco-cms/search/core')
  .then(m => ({ default: m.UmbSearchRepository }))

// Or in code
const { UmbSearchRepository } = await import('@umbraco-cms/search/core');
```

### Exporting Classes with Default Export

```typescript
// src/core/repositories/search.repository.ts
export class UmbSearchRepository { }

// src/core/index.ts
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

1. **Global vs Core**: SignalR listeners must be in `search-global.js`, not `search-core.js`
2. **Default Exports**: Classes used in manifests need both named and default exports
3. **Path Mapping**: TypeScript paths point to `index.ts`, not entry files
4. **Importmap Scope**: Only `core` and `global` are in importmap; `bundle` loads via Umbraco
5. **External Dependencies**: All `@umbraco-cms/backoffice/*` imports must be external

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
