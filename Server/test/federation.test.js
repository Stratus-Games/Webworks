const test = require('node:test');
const assert = require('node:assert');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const http = require('node:http');
const net = require('node:net');
const os = require('node:os');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const WebSocket = require('ws');

const SERVER = path.join(__dirname, '..', 'server.js');
const KEY = 'test-gwas-server-key';
const MOUNT = '/api/Unity/SteamworksNET';

const tickets = require('../src/federation/tickets');
const targets = require('../src/federation/targets');
const federation = require('../src/federation');

// --- helpers -----------------------------------------------------------------

function freePort() {
  return new Promise((resolve) => {
    const srv = net.createServer();
    srv.listen(0, '127.0.0.1', () => {
      const port = srv.address().port;
      srv.close(() => resolve(port));
    });
  });
}

async function waitForHttp(url, timeoutMs = 15000) {
  const start = Date.now();
  let last;
  while (Date.now() - start < timeoutMs) {
    try {
      const resp = await fetch(url);
      if (resp.ok) return resp;
    } catch (err) {
      last = err;
    }
    await new Promise((r) => setTimeout(r, 100));
  }
  throw new Error('server did not become ready: ' + (last && last.message));
}

async function waitUntil(fn, timeoutMs = 8000, label = 'condition') {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    if (await fn()) return;
    await new Promise((r) => setTimeout(r, 100));
  }
  throw new Error(`timed out waiting for ${label}`);
}

// A GWAS directory served over plain HTTP, using the worker's real core so the
// node client and the worker cannot drift apart without a test failing.
async function startDirectory() {
  const { Directory } = await import(pathToFileURL(path.join(__dirname, '..', '..', 'GWAS', 'src', 'directory.js')).href);
  const directory = new Directory({ allowInsecure: true });

  const server = http.createServer(async (req, res) => {
    const send = (status, body) => {
      res.writeHead(status, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify(body));
    };
    if (req.headers['x-gwas-server-key'] !== KEY) return send(401, { error: 'invalid_server_key' });

    let raw = '';
    for await (const chunk of req) raw += chunk;
    let body = {};
    if (raw) {
      try { body = JSON.parse(raw); } catch { return send(400, { error: 'bad_json' }); }
    }

    if (req.url === '/directory/heartbeat') {
      const result = directory.register(body);
      return send(result.status, result.body);
    }
    if (req.url === '/directory/rendezvous') {
      const result = await directory.rendezvous(body);
      return send(result.status, result.body);
    }
    if (req.url === '/directory/leave') {
      const result = directory.leave(body);
      return send(result.status, result.body);
    }
    return send(404, { error: 'not_found' });
  });

  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const url = `http://127.0.0.1:${server.address().port}`;
  return { directory, url, close: () => new Promise((resolve) => server.close(resolve)) };
}

async function startServer(directoryUrl, extraEnv = {}) {
  const port = await freePort();
  const dataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-fed-'));

  const child = spawn(process.execPath, [SERVER], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, {
      WEBWORKS_ACCOUNT_MODE: 'local',
      WEBWORKS_NO_PROMPT: '1',
      WEBWORKS_DATA_DIR: dataDir,
      WEBWORKS_PORT: String(port),
      WEBWORKS_CONFIG: path.join(dataDir, 'webworks.config.json'),
      WEBWORKS_LOG: 'normal',
      WEBWORKS_NETWORKING: 'gwas',
      WEBWORKS_PUBLIC_URL: `http://127.0.0.1:${port}`,
      WEBWORKS_GWAS_URL: directoryUrl,
      WEBWORKS_GWAS_SERVER_KEY: KEY,
      WEBWORKS_FEDERATION_ALLOW_INSECURE: '1',
      WEBWORKS_FEDERATION_HEARTBEAT_MS: '30000',
      PORT: '',
    }, extraEnv),
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  let output = '';
  child.stdout.on('data', (d) => { output += d.toString(); });
  child.stderr.on('data', (d) => { output += d.toString(); });

  const base = `http://127.0.0.1:${port}`;
  await waitForHttp(`${base}/health`);

  return {
    base,
    port,
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

function request(base, pathname, { method = 'POST', token, body, headers = {} } = {}) {
  const init = { method, headers: Object.assign({}, headers) };
  if (body !== undefined) {
    init.headers['Content-Type'] = 'application/json';
    init.body = JSON.stringify(body);
  }
  if (token) init.headers['x-webworks-session'] = token;
  return fetch(base + pathname, init).then(async (resp) => ({
    status: resp.status,
    body: await resp.json().catch(() => null),
  }));
}

async function guest(base, name) {
  const resp = await request(base, `${MOUNT}/User/GuestSession`, { body: { appId: '480', name } });
  assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
  return resp.body;
}

function openRealtime(base, token) {
  return new Promise((resolve, reject) => {
    const socket = new WebSocket(base.replace('http', 'ws') + '/realtime?session=' + encodeURIComponent(token));
    const messages = [];
    socket.on('message', (raw) => {
      try { messages.push(JSON.parse(raw.toString())); } catch {  }
    });
    const timer = setTimeout(() => reject(new Error('realtime socket did not open')), 5000);
    socket.once('open', () => {
      clearTimeout(timer);
      resolve({
        socket,
        messages,
        close: () => socket.close(),
        next: async (type, timeoutMs = 5000) => {
          const start = Date.now();
          while (Date.now() - start < timeoutMs) {
            const found = messages.find((m) => m && m.type === type);
            if (found) return found;
            await new Promise((r) => setTimeout(r, 50));
          }
          throw new Error(`no ${type} push arrived; saw ${JSON.stringify(messages)}`);
        },
      });
    });
    socket.once('error', (err) => {
      clearTimeout(timer);
      reject(err);
    });
  });
}

function health(server) {
  return request(server.base, '/health', { method: 'GET' }).then((r) => r.body);
}

// --- tickets and targets -----------------------------------------------------

test('tickets are signed for one pair of servers and expire', () => {
  const secret = 'a'.repeat(43);
  const issued = tickets.issue(secret, { from: 'srv_a', to: 'srv_b', appId: '480', lobbyId: 'lobby_1', ttlMs: 60000 });

  const claims = tickets.verify(secret, issued.ticket);
  assert.ok(claims, 'a fresh ticket verifies');
  assert.strictEqual(claims.from, 'srv_a');
  assert.strictEqual(claims.to, 'srv_b');
  assert.strictEqual(claims.lobbyId, 'lobby_1');

  assert.strictEqual(tickets.verify('b'.repeat(43), issued.ticket), null, 'another secret cannot verify it');
  assert.strictEqual(tickets.verify(secret, issued.ticket.slice(0, -2) + 'AA'), null, 'a mangled signature fails');
  assert.strictEqual(tickets.verify(secret, 'not-a-ticket'), null);

  const expired = tickets.issue(secret, { from: 'srv_a', to: 'srv_b', appId: '480', lobbyId: 'lobby_1', ttlMs: 1000 });
  assert.strictEqual(tickets.verify(secret, expired.ticket, Date.now() + 5000), null, 'expired tickets fail');

  const cache = new tickets.NonceCache();
  assert.strictEqual(cache.take(claims.nonce, claims.exp), true);
  assert.strictEqual(cache.take(claims.nonce, claims.exp), false, 'a ticket cannot be replayed');
});

test('bridge targets are only public, secure and credential-free unless in dev mode', async () => {
  assert.strictEqual(targets.parseBridgeUrl('http://game.example.com/federation/bridge').ok, false);
  assert.strictEqual(targets.parseBridgeUrl('wss://user:pass@game.example.com/bridge').ok, false);
  assert.strictEqual(targets.parseBridgeUrl('wss://127.0.0.1/bridge').ok, false);
  assert.strictEqual(targets.parseBridgeUrl('wss://10.1.2.3/bridge').ok, false);
  assert.strictEqual(targets.parseBridgeUrl('wss://169.254.169.254/latest/meta-data').ok, false);
  assert.strictEqual(targets.parseBridgeUrl('wss://[::1]/bridge').ok, false);
  assert.strictEqual(targets.parseBridgeUrl('wss://metadata.google.internal/bridge').ok, false);
  assert.strictEqual(targets.parseBridgeUrl('wss://game.example.com/bridge').ok, true);

  assert.strictEqual((await targets.assertSafeBridgeUrl('wss://localhost/bridge')).ok, false);
  assert.strictEqual((await targets.assertSafeBridgeUrl('ws://127.0.0.1:3000/bridge', { allowInsecure: true })).ok, true);

  const allowed = { allowedHosts: ['*.example.com'] };
  assert.strictEqual(targets.parseBridgeUrl('wss://game.example.com/bridge', allowed).ok, true);
  assert.strictEqual(targets.parseBridgeUrl('wss://example.net/bridge', allowed).ok, false);
});

test('a peer cannot reach past the lobby its ticket granted', () => {
  const federation = require('../src/federation');
  const grant = { appId: '480', lobbyId: 'lobby_a' };
  const player = { steamId: '76561190000000001', appId: '480' };

  const ok = federation.validateRequest(grant, { id: '1', path: '/JoinLobby', body: { lobbyId: 'lobby_a' }, player });
  assert.strictEqual(ok.error, undefined);
  assert.strictEqual(ok.op, 'JoinLobby');

  const otherLobby = federation.validateRequest(grant, { id: '2', path: '/JoinLobby', body: { lobbyId: 'lobby_b' }, player });
  assert.strictEqual(otherLobby.error, 'lobby_not_federated', 'a ticket for one lobby is not a ticket for another');

  const otherApp = federation.validateRequest(grant, { id: '3', path: '/Lobby', query: { lobbyId: 'lobby_a' }, player: { ...player, appId: '570' } });
  assert.strictEqual(otherApp.error, 'app_mismatch');

  for (const path of ['/User/Profile', '/GameServer/LogOn', '/RemoteStorage/File', '/Matchmaking/JoinLobby/../../User', '/Accounts/Login']) {
    assert.strictEqual(
      federation.validateRequest(grant, { id: '4', path, body: { lobbyId: 'lobby_a' }, player }).error,
      'operation_not_allowed',
      path,
    );
  }

  assert.strictEqual(federation.validateRequest(grant, { id: '5', path: '/Lobby', body: { lobbyId: 'lobby_a' } }).error, 'bad_player');
  assert.strictEqual(federation.validateRequest(grant, { id: '6', path: '/Lobby', body: { lobbyId: 'lobby_a' }, player: { steamId: 'x y', appId: '480' } }).error, 'bad_player');
});

test('a GWAS without directory routes disables networking instead of breaking lobbies', async () => {
  const { DirectoryClient } = require('../src/federation/directory');
  const respond = (status, body) => async () => new Response(JSON.stringify(body), { status, headers: { 'content-type': 'application/json' } });

  const old = new DirectoryClient({
    url: 'http://directory.invalid',
    key: 'k',
    serverId: 'srv_test',
    secret: 's'.repeat(43),
    publicKey: 'p'.repeat(44),
    bridgeUrl: 'wss://games.example.com/federation/bridge',
    fetchImpl: respond(404, { error: 'not_found' }),
  });
  assert.strictEqual(old.enabled, true);
  assert.strictEqual(await old.heartbeat(true), null, 'a failed heartbeat never throws into the lobby path');
  assert.strictEqual(old.enabled, false);
  assert.match(old.disabledReason, /does not serve/);

  const wrongKey = new DirectoryClient({
    url: 'http://directory.invalid',
    key: 'k',
    serverId: 'srv_test',
    secret: 's'.repeat(43),
    publicKey: 'p'.repeat(44),
    bridgeUrl: 'wss://games.example.com/federation/bridge',
    fetchImpl: respond(401, { error: 'invalid_server_key' }),
  });
  assert.strictEqual(await wrongKey.heartbeat(true), null);
  assert.strictEqual(wrongKey.enabled, false);
  assert.match(wrongKey.disabledReason, /server key/);
});

// --- two live node servers ---------------------------------------------------

test('a lobby hosted on one node server is joinable from another', async (t) => {
  const directory = await startDirectory();
  const a = await startServer(directory.url);
  const b = await startServer(directory.url);
  t.after(async () => {
    await b.stop();
    await a.stop();
    await directory.close();
  });

  const infoA = await health(a);
  const infoB = await health(b);
  assert.strictEqual(infoA.networking.enabled, true, a.output());
  assert.strictEqual(infoA.networking.mode, 'gwas');
  assert.strictEqual(infoA.networking.disabledReason, null);
  assert.ok(infoA.networking.serverId);
  assert.notStrictEqual(infoA.networking.serverId, infoB.networking.serverId);

  const sessionA = await guest(a.base, 'Host');
  const created = await request(a.base, `${MOUNT}/Matchmaking/CreateLobby`, {
    token: sessionA.sessionToken,
    body: { maxMembers: 4, visibility: 'public' },
  });
  assert.strictEqual(created.status, 200, JSON.stringify(created.body));
  const lobbyId = created.body.id;

  // Creating a lobby publishes it within a moment, not on the next heartbeat.
  await waitUntil(() => directory.directory.stats().lobbies === 1, 8000, 'lobby publication');
  const published = directory.directory.live()[0].lobbies[0];
  assert.strictEqual(published.id, lobbyId);
  assert.strictEqual(published.appId, '480');
  assert.strictEqual(published.data && typeof published.data, 'object');

  const sessionB = await guest(b.base, 'Guest');
  const joined = await request(b.base, `${MOUNT}/Matchmaking/JoinLobby`, {
    token: sessionB.sessionToken,
    body: { lobbyId },
  });
  assert.strictEqual(joined.status, 200, JSON.stringify(joined.body) + '\n' + b.output());
  assert.strictEqual(joined.body.id, lobbyId);
  assert.strictEqual(joined.body.memberCount, 2);
  assert.ok(joined.body.members.some((m) => String(m.steamId) === String(sessionB.steamId)));

  // The hosting server owns the membership; the joiner is a federated session there.
  const onHost = await request(a.base, `${MOUNT}/Matchmaking/Lobby?lobbyId=${lobbyId}`, {
    method: 'GET',
    token: sessionA.sessionToken,
  });
  assert.strictEqual(onHost.status, 200);
  assert.strictEqual(onHost.body.memberCount, 2);

  const hostHealth = await health(a);
  const guestHealth = await health(b);
  assert.strictEqual(hostHealth.networking.links, 1);
  assert.strictEqual(guestHealth.networking.links, 1);
  assert.strictEqual(hostHealth.networking.federatedPlayers, 1);

  // A remote LobbyList does not need a request to the other server: it reads the census
  // the directory handed back with the heartbeat. Ask B directly after a publish.
  const listed = await request(b.base, `${MOUNT}/Matchmaking/LobbyList`, {
    token: sessionB.sessionToken,
    body: {},
  });
  assert.strictEqual(listed.status, 200);
  assert.strictEqual(listed.body.lobbies.length, 1);
  assert.strictEqual(listed.body.lobbies[0].remote, true);
  assert.strictEqual(listed.body.lobbies[0].serverId, infoA.networking.serverId);
});

test('pushes cross the link and a disconnect releases the remote seat', async (t) => {
  const directory = await startDirectory();
  const a = await startServer(directory.url);
  const b = await startServer(directory.url);
  t.after(async () => {
    await b.stop();
    await a.stop();
    await directory.close();
  });

  const sessionA = await guest(a.base, 'Host');
  const created = await request(a.base, `${MOUNT}/Matchmaking/CreateLobby`, {
    token: sessionA.sessionToken,
    body: { maxMembers: 4 },
  });
  const lobbyId = created.body.id;
  await waitUntil(() => directory.directory.stats().lobbies === 1, 8000, 'lobby publication');

  const sessionB = await guest(b.base, 'Guest');
  const realtimeB = await openRealtime(b.base, sessionB.sessionToken);
  t.after(() => realtimeB.close());

  const joined = await request(b.base, `${MOUNT}/Matchmaking/JoinLobby`, {
    token: sessionB.sessionToken,
    body: { lobbyId },
  });
  assert.strictEqual(joined.status, 200, JSON.stringify(joined.body));
  await realtimeB.next('LobbyMemberJoined');

  // The host writes lobby data; the player is only connected to the other server.
  const written = await request(a.base, `${MOUNT}/Matchmaking/LobbyData`, {
    token: sessionA.sessionToken,
    body: { lobbyId, key: 'map', value: 'cross' },
  });
  assert.strictEqual(written.status, 200, JSON.stringify(written.body));

  const pushed = await realtimeB.next('LobbyDataChanged');
  assert.strictEqual(pushed.lobbyId, lobbyId);
  assert.strictEqual(pushed.key, 'map');
  assert.strictEqual(pushed.value, 'cross');

  // A forwarded write from the guest reaches the host too.
  const fromGuest = await request(b.base, `${MOUNT}/Matchmaking/MemberData`, {
    token: sessionB.sessionToken,
    body: { lobbyId, key: 'ready', value: 'yes' },
  });
  assert.strictEqual(fromGuest.status, 200, JSON.stringify(fromGuest.body));
  const onHost = await request(a.base, `${MOUNT}/Matchmaking/MemberData?lobbyId=${lobbyId}&steamId=${sessionB.steamId}`, {
    method: 'GET',
    token: sessionA.sessionToken,
  });
  assert.strictEqual(onHost.status, 200);
  assert.strictEqual(onHost.body.data.ready, 'yes');

  // Lobby chat is a forwarded write whose echo comes back over the same link.
  const chat = await request(b.base, `${MOUNT}/Matchmaking/ChatSend`, {
    token: sessionB.sessionToken,
    body: { lobbyId, message: 'hello from B' },
  });
  assert.strictEqual(chat.status, 200, JSON.stringify(chat.body));
  const chatPush = await realtimeB.next('LobbyChat');
  assert.strictEqual(String(chatPush.steamId), String(sessionB.steamId));
  assert.strictEqual(chatPush.message, 'hello from B');
  assert.strictEqual(chatPush.lobbyId, lobbyId);

  const left = await request(b.base, `${MOUNT}/Matchmaking/LeaveLobby`, {
    token: sessionB.sessionToken,
    body: { lobbyId },
  });
  assert.strictEqual(left.status, 200, JSON.stringify(left.body));
  const afterLeave = await request(a.base, `${MOUNT}/Matchmaking/Lobby?lobbyId=${lobbyId}`, {
    method: 'GET',
    token: sessionA.sessionToken,
  });
  assert.strictEqual(afterLeave.body.memberCount, 1);

  // Rejoin, then drop the guest's websocket: the hosting server has to free the seat.
  const rejoined = await request(b.base, `${MOUNT}/Matchmaking/JoinLobby`, {
    token: sessionB.sessionToken,
    body: { lobbyId },
  });
  assert.strictEqual(rejoined.status, 200);
  realtimeB.close();

  await waitUntil(async () => {
    const lobby = await request(a.base, `${MOUNT}/Matchmaking/Lobby?lobbyId=${lobbyId}`, {
      method: 'GET',
      token: sessionA.sessionToken,
    });
    return lobby.status === 200 && lobby.body.memberCount === 1;
  }, 8000, 'remote seat release');
});

test('the bridge refuses a websocket without a valid ticket', async (t) => {
  const directory = await startDirectory();
  const a = await startServer(directory.url);
  t.after(async () => {
    await a.stop();
    await directory.close();
  });

  const status = await new Promise((resolve, reject) => {
    const socket = new WebSocket(`ws://127.0.0.1:${a.port}/federation/bridge`);
    socket.on('open', () => {
      socket.close();
      reject(new Error('bridge accepted a connection without a ticket'));
    });
    socket.on('unexpected-response', (req, resp) => resolve(resp.statusCode));
    socket.on('error', () => resolve(0));
    setTimeout(() => reject(new Error('no response from bridge')), 5000);
  });

  assert.ok(status === 0 || status === 403, `expected a refusal, got ${status}`);
  assert.strictEqual((await health(a)).networking.links, 0);
});

test('a server without a directory degrades to local lobbies', async (t) => {
  const port = await freePort();
  const dataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-fed-off-'));

  const child = spawn(process.execPath, [SERVER], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, {
      WEBWORKS_ACCOUNT_MODE: 'local',
      WEBWORKS_NO_PROMPT: '1',
      WEBWORKS_DATA_DIR: dataDir,
      WEBWORKS_PORT: String(port),
      WEBWORKS_CONFIG: path.join(dataDir, 'webworks.config.json'),
      WEBWORKS_NETWORKING: 'gwas',
      WEBWORKS_PUBLIC_URL: `http://127.0.0.1:${port}`,
      WEBWORKS_GWAS_URL: 'http://127.0.0.1:1',
      WEBWORKS_GWAS_SERVER_KEY: KEY,
      WEBWORKS_FEDERATION_ALLOW_INSECURE: '1',
      PORT: '',
    }),
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  t.after(() => child.kill('SIGTERM'));

  const base = `http://127.0.0.1:${port}`;
  await waitForHttp(`${base}/health`);

  const session = await guest(base, 'Solo');
  const created = await request(base, `${MOUNT}/Matchmaking/CreateLobby`, {
    token: session.sessionToken,
    body: { maxMembers: 2 },
  });
  assert.strictEqual(created.status, 200);

  const info = await health({ base });
  assert.strictEqual(info.networking.enabled, true, 'networking stays on even if the directory is down');
  assert.strictEqual(info.lobbies, 1);
});

// Setup asks nothing, and a server that has not been given an address yet still knows its
// id and its bridge path, and publishes the instant a tunnel reports one.
test('a server waits for a public address, then publishes on it', async () => {
  const dataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-wait-'));

  const before = federation.init({
    dataDir,
    networkingMode: 'gwas',
    serverToken: 'payload.signature',
    gwasUrl: 'http://127.0.0.1:1',
    federationBridgePath: '/federation/bridge',
    federationAllowInsecure: false,
  });

  assert.strictEqual(before.enabled, true);
  assert.strictEqual(before.publishing, false);
  assert.strictEqual(before.disabledReason, 'waiting_public_url');
  assert.strictEqual(before.authMode, 'claim');
  assert.ok(before.serverId);

  const after = federation.setPublicUrl('https://quiet-river-1234.trycloudflare.com');
  assert.strictEqual(after.publishing, true, JSON.stringify(after));
  assert.strictEqual(after.publicUrl, 'wss://quiet-river-1234.trycloudflare.com/federation/bridge');
  assert.strictEqual(after.disabledReason, null);
  assert.strictEqual(after.directory.authMode, 'claim');

  // A second address (a restart, or a tunnel reported twice) does not start a second
  // directory client or republish a different bridge.
  const again = federation.setPublicUrl('https://other-tunnel.trycloudflare.com');
  assert.strictEqual(again.publicUrl, after.publicUrl);

  await federation.shutdown();
});

test('without a claimed token the reason is named instead of guessing', async () => {
  const dataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-noclaim-'));
  const previous = process.env.WEBWORKS_GWAS_SERVER_KEY;
  delete process.env.WEBWORKS_GWAS_SERVER_KEY;
  try {
    const status = federation.init({
      dataDir,
      networkingMode: 'gwas',
      serverToken: '',
      federationServerKey: '',
      gwasUrl: 'https://gwas.test',
      federationPublicUrl: 'https://games.example.com',
    });
    assert.strictEqual(status.enabled, false);
    assert.strictEqual(status.disabledReason, 'missing_server_token');
    assert.match(require('../src/gwasProbe').networkingHint(status.disabledReason), /claims it automatically/);
  } finally {
    if (previous === undefined) delete process.env.WEBWORKS_GWAS_SERVER_KEY; else process.env.WEBWORKS_GWAS_SERVER_KEY = previous;
  }
});
