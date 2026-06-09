const fs = require('fs');
const path = require('path');

const testName = process.argv[2];

if (!testName) {
  console.error('Please provide a test name.');
  console.error('Usage: npm run createTest <TestName>');
  process.exit(1);
}

const template = `import {test} from '../lib';

test.describe('${testName}', () => {

  test.beforeEach(async ({searchUi}) => {
    await searchUi.search.goTo();
  });

  test('can do something', async ({searchUi}) => {
    // Arrange

    // Act

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
