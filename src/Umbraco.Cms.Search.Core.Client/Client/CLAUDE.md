# Umbraco Search Client - Code Splitting Pattern

## Overview

This backoffice client uses a two-bundle code-splitting strategy to optimize initial load performance:

- **search-bundle.js** (~1-2kb) - Manifest metadata, loaded upfront by Umbraco
- **search-library.js** (~18-20kb) - Implementation classes, lazy-loaded on demand

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

## Implementation Patterns

### For `api` Properties

When a manifest needs to instantiate a class, use this pattern:

```typescript
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'repository',
    alias: 'My.Repository',
    api: () => import('/App_Plugins/UmbracoSearch/search-library.js')
      .then(m => ({ default: m.UmbSearchCollectionRepository })),
  }
];
```

**Why the wrapper?** Umbraco expects the import to return a module with a `default` or `api` export.

### For `element` Properties with `elementName`

When using custom elements with the `elementName` property:

```typescript
export const manifests: Array<UmbExtensionManifest> = [
  {
    type: 'collectionView',
    alias: 'My.CollectionView',
    element: '/App_Plugins/UmbracoSearch/search-library.js',
    elementName: 'umb-search-root-collection-view',
  }
];
```

**Why simpler?** The module just needs to be loaded (registering the custom element via `@customElement` decorator). Umbraco then looks up the element by `elementName`.

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
        "search-library": "src/index.ts"               // Library bundle
      },
      formats: ["es"],
    },
    rollupOptions: {
      external: [
        /^@umbraco/,
        '/App_Plugins/UmbracoSearch/search-library.js'  // Treat as external
      ]
    },
  },
});
```

### TypeScript Config

Path mapping allows TypeScript to resolve the runtime path during development:

```json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "/App_Plugins/UmbracoSearch/search-library.js": ["./src/index.ts"]
    }
  }
}
```

**How it works:**
- ✅ **Development**: TypeScript resolves to `src/index.ts` for type-checking
- ✅ **Build time**: Vite treats it as external (won't bundle it)
- ✅ **Runtime**: Browser loads the actual `search-library.js` file

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

## Localization

Localization files are kept separate and loaded globally as they're always needed:

```
src/
└── lang/
    └── manifests.ts  → Loaded globally, not code-split
```
