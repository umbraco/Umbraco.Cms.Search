const fs = require('fs');
const path = require('path');

const testName = process.argv[2];

if (!testName) {
  console.error('Please provide a test name.');
  console.error('Usage: npm run createTest <TestName>');
  process.exit(1);
}

const template = `import {test} from '@playwright/test';
import {HomePage} from '../pages';

test.describe('${testName}', () => {

  test('can do something', {tag: '@smoke'}, async ({page, baseURL}) => {
    // Arrange
    const homePage = new HomePage(page, baseURL!);

    // Act
    await homePage.goto();

    // Assert
  });

});
`;

const testDir = path.join(__dirname, 'tests');
const testPath = path.join(testDir, `${testName}.spec.ts`);

if (!fs.existsSync(testDir)) {
  fs.mkdirSync(testDir, { recursive: true });
}

if (fs.existsSync(testPath)) {
  console.error(`Test file already exists: ${testPath}`);
  process.exit(1);
}

fs.writeFileSync(testPath, template);
console.log(`Created test file: ${testPath}`);
