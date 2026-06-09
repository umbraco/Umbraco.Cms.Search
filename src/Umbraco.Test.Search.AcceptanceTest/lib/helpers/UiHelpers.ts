import {Page} from '@playwright/test';
import {SearchPage} from '../pageobjects/SearchPage';

export class UiHelpers {
  readonly search: SearchPage;

  constructor(page: Page, baseURL: string) {
    this.search = new SearchPage(page, baseURL);
  }
}
