const test = require('node:test');
const assert = require('node:assert');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

// Presence without a game socket and the bootstrap's lobby-invite inbox. Both are what
// make the friends page show "Online" and "X invited you" before a game has opened its
// realtime connection.

const SERVER = path.join(__dirname, '..', 'server.js');
const API = '/api/Unity/SteamworksNET';

function freePort() {
  return 44000 + Math.floor(Math.random() * 2000);
}

async function waitFor(url, timeoutMs = 8000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    try {
      const resp = await fetch(url);
      if (resp.ok) return resp;
    } catch (err) { /* not up yet */ }
    await new Promise((r) => setTimeout(r, 100));
  }
  throw new Error('server did not become ready');
}

async function startServer(env = {}) {
  const port = freePort();
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-presence-'));
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

async function call(base, pathname, { method = 'GET', body, token, account } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['x-webworks-session'] = token;
  if (account) headers['x-webworks-account'] = account;
  const resp = await fetch(base + pathname, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  return { status: resp.status, body: await resp.json().catch(() => null) };
}

async function register(base, username) {
  const resp = await call(base, '/accounts/Register', {
    method: 'POST',
    body: { username, password: 'hunter22', displayName: username },
  });
  assert.strictEqual(resp.status, 201, JSON.stringify(resp.body));
  return resp.body;
}

async function session(base, accountToken) {
  const resp = await call(base, `${API}/User/GuestSession`, {
    method: 'POST',
    body: { appId: '480', accountToken },
  });
  assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
  return resp.body;
}

async function befriend(base, a, b) {
  let resp = await call(base, '/accounts/Friends/Add', { method: 'POST', account: a.token, body: { username: b.account.username } });
  assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
  resp = await call(base, '/accounts/Friends/Accept', { method: 'POST', account: b.token, body: { username: a.account.username } });
  assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
}

test('a heartbeat keeps a signed-in player online without a realtime socket', async (t) => {
  const server = await startServer({ WEBWORKS_PING_TTL_MS: '1500' });
  t.after(() => server.stop());

  const alex = await register(server.base, 'alex');
  const bob = await register(server.base, 'bob');
  await session(server.base, alex.token);
  const bobSession = await session(server.base, bob.token);
  await befriend(server.base, alex, bob);

  // The bootstrap only holds the account token, so session routes accept it too.
  let beat = await call(server.base, `${API}/User/Heartbeat`, { method: 'POST', body: {}, token: bob.token });
  assert.strictEqual(beat.status, 200, JSON.stringify(beat.body));
  assert.strictEqual(beat.body.steamId, bobSession.steamId);
  assert.strictEqual(beat.body.online, true);

  let graph = await call(server.base, `${API}/Friends/Graph`, { token: alex.token });
  assert.strictEqual(graph.status, 200);
  assert.strictEqual(graph.body.accepted.length, 1);
  assert.strictEqual(graph.body.accepted[0].username, 'bob');
  assert.strictEqual(graph.body.accepted[0].personaState, 'Online');
  assert.strictEqual(graph.body.onlineCount, 1);

  // Going to another tab keeps heartbeating; closing the page sends /Offline.
  const gone = await call(server.base, `${API}/User/Offline`, { method: 'POST', body: {}, token: bob.token });
  assert.strictEqual(gone.status, 200);
  graph = await call(server.base, `${API}/Friends/Graph`, { token: alex.token });
  assert.strictEqual(graph.body.accepted[0].personaState, 'Offline');
  assert.strictEqual(graph.body.onlineCount, 0);

  // And without the explicit signal the heartbeat window is what expires.
  beat = await call(server.base, `${API}/User/Heartbeat`, { method: 'POST', body: {}, token: bob.token });
  assert.strictEqual(beat.body.cameOnline, true);
  await new Promise((r) => setTimeout(r, 1700));

  graph = await call(server.base, `${API}/Friends/Graph`, { token: alex.token });
  assert.strictEqual(graph.body.accepted[0].personaState, 'Offline', 'stale heartbeat should expire');

  const status = await call(server.base, `${API}/User/Status`, { token: alex.token });
  assert.strictEqual(status.status, 200);
  assert.strictEqual(status.body.online, 0, 'both heartbeats have expired');
});

test('a refreshed heartbeat does not flap the persona between Online and Offline', async (t) => {
  const server = await startServer({ WEBWORKS_PING_TTL_MS: '1200' });
  t.after(() => server.stop());

  const alex = await register(server.base, 'alex');
  const bob = await register(server.base, 'bob');
  await session(server.base, alex.token);
  await session(server.base, bob.token);
  await befriend(server.base, alex, bob);

  for (let i = 0; i < 4; i++) {
    const beat = await call(server.base, `${API}/User/Heartbeat`, { method: 'POST', body: {}, token: bob.token });
    assert.strictEqual(beat.status, 200);
    if (i > 0) assert.strictEqual(beat.body.cameOnline, false, 'already online, so no persona change');
    await new Promise((r) => setTimeout(r, 500));
  }

  const graph = await call(server.base, `${API}/Friends/Graph`, { token: alex.token });
  assert.strictEqual(graph.body.accepted[0].personaState, 'Online');
});

test('lobby invites land in the invited player\'s inbox and clear when used', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const alex = await register(server.base, 'alex');
  const bob = await register(server.base, 'bob');
  await session(server.base, alex.token);
  await session(server.base, bob.token);
  await befriend(server.base, alex, bob);

  const lobby = await call(server.base, `${API}/Matchmaking/CreateLobby`, { method: 'POST', token: alex.token, body: { maxMembers: 8 } });
  assert.strictEqual(lobby.status, 200, JSON.stringify(lobby.body));

  const graph = await call(server.base, `${API}/Friends/Graph`, { token: alex.token });
  const bobSteamId = graph.body.accepted[0].steamId;

  const self = await call(server.base, `${API}/Matchmaking/InviteFriend`, {
    method: 'POST',
    token: alex.token,
    body: { lobbyId: lobby.body.id, friendSteamId: 'someone-else-invited-me' },
  });
  assert.strictEqual(self.status, 200);

  const invite = await call(server.base, `${API}/Matchmaking/InviteFriend`, {
    method: 'POST',
    token: alex.token,
    body: { lobbyId: lobby.body.id, friendSteamId: bobSteamId },
  });
  assert.strictEqual(invite.status, 200, JSON.stringify(invite.body));

  // Inviting yourself is a mistake worth naming rather than a lobby entry.
  const me = await call(server.base, `${API}/Matchmaking/InviteFriend`, {
    method: 'POST',
    token: alex.token,
    body: { lobbyId: lobby.body.id, friendSteamId: (await call(server.base, `${API}/User/Profile`, { token: alex.token })).body.steamId },
  });
  assert.strictEqual(me.status, 400);
  assert.strictEqual(me.body.error, 'cannot_invite_yourself');

  let inbox = await call(server.base, `${API}/Matchmaking/Invites`, { token: bob.token });
  assert.strictEqual(inbox.status, 200);
  assert.strictEqual(inbox.body.invites.length, 1);
  assert.strictEqual(inbox.body.invites[0].lobbyId, lobby.body.id);
  assert.strictEqual(inbox.body.invites[0].fromName, 'alex');
  assert.strictEqual(inbox.body.invites[0].inviteToken, invite.body.token);

  const joined = await call(server.base, `${API}/Matchmaking/JoinLobby`, { method: 'POST', token: bob.token, body: { lobbyId: lobby.body.id } });
  assert.strictEqual(joined.status, 200, JSON.stringify(joined.body));

  inbox = await call(server.base, `${API}/Matchmaking/Invites`, { token: bob.token });
  assert.strictEqual(inbox.body.invites.length, 0, 'joining the lobby clears the invite');

  // Re-invite, then dismiss it by hand.
  await call(server.base, `${API}/Matchmaking/InviteFriend`, { method: 'POST', token: alex.token, body: { lobbyId: lobby.body.id, friendSteamId: bobSteamId } });
  inbox = await call(server.base, `${API}/Matchmaking/Invites`, { token: bob.token });
  assert.strictEqual(inbox.body.invites.length, 1);
  const dismissed = await call(server.base, `${API}/Matchmaking/Invites/Dismiss`, { method: 'POST', token: bob.token, body: { lobbyId: lobby.body.id } });
  assert.strictEqual(dismissed.body.removed, 1);
  inbox = await call(server.base, `${API}/Matchmaking/Invites`, { token: bob.token });
  assert.strictEqual(inbox.body.invites.length, 0);

  // A player who was never invited sees nothing.
  const carol = await register(server.base, 'carol');
  await session(server.base, carol.token);
  inbox = await call(server.base, `${API}/Matchmaking/Invites`, { token: carol.token });
  assert.deepStrictEqual(inbox.body.invites, []);
});
