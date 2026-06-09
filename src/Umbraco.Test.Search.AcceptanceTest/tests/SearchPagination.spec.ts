import {test} from '../lib';
import {expect} from '@playwright/test';

test.beforeEach(async ({searchUi}) => {
  await searchUi.search.goTo();
});

test('displays pagination when results exceed page size', async ({searchUi}) => {
  // Assert
  await searchUi.search.expectPaginationVisible();
});

test('can navigate to next page', async ({searchUi}) => {
  // Act
  const firstPageTitles = await searchUi.search.getBookTitles();
  await searchUi.search.goToPage(2);

  // Assert
  const secondPageTitles = await searchUi.search.getBookTitles();
  expect(secondPageTitles).not.toEqual(firstPageTitles);
});
