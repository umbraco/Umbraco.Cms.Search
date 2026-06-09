import {test as base} from '@playwright/test';
import {UiHelpers} from './helpers/UiHelpers';

export const test = base.extend<{searchUi: UiHelpers}>({
  searchUi: async ({page, baseURL}, use) => {
    await use(new UiHelpers(page, baseURL!));
  },
});
