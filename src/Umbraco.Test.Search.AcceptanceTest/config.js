const prompt = require('prompt');
const fs = require('fs');

console.log('Umbraco Search Acceptance Test Configuration');
console.log('============================================');
console.log('Please provide the following configuration values:');
console.log('');

prompt.start();

prompt.get([
  {
    name: 'email',
    description: 'Umbraco superadmin username (email)',
    required: true
  },
  {
    name: 'password',
    description: 'Umbraco superadmin password',
    required: true,
    hidden: true
  },
  {
    name: 'url',
    description: 'CMS URL (default: https://localhost:44324)',
    default: 'https://localhost:44324'
  }
], function (err, result) {
  if (err) {
    console.error('Configuration cancelled.');
    return;
  }

  const envContent = `UMBRACO_USER_LOGIN=${result.email}
UMBRACO_USER_PASSWORD=${result.password}
URL=${result.url}
STORAGE_STATE_PATH=playwright/.auth/user.json
CONSOLE_ERRORS_PATH=console-errors.json
`;

  fs.writeFileSync('.env', envContent);
  console.log('');
  console.log('Configuration saved to .env file.');
  console.log('You can now run the tests with: npm run test');
});
