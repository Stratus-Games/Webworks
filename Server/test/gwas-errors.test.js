const test = require('node:test');
const assert = require('node:assert');
const http = require('node:http');

const { GwasAccountStore } = require('../src/accounts/gwas');
const { AccountError } = require('../src/accounts/errors');

// A stand-in for the account service that always answers the same way.
async function answerWith(status, body) {
  const server = http.createServer((req, res) => {
    let raw = '';
    req.on('data', (chunk) => { raw += chunk; });
    req.on('end', () => {
      res.writeHead(status, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify(body));
    });
  });
  await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
  const url = `http://127.0.0.1:${server.address().port}`;
  return { url, close: () => new Promise((resolve) => server.close(resolve)) };
}

async function registerAgainst(status, body) {
  const stub = await answerWith(status, body);
  const store = new GwasAccountStore();
  store.init({ gwasUrl: stub.url, dataDir: null });
  try {
    await store.register({ username: 'alex', password: 'hunter22' });
    return { error: null };
  } catch (err) {
    return { error: err };
  } finally {
    await stub.close();
  }
}

// Registering used to answer 502 for every rejection, which the browser shows as
// "Bad Gateway" and which hides the actual reason (a short password, a name with a
// space, a name that is taken) from the player.
test('a rejected signup keeps the account service status, not 502', async () => {
  const cases = [
    [{ error: 'invalid_username' }, 400],
    [{ error: 'weak_password' }, 400],
    [{ error: 'username_taken' }, 409],
    [{ error: 'rate_limited' }, 429],
    [{ error: 'account_banned' }, 403],
    [{ error: 'bad_credentials' }, 401],
    // A status the server has no opinion about still belongs to the caller.
    [{ error: 'some_new_code' }, 400],
    [{ error: 'invalid_payload' }, 422],
  ];

  for (const [body, status] of cases) {
    const { error } = await registerAgainst(status, body);
    assert.ok(error instanceof AccountError, `${body.error} should raise an AccountError`);
    assert.strictEqual(error.code, body.error);
    assert.strictEqual(error.status, status, `${body.error} should keep ${status}`);
  }
});

test('only a failing account service reads as a gateway error', async () => {
  const failing = await registerAgainst(500, { error: 'internal_error' });
  assert.strictEqual(failing.error.status, 502);

  const unreadable = await registerAgainst(503, null);
  assert.strictEqual(unreadable.error.status, 502);
  assert.match(unreadable.error.code, /^upstream_503$|^internal_error$/);
});

test('an unreachable account service is unavailable, not a bad password', async () => {
  const store = new GwasAccountStore();
  // Port 1 on loopback refuses immediately, so the test does not wait on a timeout.
  store.init({ gwasUrl: 'http://127.0.0.1:1', dataDir: null });
  await assert.rejects(
    () => store.register({ username: 'alex', password: 'hunter22' }),
    (err) => err instanceof AccountError && err.code === 'accounts_unavailable' && err.status === 503,
  );
});
