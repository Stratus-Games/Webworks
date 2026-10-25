// Is the GWAS deployment this server points at actually answering?
//
// Picking GWAS as the account backend only changes where sign-ins go; it cannot make the
// remote worker exist, and a worker that answers accounts but not its lobby directory looks
// exactly like "this server stays local forever". This module asks both halves and turns the
// answer into lines a person can act on, so nothing falls back silently.

const DEFAULT_TIMEOUT_MS = 5000;

class ProbeError extends Error {
  constructor(code, status) {
    super(code);
    this.code = code;
    this.status = status || 0;
  }
}

async function getJson(fetchImpl, url, { timeoutMs, headers } = {}) {
  const options = {
    method: 'GET',
    headers: Object.assign({ Accept: 'application/json' }, headers || {}),
  };
  // AbortSignal.timeout is on Node 17.3+; the server requires 18, but a caller may pass its
  // own fetch stand-in, so guard it rather than assume.
  if (typeof AbortSignal !== 'undefined' && AbortSignal.timeout) options.signal = AbortSignal.timeout(timeoutMs);

  let resp;
  try {
    resp = await fetchImpl(url, options);
  } catch (err) {
    const timedOut = err && (err.name === 'TimeoutError' || err.name === 'AbortError' || err.code === 'ERR_CANCELED');
    throw new ProbeError(timedOut ? 'timeout' : 'unreachable', 0);
  }

  const text = await resp.text();
  let data = null;
  if (text) {
    try { data = JSON.parse(text); } catch { data = null; }
  }
  if (!resp.ok) throw new ProbeError((data && data.error) || `http_${resp.status}`, resp.status);
  return data || {};
}

function trimUrl(url) {
  return String(url || '').trim().replace(/\/+$/, '');
}

// Reachability of the account half, then of the lobby directory. The directory answer says
// which side is missing the shared key: the worker (no GWAS_SERVER_KEY secret) or this
// server (no matching key in its own config).
async function probeGwas(url, {
  fetchImpl = fetch,
  timeoutMs = DEFAULT_TIMEOUT_MS,
  serverKey = '',
  serverToken = '',
} = {}) {
  const base = trimUrl(url);
  const probe = {
    url: base,
    checkedAt: Date.now(),
    reachable: false,
    error: null,
    version: null,
    storage: null,
    directory: {
      status: 'unknown', // ready | not_configured | unauthorized | missing | unreachable
      statusCode: 0,
      error: null,
      servers: null,
      lobbies: null,
      tickets: null,
    },
  };

  if (!/^https?:\/\//i.test(base)) {
    probe.error = 'invalid_url';
    probe.directory.status = 'unreachable';
    probe.directory.error = 'invalid_url';
    return probe;
  }

  try {
    const health = await getJson(fetchImpl, `${base}/health`, { timeoutMs });
    probe.reachable = true;
    probe.version = health.version || null;
    probe.storage = health.storage || null;
  } catch (err) {
    probe.error = err instanceof ProbeError ? err.code : 'unreachable';
    probe.directory.status = 'unreachable';
    probe.directory.error = probe.error;
    return probe;
  }

  try {
    // The token from this machine's claim is the normal way in; the shared key is the
    // operator-wide fallback for a fleet that sets GWAS_SERVER_KEY.
    const headers = serverToken ? { 'x-gwas-server-token': serverToken }
      : serverKey ? { 'x-gwas-server-key': serverKey }
      : {};
    const stats = await getJson(fetchImpl, `${base}/directory/health`, { timeoutMs, headers });
    probe.directory.status = 'ready';
    probe.directory.statusCode = 200;
    probe.directory.servers = Number(stats.servers) || 0;
    probe.directory.lobbies = Number(stats.lobbies) || 0;
    probe.directory.tickets = Number(stats.tickets) || 0;
  } catch (err) {
    const code = err instanceof ProbeError ? err.code : 'unreachable';
    const status = err instanceof ProbeError ? err.status : 0;
    probe.directory.statusCode = status;
    probe.directory.error = code;
    if (status === 503 && code === 'server_key_not_configured') probe.directory.status = 'not_configured';
    else if (status === 503) probe.directory.status = 'not_configured';
    else if (status === 401 && (code === 'server_claim_required' || code === 'server_password_required')) probe.directory.status = 'unclaimed';
    else if (status === 401) probe.directory.status = 'unauthorized';
    else if (status === 404 || status === 405) probe.directory.status = 'missing';
    else probe.directory.status = 'unreachable';
  }

  return probe;
}

function friendlyError(code) {
  switch (code) {
    case 'timeout': return 'timed out';
    case 'unreachable': return 'no answer -- wrong URL, offline, or blocked by a firewall';
    case 'invalid_url': return 'not an http(s) URL';
    default: return code || 'unknown error';
  }
}

// What the worker's directory said, in terms of who has to fix it.
function directoryWarnings(probe) {
  if (!probe || !probe.reachable) return [];
  const dir = probe.directory || {};
  switch (dir.status) {
    case 'ready':
      return [];
    case 'not_configured':
      return [
        'the GWAS worker has no GWAS_SERVER_KEY secret, so its lobby directory answers 503',
        '  server_key_not_configured -- cross-server lobbies stay local until that key exists.',
        '  fix it once on the worker:  cd GWAS && npx wrangler secret put GWAS_SERVER_KEY',
      ];
    case 'unclaimed':
      return [
        'this server has not claimed its id yet, so the directory will not publish its lobbies.',
        '  start it once with a connection to GWAS; claiming needs nothing typed.',
      ];
    case 'unauthorized':
      return dir.error === 'invalid_server_key'
        ? [
          'the GWAS lobby directory rejected this server\'s key (401 invalid_server_key).',
          '  set WEBWORKS_GWAS_SERVER_KEY (or federationServerKey) to the worker\'s GWAS_SERVER_KEY.',
        ]
        : [
          'the GWAS lobby directory rejected this server\'s token (401).',
          '  delete webworks.config.json so the next start claims the id again.',
        ];
    case 'missing':
      return [
        'this GWAS deployment has no /directory routes (it predates GWAS networking),',
        '  so cross-server lobbies stay local. Deploy the current GWAS worker to enable them.',
      ];
    default:
      return [`the GWAS lobby directory did not answer (${friendlyError(dir.error)}).`];
  }
}

// Summary lines for the startup banner and /health.
function describeGwas(probe) {
  if (!probe) return { line: 'not checked', warnings: [] };
  if (!probe.reachable) {
    return {
      line: `${probe.url || '(no URL)'}  NOT reachable (${friendlyError(probe.error)})`,
      warnings: [
        'players cannot sign in while GWAS is unreachable; nothing falls back to local accounts.',
        '  check this machine\'s connection, or start with WEBWORKS_ACCOUNT_MODE=local for offline work.',
      ],
    };
  }

  const bits = [probe.version ? `GWAS ${probe.version}` : 'GWAS'];
  if (probe.storage) bits.push(probe.storage);
  return { line: `${probe.url}  reachable (${bits.join(', ')})`, warnings: directoryWarnings(probe) };
}

// Every reason federation.init() can switch GWAS networking off, with the fix.
const NETWORKING_HINTS = {
  networking_mode_local: 'networking is set to local (WEBWORKS_NETWORKING=local); lobbies then never leave this server',
  waiting_public_url: 'waiting for a public address: accept the Cloudflare tunnel prompt or set WEBWORKS_PUBLIC_URL',
  invalid_public_url: 'the public address is not a usable URL -- it needs to be http(s):// with a host other servers can resolve',
  missing_server_token: 'this server has not claimed its id yet, so GWAS will not publish it; a start with a GWAS connection claims it automatically',
  missing_server_key: 'set WEBWORKS_GWAS_SERVER_KEY to the GWAS worker\'s GWAS_SERVER_KEY so the directory accepts this server',
  no_loopback: 'the server could not find its own listener; restart it (this is a bug, not a setting)',
  'this GWAS does not serve the federation directory': 'deploy the current GWAS worker: its /directory routes are missing',
  'GWAS rejected the server key for the federation directory': 'the GWAS_SERVER_KEY in this server\'s config does not match the worker secret',
  'GWAS rejected this server\'s saved token; delete webworks.config.json to claim it again': 'the saved token no longer works; delete webworks.config.json so the next start claims the id again',
  'GWAS says this server\'s token belongs to a different id': 'the token was issued for another server id; delete data/federation.json and start again',
  directory_unreachable: 'the GWAS directory could not be reached; check WEBWORKS_FEDERATION_URL and this machine\'s connection',
};

function networkingHint(reason) {
  if (!reason) return null;
  return NETWORKING_HINTS[reason] || `the directory stopped this server: ${reason}`;
}

module.exports = { probeGwas, describeGwas, directoryWarnings, networkingHint, friendlyError, ProbeError, DEFAULT_TIMEOUT_MS };
