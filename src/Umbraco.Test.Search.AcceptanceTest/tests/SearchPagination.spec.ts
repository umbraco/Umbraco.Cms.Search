import {test, expect} from '@playwright/test';
import {SearchPage} from '../pages';

test.describe('Search Pagination', () => {
  let searchPage: SearchPage;
  let resultCount: number;

  test.beforeEach(async ({page, baseURL}) => {
    searchPage = new SearchPage(page, baseURL!);
    await searchPage.goTo();
    resultCount = await searchPage.getResultCount();
  });

  test('displays pagination when results exceed page size', async () => {
    // Assert
    if (resultCount > 5) {
      await searchPage.expectPaginationVisible();
    }
  });

  test('can navigate to next page', async () => {
    // Arrange
    if (resultCount <= 5) {
      test.skip();
      return;
    }

    // Act
    const firstPageTitles = await searchPage.getBookTitles();
    await searchPage.goToPage(2);

    // Assert
    const secondPageTitles = await searchPage.getBookTitles();
    expect(secondPageTitles).not.toEqual(firstPageTitles);
  });
});
