import {Page, Locator, expect} from '@playwright/test';
import {BasePage} from './BasePage';

export class HomePage extends BasePage {
  // Locators
  readonly heading: Locator;
  readonly searchInput: Locator;
  readonly searchButton: Locator;

  constructor(page: Page, baseURL: string) {
    super(page, baseURL);

    // Initialize locators
    this.heading = page.locator('h1');
    this.searchInput = page.getByRole('textbox', {name: 'search'});
    this.searchButton = page.getByRole('button', {name: 'Search'});
  }

  async goto() {
    await this.navigate('/');
  }

  async expectTitle(title: string) {
    await expect(this.page).toHaveTitle(title);
  }

  async expectHeadingText(text: string) {
    await expect(this.heading).toHaveText(text);
  }

  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchButton.click();
  }
}
