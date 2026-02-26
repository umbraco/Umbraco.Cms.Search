import {Page, Locator, expect} from '@playwright/test';
import {BasePage} from './BasePage';

export class SearchPage extends BasePage {
  // Locators
  readonly searchInput: Locator;
  readonly searchResults: Locator;
  readonly searchFilters: Locator;
  readonly resultsHeading: Locator;
  readonly bookResults: Locator;
  readonly paginationButtons: Locator;
  readonly sortByDropdown: Locator;
  readonly sortDirectionDropdown: Locator;

  constructor(page: Page, baseURL: string) {
    super(page, baseURL);

    // Initialize locators
    this.searchInput = page.locator('#query');
    this.searchResults = page.locator('#searchResults');
    this.searchFilters = page.locator('#searchFilters');
    this.resultsHeading = page.locator('#searchResults h2');
    this.bookResults = page.locator('.book-result');
    this.paginationButtons = page.locator('.pagination-button');
    this.sortByDropdown = page.locator('#sortBy');
    this.sortDirectionDropdown = page.locator('#sortDirection');
  }

  async goto() {
    await this.navigate('/');
    await this.waitForSearchResults();
  }

  async waitForSearchResults() {
    await this.searchResults.waitFor({state: 'visible'});
    await this.page.waitForSelector('#searchResults h2');
  }

  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
    await this.waitForSearchResults();
  }

  async clearSearch() {
    await this.searchInput.clear();
    await this.searchInput.press('Enter');
    await this.waitForSearchResults();
  }

  async getResultCount(): Promise<number> {
    const headingText = await this.resultsHeading.textContent();
    if (!headingText) return 0;

    // Match "Found X book(s)" or "No books found"
    const match = headingText.match(/Found (\d+) books?/);
    if (match) {
      return parseInt(match[1], 10);
    }

    if (headingText.includes('No books found')) {
      return 0;
    }

    return 0;
  }

  async getBookTitles(): Promise<string[]> {
    const titles = await this.bookResults.locator('h3').allTextContents();
    return titles;
  }

  async expectResultCount(count: number) {
    const actualCount = await this.getResultCount();
    expect(actualCount).toBe(count);
  }

  async expectResultCountGreaterThan(count: number) {
    const actualCount = await this.getResultCount();
    expect(actualCount).toBeGreaterThan(count);
  }

  async expectResultCountToBe(count: number) {
    await expect(this.resultsHeading).toContainText(`Found ${count} book`);
  }

  async expectNoResults() {
    await expect(this.resultsHeading).toContainText('No books found');
  }

  async expectResultsContainBook(bookTitle: string) {
    const titles = await this.getBookTitles();
    expect(titles.some(title => title.includes(bookTitle))).toBeTruthy();
  }

  async expectResultsForQuery(query: string) {
    await expect(this.resultsHeading).toContainText(`for "${query}"`);
  }

  // Facet interactions
  async toggleFacet(facetName: string, value: string) {
    const checkbox = this.page.locator(`input[name="${facetName}"][value="${value}"]`);
    await checkbox.click();
    await this.waitForSearchResults();
  }

  async selectLengthFacet(length: string) {
    await this.toggleFacet('length', length);
  }

  async selectNationalityFacet(nationality: string) {
    await this.toggleFacet('authorNationality', nationality);
  }

  async selectCenturyFacet(minYear: number, maxYear: number) {
    await this.toggleFacet('publishYear', `${minYear},${maxYear}`);
  }

  // Sorting
  async sortBy(sortOption: 'relevance' | 'title' | 'publishYear') {
    await this.sortByDropdown.selectOption(sortOption);
    await this.waitForSearchResults();
  }

  async setSortDirection(direction: 'asc' | 'desc') {
    await this.sortDirectionDropdown.selectOption(direction);
    await this.waitForSearchResults();
  }

  // Pagination
  async goToPage(pageNumber: number) {
    const pageButton = this.paginationButtons.nth(pageNumber - 1);
    await pageButton.click();
    await this.waitForSearchResults();
  }

  async expectPaginationVisible() {
    await expect(this.paginationButtons.first()).toBeVisible();
  }

  async expectNoPagination() {
    await expect(this.paginationButtons).toHaveCount(0);
  }
}
