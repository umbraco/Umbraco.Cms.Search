import {Page} from '@playwright/test';
import {SearchPageUiHelper} from './SearchPageUiHelper';

export class UiHelpers {
  readonly search: SearchPageUiHelper;

  constructor(page: Page, baseURL: string) {
    this.search = new SearchPageUiHelper(page, baseURL);
  }
}
