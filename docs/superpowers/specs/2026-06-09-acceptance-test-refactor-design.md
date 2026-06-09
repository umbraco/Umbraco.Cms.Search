# Acceptance Test Refactor — Full Umbraco-CMS Mirror

**Date:** 2026-06-09
**Project:** `src/Umbraco.Test.Search.AcceptanceTest`
**Branch:** `qa/add-acceptance-test`

## Goal

Refactor the Playwright acceptance test suite for the book-search frontend to mirror
the Umbraco-CMS acceptance test conventions as closely as a frontend (non-backoffice)
suite allows. The refactor is purely structural — no test behaviour changes.

Driving concerns (all in scope):
- Organize the large `BasePage` shared library (keep all methods, do not trim).
- Align naming and structure with Umbraco-CMS (`lib/` layout, fixture layer,
  `ConstantHelper`, `doesXxx` assertion naming, AAA structure).
- Remove duplication and dead/fragile code.

## Non-Goals

- No trimming of `BasePage` methods — it is an intentional shared toolkit.
- No `searchApi` fixture — there is no management API for this frontend demo, so an
  API helper would be an empty abstraction (YAGNI).
- No new test cases or changed assertions — structure only.
- No re-adding `@smoke` / `@release` tags (deliberately removed in an earlier commit).

## Target Folder Structure

Mirror the internal layout of `@umbraco/playwright-testhelpers` (POMs + helpers + a
fixture that builds the extended `test`):

```
src/Umbraco.Test.Search.AcceptanceTest/
  lib/
    index.ts                 # public API — re-exports test, ConstantHelper, page objects
    fixtures.ts              # const test = base.extend<{searchUi}>(...)  ← the umbracoUi analog
    helpers/
      ConstantHelper.ts      # moved from helpers/ (unchanged content)
      UiHelpers.ts           # the `searchUi` aggregator class
    pageobjects/
      BasePage.ts            # moved from pages/, reorganized into sections
      SearchPage.ts          # moved from pages/, search() consolidated, getResultCount hardened
  tests/                     # same location; imports updated to use the fixture
                             # auth.setup.ts deleted (no login needed)
  ...config files mostly unchanged (config.js, postinstall.js);
     playwright.config.ts loses the auth.setup testIgnore + STORAGE_STATE export
```

- The old `pages/` and `helpers/` folders are removed after their contents move into `lib/`.
- `HomePage.ts` is **deleted** (see Decisions).
- `tsconfig.json`: there are no path aliases today, and `include` is
  `["tests/**/*.ts", "*.ts"]` (the old `pages/`/`helpers/` compiled only transitively via
  imports). Add `"lib/**/*.ts"` to `include` so the new layout is compiled explicitly.
- `createTest.js` template and `README.md` examples are updated to the new layout and to
  reference `SearchPage` instead of `HomePage`.

## The Fixture Layer (headline change)

`lib/fixtures.ts` extends Playwright's base `test` to inject a `searchUi` helper — the
direct analog of Umbraco's `umbracoUi`:

```ts
import {test as base} from '@playwright/test';
import {UiHelpers} from './helpers/UiHelpers';

export const test = base.extend<{searchUi: UiHelpers}>({
  searchUi: async ({page, baseURL}, use) => {
    await use(new UiHelpers(page, baseURL!));
  },
});
```

`UiHelpers` exposes the page objects as sub-areas, matching `umbracoUi.content` /
`umbracoUi.media`:

```ts
export class UiHelpers {
  readonly search: SearchPage;

  constructor(page: Page, baseURL: string) {
    this.search = new SearchPage(page, baseURL);
  }
}
```

Specs drop `new SearchPage(...)` entirely:

```ts
import {test} from '../lib';
import {expect} from '@playwright/test';

test.describe('Full-Text Search', () => {
  test.beforeEach(async ({searchUi}) => {
    await searchUi.search.goTo();
  });

  test('can search for books by title', async ({searchUi}) => {
    // Act
    await searchUi.search.search('Ulysses');
    // Assert
    await searchUi.search.doesResultContainForQuery('Ulysses');
    await searchUi.search.doesResultContainBook('Ulysses');
  });
});
```

`lib/index.ts` is the public entry point and re-exports `test`, `ConstantHelper`, and
the page object types so specs import from one place (`../lib`).

## BasePage — Organized, Not Trimmed

All current methods are kept (the "shared library" decision). `BasePage` is reorganized
into clearly-banked sections with section banner comments, in this order:

1. **Navigation** (`navigate`, `getTitle`)
2. **Interactions** (click family, text entry, select, check/uncheck, hover, focus, drag)
3. **Waiting** (all `waitFor*`)
4. **Assertions** (`isVisible`, `containsText`, `hasCount`, …)
5. **Getters** (`getText`, `getValue`, `getAttribute`, …)
6. **Files** (`setInputFiles`, `clearInputFiles`)

No method signatures or bodies change. File moves to `lib/pageobjects/BasePage.ts`.

## Dedup & Hardening

- **`search()` duplication:** `HomePage` and `SearchPage` both defined `search()` with
  different locators. With `HomePage` removed, the single canonical search flow lives in
  `SearchPage`.
- **`getResultCount()` fragility:** currently returns `0` for *both* "No books found"
  *and* an unparseable heading, so a broken selector would silently pass a
  "0 results" assertion. New behaviour:
  - heading matches `Found N book(s)` → `N`
  - heading matches `No books found` → `0`
  - anything else → **throw** a clear error naming the unexpected heading text.

## Decisions

- **HomePage: removed.** The site root `/` is the search page (both `goTo` navigated
  `/`), `HomePage` used role-based locators that do not match the real `#query` UI, and
  no spec referenced it. `createTest.js` template and `README.md` are updated to use
  `SearchPage`.
- **auth.setup.ts: removed.** The search frontend needs no login, so the stub is dead
  weight. Deleting it orphans two things, which are cleaned up with it:
  - `playwright.config.ts`: the `testIgnore: /auth\.setup\.ts/` line (now pointless) and
    the `export const STORAGE_STATE = ...` (only `auth.setup.ts` imported it).
  - `README.md`: the `auth.setup.ts` line in the folder-structure tree.

  `STORAGE_STATE_PATH` in `config.js` / `.env.example` is pre-existing and not orphaned by
  this change (it's an env var, never read), so it is left untouched.

## Verification

- `npm run build` (tsc) passes with the new `lib/` layout and updated imports.
- All four spec files (`FullTextSearch`, `SearchFacets`, `SearchPagination`,
  `SearchSorting`) run green against the test site (URL supplied at run time).
- No behavioural change to tests — only structure, naming, and imports.

## Files Touched (summary)

- **Add:** `lib/index.ts`, `lib/fixtures.ts`, `lib/helpers/UiHelpers.ts`
- **Move:** `helpers/ConstantHelper.ts` → `lib/helpers/`; `pages/BasePage.ts`,
  `pages/SearchPage.ts` → `lib/pageobjects/`
- **Delete:** `pages/HomePage.ts`, `pages/index.ts`, old `pages/` + `helpers/` folders,
  `tests/auth.setup.ts`
- **Edit:** 4 spec files (imports + fixture usage),
  `playwright.config.ts` (drop `auth.setup` testIgnore + `STORAGE_STATE` export),
  `createTest.js` (template → SearchPage + new import path),
  `README.md` (examples + structure tree),
  `tsconfig.json` (add `lib/**/*.ts` to `include`)
