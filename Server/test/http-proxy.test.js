const test = require('node:test');
const assert = require('node:assert');
const { spawn } = require('node:child_process');
const http = require('node:http');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const SERVER = path.join(__dirname, '..', 'server.js');
const API = '/api/Unity/SteamworksNET';

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
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'webworks-http-'));
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

function json(base, pathname, options = {}) {
  const headers = Object.assign({ 'Content-Type': 'application/json' }, options.headers || {});
  return fetch(base + pathname, Object.assign({}, options, { headers })).then(async (resp) => ({
    status: resp.status,
    body: await resp.json().catch(() => null),
  }));
}

async function sessionFor(base, appId) {
  const resp = await json(base, `${API}/User/GuestSession`, {
    method: 'POST', body: JSON.stringify({ appId, name: 'Proxy' }),
  });
  assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
  return resp.body.sessionToken;
}

test('http proxy refuses private targets and unsupported methods', async (t) => {
  const server = await startServer();
  t.after(() => server.stop());

  const token = await sessionFor(server.base, '7001');
  const headers = { 'x-webworks-session': token };

  const privateTarget = await json(server.base, `${API}/HTTP/Request`, {
    method: 'POST', headers, body: JSON.stringify({ method: 'GET', url: 'http://127.0.0.1:9/secret' }),
  });
  assert.strictEqual(privateTarget.status, 400);
  assert.strictEqual(privateTarget.body.error, 'private_target');

  const scheme = await json(server.base, `${API}/HTTP/Request`, {
    method: 'POST', headers, body: JSON.stringify({ method: 'GET', url: 'file:///etc/passwd' }),
  });
  assert.strictEqual(scheme.status, 400);
  assert.strictEqual(scheme.body.error, 'unsupported_scheme');

  const method = await json(server.base, `${API}/HTTP/Request`, {
    method: 'POST', headers, body: JSON.stringify({ method: 'TRACE', url: 'https://example.com/' }),
  });
  assert.strictEqual(method.status, 400);
  assert.strictEqual(method.body.error, 'unsupported_method');
});

test('http proxy forwards a real request when private targets are allowed', async (t) => {
  const upstream = http.createServer((req, res) => {
    let body = '';
    req.on('data', (chunk) => { body += chunk; });
    req.on('end', () => {
      res.writeHead(201, { 'content-type': 'application/json', 'x-echo': req.headers['x-test'] || '' });
      res.end(JSON.stringify({ method: req.method, body, ok: true }));
    });
  });
  await new Promise((resolve) => upstream.listen(0, '127.0.0.1', resolve));
  const upstreamPort = upstream.address().port;
  t.after(() => upstream.close());

  const server = await startServer({ WEBWORKS_ALLOW_PRIVATE_HTTP: 'true' });
  t.after(() => server.stop());

  const token = await sessionFor(server.base, '7002');
  const resp = await json(server.base, `${API}/HTTP/Request`, {
    method: 'POST',
    headers: { 'x-webworks-session': token },
    body: JSON.stringify({
      method: 'POST',
      url: `http://127.0.0.1:${upstreamPort}/echo`,
      headers: { 'x-test': 'webworks' },
      bodyBase64: Buffer.from('payload').toString('base64'),
    }),
  });

  assert.strictEqual(resp.status, 200, JSON.stringify(resp.body));
  assert.strictEqual(resp.body.ok, true);
  assert.strictEqual(resp.body.status, 201);
  assert.strictEqual(resp.body.headers['x-echo'], 'webworks');
  const payload = JSON.parse(Buffer.from(resp.body.bodyBase64, 'base64').toString('utf8'));
  assert.deepStrictEqual(payload, { method: 'POST', body: 'payload', ok: true });
});
