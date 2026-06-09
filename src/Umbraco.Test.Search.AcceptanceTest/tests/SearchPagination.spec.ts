import {test} from '../lib';
import {expect} from '@playwright/test';

test.describe('Search Pagination', () => {
  let resultCount: number;

  test.beforeEach(async ({searchUi}) => {
    await searchUi.search.goTo();
    resultCount = await searchUi.search.getResultCount();
  });

  test('displays pagination when results exceed page size', async ({searchUi}) => {
    // Assert
    if (resultCount > 5) {
      await searchUi.search.expectPaginationVisible();
    }
  });

  test('can navigate to next page', async ({searchUi}) => {
    // Arrange
    if (resultCount <= 5) {
      test.skip();
      return;
    }

    // Act
    const firstPageTitles = await searchUi.search.getBookTitles();
    await searchUi.search.goToPage(2);

    // Assert
    const secondPageTitles = await searchUi.search.getBookTitles();
    expect(secondPageTitles).not.toEqual(firstPageTitles);
  });
});
