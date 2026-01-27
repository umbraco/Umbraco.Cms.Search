# Umbraco Search Client - Code Splitting Pattern with Importmap

## Overview

This backoffice client uses a three-bundle code-splitting strategy with importmap pattern to optimize initial load performance and align with Umbraco CMS conventions:

- **search-bundle.js** (~3kb) - Manifest metadata, loaded upfront by Umbraco
- **search-global.js** (~1.5kb) - Global contexts, loaded upfront for server event subscriptions
- **search-core.js** (~22kb) - Core implementation classes, lazy-loaded on demand
- **Importmap** - Logical module identifiers (`@umbraco-cms/search/global` and `@umbraco-cms/search/core`) resolve to physical files at runtime

## Architecture

### Bundle Structure

```
src/
├── bundle/
│   ├── search-bundle.ts         → search-bundle.js (manifests metadata)
│   ├── repositories.manifests.ts
│   ├── collectionViews.manifests.ts
│   ├── collectionActions.manifests.ts
│   └── entityActions.manifests.ts
├── global/
│   ├── search-global.ts         → search-global.js (global contexts entry)
│   ├── index.ts                 → Re-exports
│   └── globalContexts/
│       ├── search-notification.global-context.ts
│       └── search-notification.context-token.ts
└── core/
    ├── search-core.ts           → search-core.js (core library entry)
    ├── index.ts                 → Core library exports
    ├── collectionActions/       → Implementation classes
    ├── collectionViews/
    ├── entityActions/
    ├── repositories/
    └── api/
```

### How It Works

1. **Upfront Load - Manifests**: Umbraco loads `search-bundle.js` which contains only manifest metadata
2. **Upfront Load - Global**: Browser immediately loads `search-global.js` to initialize global contexts (e.g., notification listeners)
3. **Lazy Load - Core**: When user accesses search functionality, manifests trigger import of `search-core.js`
4. **Shared Bundles**: Core and global code loaded once and shared across all manifests

### The Importmap Principle

Instead of using raw file paths, this package uses logical module identifiers that resolve differently in different contexts:

**Logical Import (What You Write):**
```typescript
import('@umbraco-cms/search/core')
```

**Development Resolution (TypeScript):**
- TypeScript sees `tsconfig.json` paths mapping
- Resolves to `./src/core/index.ts` for type-checking
- ✅ Full IntelliSense and type safety

**Build Resolution (Vite):**
- Vite sees it's marked as external in `rollupOptions`
- ✅ Not bundled, left as-is in output

**Runtime Resolution (Browser):**
- Browser checks importmap in `umbraco-package.json`
- Resolves to `/App_Plugins/UmbracoSearch/search-core.js`
- ✅ Loads the actual built file

**Why This Is Clever:**
1. **Abstraction Layer**: Import paths are logical identifiers, not physical file locations
2. **Convention Alignment**: Matches Umbraco CMS core pattern (`@umbraco-cms/backoffice/*`)
3. **Professional Naming**: Namespaced package names follow Node.js conventions
4. **Future-Proof**: Can change file structure without updating imports throughout codebase
5. **Extensibility**: Third-party developers can import your runtime in their own packages
6. **V19 Ready**: Named for eventual merge into core with no breaking changes needed

## Implementation Patterns

### For `api` Properties

When a manifest needs to instantiate a class, use this pattern:

```typescript
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'repository',
    alias: 'My.Repository',
    api: () => import('@umbraco-cms/search/core')
      .then(m => ({ default: m.UmbSearchCollectionRepository })),
  }
];
```

**Why the wrapper?** Umbraco expects the import to return a module with a `default` or `api` export.

**Note:** The logical import `@umbraco-cms/search/core` resolves to the actual file via the importmap at runtime.

### For `element` Properties with `elementName`

When using custom elements with the `elementName` property:

```typescript
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'collectionView',
    alias: 'My.CollectionView',
    element: '@umbraco-cms/search/core',
    elementName: 'umb-search-root-collection-view',
  }
];
```

**Why simpler?** The module just needs to be loaded (registering the custom element via `@customElement` decorator). Umbraco then looks up the element by `elementName`.

**Note:** The logical import `@umbraco-cms/search/core` resolves to the actual file via the importmap at runtime.

### For Global Contexts

Global contexts need to be loaded upfront (not lazy-loaded) because they listen for server events. Import them directly in your components:

```typescript
import { UMB_SEARCH_NOTIFICATION_CONTEXT } from '@umbraco-cms/search/global';

// In your component:
this.consumeContext(UMB_SEARCH_NOTIFICATION_CONTEXT, (instance) => {
  // Use the global notification context
  instance.setUserWaitingForIndexUpdate('myIndex', true);
});
```

**Why upfront?** Global contexts must subscribe to SignalR events immediately, so they're loaded with the bundle rather than lazy-loaded.

## Configuration

### Vite Config

Three entry points create the three bundles:

```typescript
// vite.config.ts
export default defineConfig({
  build: {
    lib: {
      entry: {
        "search-bundle": "src/bundle/search-bundle.ts",    // Manifests bundle
        "search-global": "src/global/search-global.ts",    // Global contexts bundle
        "search-core": "src/core/search-core.ts"           // Core implementation bundle
      },
      formats: ["es"],
    },
    rollupOptions: {
      external: [
        /^@umbraco/  // All @umbraco imports treated as external
      ]
    },
  },
});
```

### TypeScript Config

Path mapping allows TypeScript to resolve the logical imports during development:

```json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@umbraco-cms/search/global": ["./src/global/index.ts"],
      "@umbraco-cms/search/core": ["./src/core/index.ts"]
    }
  }
}
```

**Note:** Maps to index.ts files (the actual exports) in each package, not the entry files (search-global.ts, search-core.ts) which re-export from index.

### Umbraco Package Config

The importmap in `umbraco-package.json` provides runtime resolution:

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

**Note:** The bundle (search-bundle.js) is loaded by Umbraco via the extensions array, not via importmap. Only the global and core subpaths are in the importmap.

### How It Works Together

**Developer writes:**
```typescript
import { UmbSearchRepository } from '@umbraco-cms/search/core';
```

**During Development:**
1. TypeScript sees `@umbraco-cms/search/core`
2. Checks tsconfig.json `paths` mapping
3. Resolves to `./src/core/index.ts` for type-checking
4. ✅ Full IntelliSense and type safety

**During Build:**
1. Vite encounters `@umbraco-cms/search/core`
2. Marked as external in rollupOptions
3. ✅ Not bundled, left as-is in output

**At Runtime:**
1. Browser encounters `import('@umbraco-cms/search/core')`
2. Checks importmap in umbraco-package.json
3. Resolves to `/App_Plugins/UmbracoSearch/search-core.js`
4. ✅ Loads the actual built file

## Package Exports

The package is organized into two logical subpaths, each with its own exports:

### Core Package (`@umbraco-cms/search/core`)

All core implementation classes are exported from `src/core/index.ts`:

```typescript
// src/core/index.ts
export * from './collectionActions/reload.collection-action.js';
export * from './collectionViews/search-root-collection-view.element.js';
export { default as UmbSearchCollectionViewRootElement } from './collectionViews/search-root-collection-view.element.js';
export * from './search-collection.context.js';
export * from './repositories/search.repository.js';
export * from './entityActions/rebuild-index.action.js';
export * from './api/index.js';
```

### Global Package (`@umbraco-cms/search/global`)

All global contexts are exported from `src/global/index.ts`:

```typescript
// src/global/index.ts
export * from './globalContexts/search-notification.global-context.js';
export * from './globalContexts/search-notification.context-token.js';
```

**Note**: Default exports need explicit named re-exports for the import pattern to work.

## Benefits

1. **Performance**: Only ~4.5kb loaded upfront (bundle + global) instead of full ~26.5kb
2. **Maintainability**: Clear separation between manifests, global contexts, and core implementation
3. **Shared Bundles**: Core and global code loaded once and shared across all components
4. **Real-time Notifications**: Global contexts can listen to SignalR events immediately
5. **Type Safety**: Full TypeScript support via path mapping for both subpaths
6. **Convention Alignment**: Uses subpath patterns like Umbraco CMS core (`@umbraco-cms/backoffice/*`)
7. **Abstraction Layer**: Logical imports hide physical file locations
8. **Extensibility**: Third-party developers can import both core and global exports
9. **Future-Proof**: Scoped package naming ready for ecosystem growth

## Localization

Localization files are kept separate and loaded globally as they're always needed:

```
src/
└── lang/
    └── manifests.ts  → Loaded globally, not code-split
```
