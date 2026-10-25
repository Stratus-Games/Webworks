const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const { localUrls, tunnelTarget, URL_PATTERN, isWildcardHost, startQuickTunnel } = require('../src/cloudflareTunnel');

test('the server reports every address a player can use', () => {
  const wildcard = localUrls('0.0.0.0', 3000);
  assert.ok(wildcard.includes('http://localhost:3000'));
  assert.ok(wildcard.includes('http://127.0.0.1:3000'));
  // Every extra entry is a plain LAN address on the same port, never a wildcard host.
  for (const url of wildcard.slice(2)) assert.match(url, /^http:\/\/\d+\.\d+\.\d+\.\d+:3000$/);

  assert.deepStrictEqual(localUrls('127.0.0.1', 3000), ['http://127.0.0.1:3000']);
  assert.deepStrictEqual(localUrls('192.168.1.20', 3000), ['http://192.168.1.20:3000', 'http://localhost:3000']);
  assert.deepStrictEqual(localUrls('localhost', 8080), ['http://localhost:8080']);
  assert.deepStrictEqual(localUrls('0.0.0.0', 443, 'https')[0], 'https://localhost:443');

  assert.strictEqual(isWildcardHost('::'), true);
  assert.strictEqual(isWildcardHost('0.0.0.0'), true);
  assert.strictEqual(isWildcardHost('10.0.0.5'), false);
  assert.strictEqual(tunnelTarget(3000), 'http://localhost:3000');
});

test('only trycloudflare hosts count as a tunnel URL', () => {
  const line = 'Your quick Tunnel has been created! Visit it at https://brave-tiger-lamp.trycloudflare.com';
  assert.strictEqual(URL_PATTERN.exec(line)[0], 'https://brave-tiger-lamp.trycloudflare.com');
  assert.strictEqual(URL_PATTERN.exec('http://localhost:3000'), null);
  assert.strictEqual(URL_PATTERN.exec('https://gwas.webeam.workers.dev'), null);
  assert.strictEqual(URL_PATTERN.exec('https://evil.trycloudflare.com.attacker.test'), null);
});

// A stand-in for wrangler, so the tunnel plumbing is tested without downloading the real
// CLI or exposing this machine to the internet.
test('a quick tunnel reports the URL wrangler prints and can be stopped', async () => {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-wrangler-'));
  const script = path.join(dir, process.platform === 'win32' ? 'wrangler.cmd' : 'wrangler');
  const created = 'Your quick Tunnel has been created! Visit it at https://fake-tiger-lamp.trycloudflare.com';

  if (process.platform === 'win32') {
    const lines = [
      '@echo off',
      'if "%1"=="--version" (echo wrangler 4.0.0-test & exit /b 0)',
      `echo ${created} 1>&2`,
      'ping -n 4 127.0.0.1 >nul',
    ];
    fs.writeFileSync(script, lines.join('\r\n') + '\r\n');
  } else {
    const lines = [
      '#!/bin/sh',
      'if [ "$1" = "--version" ]; then echo "wrangler 4.0.0-test"; exit 0; fi',
      `echo "${created}" >&2`,
      'sleep 5',
    ];
    fs.writeFileSync(script, lines.join('\n') + '\n', { mode: 0o755 });
  }

  const previousPath = process.env.PATH;
  process.env.PATH = dir + path.delimiter + previousPath;
  try {
    const started = await startQuickTunnel({ port: 31234, log: { notice() {}, warn() {} } });
    assert.ok(started, 'tunnel should start');
    assert.strictEqual(started.url, 'https://fake-tiger-lamp.trycloudflare.com');
    assert.strictEqual(started.target, 'http://localhost:31234');
    started.stop();
  } finally {
    process.env.PATH = previousPath;
    await removeTempDir(dir);
  }
});

// Windows keeps a directory busy until the stand-in's child process lets go of it, which can
// take a moment after stop(). A temp folder left behind is harmless, so this waits rather
// than turning a cleanup race into a failed test.
async function removeTempDir(dir, timeoutMs = 8000) {
  const start = Date.now();
  for (;;) {
    try {
      fs.rmSync(dir, { recursive: true, force: true });
      return;
    } catch (err) {
      if (!['EPERM', 'ENOTEMPTY', 'EBUSY'].includes(err.code)) throw err;
      if (Date.now() - start > timeoutMs) return;
      await new Promise((r) => setTimeout(r, 100));
    }
  }
}
