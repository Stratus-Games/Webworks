const test = require('node:test');
const assert = require('node:assert');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const SERVER = path.join(__dirname, '..', 'server.js');
const API = '/api/Unity/SteamworksNET';

function freePort() {
  return 44000 + Math.floor(Math.random() * 2000);
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

async function startServer({ dataDir, mode = 'local' } = {}) {
  const port = freePort();
  const dir = dataDir || fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-ach-'));
  const child = spawn(process.execPath, [SERVER], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, {
      WEBWORKS_ACCOUNT_MODE: mode,
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
    dir,
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

async function register(base, username) {
  const resp = await json(base, '/accounts/Register', {
    method: 'POST',
    body: JSON.stringify({ username, password: 'hunter22', displayName: username }),
  });
  assert.strictEqual(resp.status, 201, JSON.stringify(resp.body));
  return resp.body;
}

async function gameSession(base, accountToken, appId) {
  const resp = await json(base, `${API}/User/GuestSession`, {
    method: 'POST',
    body: JSON.stringify({ appId, accountToken, name: 'Alex' }),
  });
  assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
  return resp.body;
}

const session = (token) => ({ 'x-webworks-session': token });

test('achievement catalog assigns stable numeric ids per game', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const alex = await register(server.base, 'alex');
  const game = await gameSession(server.base, alex.token, '9001');

  const defined = await json(server.base, `${API}/UserStats/Achievements/Define`, {
    method: 'POST', headers: session(game.sessionToken),
    body: JSON.stringify({ achievements: [{ key: 'ACH_FIRST_BLOOD', name: 'First Blood' }, { key: 'ACH_WIN', name: 'Winner' }] }),
  });
  assert.strictEqual(defined.status, 200, JSON.stringify(defined.body));
  assert.deepStrictEqual(defined.body.achievements.map((a) => [a.id, a.key]), [[1, 'ACH_FIRST_BLOOD'], [2, 'ACH_WIN']]);

  // Redefining an existing key keeps its number, and new keys continue from there.
  const again = await json(server.base, `${API}/UserStats/Achievements/Define`, {
    method: 'POST', headers: session(game.sessionToken),
    body: JSON.stringify({ achievements: [{ key: 'ACH_WIN', name: 'Winner!' }, { key: 'ACH_SECRET', name: 'Secret', hidden: true }] }),
  });
  assert.strictEqual(again.status, 200);
  const win = again.body.achievements.find((a) => a.key === 'ACH_WIN');
  assert.strictEqual(win.id, 2);
  assert.strictEqual(win.name, 'Winner!');
  assert.strictEqual(again.body.achievements.find((a) => a.key === 'ACH_SECRET').id, 3);

  // A different game has its own numbering.
  const other = await gameSession(server.base, alex.token, '9002');
  const catalogTwo = await json(server.base, `${API}/UserStats/Achievements/Catalog`, { headers: session(other.sessionToken) });
  assert.deepStrictEqual(catalogTwo.body.achievements, []);
});

test('unlocking stores the game id and achievement number against the user', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const alex = await register(server.base, 'alex');
  const game = await gameSession(server.base, alex.token, '9100');
  await json(server.base, `${API}/UserStats/Achievements/Define`, {
    method: 'POST', headers: session(game.sessionToken),
    body: JSON.stringify({ achievements: [{ key: 'ACH_BOSS', name: 'Boss' }, { key: 'ACH_COINS', name: 'Coins', maxProgress: 10 }] }),
  });

  const unlock = await json(server.base, `${API}/UserStats/Achievements/Unlock`, {
    method: 'POST', headers: session(game.sessionToken), body: JSON.stringify({ key: 'ACH_BOSS' }),
  });
  assert.strictEqual(unlock.status, 200, JSON.stringify(unlock.body));
  assert.strictEqual(unlock.body.achievement.appId, '9100');
  assert.strictEqual(unlock.body.achievement.achievementId, 1);
  assert.ok(unlock.body.achievement.unlockTime > 0);

  const byId = await json(server.base, `${API}/UserStats/Achievements/Unlock`, {
    method: 'POST', headers: session(game.sessionToken), body: JSON.stringify({ id: 2, progress: 10, max: 10 }),
  });
  assert.strictEqual(byId.status, 200);
  assert.strictEqual(byId.body.achievement.achievementId, 2);

  const earned = await json(server.base, `${API}/UserStats/Achievements/Earned`, { headers: session(game.sessionToken) });
  assert.strictEqual(earned.body.achievements.length, 2);
  assert.deepStrictEqual(earned.body.achievements.map((a) => a.achievementId).sort(), [1, 2]);

  // The legacy list exposes the number next to the name the old client uses.
  const legacy = await json(server.base, `${API}/UserStats/Achievements`, { headers: session(game.sessionToken) });
  const boss = legacy.body.achievements.find((a) => a.id === 'ACH_BOSS');
  assert.strictEqual(boss.numericId, 1);
  assert.strictEqual(boss.unlocked, true);

  const profile = await json(server.base, `${API}/UserStats/Achievements/Profile`, { headers: session(game.sessionToken) });
  assert.strictEqual(profile.body.total, 2);
  assert.deepStrictEqual(profile.body.games.map((g) => g.appId), ['9100']);
  assert.deepStrictEqual(profile.body.games[0].achievements.slice().sort((a, b) => a - b), [1, 2]);

  const unknown = await json(server.base, `${API}/UserStats/Achievements/Unlock`, {
    method: 'POST', headers: session(game.sessionToken), body: JSON.stringify({ key: 'ACH_MISSING' }),
  });
  assert.strictEqual(unknown.status, 404);
});

test('achievements survive a restart and keep their numbers', async (t) => {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-ach-persist-'));
  const first = await startServer({ dataDir: dir });
  const alex = await register(first.base, 'alex');
  const game = await gameSession(first.base, alex.token, '9200');
  await json(first.base, `${API}/UserStats/Achievements/Define`, {
    method: 'POST', headers: session(game.sessionToken),
    body: JSON.stringify({ achievements: [{ key: 'ACH_ONE', name: 'One' }] }),
  });
  await json(first.base, `${API}/UserStats/Achievements/Unlock`, {
    method: 'POST', headers: session(game.sessionToken), body: JSON.stringify({ key: 'ACH_ONE' }),
  });
  await first.stop();

  const second = await startServer({ dataDir: dir });
  t.after(() => second.stop());

  const relogin = await json(second.base, '/accounts/Login', {
    method: 'POST', body: JSON.stringify({ username: 'alex', password: 'hunter22' }),
  });
  const gameTwo = await gameSession(second.base, relogin.body.token, '9200');

  const catalog = await json(second.base, `${API}/UserStats/Achievements/Catalog`, { headers: session(gameTwo.sessionToken) });
  assert.strictEqual(catalog.body.achievements[0].id, 1);

  const earned = await json(second.base, `${API}/UserStats/Achievements/Earned`, { headers: session(gameTwo.sessionToken) });
  assert.strictEqual(earned.body.achievements.length, 1);
  assert.strictEqual(earned.body.achievements[0].achievementId, 1);
  assert.strictEqual(earned.body.achievements[0].appId, '9200');

  const added = await json(second.base, `${API}/UserStats/Achievements/Define`, {
    method: 'POST', headers: session(gameTwo.sessionToken),
    body: JSON.stringify({ achievements: [{ key: 'ACH_TWO', name: 'Two' }] }),
  });
  assert.strictEqual(added.body.achievements.find((a) => a.key === 'ACH_TWO').id, 2);
});

test('achievement tracking is local-accounts only', async (t) => {
  const server = await startServer({ mode: 'gwas' });
  t.after(() => server.stop());

  const game = await gameSession(server.base, null, '9300');
  const defined = await json(server.base, `${API}/UserStats/Achievements/Define`, {
    method: 'POST', headers: session(game.sessionToken),
    body: JSON.stringify({ achievements: [{ key: 'ACH_X', name: 'X' }] }),
  });
  assert.strictEqual(defined.status, 501);
  assert.strictEqual(defined.body.error, 'achievements_local_only');

  const unlock = await json(server.base, `${API}/UserStats/Achievements/Unlock`, {
    method: 'POST', headers: session(game.sessionToken), body: JSON.stringify({ key: 'ACH_X' }),
  });
  assert.strictEqual(unlock.status, 501);
});
