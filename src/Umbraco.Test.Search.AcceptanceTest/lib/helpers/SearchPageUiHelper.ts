import {Page, Locator, expect} from '@playwright/test';
import {BasePage} from './BasePage';

export class SearchPageUiHelper extends BasePage {
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
    this.searchInput = page.locator('#query');
    this.searchResults = page.locator('#searchResults');
    this.searchFilters = page.locator('#searchFilters');
    this.resultsHeading = page.locator('#searchResults h2');
    this.bookResults = page.locator('.book-result');
    this.paginationButtons = page.locator('.pagination-button');
    this.sortByDropdown = page.locator('#sortBy');
    this.sortDirectionDropdown = page.locator('#sortDirection');
  }

  async goTo() {
    await this.navigate('/');
    await this.waitForSearchResults();
  }

  async waitForSearchResults() {
    await this.waitForVisible(this.searchResults);
    await this.waitForAttached(this.resultsHeading);
  }

  async search(query: string) {
    await this.enterText(this.searchInput, query);
    await this.pressKey(this.searchInput, 'Enter');
    await this.waitForSearchResults();
  }

  async clearSearch() {
    await this.clearText(this.searchInput);
    await this.pressKey(this.searchInput, 'Enter');
    await this.waitForSearchResults();
  }

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

  async getBookTitles(): Promise<string[]> {
    const titles = await this.bookResults.locator('h3').allTextContents();
    return titles;
  }

  async doesResultHaveCount(count: number) {
    const actualCount = await this.getResultCount();
    expect(actualCount).toBe(count);
  }

  async doesResultCountGreaterThan(count: number) {
    const actualCount = await this.getResultCount();
    expect(actualCount).toBeGreaterThan(count);
  }

  async doesResultHeadingContainCount(count: number) {
    await this.containsText(this.resultsHeading, `Found ${count} book`);
  }

  async doesResultHeadingContainNoResult() {
    await this.containsText(this.resultsHeading, 'No books found');
  }

  async doesResultContainBook(bookTitle: string) {
    const titles = await this.getBookTitles();
    expect(titles.some(title => title.includes(bookTitle))).toBeTruthy();
  }

  async doesResultContainForQuery(query: string) {
    await this.containsText(this.resultsHeading, `for "${query}"`);
  }

  // Facet interactions
  async toggleFacet(facetName: string, value: string) {
    const checkbox = this.page.locator(`input[name="${facetName}"][value="${value}"]`);
    await this.click(checkbox);
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
    await this.selectByValue(this.sortByDropdown, sortOption);
    await this.waitForSearchResults();
  }

  async setSortDirection(direction: 'asc' | 'desc') {
    await this.selectByValue(this.sortDirectionDropdown, direction);
    await this.waitForSearchResults();
  }

  // Pagination
  async goToPage(pageNumber: number) {
    const pageButton = this.paginationButtons.nth(pageNumber - 1);
    await this.click(pageButton);
    await this.waitForSearchResults();
  }

  async expectPaginationVisible() {
    await this.isVisible(this.paginationButtons.first());
  }

  async expectNoPagination() {
    await this.hasCount(this.paginationButtons, 0);
  }
}
