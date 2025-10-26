// Quick test to verify server starts
const { spawn } = require('child_process');

console.log('Starting GOFUS Game Server test...');

// Start the server
const server = spawn('npm', ['run', 'dev'], {
  cwd: __dirname,
  shell: true
});

let output = '';

server.stdout.on('data', (data) => {
  output += data.toString();
  console.log(data.toString());

  // Check if server started successfully
  if (output.includes('Game Server started on port')) {
    console.log('\n✅ Server started successfully!');
    console.log('Shutting down test server...');
    server.kill();
    process.exit(0);
  }
});

server.stderr.on('data', (data) => {
  console.error('Error:', data.toString());
});

server.on('error', (error) => {
  console.error('Failed to start server:', error);
  process.exit(1);
});

// Timeout after 10 seconds
setTimeout(() => {
  console.error('❌ Server failed to start within 10 seconds');
  server.kill();
  process.exit(1);
}, 10000);