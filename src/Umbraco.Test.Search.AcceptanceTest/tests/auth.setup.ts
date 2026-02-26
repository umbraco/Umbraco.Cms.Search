import {test as setup} from '@playwright/test';
import {STORAGE_STATE} from '../playwright.config';

setup('authenticate', async ({page, baseURL}) => {
  // // Navigate to backoffice
  // await page.goto(`${baseURL}/umbraco`);

  // // Wait for login form and enter credentials
  // await page.getByLabel('Email').fill(process.env.UMBRACO_USER_LOGIN || '');
  // await page.getByLabel('Password').fill(process.env.UMBRACO_USER_PASSWORD || '');
  // await page.getByRole('button', {name: 'Login'}).click();

  // // Wait for backoffice to load (wait for the sidebar to be visible)
  // await page.waitForSelector('[data-element="editor-container"]', {timeout: 30000});

  // // Save authentication state
  // await page.context().storageState({path: STORAGE_STATE});
});
