import {test, expect} from '@playwright/test';
import {SearchPage} from '../pages';

test.describe('Search Facets', () => {
  let searchPage: SearchPage;

  test.beforeEach(async ({page, baseURL}) => {
    searchPage = new SearchPage(page, baseURL!);
    await searchPage.goto();
  });

  test('can filter by book length', async () => {
    const initialCount = await searchPage.getResultCount();

    await searchPage.selectLengthFacet('Long');

    const filteredCount = await searchPage.getResultCount();
    expect(filteredCount).toBeLessThanOrEqual(initialCount);
    expect(filteredCount).toBeGreaterThan(0);
  });

  test('can filter by century', async () => {
    await searchPage.selectCenturyFacet(1900, 2000);

    await searchPage.expectResultCountGreaterThan(0);
  });

  test('can combine search with facet filter', async () => {
    await searchPage.search('novel');
    await searchPage.selectLengthFacet('Long');

    await searchPage.expectResultsForQuery('novel');
    await searchPage.expectResultCountGreaterThan(0);
  });

});
