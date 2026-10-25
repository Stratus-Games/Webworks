const test = require('node:test');
const assert = require('node:assert');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const http = require('node:http');
const os = require('node:os');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const WebSocket = require('ws');

// Runs the real GWAS worker in-process behind a tiny Node HTTP adapter, then points a
// Webworks server at it. This exercises the actual account, achievement, inventory,
// cloud-save and friend-lobby paths rather than mocking the worker.

const SERVER = path.join(__dirname, '..', 'server.js');

// The worker ships next to the server as GWAS/. Older checkouts kept it one level above
// the project root, so that layout is still accepted, and WEBWORKS_GWAS_DIR points at a
// copy that lives anywhere else.
const ROOT = path.join(__dirname, '..', '..');
const GWAS_DIR = [
  process.env.WEBWORKS_GWAS_DIR,
  process.env.WEBWORKS_GWAS_ACCOUNTS_DIR,
  path.join(ROOT, 'GWAS'),
  path.join(ROOT, '..', 'GWAS'),
].filter(Boolean).find((dir) => fs.existsSync(path.join(dir, 'src', 'index.js')));

if (!GWAS_DIR) {
  throw new Error('GWAS worker not found: expected GWAS/ beside Server/ or set WEBWORKS_GWAS_DIR');
}
const API = '/api/Unity/SteamworksNET';

function freePort() {
  return 45000 + Math.floor(Math.random() * 2000);
}

async function waitFor(url, timeoutMs = 10000) {
  const start = Date.now();
  let lastErr;
  while (Date.now() - start < timeoutMs) {
    try {
      const resp = await fetch(url);
      if (resp.ok) return resp;
    } catch (err) {
      lastErr = err;
    }
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error('server did not become ready: ' + (lastErr && lastErr.message));
}

async function startGwas() {
  const { default: worker } = await import(pathToFileURL(path.join(GWAS_DIR, 'src', 'index.js')).href);
  const { MemoryStore } = await import(pathToFileURL(path.join(GWAS_DIR, 'src', 'store.js')).href);
  const env = { __store: new MemoryStore() };

  const server = http.createServer(async (req, res) => {
    try {
      const chunks = [];
      for await (const chunk of req) chunks.push(chunk);
      const body = chunks.length ? Buffer.concat(chunks) : undefined;
      const response = await worker.fetch(
        new Request(`http://127.0.0.1${req.url}`, {
          method: req.method,
          headers: req.headers,
          body: req.method === 'GET' || req.method === 'HEAD' ? undefined : body,
        }),
        env,
      );
      res.writeHead(response.status, Object.fromEntries(response.headers));
      res.end(Buffer.from(await response.arrayBuffer()));
    } catch (err) {
      res.writeHead(500);
      res.end(String(err && err.message));
    }
  });

  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  return { server, url: `http://127.0.0.1:${server.address().port}` };
}

async function startServer(gwasUrl) {
  const port = freePort();
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-gwas-'));
  const child = spawn(process.execPath, [SERVER], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, {
      WEBWORKS_ACCOUNT_MODE: 'gwas',
      WEBWORKS_GWAS_URL: gwasUrl,
      WEBWORKS_NO_PROMPT: '1',
      WEBWORKS_DATA_DIR: dir,
      WEBWORKS_PORT: String(port),
      WEBWORKS_LOG: 'verbose',
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
    async stop() {
      child.kill('SIGTERM');
      await new Promise((resolve) => {
        child.once('exit', resolve);
        setTimeout(resolve, 2000);
      });
    },
  };
}

async function json(base, pathname, options = {}) {
  const headers = Object.assign({ 'Content-Type': 'application/json' }, options.headers || {});
  const resp = await fetch(base + pathname, Object.assign({}, options, { headers }));
  return { status: resp.status, body: await resp.json().catch(() => null) };
}

const session = (token) => ({ 'x-webworks-session': token });

// Presence (and therefore friends-active) keys off a live realtime socket.
function connectRealtime(base, token) {
  return new Promise((resolve, reject) => {
    const socket = new WebSocket(`${base.replace(/^http/, 'ws')}/realtime?session=${encodeURIComponent(token)}`);
    socket.once('open', () => resolve(socket));
    socket.once('error', reject);
  });
}

test('GWAS mode: accounts, achievements, inventory, saves, persona and friend lobbies', async (t) => {
  const gwas = await startGwas();
  const server = await startServer(gwas.url);
  const sockets = [];
  t.after(async () => {
    for (const socket of sockets) socket.close();
    await server.stop();
    await new Promise((resolve) => gwas.server.close(resolve));
  });

  const appId = '480';

  // Setup asked nothing, and the claim really happened against the worker: this is the whole
  // first-run story, end to end, with no password anywhere.
  const claimed = await json(server.base, '/health');
  assert.strictEqual(claimed.body.accounts, 'gwas');
  assert.match(server.output(), /claimed with GWAS/);

  async function account(username) {
    const registered = await json(server.base, '/accounts/Register', {
      method: 'POST',
      body: JSON.stringify({ username, password: 'hunter22', displayName: username.toUpperCase() }),
    });
    assert.strictEqual(registered.status, 201, JSON.stringify(registered.body));
    return registered.body;
  }

  async function game(accountToken) {
    const resp = await json(server.base, `${API}/User/GuestSession`, {
      method: 'POST',
      body: JSON.stringify({ appId, accountToken }),
    });
    assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
    return resp.body;
  }

  const alex = await account('alex');
  const alexGame = await game(alex.token);
  assert.strictEqual(alexGame.linkedAccount, 'alex');
  assert.strictEqual(alexGame.accountSystem, 'gwas');
  sockets.push(await connectRealtime(server.base, alexGame.sessionToken));

  await t.test('achievements live on the account service', async () => {
    const defined = await json(server.base, `${API}/UserStats/Achievements/Define`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({
        achievements: [
          { key: 'ACH_FIRST', name: 'First', description: 'Do the thing' },
          { key: 'ACH_TEN', name: 'Ten', maxProgress: 10 },
        ],
      }),
    });
    assert.strictEqual(defined.status, 200, JSON.stringify(defined.body));
    assert.deepStrictEqual(defined.body.added, [1, 2]);
    assert.strictEqual(defined.body.achievements[0].key, 'ACH_FIRST');

    const unlock = await json(server.base, `${API}/UserStats/Achievements/Unlock`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ key: 'ACH_FIRST' }),
    });
    assert.strictEqual(unlock.status, 200, JSON.stringify(unlock.body));
    assert.strictEqual(unlock.body.achievement.achievementId, 1);
    assert.strictEqual(unlock.body.achievement.unlocked, true);

    const progress = await json(server.base, `${API}/UserStats/AchievementProgress`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ name: 'ACH_TEN', current: 4, max: 10 }),
    });
    assert.strictEqual(progress.status, 200, JSON.stringify(progress.body));

    const legacy = await json(server.base, `${API}/UserStats/Achievements`, { headers: session(alexGame.sessionToken) });
    const first = legacy.body.achievements.find((a) => a.id === 'ACH_FIRST');
    assert.strictEqual(first.numericId, 1);
    assert.strictEqual(first.unlocked, true);
    const ten = legacy.body.achievements.find((a) => a.id === 'ACH_TEN');
    assert.strictEqual(ten.progress, 4);
    assert.strictEqual(ten.unlocked, false);

    const profile = await json(server.base, `${API}/UserStats/Achievements/Profile`, { headers: session(alexGame.sessionToken) });
    assert.strictEqual(profile.body.total, 1);
    assert.deepStrictEqual(profile.body.games.map((g) => g.appId), [appId]);

    const catalog = await json(server.base, `${API}/UserStats/Achievements/Catalog`, { headers: session(alexGame.sessionToken) });
    assert.deepStrictEqual(catalog.body.achievements.map((a) => [a.id, a.key]), [[1, 'ACH_FIRST'], [2, 'ACH_TEN']]);

    const stats = await json(server.base, `${API}/UserStats/Stats`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ stats: { kills: 7 } }),
    });
    assert.strictEqual(stats.status, 200, JSON.stringify(stats.body));

    const read = await json(server.base, `${API}/UserStats/Stats`, { headers: session(alexGame.sessionToken) });
    assert.strictEqual(read.body.stats.kills, 7);

    const count = await json(server.base, `${API}/UserStats/PlayerCount`, { headers: session(alexGame.sessionToken) });
    assert.strictEqual(count.body.count, 1);

    // The achievement really is in the worker, not just echoed back by the server.
    const remote = await json(gwas.url, `/achievements?appId=${appId}`, { headers: { Authorization: `Bearer ${alex.token}` } });
    assert.strictEqual(remote.body.achievements.length, 2);
    assert.strictEqual(remote.body.achievements.find((a) => a.key === 'ACH_FIRST').unlocked, true);
  });

  await t.test('inventory lives on the account service', async () => {
    const logOn = await json(server.base, `${API}/GameServer/LogOn`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({}),
    });
    assert.strictEqual(logOn.status, 200, JSON.stringify(logOn.body));
    const serverSession = logOn.body.sessionToken;

    for (const def of [
      { itemDefId: 'case', name: 'Case', type: 'container', clientGrantable: true, tradable: true },
      { itemDefId: 'key', name: 'Key', type: 'item', tradable: true },
    ]) {
      const defined = await json(server.base, `${API}/Inventory/Define`, {
        method: 'POST',
        headers: session(serverSession),
        body: JSON.stringify(def),
      });
      assert.strictEqual(defined.status, 200, JSON.stringify(defined.body));
    }

    const grant = await json(server.base, `${API}/Inventory/GrantSelf`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ itemDefId: 'case', quantity: 3 }),
    });
    assert.strictEqual(grant.status, 200, JSON.stringify(grant.body));
    assert.strictEqual(grant.body.granted[0].quantity, 3);

    const consume = await json(server.base, `${API}/Inventory/Consume`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ itemDefId: 'case', quantity: 1 }),
    });
    assert.strictEqual(consume.body.remaining, 2);

    const exchange = await json(server.base, `${API}/Inventory/Exchange`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ destroy: [{ itemDefId: 'case', quantity: 1 }], generate: [{ itemDefId: 'key', quantity: 2 }] }),
    });
    assert.strictEqual(exchange.status, 200, JSON.stringify(exchange.body));

    const properties = await json(server.base, `${API}/Inventory/Properties`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ itemDefId: 'key', properties: { shiny: 'yes' } }),
    });
    assert.strictEqual(properties.status, 200, JSON.stringify(properties.body));

    const items = await json(server.base, `${API}/Inventory/Items`, { headers: session(alexGame.sessionToken) });
    const key = items.body.items.find((i) => i.itemDefId === 'key');
    assert.strictEqual(key.quantity, 2);
    assert.strictEqual(key.properties.shiny, 'yes');
    const cases = items.body.items.find((i) => i.itemDefId === 'case');
    assert.strictEqual(cases.quantity, 1);

    const inspectToken = `item:${Buffer.from(JSON.stringify({ appId, itemDefId: 'key' })).toString('base64url')}`;
    const inspect = await json(server.base, `${API}/Inventory/Inspect`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ token: inspectToken }),
    });
    assert.strictEqual(inspect.status, 200, JSON.stringify(inspect.body));
    assert.strictEqual(inspect.body.item.definition.name, 'Key');
  });

  await t.test('cloud saves go to GWAS', async () => {
    const payload = Buffer.from('remote-save-data').toString('base64');
    const write = await json(server.base, `${API}/RemoteStorage/Write`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ name: 'profile.sav', data: payload }),
    });
    assert.strictEqual(write.status, 200, JSON.stringify(write.body));
    assert.strictEqual(write.body.quota, 5 * 1024 * 1024);

    const files = await json(server.base, `${API}/RemoteStorage/Files`, { headers: session(alexGame.sessionToken) });
    assert.strictEqual(files.body.files.length, 1);
    assert.strictEqual(files.body.files[0].name, 'profile.sav');
    assert.strictEqual(files.body.files[0].data, payload);

    const remote = await json(gwas.url, `/storage/files?appId=${appId}`, { headers: { Authorization: `Bearer ${alex.token}` } });
    assert.strictEqual(remote.body.files[0].name, 'profile.sav');

    const removed = await json(server.base, `${API}/RemoteStorage/Delete`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ name: 'profile.sav' }),
    });
    assert.strictEqual(removed.status, 200);
    assert.strictEqual(removed.body.used, 0);
  });

  await t.test('persona state is stored on the account', async () => {
    const changed = await json(server.base, `${API}/User/PersonaState`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ state: 'Away', name: 'Alex Away' }),
    });
    assert.strictEqual(changed.status, 200, JSON.stringify(changed.body));
    assert.strictEqual(changed.body.personaState, 'Away');

    const local = await json(server.base, `${API}/User/PersonaState`, { headers: session(alexGame.sessionToken) });
    assert.strictEqual(local.body.personaState, 'Away');
    assert.strictEqual(local.body.personaName, 'Alex Away');

    const remote = await json(gwas.url, '/accounts/me', { headers: { Authorization: `Bearer ${alex.token}` } });
    assert.strictEqual(remote.body.account.personaState, 'Away');

    const invalid = await json(server.base, `${API}/User/PersonaState`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ state: 'Dancing' }),
    });
    assert.strictEqual(invalid.status, 400);
  });

  await t.test('friends see each other and can join private lobbies', async () => {
    const bob = await account('bob');
    const bobGame = await game(bob.token);
    sockets.push(await connectRealtime(server.base, bobGame.sessionToken));

    const added = await json(server.base, `${API}/Friends/Add`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ username: 'bob' }),
    });
    assert.strictEqual(added.status, 200, JSON.stringify(added.body));

    const accepted = await json(server.base, `${API}/Friends/Accept`, {
      method: 'POST',
      headers: session(bobGame.sessionToken),
      body: JSON.stringify({ username: 'alex' }),
    });
    assert.strictEqual(accepted.status, 200, JSON.stringify(accepted.body));

    // Bob sees Alex's account persona, not just a hardcoded Online.
    const bobFriends = await json(gwas.url, '/accounts/friends', { headers: { Authorization: `Bearer ${bob.token}` } });
    assert.strictEqual(bobFriends.body.accepted[0].username, 'alex');
    assert.strictEqual(bobFriends.body.accepted[0].personaState, 'Away');

    const created = await json(server.base, `${API}/Matchmaking/CreateLobby`, {
      method: 'POST',
      headers: session(alexGame.sessionToken),
      body: JSON.stringify({ maxMembers: 8 }),
    });
    assert.strictEqual(created.status, 200, JSON.stringify(created.body));
    const lobbyId = created.body.id;

    for (const [route, body] of [
      ['LobbyVisibility', { lobbyId, mode: 'private' }],
      ['LobbyJoinable', { lobbyId, joinable: false }],
      ['LobbyData', { lobbyId, key: 'mode', value: 'coop' }],
    ]) {
      const resp = await json(server.base, `${API}/Matchmaking/${route}`, {
        method: 'POST',
        headers: session(alexGame.sessionToken),
        body: JSON.stringify(body),
      });
      assert.strictEqual(resp.status, 200, `${route}: ${JSON.stringify(resp.body)}`);
    }

    // A stranger cannot see it.
    const craig = await account('craig');
    const craigGame = await game(craig.token);
    const strangerList = await json(server.base, `${API}/Matchmaking/LobbyList`, {
      method: 'POST',
      headers: session(craigGame.sessionToken),
      body: JSON.stringify({}),
    });
    assert.strictEqual(strangerList.body.lobbies.length, 0);

    // The friend can, and it is flagged as a friend lobby.
    const friendList = await json(server.base, `${API}/Matchmaking/LobbyList`, {
      method: 'POST',
      headers: session(bobGame.sessionToken),
      body: JSON.stringify({}),
    });
    const found = friendList.body.lobbies.find((l) => String(l.id) === String(lobbyId));
    assert.ok(found, JSON.stringify(friendList.body));
    assert.strictEqual(found.friend, true);
    assert.strictEqual(found.visibility, 'private');

    const joined = await json(server.base, `${API}/Matchmaking/JoinLobby`, {
      method: 'POST',
      headers: session(bobGame.sessionToken),
      body: JSON.stringify({ lobbyId }),
    });
    assert.strictEqual(joined.status, 200, JSON.stringify(joined.body));
    assert.ok(joined.body.members.some((m) => String(m.steamId) === String(bobGame.steamId)));

    const active = await json(server.base, `${API}/Matchmaking/FriendsActive`, { headers: session(bobGame.sessionToken) });
    assert.ok(active.body.friends.some((f) => f.username === 'alex'));

    // A stranger still cannot join.
    const blocked = await json(server.base, `${API}/Matchmaking/JoinLobby`, {
      method: 'POST',
      headers: session(craigGame.sessionToken),
      body: JSON.stringify({ lobbyId }),
    });
    assert.strictEqual(blocked.status, 403);
  });

  await t.test('guest sessions in GWAS mode still cannot track achievements', async () => {
    const guest = await json(server.base, `${API}/User/GuestSession`, {
      method: 'POST',
      body: JSON.stringify({ appId, name: 'Guest' }),
    });
    assert.strictEqual(guest.status, 200);

    const denied = await json(server.base, `${API}/UserStats/Achievements/Unlock`, {
      method: 'POST',
      headers: session(guest.body.sessionToken),
      body: JSON.stringify({ key: 'ACH_FIRST' }),
    });
    assert.strictEqual(denied.status, 501);
    assert.strictEqual(denied.body.error, 'achievements_local_only');
  });
});
