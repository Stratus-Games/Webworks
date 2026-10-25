const test = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const http = require('node:http');

const serverClaim = require('../src/serverClaim');
const identity = require('../src/federation/identity');
const { normalize, resolve } = require('../src/config');

// Setup asks nothing. The machine generates a 32-byte secret on first start, claims its own
// server id with it, and keeps only the token that comes back. These tests hold that down:
// the GWAS address is fixed, no password exists anywhere, and a restart does not claim twice.

function tokenWith(payload) {
  return Buffer.from(JSON.stringify(payload)).toString('base64url') + '.signature';
}

function tempDir(name) {
  return fs.mkdtempSync(path.join(os.tmpdir(), name));
}

test('the setup defaults to GWAS at one fixed address', () => {
  const previous = process.env.WEBWORKS_ACCOUNT_MODE;
  delete process.env.WEBWORKS_ACCOUNT_MODE;
  try {
    const cfg = normalize({});
    assert.strictEqual(cfg.gwasUrl, 'https://gwas.webeam.workers.dev');
    assert.strictEqual(cfg.accountMode, 'gwas');
    assert.strictEqual(cfg.networkingMode, 'gwas');
    assert.strictEqual(cfg.serverPassword, undefined, 'there is no server password to configure');
  } finally {
    if (previous !== undefined) process.env.WEBWORKS_ACCOUNT_MODE = previous;
  }
});

test('an old config file that says local does not keep the server off GWAS', () => {
  const previous = process.env.WEBWORKS_ACCOUNT_MODE;
  delete process.env.WEBWORKS_ACCOUNT_MODE;
  try {
    const cfg = normalize({ accountMode: 'local', gwasUrl: '' });
    assert.strictEqual(cfg.accountMode, 'gwas');
    assert.strictEqual(cfg.gwasUrl, 'https://gwas.webeam.workers.dev');
  } finally {
    if (previous !== undefined) process.env.WEBWORKS_ACCOUNT_MODE = previous;
  }
});

test('a saved token is reused until it is nearly expired', () => {
  const fresh = tokenWith({ srv: 'srv_abc', exp: Date.now() + 90 * 24 * 60 * 60 * 1000 });
  const old = tokenWith({ srv: 'srv_abc', exp: Date.now() + 2 * 24 * 60 * 60 * 1000 });

  const cfg = normalize({ serverToken: fresh, serverId: 'srv_abc' });
  assert.strictEqual(serverClaim.isUsable(cfg.serverToken), true);
  assert.strictEqual(serverClaim.isUsable(old), false, 'a token about to lapse is renewed instead');
  assert.strictEqual(serverClaim.isUsable('garbage'), false);
});

test('claiming posts the id and the generated secret, and returns the token', async () => {
  const seen = [];
  const result = await serverClaim.claim({
    url: 'https://gwas.test',
    serverId: 'srv_home',
    secret: 's'.repeat(43),
    fetchImpl: async (url, options) => {
      seen.push({ url, body: JSON.parse(options.body) });
      return { ok: true, status: 200, text: async () => JSON.stringify({ token: tokenWith({ srv: 'srv_home' }), created: true }) };
    },
  });

  assert.strictEqual(seen.length, 1);
  assert.strictEqual(seen[0].url, 'https://gwas.test/servers/claim');
  assert.strictEqual(seen[0].body.serverId, 'srv_home');
  assert.strictEqual(seen[0].body.secret, 's'.repeat(43));
  // The same value also travels as `password`, the field a worker from the previous build
  // reads, so upgrading this server first does not force a worker redeploy to keep working.
  assert.strictEqual(seen[0].body.password, seen[0].body.secret);
  assert.strictEqual(result.created, true);
  assert.ok(result.token.includes('.'));
});

test('a refused claim and an unreachable GWAS are both named, never silent', async () => {
  const wrong = await serverClaim.claim({
    url: 'https://gwas.test',
    serverId: 'srv_home',
    secret: 's'.repeat(43),
    fetchImpl: async () => ({ ok: false, status: 401, text: async () => JSON.stringify({ error: 'bad_secret' }) }),
  }).catch((err) => err);
  assert.strictEqual(wrong.code, 'bad_secret');
  assert.match(serverClaim.explain(wrong).join('\n'), /federation\.json/);

  const offline = await serverClaim.claim({
    url: 'https://gwas.test',
    serverId: 'srv_home',
    secret: 's'.repeat(43),
    fetchImpl: async () => { throw new TypeError('fetch failed'); },
  }).catch((err) => err);
  assert.strictEqual(offline.code, 'unreachable');
  assert.match(serverClaim.explain(offline).join('\n'), /cross-server lobbies stay off/);

  const short = await serverClaim.claim({
    url: 'https://gwas.test',
    serverId: 'srv_home',
    secret: 'too-short',
    fetchImpl: async () => { throw new Error('should not be called'); },
  }).catch((err) => err);
  assert.strictEqual(short.code, 'weak_secret');
});

test('the identity file carries the claim secret, once, and nobody is asked for it', () => {
  const dataDir = tempDir('webworks-identity-');
  const first = identity.init({ dataDir });
  assert.match(first.serverId, /^srv_[0-9a-f]{18}$/);
  assert.ok(first.claimSecret.length >= 32);
  assert.notStrictEqual(first.claimSecret, first.secret, 'claiming has its own secret');

  identity.reset();
  const again = identity.init({ dataDir });
  assert.strictEqual(again.serverId, first.serverId);
  assert.strictEqual(again.claimSecret, first.claimSecret, 'the same machine keeps its secret');

  const onDisk = JSON.parse(fs.readFileSync(path.join(dataDir, 'federation.json'), 'utf8'));
  assert.strictEqual(onDisk.claimSecret, first.claimSecret);
});

test('a start claims its own id with no prompt, and the next start asks GWAS nothing', async () => {
  let claims = 0;
  const worker = http.createServer((req, res) => {
    let body = '';
    req.on('data', (chunk) => { body += chunk; });
    req.on('end', () => {
      claims++;
      const posted = JSON.parse(body);
      assert.ok(posted.secret.length >= 32, 'a generated secret, not a password');
      const token = tokenWith({ srv: posted.serverId, exp: Date.now() + 1e10 });
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ token, serverId: posted.serverId, created: claims === 1 }));
    });
  });
  await new Promise((done) => worker.listen(0, '127.0.0.1', done));

  const dir = tempDir('webworks-claim-');
  const saved = {
    url: process.env.WEBWORKS_GWAS_URL,
    config: process.env.WEBWORKS_CONFIG,
    data: process.env.WEBWORKS_DATA_DIR,
  };
  process.env.WEBWORKS_GWAS_URL = `http://127.0.0.1:${worker.address().port}`;
  process.env.WEBWORKS_CONFIG = path.join(dir, 'webworks.config.json');
  process.env.WEBWORKS_DATA_DIR = path.join(dir, 'data');

  try {
    identity.reset();
    const first = await resolve();
    assert.strictEqual(claims, 1);
    assert.ok(first.serverToken.includes('.'));

    const savedConfig = fs.readFileSync(process.env.WEBWORKS_CONFIG, 'utf8');
    assert.ok(JSON.parse(savedConfig).serverToken, 'the token is written to the config file');
    const onDisk = JSON.parse(fs.readFileSync(path.join(dir, 'data', 'federation.json'), 'utf8'));
    assert.ok(!savedConfig.includes(onDisk.claimSecret), 'the secret is not copied into the config');
    assert.ok(!savedConfig.includes(onDisk.secret), 'the bridge secret is not copied either');

    identity.reset();
    const second = await resolve();
    assert.strictEqual(claims, 1, 'a restart with a live token does not claim again');
    assert.strictEqual(second.serverToken, first.serverToken);
  } finally {
    for (const [key, value] of Object.entries({
      WEBWORKS_GWAS_URL: saved.url,
      WEBWORKS_CONFIG: saved.config,
      WEBWORKS_DATA_DIR: saved.data,
    })) {
      if (value === undefined) delete process.env[key];
      else process.env[key] = value;
    }
    await new Promise((done) => worker.close(done));
  }
});

test('an unreachable GWAS leaves the server running locally instead of blocking setup', async () => {
  const dir = tempDir('webworks-offline-');
  const saved = {
    url: process.env.WEBWORKS_GWAS_URL,
    config: process.env.WEBWORKS_CONFIG,
    data: process.env.WEBWORKS_DATA_DIR,
  };
  // A port nothing is listening on: the claim fails fast with a connection error.
  process.env.WEBWORKS_GWAS_URL = 'http://127.0.0.1:1';
  process.env.WEBWORKS_CONFIG = path.join(dir, 'webworks.config.json');
  process.env.WEBWORKS_DATA_DIR = path.join(dir, 'data');

  try {
    identity.reset();
    const cfg = await resolve();
    assert.strictEqual(cfg.serverToken, '', 'no token yet');
    assert.strictEqual(cfg.gwasUrl, 'http://127.0.0.1:1');
    assert.ok(fs.existsSync(process.env.WEBWORKS_CONFIG), 'the rest of the config is still written');
  } finally {
    for (const [key, value] of Object.entries({
      WEBWORKS_GWAS_URL: saved.url,
      WEBWORKS_CONFIG: saved.config,
      WEBWORKS_DATA_DIR: saved.data,
    })) {
      if (value === undefined) delete process.env[key];
      else process.env[key] = value;
    }
  }
});
