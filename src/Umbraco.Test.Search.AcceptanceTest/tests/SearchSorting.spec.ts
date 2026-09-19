import {test} from '../lib';
import {expect} from '@playwright/test';

test.beforeEach(async ({searchUi}) => {
  await searchUi.search.goTo();
});

test('can sort by title ascending', async ({searchUi}) => {
  // Act
  await searchUi.search.sortBy('title');
  await searchUi.search.setSortDirection('asc');

  // Assert
  const titles = await searchUi.search.getBookTitles();
  expect(titles.length).toBeGreaterThan(0);
  const sortedTitles = [...titles].sort((a, b) => a.localeCompare(b));
  expect(titles).toEqual(sortedTitles);
});

test('can sort by title descending', async ({searchUi}) => {
  // Act
  await searchUi.search.sortBy('title');
  await searchUi.search.setSortDirection('desc');

  // Assert
  const titles = await searchUi.search.getBookTitles();
  expect(titles.length).toBeGreaterThan(0);
  const sortedTitles = [...titles].sort((a, b) => b.localeCompare(a));
  expect(titles).toEqual(sortedTitles);
});

test('can sort by publish year', async ({searchUi}) => {
  // Act
  await searchUi.search.sortBy('publishYear');

  // Assert
  await searchUi.search.doesResultCountGreaterThan(0);
});

test('can sort by relevance after search', async ({searchUi}) => {
  // Act
  await searchUi.search.search('novel');
  await searchUi.search.sortBy('relevance');

  // Assert
  await searchUi.search.doesResultCountGreaterThan(0);
});
