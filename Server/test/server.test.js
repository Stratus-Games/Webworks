const test = require('node:test');
const assert = require('node:assert');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const WebSocket = require('ws');
const { identicon } = require('../src/avatars');

const SERVER = path.join(__dirname, '..', 'server.js');
const API = '/api/Unity/SteamworksNET';

function freePort() {
  return 42000 + Math.floor(Math.random() * 2000);
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

async function startServer({ dataDir, mode = 'local', env = {}, port: fixedPort, args = [] } = {}) {
  const port = fixedPort || freePort();
  const dir = dataDir || fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-test-'));
  const child = spawn(process.execPath, [SERVER, ...args], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, {
      WEBWORKS_ACCOUNT_MODE: mode,
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
    port,
    dir,
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
    body: await resp.json().catch(() => null),
  }));
}

async function register(base, username, displayName, extra = {}) {
  const resp = await json(base, '/accounts/Register', {
    method: 'POST',
    body: JSON.stringify(Object.assign({ username, password: 'hunter22', displayName }, extra)),
  });
  assert.strictEqual(resp.status, 201, JSON.stringify(resp.body));
  return resp.body;
}

async function gameSession(base, accountToken, appId, name) {
  const resp = await json(base, `${API}/User/GuestSession`, {
    method: 'POST',
    body: JSON.stringify({ appId, accountToken, name }),
  });
  assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
  return resp.body;
}

function connect(base, sessionToken) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(`${base.replace('http', 'ws')}/realtime?session=${sessionToken}`);
    const messages = [];
    ws.on('message', (raw) => {
      try { messages.push(JSON.parse(raw.toString())); } catch {  }
    });
    ws.on('open', () => resolve({ ws, messages }));
    ws.on('error', reject);
  });
}

test('local accounts: register, login, change password, persisted across restart', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const reg = await register(server.base, 'alex', 'Alex');
  assert.ok(reg.token);
  assert.strictEqual(reg.account.displayName, 'Alex');

  const dupe = await json(server.base, '/accounts/Register', {
    method: 'POST', body: JSON.stringify({ username: 'alex', password: 'hunter22' }),
  });
  assert.strictEqual(dupe.status, 409);
  assert.strictEqual(dupe.body.error, 'username_taken');

  const bad = await json(server.base, '/accounts/Login', {
    method: 'POST', body: JSON.stringify({ username: 'alex', password: 'wrong' }),
  });
  assert.strictEqual(bad.status, 401);
  assert.strictEqual(bad.body.error, 'bad_credentials');

  const me = await json(server.base, '/accounts/Me', { headers: { 'x-webworks-account': reg.token } });
  assert.strictEqual(me.status, 200);
  assert.strictEqual(me.body.account.username, 'alex');

  const changed = await json(server.base, '/accounts/Password', {
    method: 'POST',
    headers: { 'x-webworks-account': reg.token },
    body: JSON.stringify({ currentPassword: 'hunter22', newPassword: 'newpass123' }),
  });
  assert.strictEqual(changed.status, 200);

  const stale = await json(server.base, '/accounts/Me', { headers: { 'x-webworks-account': reg.token } });
  assert.strictEqual(stale.status, 401);

  const relogin = await json(server.base, '/accounts/Login', {
    method: 'POST', body: JSON.stringify({ username: 'alex', password: 'newpass123' }),
  });
  assert.strictEqual(relogin.status, 200);

  const dir = server.dir;
  await server.stop();
  const second = await startServer({ dataDir: dir });
  t.after(() => second.stop());
  const again = await json(second.base, '/accounts/Login', {
    method: 'POST', body: JSON.stringify({ username: 'alex', password: 'newpass123' }),
  });
  assert.strictEqual(again.status, 200);
});

test('avatar upload: PNG accepted, dimensions and mime reported, junk rejected', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const png = Buffer.from(identicon('alex', 64).png, 'base64').toString('base64');
  const reg = await register(server.base, 'alex', 'Alex', { avatarBase64: png });
  assert.strictEqual(reg.account.hasAvatar, true);

  const avatar = await json(server.base, '/accounts/Avatar?username=alex');
  assert.strictEqual(avatar.status, 200);
  assert.strictEqual(avatar.body.custom, true);
  assert.strictEqual(avatar.body.mime, 'image/png');
  assert.strictEqual(avatar.body.width, 60);
  assert.ok(avatar.body.data.length > 20);

  const junk = await json(server.base, '/accounts/Register', {
    method: 'POST',
    body: JSON.stringify({ username: 'junkuser', password: 'hunter22', avatarBase64: Buffer.from('not an image at all').toString('base64') }),
  });
  assert.strictEqual(junk.status, 400);
  assert.strictEqual(junk.body.error, 'unsupported_avatar_format');

  const noAvatar = await json(server.base, '/accounts/Avatar?username=unknownperson');
  assert.strictEqual(noAvatar.status, 200);
  assert.strictEqual(noAvatar.body.custom, false);
  assert.strictEqual(noAvatar.body.mime, 'image/png');
});

test('friends and presence: two players online at once are visible to each other', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const alex = await register(server.base, 'alex', 'Alex');
  const bob = await register(server.base, 'bob', 'Bob');

  let resp = await json(server.base, '/accounts/Friends/Add', {
    method: 'POST', headers: { 'x-webworks-account': alex.token }, body: JSON.stringify({ username: 'bob' }),
  });
  assert.strictEqual(resp.status, 200);
  assert.strictEqual(resp.body.status, 'requested');

  resp = await json(server.base, '/accounts/Friends', { headers: { 'x-webworks-account': bob.token } });
  assert.deepStrictEqual(resp.body.incoming, ['alex']);

  resp = await json(server.base, '/accounts/Friends/Accept', {
    method: 'POST', headers: { 'x-webworks-account': bob.token }, body: JSON.stringify({ username: 'alex' }),
  });
  assert.strictEqual(resp.body.status, 'friends');

  const alexSession = await gameSession(server.base, alex.token, '100', 'Alex');
  const bobSession = await gameSession(server.base, bob.token, '200', 'Bob');
  assert.notStrictEqual(alexSession.steamId, bobSession.steamId);

  const alexWs = await connect(server.base, alexSession.sessionToken);
  const bobWs = await connect(server.base, bobSession.sessionToken);
  t.after(() => { alexWs.ws.close(); bobWs.ws.close(); });

  await new Promise((r) => setTimeout(r, 250));

  resp = await json(server.base, `${API}/Friends/List`, { headers: { 'x-webworks-session': alexSession.sessionToken } });
  assert.strictEqual(resp.body.friends.length, 1);
  const bobsEntry = resp.body.friends[0];
  assert.strictEqual(bobsEntry.name, 'Bob');
  assert.strictEqual(bobsEntry.personaState, 'Online');
  assert.strictEqual(bobsEntry.game, '200');
  assert.strictEqual(resp.body.onlineCount, 1);

  resp = await json(server.base, `${API}/Friends/List`, { headers: { 'x-webworks-session': bobSession.sessionToken } });
  assert.strictEqual(resp.body.friends[0].personaState, 'Online');
  assert.strictEqual(resp.body.friends[0].game, '100');

  resp = await json(server.base, '/status');
  assert.strictEqual(resp.body.online, 2);
  assert.deepStrictEqual(resp.body.onlineByGame, { 100: 1, 200: 1 });

  bobWs.ws.close();
  await new Promise((r) => setTimeout(r, 300));
  resp = await json(server.base, `${API}/Friends/List`, { headers: { 'x-webworks-session': alexSession.sessionToken } });
  assert.strictEqual(resp.body.friends[0].personaState, 'Offline');

  resp = await json(server.base, '/status');
  assert.strictEqual(resp.body.online, 1);
});

test('a single account can be online in two games at once', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const alex = await register(server.base, 'alex', 'Alex');
  const one = await gameSession(server.base, alex.token, '100', 'Alex');
  const two = await gameSession(server.base, alex.token, '200', 'Alex');

  const wsOne = await connect(server.base, one.sessionToken);
  const wsTwo = await connect(server.base, two.sessionToken);
  t.after(() => { wsOne.ws.close(); wsTwo.ws.close(); });
  await new Promise((r) => setTimeout(r, 200));

  let resp = await json(server.base, '/status');
  assert.strictEqual(resp.body.online, 1);

  wsOne.ws.close();
  await new Promise((r) => setTimeout(r, 300));
  resp = await json(server.base, '/status');
  assert.strictEqual(resp.body.online, 1, 'closing one game must not mark the account offline');

  wsTwo.ws.close();
  await new Promise((r) => setTimeout(r, 300));
  resp = await json(server.base, '/status');
  assert.strictEqual(resp.body.online, 0);
});

test('multi-game isolation: lobbies, stats and leaderboards never cross app ids', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const alex = await register(server.base, 'alex', 'Alex');
  const gameOne = await gameSession(server.base, alex.token, '111', 'Alex');
  const gameTwo = await gameSession(server.base, alex.token, '222', 'Alex');

  const created = await json(server.base, `${API}/Matchmaking/CreateLobby`, {
    method: 'POST', headers: { 'x-webworks-session': gameOne.sessionToken }, body: JSON.stringify({ maxMembers: 8 }),
  });
  assert.strictEqual(created.status, 200);

  const listTwo = await json(server.base, `${API}/Matchmaking/LobbyList`, {
    method: 'POST', headers: { 'x-webworks-session': gameTwo.sessionToken }, body: JSON.stringify({}),
  });
  assert.strictEqual(listTwo.body.lobbies.length, 0, 'game 222 must not see game 111 lobbies');

  const listOne = await json(server.base, `${API}/Matchmaking/LobbyList`, {
    method: 'POST', headers: { 'x-webworks-session': gameOne.sessionToken }, body: JSON.stringify({}),
  });
  assert.strictEqual(listOne.body.lobbies.length, 1);

  await json(server.base, `${API}/UserStats/Stat`, {
    method: 'POST', headers: { 'x-webworks-session': gameOne.sessionToken }, body: JSON.stringify({ name: 'kills', value: 7 }),
  });
  const statTwo = await json(server.base, `${API}/UserStats/Stat?name=kills`, {
    headers: { 'x-webworks-session': gameTwo.sessionToken },
  });
  assert.notStrictEqual(statTwo.body.value, 7);
});

test('a player who never signed in plays as a guest, and a stale token still yields a guest', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  // No account token at all: exactly what the shims send when the player skipped sign-in.
  const anonymous = await gameSession(server.base, undefined, '480', 'Anonymous');
  assert.strictEqual(anonymous.linkedAccount, null);
  assert.ok(anonymous.steamId && anonymous.sessionToken);
  assert.strictEqual(anonymous.accountSystem, 'local');

  // An expired or made-up token must not strand the player: the server logs it and hands
  // out a guest session instead of failing the game.
  const stale = await gameSession(server.base, 'expired-or-forged-token', '480', 'Stale');
  assert.strictEqual(stale.linkedAccount, null);
  assert.notStrictEqual(stale.steamId, anonymous.steamId);

  // The guest session is a real one: it can create and read a lobby.
  const created = await json(server.base, `${API}/Matchmaking/CreateLobby`, {
    method: 'POST',
    headers: { 'x-webworks-session': stale.sessionToken },
    body: JSON.stringify({ maxMembers: 4 }),
  });
  assert.strictEqual(created.status, 200, JSON.stringify(created.body));
  assert.ok(created.body.id);
});

test('the startup banner and /health report every local address', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const health = await json(server.base, '/health');
  assert.ok(Array.isArray(health.body.urls));
  assert.ok(health.body.urls.includes('http://localhost:' + server.port));
  assert.ok(health.body.urls.every((url) => url.endsWith(':' + server.port)));
  assert.strictEqual(health.body.publicUrl, null);
  assert.match(server.output(), new RegExp(`listening +http://localhost:${server.port}`));

  // Opening the printed address in a browser must show something, not "Cannot GET /".
  const root = await fetch(server.base + '/');
  assert.strictEqual(root.status, 200);
  assert.match(root.headers.get('content-type') || '', /text\/html/);
  const page = await root.text();
  assert.match(page, /Webworks/);
  assert.ok(page.includes(`http://localhost:${server.port}`));
  assert.match(page, /\/health/);
});

test('--port moves the server when 3000 is taken, and a busy port explains itself', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  // Someone else already holds this port: the second instance must say so and stop.
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-busy-'));
  const busy = spawn(process.execPath, [SERVER], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, {
      WEBWORKS_ACCOUNT_MODE: 'local',
      WEBWORKS_NO_PROMPT: '1',
      WEBWORKS_DATA_DIR: dir,
      WEBWORKS_CONFIG: path.join(dir, 'webworks.config.json'),
      WEBWORKS_PORT: String(server.port),
      PORT: '',
    }),
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let out = '';
  busy.stdout.on('data', (d) => { out += d.toString(); });
  busy.stderr.on('data', (d) => { out += d.toString(); });
  const code = await new Promise((resolve) => {
    const timer = setTimeout(() => { busy.kill(); resolve('timeout'); }, 15000);
    busy.once('exit', (value) => { clearTimeout(timer); resolve(value); });
  });
  assert.strictEqual(code, 1, out);
  assert.match(out, /already in use/);
  assert.match(out, /npm start -- --port \d+/);

  // And --port actually moves it, without touching the config file.
  const moved = 43000 + Math.floor(Math.random() * 900);
  // The environment says port 1; --port must win over both that and the config file.
  const second = await startServer({ port: moved, args: ['--port', String(moved)], env: { WEBWORKS_PORT: '1' } });
  t.after(() => second.stop());
  const health = await json(`http://127.0.0.1:${moved}`, '/health');
  assert.strictEqual(health.status, 200);
  assert.ok(health.body.urls.includes(`http://localhost:${moved}`));
});

test('server logs stay minimal: startup summary only, no per-request noise', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const alex = await register(server.base, 'alex', 'Alex');
  await gameSession(server.base, alex.token, '100', 'Alex');
  await json(server.base, '/status');

  const out = server.output();
  const { version } = require('../package.json');
  assert.match(out, new RegExp(`WEBWORKS ${version.replace(/\./g, '\\.')}`));
  assert.match(out, /accounts {3}Local Accounts/);
  assert.doesNotMatch(out, /GuestSession/);
  assert.doesNotMatch(out, /\b(POST|GET) \//, 'request lines must not be logged');
  assert.ok(out.split('\n').length < 25, 'startup output must stay a short summary');
});
