import {defineConfig, devices} from '@playwright/test';

require('dotenv').config();

export const STORAGE_STATE = 'playwright/.auth/user.json';

export default defineConfig({
  testDir: './tests/',
  testIgnore: /auth\.setup\.ts/,
  timeout: 30 * 1000,
  expect: {
    timeout: 5000
  },
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: process.env.CI ? [['junit', {outputFile: 'results/results.xml'}]] : [['html', {outputFolder: 'results/playwright-report', open: 'never'}]],
  use: {
    ...devices['Desktop Chrome'],
    actionTimeout: 0,
    baseURL: process.env.URL ?? 'https://localhost:44324',
    trace: 'retain-on-failure',
    ignoreHTTPSErrors: true,
    testIdAttribute: 'data-mark',
  },
  outputDir: 'results/test-results/',
});
