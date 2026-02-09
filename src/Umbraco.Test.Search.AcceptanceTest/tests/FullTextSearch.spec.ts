import {test, expect} from '@playwright/test';
import {SearchPage} from '../pages';

test.describe('Full-Text Search', () => {
  let searchPage: SearchPage;

  test.beforeEach(async ({page, baseURL}) => {
    searchPage = new SearchPage(page, baseURL!);
    await searchPage.goto();
  });

  test('can load search page with initial results', {tag: '@smoke'}, async () => {
    await searchPage.expectResultCountGreaterThan(0);
  });

  test('can search for books by title', {tag: '@smoke'}, async () => {
    await searchPage.search('Ulysses');

    await searchPage.expectResultsForQuery('Ulysses');
    await searchPage.expectResultsContainBook('Ulysses');
  });

  test('can search for books by author name', async () => {
    await searchPage.search('Joyce');

    await searchPage.expectResultsForQuery('Joyce');
    await searchPage.expectResultCountGreaterThan(0);
  });

  test('shows no results for non-matching query', async () => {
    await searchPage.search('xyznonexistentbook123');

    await searchPage.expectNoResults();
  });

  test('can clear search and return to all results', async () => {
    const initialCount = await searchPage.getResultCount();

    await searchPage.search('Ulysses');
    await searchPage.clearSearch();

    const clearedCount = await searchPage.getResultCount();
    expect(clearedCount).toBe(initialCount);
  });

  test('search is case insensitive', async () => {
    await searchPage.search('ulysses');
    const lowercaseCount = await searchPage.getResultCount();

    await searchPage.search('ULYSSES');
    const uppercaseCount = await searchPage.getResultCount();

    expect(lowercaseCount).toBe(uppercaseCount);
    expect(lowercaseCount).toBeGreaterThan(0);
  });

  test('can search with partial words', async () => {
    await searchPage.search('travel');

    await searchPage.expectResultCountGreaterThan(0);
  });

});
