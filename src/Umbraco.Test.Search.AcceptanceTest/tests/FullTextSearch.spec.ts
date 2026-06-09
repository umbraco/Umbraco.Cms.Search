import {test, expect} from '@playwright/test';
import {SearchPage} from '../pages';

test.describe('Full-Text Search', () => {
  let searchPage: SearchPage;

  test.beforeEach(async ({page, baseURL}) => {
    searchPage = new SearchPage(page, baseURL!);
    await searchPage.goTo();
  });

  test('can load search page with initial results', async () => {
    // Assert
    await searchPage.doesResultCountGreaterThan(0);
  });

  test('can search for books by title', async () => {
    // Act
    await searchPage.search('Ulysses');

    // Assert
    await searchPage.doesResultContainForQuery('Ulysses');
    await searchPage.doesResultContainBook('Ulysses');
  });

  test('can search for books by author name', async () => {
    // Act
    await searchPage.search('Joyce');

    // Assert
    await searchPage.doesResultContainForQuery('Joyce');
    await searchPage.doesResultCountGreaterThan(0);
  });

  test('shows no results for non-matching query', async () => {
    // Act
    await searchPage.search('xyznonexistentbook123');

    // Assert
    await searchPage.doesResultHeadingContainNoResult();
  });

  test('can clear search and return to all results', async () => {
    // Arrange
    const initialCount = await searchPage.getResultCount();

    // Act
    await searchPage.search('Ulysses');
    await searchPage.clearSearch();

    // Assert
    const clearedCount = await searchPage.getResultCount();
    expect(clearedCount).toBe(initialCount);
  });

  test('search is case insensitive', async () => {
    // Act
    await searchPage.search('ulysses');
    const lowercaseCount = await searchPage.getResultCount();
    await searchPage.search('ULYSSES');
    const uppercaseCount = await searchPage.getResultCount();

    // Assert
    expect(lowercaseCount).toBe(uppercaseCount);
    expect(lowercaseCount).toBeGreaterThan(0);
  });

  test('can search with partial words', async () => {
    // Act
    await searchPage.search('travel');

    // Assert
    await searchPage.doesResultCountGreaterThan(0);
  });
});
