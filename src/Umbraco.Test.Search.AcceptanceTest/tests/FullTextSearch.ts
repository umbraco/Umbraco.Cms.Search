import {test} from '@playwright/test';
import {HomePage} from '../pages';

test.describe('Full-text search tests', () => {

  test('can open the test site', {tag: '@smoke'}, async ({page, baseURL}) => {
    // Arrange
    const homePage = new HomePage(page, baseURL!);

    // Act
    await homePage.goto();

    // Assert
    await homePage.expectTitle('Book search');
  });

});
