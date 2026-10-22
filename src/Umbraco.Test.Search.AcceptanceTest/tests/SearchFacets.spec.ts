import {test} from '../lib';
import {expect} from '@playwright/test';

test.beforeEach(async ({searchUi}) => {
  await searchUi.search.goTo();
});

test('can filter by book length', async ({searchUi}) => {
  // Arrange
  const initialCount = await searchUi.search.getResultCount();

  // Act
  await searchUi.search.selectLengthFacet('Long');

  // Assert
  const filteredCount = await searchUi.search.getResultCount();
  expect(filteredCount).toBeLessThanOrEqual(initialCount);
  expect(filteredCount).toBeGreaterThan(0);
});

test('can filter by century', async ({searchUi}) => {
  // Act
  await searchUi.search.selectCenturyFacet(1900, 2000);

  // Assert
  await searchUi.search.doesResultCountGreaterThan(0);
});

test('can combine search with facet filter', async ({searchUi}) => {
  // Act
  await searchUi.search.search('novel');
  await searchUi.search.selectLengthFacet('Long');

  // Assert
  await searchUi.search.doesResultContainForQuery('novel');
  await searchUi.search.doesResultCountGreaterThan(0);
});