import {test} from '../lib';
import {expect} from '@playwright/test';

test.beforeEach(async ({searchUi}) => {
  await searchUi.search.goTo();
});

test('can load search page with initial results', async ({searchUi}) => {
  // Assert
  await searchUi.search.doesResultCountGreaterThan(0);
});

test('can search for books by title', async ({searchUi}) => {
  // Act
  await searchUi.search.search('Ulysses');

  // Assert
  await searchUi.search.doesResultContainForQuery('Ulysses');
  await searchUi.search.doesResultContainBook('Ulysses');
});

test('can search for books by author name', async ({searchUi}) => {
  // Act
  await searchUi.search.search('Joyce');

  // Assert
  await searchUi.search.doesResultContainForQuery('Joyce');
  await searchUi.search.doesResultCountGreaterThan(0);
});

test('shows no results for non-matching query', async ({searchUi}) => {
  // Act
  await searchUi.search.search('xyznonexistentbook123');

  // Assert
  await searchUi.search.doesResultHeadingContainNoResult();
});

test('can clear search and return to all results', async ({searchUi}) => {
  // Arrange
  const initialCount = await searchUi.search.getResultCount();

  // Act
  await searchUi.search.search('Ulysses');
  await searchUi.search.clearSearch();

  // Assert
  const clearedCount = await searchUi.search.getResultCount();
  expect(clearedCount).toBe(initialCount);
});

test('search is case insensitive', async ({searchUi}) => {
  // Act
  await searchUi.search.search('ulysses');
  const lowercaseCount = await searchUi.search.getResultCount();
  await searchUi.search.search('ULYSSES');
  const uppercaseCount = await searchUi.search.getResultCount();

  // Assert
  expect(lowercaseCount).toBe(uppercaseCount);
  expect(lowercaseCount).toBeGreaterThan(0);
});

test('can search with partial words', async ({searchUi}) => {
  // Act
  await searchUi.search.search('travel');

  // Assert
  await searchUi.search.doesResultCountGreaterThan(0);
});