// The server secret, and the token it buys.
//
// Nobody types anything. On first start this machine generates a 32-byte secret (kept in
// data/federation.json) and sends it once to GWAS, which keeps only a salted hash. That is
// what proves this server owns its own id, so nobody else can publish lobbies as it, take
// the id over, or get the directory to introduce them to its players. GWAS answers with a
// signed token that the lobby directory checks without reading its database, so a busy
// server still costs no storage calls.
//
// The secret is never logged and never written to the saved config: only the token is.

const CLAIM_PATH = '/servers/claim';
const DEFAULT_TIMEOUT_MS = 8000;
// 32 bytes base64url is 43 characters; anything shorter is a mistake, not a secret.
const MIN_SECRET = 32;
const RENEW_WITHIN_MS = 7 * 24 * 60 * 60 * 1000;

class ClaimError extends Error {
  constructor(code, status) {
    super(code);
    this.name = 'ClaimError';
    this.code = code;
    this.status = status || 0;
  }
}

// A token is `base64url(json).base64url(hmac)`. Only the payload is read here (the
// worker is the one that verifies the signature), so an expired token can be spotted
// before a request is wasted on it.
function payloadOf(token) {
  const text = String(token || '');
  const dot = text.indexOf('.');
  if (dot <= 0) return null;
  try {
    return JSON.parse(Buffer.from(text.slice(0, dot), 'base64url').toString('utf8'));
  } catch {
    return null;
  }
}

function expiresAt(token) {
  const payload = payloadOf(token);
  return payload && payload.exp ? Number(payload.exp) : 0;
}

function serverIdOf(token) {
  const payload = payloadOf(token);
  return payload && typeof payload.srv === 'string' ? payload.srv : '';
}

// Usable means "still good for a while", not merely unexpired: a token about to lapse
// is renewed at startup rather than in the middle of a session.
function isUsable(token, now = Date.now()) {
  const payload = payloadOf(token);
  if (!payload || typeof payload.srv !== 'string') return false;
  if (!payload.exp) return true;
  return Number(payload.exp) - now > RENEW_WITHIN_MS;
}

async function claim({ url, serverId, secret, fetchImpl = fetch, timeoutMs = DEFAULT_TIMEOUT_MS } = {}) {
  const base = String(url || '').trim().replace(/\/+$/, '');
  if (!/^https?:\/\//i.test(base)) throw new ClaimError('invalid_url');
  if (!/^[A-Za-z0-9_-]{4,64}$/.test(String(serverId || ''))) throw new ClaimError('invalid_server_id');
  if (String(secret || '').length < MIN_SECRET) throw new ClaimError('weak_secret');

  const options = {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    // `secret` is the field this build's worker reads. `password` carries the same value so a
    // worker still running the previous build accepts the claim too: same hash, same id, so
    // upgrading the node before the worker does not cost an operator a redeploy first.
    body: JSON.stringify({ serverId, secret, password: secret }),
  };
  if (typeof AbortSignal !== 'undefined' && AbortSignal.timeout) options.signal = AbortSignal.timeout(timeoutMs);

  let resp;
  try {
    resp = await fetchImpl(`${base}${CLAIM_PATH}`, options);
  } catch (err) {
    const timedOut = err && (err.name === 'TimeoutError' || err.name === 'AbortError');
    throw new ClaimError(timedOut ? 'timeout' : 'unreachable');
  }

  const text = await resp.text();
  let data = null;
  if (text) {
    try { data = JSON.parse(text); } catch { data = null; }
  }

  if (!resp.ok) throw new ClaimError((data && data.error) || `http_${resp.status}`, resp.status);
  if (!data || typeof data.token !== 'string' || !data.token.includes('.')) throw new ClaimError('bad_response');

  return {
    token: data.token,
    serverId: data.serverId || serverId,
    created: Boolean(data.created),
    expiresAt: Number(data.expiresAt) || expiresAt(data.token),
  };
}

// One failure, one paragraph a person can act on.
function explain(err) {
  const code = (err && err.code) || 'error';
  switch (code) {
    case 'bad_secret':
    case 'bad_password':
      return [
        'GWAS knows this server id, but not with the secret this machine generated for it.',
        'That happens when the data folder is copied between machines. To get a fresh id:',
        '  delete the federation.json file in this server\'s data folder, then start again.',
      ];
    case 'weak_secret':
      return ['The generated server secret was too short, so the claim was refused.'];
    case 'weak_password':
      return [
        'That GWAS deployment refused the claim secret: it is running a worker from an older',
        'build. Deploy the current worker, then start this server again:',
        '  cd GWAS && npx wrangler deploy',
      ];
    case 'server_id_taken':
      return [
        'Another machine already holds this server id, so this one cannot claim it.',
        'Delete the federation.json file in this server\'s data folder to get a fresh id.',
      ];
    case 'unreachable':
    case 'timeout':
      return [
        'Could not reach GWAS to claim this server, so cross-server lobbies stay off for now.',
        'Check this machine\'s internet connection and run the server again.',
      ];
    case 'invalid_url':
      return ['The GWAS address is not an http(s) URL, so the server could not be claimed.'];
    case 'http_404':
      return [
        'That GWAS deployment does not know /servers/claim, so it predates server ids.',
        'Deploy the current GWAS worker, then start this server again.',
      ];
    case 'internal_error':
    case 'http_500':
      return [
        'The GWAS worker failed while claiming this server, which is what a worker whose D1',
        'database has not had the current schema applied looks like (the table that stores server',
        'ids is missing). Apply the schema and deploy the current worker together:',
        '  cd GWAS && npx wrangler d1 execute gwas --remote --file=schema.sql && npx wrangler deploy',
      ];
    default:
      return [`GWAS refused the claim (${code}), so cross-server lobbies stay off for now.`];
  }
}

module.exports = {
  claim,
  explain,
  isUsable,
  expiresAt,
  serverIdOf,
  payloadOf,
  ClaimError,
  MIN_SECRET,
  CLAIM_PATH,
};
