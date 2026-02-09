import {test, expect} from '@playwright/test';
import {SearchPage} from '../pages';

test.describe('Search Sorting', () => {
  let searchPage: SearchPage;

  test.beforeEach(async ({page, baseURL}) => {
    searchPage = new SearchPage(page, baseURL!);
    await searchPage.goto();
  });

  test('can sort by title ascending', async () => {
    await searchPage.sortBy('title');
    await searchPage.setSortDirection('asc');

    const titles = await searchPage.getBookTitles();
    expect(titles.length).toBeGreaterThan(0);

    const sortedTitles = [...titles].sort((a, b) => a.localeCompare(b));
    expect(titles).toEqual(sortedTitles);
  });

  test('can sort by title descending', async () => {
    await searchPage.sortBy('title');
    await searchPage.setSortDirection('desc');

    const titles = await searchPage.getBookTitles();
    expect(titles.length).toBeGreaterThan(0);

    const sortedTitles = [...titles].sort((a, b) => b.localeCompare(a));
    expect(titles).toEqual(sortedTitles);
  });

  test('can sort by publish year', async () => {
    await searchPage.sortBy('publishYear');

    await searchPage.expectResultCountGreaterThan(0);
  });

  test('can sort by relevance after search', async () => {
    await searchPage.search('novel');
    await searchPage.sortBy('relevance');

    await searchPage.expectResultCountGreaterThan(0);
  });

});
