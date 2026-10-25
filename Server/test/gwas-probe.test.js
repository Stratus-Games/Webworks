const test = require('node:test');
const assert = require('node:assert');
const http = require('node:http');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const { probeGwas, describeGwas, networkingHint } = require('../src/gwasProbe');

const SERVER = path.join(__dirname, '..', 'server.js');

function fakeFetch(routes) {
  return async (url) => {
    const pathname = new URL(url).pathname;
    const route = routes[pathname];
    if (!route) return { ok: false, status: 404, text: async () => JSON.stringify({ error: 'not_found' }), headers: new Map() };
    return {
      ok: route.status >= 200 && route.status < 300,
      status: route.status,
      text: async () => JSON.stringify(route.body || {}),
      headers: new Map(),
    };
  };
}

test('a reachable GWAS reports its storage and a ready directory', async () => {
  const probe = await probeGwas('https://gwas.test/', {
    fetchImpl: fakeFetch({
      '/health': { status: 200, body: { ok: true, version: '1.0.0', storage: 'd1' } },
      '/directory/health': { status: 200, body: { servers: 3, lobbies: 7, tickets: 12 } },
    }),
  });

  assert.strictEqual(probe.url, 'https://gwas.test');
  assert.strictEqual(probe.reachable, true);
  assert.strictEqual(probe.storage, 'd1');
  assert.strictEqual(probe.directory.status, 'ready');
  assert.strictEqual(probe.directory.servers, 3);
  assert.deepStrictEqual(describeGwas(probe).warnings, []);
  assert.match(describeGwas(probe).line, /reachable/);
});

// A worker deployed without GWAS_SERVER_KEY answers its directory with 503. That is the
// state that reads as "the node server stays local forever", so it has to be named.
test('a worker without GWAS_SERVER_KEY is reported as not configured, with the fix', async () => {
  const probe = await probeGwas('https://gwas.test', {
    fetchImpl: fakeFetch({
      '/health': { status: 200, body: { ok: true, version: '1.0.0', storage: 'd1' } },
      '/directory/health': { status: 503, body: { error: 'server_key_not_configured' } },
    }),
  });

  assert.strictEqual(probe.reachable, true);
  assert.strictEqual(probe.directory.status, 'not_configured');
  const warnings = describeGwas(probe).warnings.join('\n');
  assert.match(warnings, /GWAS_SERVER_KEY/);
  assert.match(warnings, /wrangler secret put/);
});

test('a directory that rejects this server\'s key says so', async () => {
  const probe = await probeGwas('https://gwas.test', {
    fetchImpl: fakeFetch({
      '/health': { status: 200, body: { ok: true } },
      '/directory/health': { status: 401, body: { error: 'invalid_server_key' } },
    }),
  });

  assert.strictEqual(probe.directory.status, 'unauthorized');
  assert.match(describeGwas(probe).warnings.join('\n'), /WEBWORKS_GWAS_SERVER_KEY/);
});

test('an older GWAS without /directory routes degrades to "missing"', async () => {
  const probe = await probeGwas('https://gwas.test', {
    fetchImpl: fakeFetch({ '/health': { status: 200, body: { ok: true } } }),
  });

  assert.strictEqual(probe.directory.status, 'missing');
  assert.match(describeGwas(probe).warnings.join('\n'), /predates GWAS networking/);
});

test('an unreachable GWAS never looks like local accounts', async () => {
  const probe = await probeGwas('https://gwas.test', {
    fetchImpl: async () => { throw new TypeError('fetch failed'); },
  });

  assert.strictEqual(probe.reachable, false);
  assert.strictEqual(probe.error, 'unreachable');
  const described = describeGwas(probe);
  assert.match(described.line, /NOT reachable/);
  assert.match(described.warnings.join('\n'), /cannot sign in/);
});

test('a URL that is not http(s) is rejected before any request', async () => {
  let called = false;
  const probe = await probeGwas('gwas.example.com', { fetchImpl: async () => { called = true; } });
  assert.strictEqual(called, false);
  assert.strictEqual(probe.error, 'invalid_url');
  assert.strictEqual(probe.reachable, false);
});

test('networking hints cover every reason the directory can switch off', () => {
  for (const reason of [
    'networking_mode_local', 'waiting_public_url', 'invalid_public_url', 'missing_server_token', 'missing_server_key',
    'no_loopback', 'directory_unreachable', 'this GWAS does not serve the federation directory',
    'GWAS rejected the server key for the federation directory',
  ]) {
    const hint = networkingHint(reason);
    assert.ok(hint && hint.length > 10, `no hint for ${reason}`);
    assert.doesNotMatch(hint, /stopped this server/, `unmapped reason: ${reason}`);
  }
  assert.strictEqual(networkingHint(null), null);
});

// --- live server checks ------------------------------------------------------

function freePort() {
  return 44000 + Math.floor(Math.random() * 1500);
}

async function waitFor(url, timeoutMs = 10000) {
  const start = Date.now();
  let last;
  while (Date.now() - start < timeoutMs) {
    try {
      const resp = await fetch(url);
      if (resp.ok) return resp;
    } catch (err) { last = err; }
    await new Promise((r) => setTimeout(r, 100));
  }
  throw new Error('server did not start: ' + (last && last.message));
}

async function startServer(env) {
  const port = freePort();
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-gwas-'));
  const child = spawn(process.execPath, [SERVER, '--no-prompt'], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, {
      WEBWORKS_NO_PROMPT: '1',
      WEBWORKS_DATA_DIR: dir,
      WEBWORKS_PORT: String(port),
      WEBWORKS_CONFIG: path.join(dir, 'webworks.config.json'),
      WEBWORKS_LOG: 'normal',
      PORT: '',
    }, env),
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  let output = '';
  child.stdout.on('data', (d) => { output += d.toString(); });
  child.stderr.on('data', (d) => { output += d.toString(); });

  const base = `http://127.0.0.1:${port}`;
  await waitFor(`${base}/health`);
  return {
    base,
    child,
    output: () => output,
    async stop() {
      child.kill('SIGTERM');
      await new Promise((resolve) => {
        child.once('exit', resolve);
        setTimeout(resolve, 2000);
      });
    },
  };
}

// A stand-in GWAS worker: accounts answer, the directory is deployed without its secret.
async function startFakeGwas() {
  const server = http.createServer((req, res) => {
    const send = (status, body) => {
      res.writeHead(status, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify(body));
    };
    if (req.url === '/health') return send(200, { ok: true, service: 'gwas', version: '1.0.0', storage: 'd1' });
    if (req.url === '/directory/health') return send(503, { error: 'server_key_not_configured' });
    return send(404, { error: 'not_found' });
  });
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const { port } = server.address();
  return { url: `http://127.0.0.1:${port}`, close: () => new Promise((r) => server.close(r)) };
}

test('a GWAS-mode server reports the worker it is talking to, not "local"', async () => {
  const gwas = await startFakeGwas();
  const server = await startServer({
    WEBWORKS_ACCOUNT_MODE: 'gwas',
    WEBWORKS_GWAS_URL: gwas.url,
    WEBWORKS_NETWORKING: 'local',
  });
  try {
    const health = await (await fetch(`${server.base}/health`)).json();
    assert.strictEqual(health.accounts, 'gwas');
    assert.strictEqual(health.accountsDetail.reachable, true);
    assert.strictEqual(health.accountsDetail.url, gwas.url);
    assert.strictEqual(health.accountsDetail.storage, 'd1');
    assert.strictEqual(health.accountsDetail.directory.status, 'not_configured');

    const banner = server.output();
    assert.match(banner, /GWAS \(remote\)/);
    assert.match(banner, /reachable/);
    assert.match(banner, /GWAS_SERVER_KEY/);
  } finally {
    await server.stop();
    await gwas.close();
  }
});

// The function tester is part of the server: one page to drive every mount from a browser.
test('the function tester and its assets are served', async () => {
  const server = await startServer({ WEBWORKS_ACCOUNT_MODE: 'local' });
  try {
    const page = await fetch(`${server.base}/harness/`);
    assert.strictEqual(page.status, 200);
    assert.match(page.headers.get('content-type') || '', /text\/html/);
    const html = await page.text();
    assert.match(html, /Webworks function tester/);
    assert.match(html, /harness\.js/);

    for (const asset of ['harness.css', 'harness.js', 'game.js', 'logo.png']) {
      const reply = await fetch(`${server.base}/harness/${asset}`);
      assert.strictEqual(reply.status, 200, `${asset} should be served`);
    }

    const redirect = await fetch(`${server.base}/test`, { redirect: 'manual' });
    assert.strictEqual(redirect.status, 302);
    assert.strictEqual(redirect.headers.get('location'), '/harness/');

    const home = await (await fetch(`${server.base}/`)).text();
    assert.match(home, /\/harness\//);
  } finally {
    await server.stop();
  }
});

test('an unreachable GWAS is reported at startup instead of silently going local', async () => {
  const server = await startServer({
    WEBWORKS_ACCOUNT_MODE: 'gwas',
    WEBWORKS_GWAS_URL: `http://127.0.0.1:${freePort()}`,
    WEBWORKS_NETWORKING: 'local',
  });
  try {
    const health = await (await fetch(`${server.base}/health`)).json();
    assert.strictEqual(health.accounts, 'gwas');
    assert.strictEqual(health.accountsDetail.reachable, false);
    assert.match(server.output(), /NOT reachable/);

    const page = await (await fetch(`${server.base}/`)).text();
    assert.match(page, /not reachable/);
  } finally {
    await server.stop();
  }
});

// The automatic claim in practice: the server holds its token (no shared key, no public URL
// yet) and must report "waiting for an address" rather than "local".
async function startTokenGwas(token) {
  const server = http.createServer((req, res) => {
    const send = (status, body) => {
      res.writeHead(status, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify(body));
    };
    if (req.url === '/health') return send(200, { ok: true, service: 'gwas', version: '1.0.0', storage: 'd1' });
    if (req.url === '/directory/health') {
      if (req.headers['x-gwas-server-token'] !== token) return send(401, { error: 'server_claim_required' });
      return send(200, { servers: 0, lobbies: 0, tickets: 0 });
    }
    return send(404, { error: 'not_found' });
  });
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const { port } = server.address();
  return { url: `http://127.0.0.1:${port}`, close: () => new Promise((r) => server.close(r)) };
}

test('a server with a claimed token waits for a public address instead of going local', async () => {
  const token = Buffer.from(JSON.stringify({ srv: 'srv_test', exp: Date.now() + 60 * 24 * 60 * 60 * 1000 })).toString('base64url') + '.sig';
  const gwas = await startTokenGwas(token);
  const server = await startServer({
    WEBWORKS_SERVER_TOKEN: token,
    WEBWORKS_GWAS_URL: gwas.url,
    WEBWORKS_NETWORKING: 'gwas',
    WEBWORKS_PUBLIC_URL: '',
  });
  try {
    const health = await (await fetch(`${server.base}/health`)).json();
    assert.strictEqual(health.accounts, 'gwas');
    assert.strictEqual(health.accountsDetail.directory.status, 'ready');
    assert.strictEqual(health.networking.enabled, true);
    assert.strictEqual(health.networking.publishing, false);
    assert.strictEqual(health.networking.disabledReason, 'waiting_public_url');
    assert.strictEqual(health.networking.authMode, 'claim');
    assert.match(health.networkingHint || '', /tunnel|WEBWORKS_PUBLIC_URL/);
    assert.ok(health.networking.serverId);

    assert.match(server.output(), /waiting for a public address/);

    const page = await (await fetch(`${server.base}/`)).text();
    assert.match(page, /waiting for a public address/);
  } finally {
    await server.stop();
    await gwas.close();
  }
});

test('a server that has not claimed its id says so once, and keeps hosting locally', async () => {
  const server = await startServer({
    WEBWORKS_SERVER_TOKEN: '',
    WEBWORKS_NETWORKING: 'gwas',
    WEBWORKS_GWAS_URL: `http://127.0.0.1:${freePort()}`,
  });
  try {
    const health = await (await fetch(`${server.base}/health`)).json();
    assert.strictEqual(health.networking.enabled, false);
    assert.strictEqual(health.networking.disabledReason, 'missing_server_token');
    assert.match(health.networkingHint || '', /claims it automatically/);
  } finally {
    await server.stop();
  }
});
