const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawn } = require('node:child_process');

// The server is reached from wherever it is hosted: localhost, a LAN address, a tunnel URL or
// a custom domain, and sometimes from a page served by another origin entirely. Nothing about
// that should need configuration.

const SERVER = path.join(__dirname, '..', 'server.js');

function freePort() {
  return 46000 + Math.floor(Math.random() * 2000);
}

async function waitFor(url, timeoutMs = 15000) {
  const start = Date.now();
  let lastErr;
  while (Date.now() - start < timeoutMs) {
    try {
      const resp = await fetch(url);
      if (resp.ok) return resp;
    } catch (err) { lastErr = err; }
    await new Promise((r) => setTimeout(r, 100));
  }
  throw new Error('server did not become ready: ' + (lastErr && lastErr.message));
}

async function startServer() {
  const port = freePort();
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-cors-'));
  const child = spawn(process.execPath, [SERVER], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, {
      WEBWORKS_ACCOUNT_MODE: 'local',
      WEBWORKS_NO_PROMPT: '1',
      WEBWORKS_DATA_DIR: dir,
      WEBWORKS_PORT: String(port),
      WEBWORKS_LOG: 'normal',
      WEBWORKS_CONFIG: path.join(dir, 'webworks.config.json'),
      PORT: '',
    }),
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let out = '';
  child.stdout.on('data', (d) => { out += d.toString(); });
  child.stderr.on('data', (d) => { out += d.toString(); });

  const base = `http://127.0.0.1:${port}`;
  await waitFor(`${base}/health`);
  return {
    base,
    output: () => out,
    stop: () => new Promise((done) => {
      child.once('exit', () => done());
      child.kill('SIGTERM');
    }),
  };
}

test('any origin can preflight and call the server, including headers it has never seen', async () => {
  const server = await startServer();
  try {
    const origins = [
      'http://localhost:3000',
      'http://192.168.1.5:3000',
      'https://games.example.com',
      'https://brave-tiger-lamp.trycloudflare.com',
    ];

    for (const origin of origins) {
      const pre = await fetch(`${server.base}/api/Unity/SteamworksNET/Accounts/Register`, {
        method: 'OPTIONS',
        headers: {
          Origin: origin,
          'Access-Control-Request-Method': 'POST',
          'Access-Control-Request-Headers': 'content-type,x-webworks-session,x-webworks-account,x-future-header',
        },
      });
      assert.strictEqual(pre.status, 204, origin);
      assert.strictEqual(pre.headers.get('access-control-allow-origin'), '*', origin);
      const allowed = pre.headers.get('access-control-allow-headers') || '';
      assert.match(allowed, /x-webworks-session/i, origin);
      assert.match(allowed, /x-webworks-account/i, origin);
      assert.match(allowed, /x-future-header/i, `${origin}: a header this build does not know is echoed`);

      const health = await fetch(`${server.base}/health`, { headers: { Origin: origin } });
      assert.strictEqual(health.status, 200, origin);
      assert.strictEqual(health.headers.get('access-control-allow-origin'), '*', origin);
    }

    // A preflight for a mount that does not exist still answers, so a browser never sees a
    // confusing 404 instead of the CORS failure it would otherwise report.
    const unknown = await fetch(`${server.base}/api/Unity/SomeFutureMount/Accounts/Register`, {
      method: 'OPTIONS',
      headers: { Origin: 'https://games.example.com', 'Access-Control-Request-Method': 'POST' },
    });
    assert.strictEqual(unknown.status, 204);
    assert.strictEqual(unknown.headers.get('access-control-allow-origin'), '*');
  } finally {
    await server.stop();
  }
});
