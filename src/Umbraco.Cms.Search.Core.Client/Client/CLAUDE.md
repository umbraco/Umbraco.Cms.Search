# Umbraco Search Client - Code Splitting Pattern with Importmap

## Overview

This backoffice client uses a two-bundle code-splitting strategy with importmap pattern to optimize initial load performance and align with Umbraco CMS conventions:

- **search-bundle.js** (~1-2kb) - Manifest metadata, loaded upfront by Umbraco
- **search-library.js** (~18-20kb) - Implementation classes, lazy-loaded on demand
- **Importmap** - Logical module identifiers (`@umbraco-cms/search`) resolve to physical files at runtime

## Architecture

### Bundle Structure

```
src/
├── bundle.manifests.ts    → search-bundle.js (entry point, manifests only)
├── index.ts               → search-library.js (library exports)
└── search/
    ├── manifests.ts       → Aggregates all manifest files
    ├── collectionActions/
    │   └── manifests.ts   → Manifest declarations
    │   └── *.ts           → Implementation classes
    ├── collectionViews/
    ├── entityActions/
    └── repositories/
```

### How It Works

1. **Upfront Load**: Umbraco loads `search-bundle.js` which contains only manifest metadata
2. **Lazy Load**: When user accesses search functionality, manifests trigger import of `search-library.js`
3. **Shared Bundle**: All implementations are in one library file, imported once and shared

### The Importmap Principle

Instead of using raw file paths, this package uses logical module identifiers that resolve differently in different contexts:

**Logical Import (What You Write):**
```typescript
import('@umbraco-cms/search')
```

**Development Resolution (TypeScript):**
- TypeScript sees `tsconfig.json` paths mapping
- Resolves to `./src/lib/index.ts` for type-checking
- ✅ Full IntelliSense and type safety

**Build Resolution (Vite):**
- Vite sees it's marked as external in `rollupOptions`
- ✅ Not bundled, left as-is in output

**Runtime Resolution (Browser):**
- Browser checks importmap in `umbraco-package.json`
- Resolves to `/App_Plugins/UmbracoSearch/search-library.js`
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
    api: () => import('@umbraco-cms/search')
      .then(m => ({ default: m.UmbSearchCollectionRepository })),
  }
];
```

**Why the wrapper?** Umbraco expects the import to return a module with a `default` or `api` export.

**Note:** The logical import `@umbraco-cms/search` resolves to the actual file via the importmap at runtime.

### For `element` Properties with `elementName`

When using custom elements with the `elementName` property:

```typescript
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'collectionView',
    alias: 'My.CollectionView',
    element: '@umbraco-cms/search',
    elementName: 'umb-search-root-collection-view',
  }
];
```

**Why simpler?** The module just needs to be loaded (registering the custom element via `@customElement` decorator). Umbraco then looks up the element by `elementName`.

**Note:** The logical import `@umbraco-cms/search` resolves to the actual file via the importmap at runtime.

## Configuration

### Vite Config

Two key configurations enable this pattern:

```typescript
// vite.config.ts
export default defineConfig({
  build: {
    lib: {
      entry: {
        "search-bundle": "src/bundle.manifests.ts",    // Manifests bundle
        "search-library": "src/lib/search-library.ts"  // Library bundle
      },
      formats: ["es"],
    },
    rollupOptions: {
      external: [
        /^@umbraco/,
        '@umbraco-cms/search'  // Treat logical import as external
      ]
    },
  },
});
```

### TypeScript Config

Path mapping allows TypeScript to resolve the logical import during development:

```json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@umbraco-cms/search": ["./src/lib/index.ts"]
    }
  }
}
```

**Note:** Maps to `src/lib/index.ts` (the actual exports file), not `src/lib/search-library.ts` (which re-exports from index).

### Umbraco Package Config

The importmap in `umbraco-package.json` provides runtime resolution:

```json
{
  "id": "Umbraco.Cms.Search.Core.Client",
  "extensions": [ /* ... */ ],
  "importmap": {
    "imports": {
      "@umbraco-cms/search": "/App_Plugins/UmbracoSearch/search-library.js"
    }
  }
}
```

### How It Works Together

**Developer writes:**
```typescript
import { UmbSearchRepository } from '@umbraco-cms/search';
```

**During Development:**
1. TypeScript sees `@umbraco-cms/search`
2. Checks tsconfig.json `paths` mapping
3. Resolves to `./src/lib/index.ts` for type-checking
4. ✅ Full IntelliSense and type safety

**During Build:**
1. Vite encounters `@umbraco-cms/search`
2. Marked as external in rollupOptions
3. ✅ Not bundled, left as-is in output

**At Runtime:**
1. Browser encounters `import('@umbraco-cms/search')`
2. Checks importmap in umbraco-package.json
3. Resolves to `/App_Plugins/UmbracoSearch/search-library.js`
4. ✅ Loads the actual built file

## Library Exports

All implementation classes must be exported from `src/index.ts`:

```typescript
// src/index.ts
export * from './search/index.js';

// src/search/index.ts
export * from './collectionActions/reload.collection-action.js';
export * from './collectionViews/search-root-collection-view.element.js';
export { default as UmbSearchCollectionViewRootElement } from './collectionViews/search-root-collection-view.element.js';
export * from './search-collection.context.js';
export * from './repositories/search.repository.js';
export * from './entityActions/rebuild-index.action.js';
```

**Note**: Default exports need explicit named re-exports for the import pattern to work.

## Benefits

1. **Performance**: Only ~1-2kb loaded upfront instead of full ~20kb
2. **Maintainability**: Clear separation between metadata and implementation
3. **Shared Bundle**: Library code loaded once, shared across all manifests
4. **Type Safety**: Full TypeScript support via path mapping
5. **Convention Alignment**: Matches Umbraco CMS core patterns (`@umbraco-cms/backoffice/*`)
6. **Abstraction Layer**: Logical imports hide physical file locations
7. **Extensibility**: Third-party developers can import and extend your runtime
8. **Future-Proof**: Named for eventual V19 core merge with no breaking changes

## Localization

Localization files are kept separate and loaded globally as they're always needed:

```
src/
└── lang/
    └── manifests.ts  → Loaded globally, not code-split
```
