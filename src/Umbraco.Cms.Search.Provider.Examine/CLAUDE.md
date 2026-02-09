# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the Examine provider client code in this project.

**See also:** [Main repository CLAUDE.md](../../CLAUDE.md) for server-side architecture and build commands.

## Overview

The Examine provider client adds a **"Show Fields"** feature to the Search backoffice UI. When a user clicks a search document in the Core Client's search results, this provider adds an entity action that opens a sidebar modal showing all indexed fields for that document.

This is a simpler client than the Core Client - it uses a **single-bundle pattern** (no importmap, no code-splitting across multiple entry points).

## Build Commands

This workspace is part of an **npm workspaces monorepo** rooted at `src/`. Shared config (`tsconfig.base.json`, `.prettierrc.json`, `.nvmrc`) lives there.

```bash
# From the monorepo root (src/)
cd src

# Install all workspaces (run from src/, not from this directory)
npm install

# Build all workspaces
npm run build

# Build only this workspace
npm run build --workspace=Umbraco.Cms.Search.Provider.Examine/Client

# Watch only this workspace
npm run watch --workspace=Umbraco.Cms.Search.Provider.Examine/Client

# Generate OpenAPI client (requires test site at https://localhost:44324)
npm run generate-client --workspace=Umbraco.Cms.Search.Provider.Examine/Client
```

**Requirements:** Node.js 24 (see `src/.nvmrc`)

## Architecture

### Bundle Pattern

Uses Umbraco's **bundle extension type** - a single `umbraco-package.json` declares a bundle, and the bundle JS file exports a `manifests` array:

```
Client/
├── src/
│   ├── examine-bundle.ts              (Entry point - re-exports manifests)
│   ├── examine-bundle.manifests.ts    (Extension manifests array)
│   ├── examine-provider.repository.ts (API calls to Examine endpoints)
│   ├── show-fields.entity-action.ts   (Entity action triggering the modal)
│   └── show-fields.modal.ts           (Sidebar modal showing indexed fields)
├── public/
│   └── umbraco-package.json           (Bundle declaration)
├── package.json
├── tsconfig.json
├── vite.config.ts
├── eslint.config.js
└── .prettierignore
```

### How It Works

1. **`umbraco-package.json`** declares a bundle extension pointing to `examine-bundle.js`
2. **`examine-bundle.ts`** re-exports the `manifests` array from `examine-bundle.manifests.ts`
3. **`examine-bundle.manifests.ts`** registers two extensions:
   - An **entity action** (`entityAction`) for entity type `search-document` with lazy-loaded `api`
   - A **modal** (`modal`) with lazy-loaded `element`
4. Vite code-splits: the bundle is ~0.85kb, with lazy chunks for the action + modal loaded on demand

### Key Files

**`examine-provider.repository.ts`** - Repository for Examine-specific API calls:
- `requestSearchDocument(unique, indexAlias)` - Fetches all indexed fields for a specific document
- Uses `UMB_AUTH_CONTEXT.getLatestToken()` for authentication
- Uses `umbHttpClient` and `tryExecute()` from the backoffice SDK
- Calls `/umbraco/examine/api/v1/{indexAlias}/document/{unique}`

**`show-fields.entity-action.ts`** - Entity action registered for `search-document` entities:
- Receives `searchDocument` and `indexAlias` via extended action args (type assertion needed)
- Opens the fields modal via `umbOpenModal()`

**`show-fields.modal.ts`** - Sidebar modal (~360 lines) showing indexed fields:
- Loads field data from the repository on `firstUpdated()`
- Groups fields by type with collapsible sections
- Provides search/filter functionality
- Expand/collapse all toggle
- Copy field values to clipboard
- Uses `@state()` decorators from `@umbraco-cms/backoffice/external/lit`

### Types

```typescript
// In examine-provider.repository.ts
interface ExamineField {
  name: string;
  type: string;
  values: Array<string>;
}

interface ExamineDocument {
  fields: Array<ExamineField>;
}

// In show-fields.modal.ts
interface ShowFieldsModalData {
  searchDocument: { unique: string };
  indexAlias: string;
}
```

## Configuration Files

### vite.config.ts

Single entry point, outputs to provider's wwwroot:

```typescript
export default defineConfig({
  build: {
    lib: {
      entry: 'src/examine-bundle.ts',
      formats: ['es'],
      fileName: () => 'examine-bundle.js',
    },
    outDir: '../wwwroot/App_Plugins/UmbracoSearchExamine',
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});
```

### tsconfig.json

Extends the shared base config. No path mappings needed (no importmap pattern):

```json
{
  "extends": "../../tsconfig.base.json",
  "compilerOptions": {
    "types": ["@umbraco-cms/backoffice/extension-types"]
  },
  "include": ["src"]
}
```

### public/umbraco-package.json

Simple bundle declaration:

```json
{
  "id": "Umbraco.Cms.Search.Provider.Examine",
  "name": "@umbraco-cms/search/examine",
  "extensions": [
    {
      "type": "bundle",
      "alias": "Umbraco.Cms.Search.Provider.Examine.Bundle",
      "name": "Umbraco Search Examine Bundle",
      "js": "/App_Plugins/UmbracoSearchExamine/examine-bundle.js"
    }
  ]
}
```

## Key Differences from Core Client

| Aspect | Core Client | Examine Client |
|--------|-------------|----------------|
| Bundle strategy | 3 bundles (importmap) | 1 bundle (simple) |
| Entry points | 3 (bundle, global, settings) | 1 (examine-bundle) |
| Code-splitting | Via importmap + lazy imports | Via Vite's built-in chunking |
| tsconfig paths | Yes (logical imports) | No (relative imports) |
| API generation | Yes (OpenAPI) | Yes (OpenAPI, separate Examine swagger) |
| Output directory | `UmbracoSearch/` | `UmbracoSearchExamine/` |

## Common Gotchas

1. **Entity Action Args**: The `searchDocument` and `indexAlias` properties are dynamically added to entity action args by the Core Client. Use type assertion: `const args = this.args as typeof this.args & { searchDocument?: ...; indexAlias?: string }`

2. **Auth Token**: Use `UMB_AUTH_CONTEXT.getLatestToken()` for authentication, not raw `config.auth()`.

3. **Lit Decorators**: Import `@state()` from `@umbraco-cms/backoffice/external/lit`, not from a separate decorators module.

4. **Bundle Output**: Built files go to `../wwwroot/App_Plugins/UmbracoSearchExamine/` which is gitignored. The `.csproj` serves these as static web assets.

5. **Entity Type**: The entity action registers for `search-document` entity type (set by Core Client when rendering search results).

## Testing

Test through the test site:

1. Run: `dotnet run --project src/Umbraco.Web.TestSite.V17`
2. Navigate to Settings > Search
3. Click on an index, then search for documents
4. Click the "..." menu on a search result document
5. Select "Show Fields" to open the sidebar modal
6. Verify fields load, filtering works, expand/collapse works, copy works

For development with watch mode:

```bash
cd src && npm run watch --workspace=Umbraco.Cms.Search.Provider.Examine/Client
```
