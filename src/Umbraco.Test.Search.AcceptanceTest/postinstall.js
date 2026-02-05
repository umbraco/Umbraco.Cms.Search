const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Check if .env file exists
const envPath = path.join(__dirname, '.env');
const consoleErrorsPath = path.join(__dirname, 'console-errors.json');

if (!fs.existsSync(envPath)) {
  console.log('No .env file found. Running configuration...');
  console.log('');
  execSync('node config.js', { stdio: 'inherit' });
} else {
  console.log('.env file found. Skipping configuration.');
  console.log('Run "npm run config" to reconfigure.');
}

// Create console-errors.json if it doesn't exist
if (!fs.existsSync(consoleErrorsPath)) {
  fs.writeFileSync(consoleErrorsPath, '[]');
  console.log('Created console-errors.json file.');
}

// Create auth directory if it doesn't exist
const authDir = path.join(__dirname, 'playwright', '.auth');
if (!fs.existsSync(authDir)) {
  fs.mkdirSync(authDir, { recursive: true });
  console.log('Created playwright/.auth directory.');
}
