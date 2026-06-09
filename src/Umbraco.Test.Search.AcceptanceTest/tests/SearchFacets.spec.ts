import {test, expect} from '@playwright/test';
import {SearchPage} from '../pages';

test.describe('Search Facets', () => {
  let searchPage: SearchPage;

  test.beforeEach(async ({page, baseURL}) => {
    searchPage = new SearchPage(page, baseURL!);
    await searchPage.goTo();
  });

  test('can filter by book length', async () => {
    // Arrange
    const initialCount = await searchPage.getResultCount();

    // Act
    await searchPage.selectLengthFacet('Long');

    // Assert
    const filteredCount = await searchPage.getResultCount();
    expect(filteredCount).toBeLessThanOrEqual(initialCount);
    expect(filteredCount).toBeGreaterThan(0);
  });

  test('can filter by century', async () => {
    // Act
    await searchPage.selectCenturyFacet(1900, 2000);

    // Assert
    await searchPage.doesResultCountGreaterThan(0);
  });

  test('can combine search with facet filter', async () => {
    // Act
    await searchPage.search('novel');
    await searchPage.selectLengthFacet('Long');

    // Assert
    await searchPage.doesResultContainForQuery('novel');
    await searchPage.doesResultCountGreaterThan(0);
  });
});
