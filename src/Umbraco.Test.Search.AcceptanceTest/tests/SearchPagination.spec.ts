import {test, expect} from '@playwright/test';
import {SearchPage} from '../pages';

test.describe('Search Pagination', () => {
  let searchPage: SearchPage;

  test.beforeEach(async ({page, baseURL}) => {
    searchPage = new SearchPage(page, baseURL!);
    await searchPage.goto();
  });

  test('displays pagination when results exceed page size', async () => {
    const resultCount = await searchPage.getResultCount();
    if (resultCount > 5) {
      await searchPage.expectPaginationVisible();
    }
  });

  test('can navigate to next page', async () => {
    const resultCount = await searchPage.getResultCount();
    if (resultCount <= 5) {
      test.skip();
      return;
    }

    const firstPageTitles = await searchPage.getBookTitles();
    await searchPage.goToPage(2);

    const secondPageTitles = await searchPage.getBookTitles();
    expect(secondPageTitles).not.toEqual(firstPageTitles);
  });

});
