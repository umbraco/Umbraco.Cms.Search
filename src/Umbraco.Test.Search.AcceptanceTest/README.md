# Umbraco Search Acceptance Tests

This project contains Playwright-based end-to-end acceptance tests for the Umbraco Search package.

## Prerequisites

- Node.js 22+ (or use `.nvmrc` if present)
- Umbraco Search Test Site running at `https://localhost:44324`
- Superadmin user credentials

## Setup

1. Navigate to this directory:
   ```bash
   cd src/Umbraco.Test.Search.AcceptanceTest
   ```

2. Install dependencies:
   ```bash
   npm ci
   ```

3. Install Playwright browsers:
   ```bash
   npx playwright install
   ```

4. Configure environment (if not prompted during install):
   ```bash
   npm run config
   ```

## Running Tests

### Run all tests
```bash
npm run test
```

### Run tests with UI mode (interactive)
```bash
npm run ui
```

### Run smoke tests only
```bash
npm run smokeTest
```

### Run specific test file
```bash
npx playwright test tests/SearchSettings.spec.ts
```

### Run tests with visible browser
```bash
npx playwright test --headed
```

### Debug mode
```bash
PWDEBUG=1 npx playwright test
```

## Creating New Tests

Use the test generator:
```bash
npm run createTest MyFeatureName
```

This creates `tests/MyFeatureName.spec.ts` with a basic template.

## Project Structure

```
Umbraco.Test.Search.AcceptanceTest/
├── playwright.config.ts    # Playwright configuration
├── package.json            # NPM dependencies and scripts
├── config.js               # Environment configuration script
├── postinstall.js          # Post-install hook
├── createTest.js           # Test generator
├── tsconfig.json           # TypeScript configuration
├── .env                    # Environment variables (git-ignored)
├── .env.example            # Example environment file
├── console-errors.json     # Console error tracking
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

## Page Object Model

This project uses the Page Object Model (POM) pattern to organize test code:

- **BasePage** - Base class with common functionality (navigate, getTitle, etc.)
- **Page Objects** - Each page/component has its own class with locators and actions

### Creating a New Page Object

1. Create a new file in `pages/` (e.g., `SearchResultsPage.ts`)
2. Extend `BasePage` and define locators and actions
3. Export from `pages/index.ts`

```typescript
import {Page, Locator, expect} from '@playwright/test';
import {BasePage} from './BasePage';

export class SearchResultsPage extends BasePage {
  readonly resultsList: Locator;
  readonly resultCount: Locator;

  constructor(page: Page, baseURL: string) {
    super(page, baseURL);
    this.resultsList = page.locator('.search-results');
    this.resultCount = page.locator('.result-count');
  }

  async expectResultCount(count: number) {
    await expect(this.resultCount).toHaveText(`${count} results`);
  }
}
```

## Test Patterns

### Basic Test Structure

```typescript
import {test} from '@playwright/test';
import {HomePage} from '../pages';

test.describe('MyFeature', () => {

  test('can do something', {tag: '@smoke'}, async ({page, baseURL}) => {
    // Arrange
    const homePage = new HomePage(page, baseURL!);

    // Act
    await homePage.goto();

    // Assert
    await homePage.expectTitle('Book search');
  });

});
```

### Available Fixtures

- `page` - Playwright page object for browser interactions
- `baseURL` - Base URL from configuration (https://localhost:44324)

## Configuration

The test site URL defaults to `https://localhost:44324`. Configure via:
- `.env` file (UMBRACO_USER_LOGIN, UMBRACO_USER_PASSWORD, URL)
- Run `npm run config` to reconfigure

## Results

- HTML Report: `results/playwright-report/`
- JUnit XML (CI): `results/results.xml`
- Test Results: `results/test-results/`
- Traces: Saved on failure for debugging

View trace files:
```bash
npx playwright show-trace results/test-results/<test-folder>/trace.zip
```
