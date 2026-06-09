import {test, expect} from '@playwright/test';
import {SearchPage} from '../pages';

test.describe('Search Sorting', () => {
  let searchPage: SearchPage;

  test.beforeEach(async ({page, baseURL}) => {
    searchPage = new SearchPage(page, baseURL!);
    await searchPage.goTo();
  });

  test('can sort by title ascending', async () => {
    // Act
    await searchPage.sortBy('title');
    await searchPage.setSortDirection('asc');

    // Assert
    const titles = await searchPage.getBookTitles();
    expect(titles.length).toBeGreaterThan(0);
    const sortedTitles = [...titles].sort((a, b) => a.localeCompare(b));
    expect(titles).toEqual(sortedTitles);
  });

  test('can sort by title descending', async () => {
    // Act
    await searchPage.sortBy('title');
    await searchPage.setSortDirection('desc');

    // Assert
    const titles = await searchPage.getBookTitles();
    expect(titles.length).toBeGreaterThan(0);
    const sortedTitles = [...titles].sort((a, b) => b.localeCompare(a));
    expect(titles).toEqual(sortedTitles);
  });

  test('can sort by publish year', async () => {
    // Act
    await searchPage.sortBy('publishYear');

    // Assert
    await searchPage.doesResultCountGreaterThan(0);
  });

  test('can sort by relevance after search', async () => {
    // Act
    await searchPage.search('novel');
    await searchPage.sortBy('relevance');

    // Assert
    await searchPage.doesResultCountGreaterThan(0);
  });
});
