const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawn } = require('node:child_process');

// What a public worker, a directory peer or a stray browser tab must never get from this
// server: the credentials that make its id its own. Everything here is about the node server
// being safe to expose, since anyone on the internet can reach it through the tunnel.

const SERVER = path.join(__dirname, '..', 'server.js');

function freePort() {
  return 47000 + Math.floor(Math.random() * 2000);
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

async function startServer(env) {
  const port = freePort();
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-hard-'));
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
    }, env || {}),
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
    stop: () => new Promise((done) => {
      child.once('exit', () => done());
      child.kill('SIGTERM');
    }),
  };
}

test('a claimed server hands out neither its token nor its secrets', async () => {
  const token = Buffer.from(JSON.stringify({ srv: 'srv_leakcheck', exp: Date.now() + 1e10 })).toString('base64url') + '.signature';
  const server = await startServer({ WEBWORKS_SERVER_TOKEN: token });

  try {
    const health = await (await fetch(`${server.base}/health`)).text();
    const page = await (await fetch(`${server.base}/`)).text();
    const status = await (await fetch(`${server.base}/status`)).text();

    assert.ok(!health.includes(token), '/health must not echo the claim token');
    assert.ok(!page.includes(token), 'the status page must not echo the claim token');
    assert.ok(!status.includes(token), '/status must not echo the claim token');

    const identity = JSON.parse(fs.readFileSync(path.join(server.dir, 'federation.json'), 'utf8'));
    assert.ok(identity.claimSecret && identity.claimSecret.length >= 32);
    for (const body of [health, page, status, server.output()]) {
      assert.ok(!body.includes(identity.claimSecret), 'the claim secret stays on this machine');
      assert.ok(!body.includes(identity.secret), 'the bridge secret stays on this machine');
      assert.ok(!body.includes(identity.privateKey), 'the signing key never leaves the process');
    }
  } finally {
    await server.stop();
  }
});

test('a request from an outsider cannot claim this machine a different identity', async () => {
  const server = await startServer({});
  try {
    const identity = JSON.parse(fs.readFileSync(path.join(server.dir, 'federation.json'), 'utf8'));
    const dir = { heartbeat: null };

    // The directory is only spoken to, never served: nothing on this server's HTTP surface
    // lets a caller set its id, its bridge address or its identity file.
    for (const target of ['/servers/claim', '/federation/identity', '/directory/register']) {
      const resp = await fetch(`${server.base}${target}`, { method: 'POST', body: '{"serverId":"srv_evil","secret":"x"}' });
      assert.ok(resp.status === 404 || resp.status === 405, `${target} must not exist (${resp.status})`);
    }
    assert.strictEqual(dir.heartbeat, null);

    const after = JSON.parse(fs.readFileSync(path.join(server.dir, 'federation.json'), 'utf8'));
    assert.strictEqual(after.serverId, identity.serverId, 'the id the machine was born with stands');
  } finally {
    await server.stop();
  }
});

test('a signed-in outsider cannot use this server to reach the host itself', async () => {
  const server = await startServer({});
  try {
    const registered = await fetch(`${server.base}/accounts/Register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: 'nosy', password: 'hunter22', displayName: 'Nosy' }),
    });
    assert.strictEqual(registered.status, 201);
    const accountToken = (await registered.json()).token;

    const guest = await fetch(`${server.base}/api/Unity/SteamworksNET/User/GuestSession`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ appId: '480', accountToken, name: 'Nosy' }),
    });
    assert.strictEqual(guest.status, 200);
    const session = (await guest.json()).sessionToken;
    assert.ok(session);

    // Even a real session cannot borrow this process to reach the machine it runs on: the
    // HTML surface and the HTTP proxy both refuse loopback and private targets by default.
    for (const target of ['http://127.0.0.1:22/', 'http://169.254.169.254/latest/meta-data/', 'http://localhost:3000/health']) {
      const page = await fetch(`${server.base}/api/Unity/SteamworksNET/HTML/Fetch?url=${encodeURIComponent(target)}`, {
        headers: { 'x-webworks-session': session },
      });
      assert.strictEqual(page.status, 400, `${target} must be refused`);
      const body = await page.text();
      assert.ok(!/SSH-|ami-|instance-id/i.test(body), 'nothing from the host leaks back');

      const proxied = await fetch(`${server.base}/api/Unity/SteamworksNET/HTTP/Request`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'x-webworks-session': session },
        body: JSON.stringify({ method: 'GET', url: target }),
      });
      assert.notStrictEqual(proxied.status, 200, `${target} must not be proxied`);
    }
  } finally {
    await server.stop();
  }
});
