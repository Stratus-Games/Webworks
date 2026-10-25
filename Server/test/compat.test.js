const test = require('node:test');
const assert = require('node:assert');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const SERVER = path.join(__dirname, '..', 'server.js');
const { ENGINE_MOUNTS, LEGACY_ENGINE_MOUNTS, applyLegacyEnv } = require('../src/compat');

function freePort() {
  return 46000 + Math.floor(Math.random() * 2000);
}

async function waitFor(url, timeoutMs = 8000) {
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

async function startServer(env = {}) {
  const port = freePort();
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-compat-'));
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
    }, env),
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  let out = '';
  child.stdout.on('data', (d) => { out += d.toString(); });
  child.stderr.on('data', (d) => { out += d.toString(); });

  const base = `http://127.0.0.1:${port}`;
  await waitFor(`${base}/health`);

  return {
    base,
    child,
    output: () => out,
    async stop() {
      child.kill('SIGTERM');
      await new Promise((resolve) => {
        child.once('exit', resolve);
        setTimeout(resolve, 2000);
      });
    },
  };
}

function json(base, pathname, options = {}) {
  const headers = Object.assign({ 'Content-Type': 'application/json' }, options.headers || {});
  return fetch(base + pathname, Object.assign({}, options, { headers })).then(async (resp) => ({
    status: resp.status,
    headers: resp.headers,
    body: await resp.json().catch(() => null),
  }));
}

async function guestSession(base, mount, headers = {}) {
  const resp = await json(base, `${mount}/User/GuestSession`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ appId: '480', name: 'Legacy' }),
  });
  assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
  assert.ok(resp.body.sessionToken);
  return resp.body;
}

test('every historical engine mount answers', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  for (const mount of ['/api/Unity/Webeam', '/api/Unity/Weeam', '/api/Unity/Steamworks.net', '/api/Unity/Webworks', '/api', '/api/Unity']) {
    const session = await guestSession(server.base, mount);
    const profile = await json(server.base, `${mount}/User/Profile`, {
      headers: { 'x-webworks-session': session.sessionToken },
    });
    assert.strictEqual(profile.status, 200, `${mount}: ${JSON.stringify(profile.body)}`);
    assert.strictEqual(String(profile.body.steamId), String(session.steamId));
  }

  // Mounts are case-insensitive like Express routers always were, so a mixed-case old client works.
  const mixed = await guestSession(server.base, '/API/unity/STEAMWORKS.NET');
  assert.ok(mixed.steamId);
});

test('x-webeam-session and ?session= authenticate like the current header', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const session = await guestSession(server.base, '/api/Unity/Facepunch');

  const legacyHeader = await json(server.base, '/api/Unity/Facepunch/User/Profile', {
    headers: { 'x-webeam-session': session.sessionToken },
  });
  assert.strictEqual(legacyHeader.status, 200, JSON.stringify(legacyHeader.body));
  assert.strictEqual(String(legacyHeader.body.steamId), String(session.steamId));

  const query = await json(server.base, `/api/Unity/Facepunch/User/Profile?session=${encodeURIComponent(session.sessionToken)}`);
  assert.strictEqual(query.status, 200, JSON.stringify(query.body));

  const missing = await json(server.base, '/api/Unity/Facepunch/User/Profile');
  assert.strictEqual(missing.status, 401);
});

test('CORS preflight allows the legacy header names', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const resp = await fetch(`${server.base}/api/Unity/Facepunch/User/Profile`, {
    method: 'OPTIONS',
    headers: { 'Access-Control-Request-Headers': 'x-webeam-session, content-type' },
  });
  assert.strictEqual(resp.status, 204);
  const allowed = resp.headers.get('access-control-allow-headers') || '';
  assert.match(allowed, /x-webeam-session/i);
  assert.match(allowed, /x-webeam-account/i);
  assert.match(allowed, /x-webworks-session/i);
});

test('lowercase /accounts/login and /accounts/register keep the old worker response shape', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const registered = await json(server.base, '/accounts/register', {
    method: 'POST',
    body: JSON.stringify({ username: 'legacyuser', password: 'hunter22', displayName: 'Legacy User' }),
  });
  assert.strictEqual(registered.status, 201, JSON.stringify(registered.body));
  assert.strictEqual(registered.body.ok, true);
  assert.ok(registered.body.token, 'legacy register response carries a token');
  assert.strictEqual(registered.body.account.displayName, 'Legacy User');

  const loggedIn = await json(server.base, '/accounts/login', {
    method: 'POST',
    body: JSON.stringify({ username: 'legacyuser', password: 'hunter22' }),
  });
  assert.strictEqual(loggedIn.status, 200, JSON.stringify(loggedIn.body));
  assert.ok(loggedIn.body.token);
  assert.ok(loggedIn.body.account);
  assert.strictEqual(loggedIn.body.account.username, 'legacyuser');

  // The legacy worker was called with the token in an Authorization header.
  const me = await json(server.base, '/accounts/me', {
    headers: { Authorization: `Bearer ${loggedIn.body.token}` },
  });
  assert.strictEqual(me.status, 200, JSON.stringify(me.body));
  assert.strictEqual(me.body.account.username, 'legacyuser');
});

test('legacy session headers reach the realtime channel and game traffic', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const session = await guestSession(server.base, '/api/Unity/Weeam');
  const lobby = await json(server.base, '/api/Unity/Weeam/Matchmaking/CreateLobby', {
    method: 'POST',
    headers: { 'x-webeam-session': session.sessionToken },
    body: JSON.stringify({ maxMembers: 4, visibility: 'friends' }),
  });
  assert.strictEqual(lobby.status, 200, JSON.stringify(lobby.body));
  assert.ok(lobby.body.id);
});

test('/health advertises current and historical engine prefixes', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const resp = await json(server.base, '/health');
  assert.strictEqual(resp.status, 200);
  assert.deepStrictEqual(resp.body.engines, ENGINE_MOUNTS);
  assert.deepStrictEqual(resp.body.legacyEngines, LEGACY_ENGINE_MOUNTS);
});

test('WEBEAM_* environment variables still configure the server', async (t) => {
  const server = await startServer({ WEBEAM_CORS_ORIGIN: 'https://legacy.example' });
  t.after(() => server.stop());

  const resp = await fetch(`${server.base}/health`);
  assert.strictEqual(resp.headers.get('access-control-allow-origin'), 'https://legacy.example');
});

test('applyLegacyEnv maps old names without overwriting current ones', () => {
  const env = {
    WEBEAM_CACHE_DIR: '/tmp/old-cache',
    WEBEAM_TICKET_SECRET: 'old-secret',
    WEBWORKS_ACCOUNTS_WORKER_URL: 'https://accounts.example.test',
  };
  applyLegacyEnv(env);
  assert.strictEqual(env.WEBWORKS_CACHE_DIR, '/tmp/old-cache');
  assert.strictEqual(env.WEBWORKS_DATA_DIR, '/tmp/old-cache');
  assert.strictEqual(env.WEBWORKS_TICKET_SECRET, 'old-secret');
  assert.strictEqual(env.WEBWORKS_GWAS_URL, 'https://accounts.example.test');
  assert.strictEqual(env.WEBWORKS_ACCOUNT_MODE, 'gwas');

  const explicit = {
    WEBEAM_DATA_DIR: '/tmp/legacy',
    WEBWORKS_DATA_DIR: '/tmp/current',
    WEBEAM_ACCOUNTS_WORKER_URL: 'https://legacy.example.test',
    WEBWORKS_GWAS_URL: 'https://gwas.example.test',
    WEBWORKS_ACCOUNT_MODE: 'local',
  };
  applyLegacyEnv(explicit);
  assert.strictEqual(explicit.WEBWORKS_DATA_DIR, '/tmp/current');
  assert.strictEqual(explicit.WEBWORKS_GWAS_URL, 'https://gwas.example.test');
  assert.strictEqual(explicit.WEBWORKS_ACCOUNT_MODE, 'local');
});

test('a deployment with only WEBEAM_ACCOUNTS_WORKER_URL keeps using remote accounts', async (t) => {
  const server = await startServer({
    WEBWORKS_ACCOUNT_MODE: '',
    WEBEAM_ACCOUNTS_WORKER_URL: 'https://accounts.example.invalid',
  });
  t.after(() => server.stop());

  const resp = await json(server.base, '/accounts/Mode');
  assert.strictEqual(resp.status, 200);
  assert.strictEqual(resp.body.mode, 'gwas');
});
