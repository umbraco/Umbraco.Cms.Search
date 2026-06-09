# Acceptance Test Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure the Playwright acceptance test suite to mirror Umbraco-CMS conventions (a `lib/` layout with a `searchUi` test fixture), with no change to test behaviour.

**Architecture:** Page objects, helpers, and an extended Playwright `test` move under `lib/`. A `searchUi` fixture (the `umbracoUi` analog) is injected into specs so they stop calling `new SearchPage(...)`. `BasePage` is kept whole and reorganized; `HomePage` and `auth.setup.ts` are removed; `getResultCount()` is hardened.

**Tech Stack:** TypeScript 4.8, Playwright 1.56, Node.js. No build script — type-check with `npx tsc --noEmit`; run specs with `npm run test` against the live test site.

**Working directory for all commands:** `src/Umbraco.Test.Search.AcceptanceTest`

---

### Task 1: Migrate to `lib/` structure + `searchUi` fixture

This is one atomic structural change — the project does not compile in intermediate states, so all moves and import rewrites land in a single commit.

**Files:**
- Move: `helpers/ConstantHelper.ts` → `lib/helpers/ConstantHelper.ts`
- Move: `pages/BasePage.ts` → `lib/pageobjects/BasePage.ts`
- Move: `pages/SearchPage.ts` → `lib/pageobjects/SearchPage.ts`
- Create: `lib/helpers/UiHelpers.ts`
- Create: `lib/fixtures.ts`
- Create: `lib/index.ts`
- Delete: `pages/HomePage.ts`, `pages/index.ts`
- Modify: `tests/FullTextSearch.spec.ts`, `tests/SearchFacets.spec.ts`, `tests/SearchPagination.spec.ts`, `tests/SearchSorting.spec.ts`
- Modify: `tsconfig.json`

- [ ] **Step 1: Move the three existing files with `git mv`**

```bash
cd src/Umbraco.Test.Search.AcceptanceTest
mkdir -p lib/helpers lib/pageobjects
git mv helpers/ConstantHelper.ts lib/helpers/ConstantHelper.ts
git mv pages/BasePage.ts lib/pageobjects/BasePage.ts
git mv pages/SearchPage.ts lib/pageobjects/SearchPage.ts
```

No content edits needed: `BasePage.ts` imports `'../helpers/ConstantHelper'` (still correct from `lib/pageobjects/`), and `SearchPage.ts` imports `'./BasePage'` (unchanged).

- [ ] **Step 2: Delete the obsolete `pages/` files**

```bash
git rm pages/HomePage.ts pages/index.ts
```

- [ ] **Step 3: Create `lib/helpers/UiHelpers.ts`**

```ts
import {Page} from '@playwright/test';
import {SearchPage} from '../pageobjects/SearchPage';

export class UiHelpers {
  readonly search: SearchPage;

  constructor(page: Page, baseURL: string) {
    this.search = new SearchPage(page, baseURL);
  }
}
```

- [ ] **Step 4: Create `lib/fixtures.ts`**

```ts
import {test as base} from '@playwright/test';
import {UiHelpers} from './helpers/UiHelpers';

export const test = base.extend<{searchUi: UiHelpers}>({
  searchUi: async ({page, baseURL}, use) => {
    await use(new UiHelpers(page, baseURL!));
  },
});
```

- [ ] **Step 5: Create `lib/index.ts`**

```ts
export {test} from './fixtures';
export {UiHelpers} from './helpers/UiHelpers';
export {ConstantHelper} from './helpers/ConstantHelper';
export {BasePage} from './pageobjects/BasePage';
export {SearchPage} from './pageobjects/SearchPage';
```

- [ ] **Step 6: Rewrite `tests/FullTextSearch.spec.ts` (full content)**

```ts
import {test} from '../lib';
import {expect} from '@playwright/test';

test.describe('Full-Text Search', () => {
  test.beforeEach(async ({searchUi}) => {
    await searchUi.search.goTo();
  });

  test('can load search page with initial results', async ({searchUi}) => {
    // Assert
    await searchUi.search.doesResultCountGreaterThan(0);
  });

  test('can search for books by title', async ({searchUi}) => {
    // Act
    await searchUi.search.search('Ulysses');

    // Assert
    await searchUi.search.doesResultContainForQuery('Ulysses');
    await searchUi.search.doesResultContainBook('Ulysses');
  });

  test('can search for books by author name', async ({searchUi}) => {
    // Act
    await searchUi.search.search('Joyce');

    // Assert
    await searchUi.search.doesResultContainForQuery('Joyce');
    await searchUi.search.doesResultCountGreaterThan(0);
  });

  test('shows no results for non-matching query', async ({searchUi}) => {
    // Act
    await searchUi.search.search('xyznonexistentbook123');

    // Assert
    await searchUi.search.doesResultHeadingContainNoResult();
  });

  test('can clear search and return to all results', async ({searchUi}) => {
    // Arrange
    const initialCount = await searchUi.search.getResultCount();

    // Act
    await searchUi.search.search('Ulysses');
    await searchUi.search.clearSearch();

    // Assert
    const clearedCount = await searchUi.search.getResultCount();
    expect(clearedCount).toBe(initialCount);
  });

  test('search is case insensitive', async ({searchUi}) => {
    // Act
    await searchUi.search.search('ulysses');
    const lowercaseCount = await searchUi.search.getResultCount();
    await searchUi.search.search('ULYSSES');
    const uppercaseCount = await searchUi.search.getResultCount();

    // Assert
    expect(lowercaseCount).toBe(uppercaseCount);
    expect(lowercaseCount).toBeGreaterThan(0);
  });

  test('can search with partial words', async ({searchUi}) => {
    // Act
    await searchUi.search.search('travel');

    // Assert
    await searchUi.search.doesResultCountGreaterThan(0);
  });
});
```

- [ ] **Step 7: Rewrite `tests/SearchFacets.spec.ts` (full content)**

```ts
import {test} from '../lib';
import {expect} from '@playwright/test';

test.describe('Search Facets', () => {
  test.beforeEach(async ({searchUi}) => {
    await searchUi.search.goTo();
  });

  test('can filter by book length', async ({searchUi}) => {
    // Arrange
    const initialCount = await searchUi.search.getResultCount();

    // Act
    await searchUi.search.selectLengthFacet('Long');

    // Assert
    const filteredCount = await searchUi.search.getResultCount();
    expect(filteredCount).toBeLessThanOrEqual(initialCount);
    expect(filteredCount).toBeGreaterThan(0);
  });

  test('can filter by century', async ({searchUi}) => {
    // Act
    await searchUi.search.selectCenturyFacet(1900, 2000);

    // Assert
    await searchUi.search.doesResultCountGreaterThan(0);
  });

  test('can combine search with facet filter', async ({searchUi}) => {
    // Act
    await searchUi.search.search('novel');
    await searchUi.search.selectLengthFacet('Long');

    // Assert
    await searchUi.search.doesResultContainForQuery('novel');
    await searchUi.search.doesResultCountGreaterThan(0);
  });
});
```

- [ ] **Step 8: Rewrite `tests/SearchPagination.spec.ts` (full content)**

```ts
import {test} from '../lib';
import {expect} from '@playwright/test';

test.describe('Search Pagination', () => {
  let resultCount: number;

  test.beforeEach(async ({searchUi}) => {
    await searchUi.search.goTo();
    resultCount = await searchUi.search.getResultCount();
  });

  test('displays pagination when results exceed page size', async ({searchUi}) => {
    // Assert
    if (resultCount > 5) {
      await searchUi.search.expectPaginationVisible();
    }
  });

  test('can navigate to next page', async ({searchUi}) => {
    // Arrange
    if (resultCount <= 5) {
      test.skip();
      return;
    }

    // Act
    const firstPageTitles = await searchUi.search.getBookTitles();
    await searchUi.search.goToPage(2);

    // Assert
    const secondPageTitles = await searchUi.search.getBookTitles();
    expect(secondPageTitles).not.toEqual(firstPageTitles);
  });
});
```

- [ ] **Step 9: Rewrite `tests/SearchSorting.spec.ts` (full content)**

```ts
import {test} from '../lib';
import {expect} from '@playwright/test';

test.describe('Search Sorting', () => {
  test.beforeEach(async ({searchUi}) => {
    await searchUi.search.goTo();
  });

  test('can sort by title ascending', async ({searchUi}) => {
    // Act
    await searchUi.search.sortBy('title');
    await searchUi.search.setSortDirection('asc');

    // Assert
    const titles = await searchUi.search.getBookTitles();
    expect(titles.length).toBeGreaterThan(0);
    const sortedTitles = [...titles].sort((a, b) => a.localeCompare(b));
    expect(titles).toEqual(sortedTitles);
  });

  test('can sort by title descending', async ({searchUi}) => {
    // Act
    await searchUi.search.sortBy('title');
    await searchUi.search.setSortDirection('desc');

    // Assert
    const titles = await searchUi.search.getBookTitles();
    expect(titles.length).toBeGreaterThan(0);
    const sortedTitles = [...titles].sort((a, b) => b.localeCompare(a));
    expect(titles).toEqual(sortedTitles);
  });

  test('can sort by publish year', async ({searchUi}) => {
    // Act
    await searchUi.search.sortBy('publishYear');

    // Assert
    await searchUi.search.doesResultCountGreaterThan(0);
  });

  test('can sort by relevance after search', async ({searchUi}) => {
    // Act
    await searchUi.search.search('novel');
    await searchUi.search.sortBy('relevance');

    // Assert
    await searchUi.search.doesResultCountGreaterThan(0);
  });
});
```

- [ ] **Step 10: Update `tsconfig.json` `include`**

Replace the `include` line (line 17):

```json
  "include": ["tests/**/*.ts", "*.ts"],
```

with:

```json
  "include": ["tests/**/*.ts", "lib/**/*.ts", "*.ts"],
```

- [ ] **Step 11: Type-check**

Run: `npx tsc --noEmit -p tsconfig.json`
Expected: no output, exit code 0 (the old `pages/` and `helpers/` folders are now empty — remove them if `git` left them: `rmdir pages helpers` on Windows / they vanish automatically once empty).

- [ ] **Step 12: Commit**

```bash
git add -A
git commit -m "Restructure acceptance tests into lib/ with searchUi fixture"
```

---

### Task 2: Reorganize `BasePage` into documented sections

Pure reordering — **no method signature or body changes**. Group the existing methods under banner comments in the order below.

**Files:**
- Modify: `lib/pageobjects/BasePage.ts`

- [ ] **Step 1: Add section banners and group methods**

Keep the class shell, constructor, and all method bodies exactly as-is. Reorder the methods into these six groups, each preceded by a banner comment in this exact format:

```ts
  // ─── Navigation ───────────────────────────────────────────────
```

Grouping (every existing method goes in exactly one group; none added, none removed):

- **Navigation:** `navigate`, `getTitle`
- **Interactions:** `click`, `doubleClick`, `rightClick`, `javascriptClick`, `enterText`, `typeText`, `clearText`, `pressKey`, `selectByValue`, `selectByText`, `selectByIndex`, `selectMultiple`, `check`, `uncheck`, `setChecked`, `hover`, `focus`, `hoverAndClick`, `scrollIntoView`, `dragTo`
- **Waiting:** `waitForVisible`, `waitForHidden`, `waitForAttached`, `waitForDetached`, `waitForPageLoad`, `waitForDOMContentLoaded`, `waitForLoadState`, `waitForEnabled`, `waitForDisabled`, `waitForText`, `waitForValue`, `waitForAttribute`, `waitForClass`, `waitForEditable`, `waitForChecked`, `waitForUnchecked`, `waitForURL`, `waitForNavigation`, `waitForTimeout`, `waitForRequest`, `waitForResponse`, `waitForFocused`, `waitForEmpty`, `waitForFunction`, `waitForCSS`
- **Assertions:** `isVisible`, `isEnabled`, `isDisabled`, `containsText`, `hasText`, `hasValue`, `hasAttribute`, `hasCount`
- **Getters:** `getText`, `getValue`, `getAttribute`, `checkIsVisible`, `isChecked`
- **Files:** `setInputFiles`, `clearInputFiles`

- [ ] **Step 2: Type-check**

Run: `npx tsc --noEmit -p tsconfig.json`
Expected: no output, exit code 0.

- [ ] **Step 3: Commit**

```bash
git add lib/pageobjects/BasePage.ts
git commit -m "Organize BasePage methods into sections"
```

---

### Task 3: Harden `getResultCount()`

**Files:**
- Modify: `lib/pageobjects/SearchPage.ts`

- [ ] **Step 1: Replace the `getResultCount` method**

Find this method:

```ts
  async getResultCount(): Promise<number> {
    const headingText = await this.getText(this.resultsHeading);
    if (!headingText) return 0;

    const match = headingText.match(/Found (\d+) books?/);
    if (match) {
      return parseInt(match[1], 10);
    }

    if (headingText.includes('No books found')) {
      return 0;
    }

    return 0;
  }
```

Replace it with:

```ts
  async getResultCount(): Promise<number> {
    const headingText = await this.getText(this.resultsHeading);

    const match = headingText.match(/Found (\d+) books?/);
    if (match) {
      return parseInt(match[1], 10);
    }

    if (headingText.includes('No books found')) {
      return 0;
    }

    throw new Error(`Unexpected results heading: "${headingText}"`);
  }
```

- [ ] **Step 2: Type-check**

Run: `npx tsc --noEmit -p tsconfig.json`
Expected: no output, exit code 0.

- [ ] **Step 3: Commit**

```bash
git add lib/pageobjects/SearchPage.ts
git commit -m "Throw on unexpected results heading in getResultCount"
```

---

### Task 4: Remove `auth.setup.ts` and clean `playwright.config.ts`

**Files:**
- Delete: `tests/auth.setup.ts`
- Modify: `playwright.config.ts`

- [ ] **Step 1: Delete the auth setup file**

```bash
git rm tests/auth.setup.ts
```

- [ ] **Step 2: Remove the `STORAGE_STATE` export from `playwright.config.ts`**

Find:

```ts
require('dotenv').config();

export const STORAGE_STATE = 'playwright/.auth/user.json';

export default defineConfig({
```

Replace with:

```ts
require('dotenv').config();

export default defineConfig({
```

- [ ] **Step 3: Remove the `testIgnore` line from `playwright.config.ts`**

Find:

```ts
  testDir: './tests/',
  testIgnore: /auth\.setup\.ts/,
  timeout: 30 * 1000,
```

Replace with:

```ts
  testDir: './tests/',
  timeout: 30 * 1000,
```

- [ ] **Step 4: Type-check**

Run: `npx tsc --noEmit -p tsconfig.json`
Expected: no output, exit code 0.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Remove unused auth.setup and its config orphans"
```

---

### Task 5: Update `createTest.js` template and `README.md`

**Files:**
- Modify: `createTest.js`
- Modify: `README.md`

- [ ] **Step 1: Replace the template string in `createTest.js`**

Find the `const template = ` block (currently importing `HomePage` and tagging `@smoke`) and replace the entire template literal with:

```js
const template = `import {test} from '../lib';

test.describe('${testName}', () => {

  test.beforeEach(async ({searchUi}) => {
    await searchUi.search.goTo();
  });

  test('can do something', async ({searchUi}) => {
    // Arrange

    // Act

    // Assert
  });

});
`;
```

- [ ] **Step 2: Update the Project Structure tree in `README.md`**

Replace this block:

```
├── fixtures/               # Test data fixtures
├── pages/                  # Page Object Model classes
│   ├── index.ts            # Barrel export
│   ├── BasePage.ts         # Base page class
│   └── HomePage.ts         # Home page object
├── playwright/
│   └── .auth/user.json     # Saved authentication state
├── tests/
│   ├── auth.setup.ts       # Authentication setup
│   └── *.spec.ts           # Test files
└── results/                # Test results and reports
```

with:

```
├── fixtures/               # Test data fixtures
├── lib/                    # Test helpers, fixtures, and page objects
│   ├── index.ts            # Public API (test fixture, page objects, ConstantHelper)
│   ├── fixtures.ts         # Extended Playwright test with the searchUi fixture
│   ├── helpers/
│   │   ├── ConstantHelper.ts  # Timeout/wait constants
│   │   └── UiHelpers.ts       # searchUi aggregator (page objects)
│   └── pageobjects/
│       ├── BasePage.ts     # Base page class
│       └── SearchPage.ts   # Search page object
├── tests/
│   └── *.spec.ts           # Test files
└── results/                # Test results and reports
```

- [ ] **Step 3: Update the "Creating a New Page Object" section in `README.md`**

Replace:

```
1. Create a new file in `pages/` (e.g., `SearchResultsPage.ts`)
2. Extend `BasePage` and define locators and actions
3. Export from `pages/index.ts`

```typescript
import {Page, Locator, expect} from '@playwright/test';
import {BasePage} from './BasePage';
```

with:

```
1. Create a new file in `lib/pageobjects/` (e.g., `SearchResultsPage.ts`)
2. Extend `BasePage` and define locators and actions
3. Export it from `lib/index.ts` and expose it on `UiHelpers` (`lib/helpers/UiHelpers.ts`)

```typescript
import {Page, Locator, expect} from '@playwright/test';
import {BasePage} from './BasePage';
```

(Leave the example class body that follows unchanged.)

- [ ] **Step 4: Update the "Basic Test Structure" example in `README.md`**

Replace:

```typescript
import {test} from '@playwright/test';
import {HomePage} from '../pages';

test.describe('MyFeature', () => {

  test('can do something', {tag: '@smoke'}, async ({page, baseURL}) => {
    // Arrange
    const homePage = new HomePage(page, baseURL!);

    // Act
    await homePage.goTo();

    // Assert
    await homePage.doesTitleHaveText('Book search');
  });

});
```

with:

```typescript
import {test} from '../lib';
import {expect} from '@playwright/test';

test.describe('MyFeature', () => {

  test.beforeEach(async ({searchUi}) => {
    await searchUi.search.goTo();
  });

  test('can search for a book', async ({searchUi}) => {
    // Act
    await searchUi.search.search('Ulysses');

    // Assert
    await searchUi.search.doesResultContainBook('Ulysses');
  });

});
```

- [ ] **Step 5: Update the "Available Fixtures" section in `README.md`**

Replace:

```
- `page` - Playwright page object for browser interactions
- `baseURL` - Base URL from configuration (https://localhost:44324)
```

with:

```
- `searchUi` - Search UI helper exposing page objects (e.g. `searchUi.search`)
- `page` - Playwright page object for browser interactions
- `baseURL` - Base URL from configuration (https://localhost:44324)
```

> Note: the pre-existing `smokeTest` script, the `Run smoke tests only` README section, and the `tests/SearchSettings.spec.ts` example path are stale but out of scope for this refactor — leave them untouched.

- [ ] **Step 6: Commit**

```bash
git add createTest.js README.md
git commit -m "Update test generator and README for lib/ + searchUi layout"
```

---

### Task 6: Final verification against the live test site

**Files:** none (verification only)

- [ ] **Step 1: Start the test site** (separate terminal, if not already running)

```bash
cd src/Umbraco.Web.TestSite.V17
dotnet run
```

Wait until it serves at `https://localhost:44324`.

- [ ] **Step 2: Run the full acceptance suite**

```bash
cd src/Umbraco.Test.Search.AcceptanceTest
npm run test
```

Expected: all tests in `FullTextSearch`, `SearchFacets`, `SearchPagination`, and `SearchSorting` pass (the "can navigate to next page" test may report skipped if the seeded data has ≤5 books). No `Unexpected results heading` errors.

- [ ] **Step 3: If any spec fails**, do NOT alter assertions to make them pass. Diagnose via the trace (`npx playwright show-trace results/test-results/<folder>/trace.zip`) — failures here indicate a wiring mistake in the refactor (wrong import, fixture not injected, locator lost), which should be fixed in the relevant `lib/` file, not the spec.

---

## Notes for the implementer

- There is **no `build` npm script**; `npx tsc --noEmit -p tsconfig.json` is the type-check gate after every task.
- The `searchUi` fixture is per-test; module-level `let resultCount` in `SearchPagination.spec.ts` is set in `beforeEach` and read in tests — this is intentional and preserved.
- Do not re-add `@smoke`/`@release` tags; they were removed deliberately in an earlier commit.
