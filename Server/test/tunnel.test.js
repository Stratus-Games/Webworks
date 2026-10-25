const test = require('node:test');
const assert = require('node:assert');
const net = require('node:net');
const dgram = require('node:dgram');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawn } = require('node:child_process');
const WebSocket = require('ws');

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
    } catch {  }
    await new Promise((r) => setTimeout(r, 100));
  }
  throw new Error('server did not become ready');
}

async function startServer() {
  const port = freePort();
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-tunnel-'));
  const child = spawn(process.execPath, [SERVER], {
    cwd: path.join(__dirname, '..'),
    env: Object.assign({}, process.env, {
      WEBWORKS_ACCOUNT_MODE: 'local',
      WEBWORKS_NO_PROMPT: '1',
      WEBWORKS_DATA_DIR: dir,
      WEBWORKS_PORT: String(port),
      WEBWORKS_LOG: 'quiet',
      WEBWORKS_CONFIG: path.join(dir, 'webworks.config.json'),
      PORT: '',
    }),
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  const base = `http://127.0.0.1:${port}`;
  await waitFor(`${base}/health`);

  return {
    base,
    port,
    dir,
    async stop() {
      child.kill('SIGTERM');
      await new Promise((resolve) => { child.once('exit', resolve); setTimeout(resolve, 2000); });
    },
  };
}

function tunnelClient(url) {
  const ws = new WebSocket(url);
  const queue = [];
  const waiters = [];

  ws.on('message', (raw) => {
    let msg;
    try { msg = JSON.parse(raw.toString()); } catch { return; }
    const waiter = waiters.shift();
    if (waiter) waiter(msg);
    else queue.push(msg);
  });

  const opened = new Promise((resolve, reject) => {
    ws.once('open', resolve);
    ws.once('error', reject);
  });

  return {
    ws,
    opened,
    send(message) { ws.send(JSON.stringify(message)); },
    async nextOp(op, timeoutMs = 4000) {
      const deadline = Date.now() + timeoutMs;
      for (;;) {
        const remaining = deadline - Date.now();
        if (remaining <= 0) throw new Error(`timed out waiting for tunnel op "${op}"`);
        const msg = queue.length
          ? queue.shift()
          : await new Promise((resolve, reject) => {
            const timer = setTimeout(() => reject(new Error(`timed out waiting for tunnel op "${op}"`)), remaining);
            waiters.push((value) => { clearTimeout(timer); resolve(value); });
          });
        if (msg.op === op) return msg;
      }
    },
    close() { ws.close(); },
  };
}

function listen(server) {
  return new Promise((resolve) => server.listen(0, '127.0.0.1', () => resolve(server.address().port)));
}

function session(base, appId = 'tunnel-test') {
  return fetch(`${base}${API}/User/GuestSession`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ appId }),
  }).then((r) => r.json());
}

test('tunnel relay', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const guest = await session(server.base);
  assert.ok(guest.sessionToken, JSON.stringify(guest));

  t.test('rejects a connection without a session', async () => {
    const ws = new WebSocket(server.base.replace('http', 'ws') + '/tunnel');
    const closed = await new Promise((resolve) => ws.on('close', (code) => resolve(code)));
    assert.strictEqual(closed, 4001);
  });

  t.test('refuses a target outside the allowlist', async () => {
    const client = tunnelClient(server.base.replace('http', 'ws') + '/tunnel?session=' + guest.sessionToken);
    await client.opened;
    client.send({ op: 'open', id: 1, protocol: 'tcp', host: '10.11.12.13', port: 9999 });
    const closed = await client.nextOp('close');
    assert.strictEqual(closed.error, 'target_not_allowed');
    client.close();
  });

  t.test('carries raw TCP bytes', async () => {
    const echo = net.createServer((socket) => socket.on('data', (chunk) => socket.write(chunk)));
    const echoPort = await listen(echo);
    t.after(() => echo.close());

    const client = tunnelClient(server.base.replace('http', 'ws') + '/tunnel?session=' + guest.sessionToken);
    await client.opened;
    client.send({ op: 'open', id: 2, protocol: 'tcp', host: '127.0.0.1', port: echoPort });
    const opened = await client.nextOp('open');
    assert.strictEqual(opened.ok, true);

    const payload = Buffer.from('ping-through-the-tunnel');
    client.send({ op: 'data', id: 2, data: payload.toString('base64') });
    const echoed = await client.nextOp('data');
    assert.strictEqual(Buffer.from(echoed.data, 'base64').toString(), payload.toString());

    client.send({ op: 'close', id: 2 });
    client.close();
  });

  t.test('carries UDP datagrams', async () => {
    const echo = dgram.createSocket('udp4');
    echo.on('message', (msg, rinfo) => echo.send(msg, rinfo.port, rinfo.address));
    await new Promise((resolve) => echo.bind(0, '127.0.0.1', resolve));
    const echoPort = echo.address().port;
    t.after(() => echo.close());

    const client = tunnelClient(server.base.replace('http', 'ws') + '/tunnel?session=' + guest.sessionToken);
    await client.opened;
    client.send({ op: 'open', id: 3, protocol: 'udp', host: '127.0.0.1', port: echoPort });
    const opened = await client.nextOp('open');
    assert.strictEqual(opened.ok, true);

    client.send({ op: 'data', id: 3, data: Buffer.from('datagram').toString('base64') });
    const packet = await client.nextOp('data');
    assert.strictEqual(Buffer.from(packet.data, 'base64').toString(), 'datagram');
    client.close();
  });

  t.test('refuses privileged listen ports', async () => {
    const client = tunnelClient(server.base.replace('http', 'ws') + '/tunnel?session=' + guest.sessionToken);
    await client.opened;
    client.send({ op: 'listen', id: 9, port: 80 });
    const closed = await client.nextOp('close');
    assert.strictEqual(closed.error, 'port_not_allowed');
    client.close();
  });

  t.test('accepts inbound connections on a reverse listen port', async () => {
    const client = tunnelClient(server.base.replace('http', 'ws') + '/tunnel?session=' + guest.sessionToken);
    await client.opened;
    client.send({ op: 'listen', id: 4, port: 0 });
    const listening = await client.nextOp('listening');
    assert.ok(listening.port > 0);

    const local = net.connect(listening.port, '127.0.0.1');
    await new Promise((resolve, reject) => { local.once('connect', resolve); local.once('error', reject); });
    const accepted = await client.nextOp('accept');
    assert.ok(accepted.id > 0);

    local.write('hello-listener');
    const inbound = await client.nextOp('data');
    assert.strictEqual(Buffer.from(inbound.data, 'base64').toString(), 'hello-listener');

    client.send({ op: 'data', id: accepted.id, data: Buffer.from('hello-guest').toString('base64') });
    const reply = await new Promise((resolve) => local.once('data', (chunk) => resolve(chunk.toString())));
    assert.strictEqual(reply, 'hello-guest');

    local.destroy();
    client.send({ op: 'close', id: 4 });
    client.close();
  });
});

test('media, party and remote play routes', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const guest = await session(server.base, 'media-test');
  const headers = { 'Content-Type': 'application/json', 'x-webworks-session': guest.sessionToken };
  const call = async (method, route, body) => {
    const resp = await fetch(server.base + API + route, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    return { status: resp.status, body: await resp.json().catch(() => null) };
  };

  await t.test('music state round-trips', async () => {
    const set = await call('POST', '/Music/State', { status: 'Playing', track: 'Theme', artist: 'Someone', volume: 0.4 });
    assert.strictEqual(set.status, 200);
    const read = await call('GET', '/Music/State');
    assert.strictEqual(read.body.track, 'Theme');
    assert.strictEqual(read.body.status, 'Playing');
    assert.strictEqual(read.body.volume, 0.4);
  });

  await t.test('video broadcast reports viewers', async () => {
    const started = await call('POST', '/Video/Broadcast/Start', { url: 'https://example.com/live', title: 'Run' });
    assert.strictEqual(started.status, 200);
    const status = await call('GET', '/Video/Status');
    assert.strictEqual(status.body.broadcasting, true);

    const watched = await call('POST', '/Video/Watch', { steamId: guest.steamId });
    assert.strictEqual(watched.body.viewers, 1);

    const url = await call('GET', '/Video/URL');
    assert.strictEqual(url.body.url, 'https://example.com/live');

    const stopped = await call('POST', '/Video/Broadcast/Stop', {});
    assert.strictEqual(stopped.body.stopped, true);
  });

  await t.test('remote play sessions accept guests and drain input', async () => {
    const started = await call('POST', '/RemotePlay/Start', { title: 'Co-op', maxGuests: 2 });
    const sessionId = started.body.session.id;

    const guestTwo = await session(server.base, 'media-test');
    const guestCall = async (method, route, body) => {
      const resp = await fetch(server.base + API + route, {
        method,
        headers: { 'Content-Type': 'application/json', 'x-webworks-session': guestTwo.sessionToken },
        body: body === undefined ? undefined : JSON.stringify(body),
      });
      return { status: resp.status, body: await resp.json().catch(() => null) };
    };

    const joined = await guestCall('POST', '/RemotePlay/Join', { sessionId });
    assert.strictEqual(joined.body.session.isGuest, true);

    const input = await guestCall('POST', '/RemotePlay/Input', { sessionId, events: [{ type: 'mouse', x: 10, y: 20 }] });
    assert.strictEqual(input.status, 200);

    const drained = await call('GET', `/RemotePlay/Input?sessionId=${sessionId}`);
    assert.strictEqual(drained.body.events.length, 1);
    assert.strictEqual(drained.body.events[0].type, 'mouse');
    assert.strictEqual(drained.body.events[0].x, 10);

    const stopped = await call('POST', '/RemotePlay/Stop', { sessionId });
    assert.strictEqual(stopped.status, 200);
  });

  await t.test('party beacons track members', async () => {
    const created = await call('POST', '/Party/Create', { metadata: 'lobby-42', maxMembers: 4 });
    const beaconId = created.body.beaconId;

    const joined = await call('POST', '/Party/Join', { beaconId });
    assert.strictEqual(joined.body.party.members.length, 1);

    const beacons = await call('GET', '/Party/Beacons');
    assert.strictEqual(beacons.body.beacons.length, 1);
    assert.strictEqual(beacons.body.beacons[0].metadata, 'lobby-42');

    const left = await call('POST', '/Party/Leave', { beaconId });
    assert.strictEqual(left.status, 200);
  });

  await t.test('client-grantable inventory items can be granted', async () => {
    const boot = await call('POST', '/User/GuestSession', { appId: 'inventory-test' });
    const bootToken = boot.body.sessionToken;
    const serverLogin = await fetch(server.base + API + '/GameServer/LogOn', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'x-webworks-session': bootToken },
      body: JSON.stringify({ gamePort: 27015, queryPort: 27016, dedicated: true }),
    }).then((r) => r.json());

    const serverHeaders = { 'Content-Type': 'application/json', 'x-webworks-session': serverLogin.sessionToken };
    const define = await fetch(server.base + API + '/Inventory/Define', {
      method: 'POST',
      headers: serverHeaders,
      body: JSON.stringify({ itemDefId: 'starter-crate', name: 'Starter Crate', clientGrantable: true }),
    });
    assert.strictEqual(define.status, 200);

    const player = await session(server.base, 'inventory-test');
    const playerCall = async (method, route, body) => {
      const resp = await fetch(server.base + API + route, {
        method,
        headers: { 'Content-Type': 'application/json', 'x-webworks-session': player.sessionToken },
        body: body === undefined ? undefined : JSON.stringify(body),
      });
      return { status: resp.status, body: await resp.json().catch(() => null) };
    };

    const grant = await playerCall('POST', '/Inventory/GrantSelf', { itemDefId: 'starter-crate', quantity: 2 });
    assert.strictEqual(grant.status, 200, JSON.stringify(grant.body));

    const items = await playerCall('GET', '/Inventory/Items');
    assert.strictEqual(items.body.items[0].itemDefId, 'starter-crate');
    assert.strictEqual(items.body.items[0].quantity, 2);

    const blocked = await playerCall('POST', '/Inventory/GrantSelf', { itemDefId: 'not-grantable' });
    assert.strictEqual(blocked.status, 403);
  });

  await t.test('html surface refuses private targets', async () => {
    const local = net.createServer((socket) => socket.end('hello'));
    const port = await listen(local);
    t.after(() => local.close());

    const blocked = await call('GET', `/HTML/Fetch?url=${encodeURIComponent(`http://127.0.0.1:${port}/`)}`);
    assert.strictEqual(blocked.status, 400);
    assert.strictEqual(blocked.body.error, 'private_target');
  });
});
