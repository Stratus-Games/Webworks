// Compatibility with every client generation this project has shipped.
//
// The wire protocol never changed: /api/{engine}/{family}/{function}. What did change over
// time is the spelling of the engine prefix (Weeam -> Webeam -> Webworks, Steamworks.net vs
// SteamworksNET), the session header (x-webeam-session -> x-webworks-session) and the env
// prefix (WEBEAM_* -> WEBWORKS_*). Old builds cannot be patched in the field, so the server
// accepts all of those dialects instead. Nothing here changes behaviour for current clients:
// every legacy form is mounted after the canonical one and only fills gaps.

// Prefixes clients are expected to use today (advertised by /health and the startup banner).
const ENGINE_MOUNTS = [
  '/api/Unity/SteamworksNET',
  '/api/Unity/Facepunch',
  '/api/Unreal/Steam',
  '/api/Godot/GodotSteam',
];

// Prefixes older shims and servers used. Order matters only for readability; all of them
// mount the same handlers.
const LEGACY_ENGINE_MOUNTS = [
  '/api/Unity/Steamworks.net',
  '/api/Unity/SteamworksNet',
  '/api/Unity/Steamworks',
  '/api/Unity/Webeam',
  '/api/Unity/Weeam',
  '/api/Unity/Webworks',
  '/api/Unity',
  '/api/Unreal/Steamworks',
  '/api/Unreal',
  '/api/Godot',
  '/api',
];

const ALL_ENGINE_MOUNTS = [...ENGINE_MOUNTS, ...LEGACY_ENGINE_MOUNTS];

// Header spellings from the Weeam/Webeam builds. A browser preflight from one of those
// clients asks for these, so they have to be in the CORS allow list too.
const LEGACY_SESSION_HEADERS = {
  'x-webeam-session': 'x-webworks-session',
  'x-webeam-account': 'x-webworks-account',
};

// Every header a shim, a browser page or the GWAS client may send. Preflight also echoes the
// request's own list (see server.js), so this stays a floor rather than a gate.
const CORS_ALLOW_HEADERS = [
  'Content-Type', 'Authorization', 'X-Requested-With',
  'x-webworks-session', 'x-webworks-account', 'x-webworks-server-token',
  'x-webeam-session', 'x-webeam-account',
  'x-gwas-token', 'x-gwas-server-key', 'x-gwas-server-token',
].join(', ');

const CORS_EXPOSE_HEADERS = 'Content-Type, Content-Length, Date';

// The known headers plus whatever a preflight asked for, so an older client still sees the
// names it expects and a newer one is not blocked by a header this build has not learned yet.
function corsAllowHeaders(asked) {
  const seen = new Set(CORS_ALLOW_HEADERS.split(',').map((name) => name.trim().toLowerCase()));
  const extra = [];
  for (const name of String(asked || '').split(',')) {
    const clean = name.trim();
    if (clean && !seen.has(clean.toLowerCase())) {
      seen.add(clean.toLowerCase());
      extra.push(clean);
    }
  }
  return extra.length ? `${CORS_ALLOW_HEADERS}, ${extra.join(', ')}` : CORS_ALLOW_HEADERS;
}

// Session token as a query parameter. Old WebGL builds could not set a header on a WebSocket
// handshake, so every version has used ?session= there; the same form is accepted on HTTP
// routes for clients that copied that habit.
const QUERY_SESSION_PARAMS = ['session'];

function legacyHeaderAliases(req, res, next) {
  for (const legacy of Object.keys(LEGACY_SESSION_HEADERS)) {
    const canonical = LEGACY_SESSION_HEADERS[legacy];
    if (!req.headers[canonical] && req.headers[legacy]) req.headers[canonical] = req.headers[legacy];
  }

  if (!req.headers['x-webworks-session']) {
    for (const name of QUERY_SESSION_PARAMS) {
      const value = req.query && req.query[name];
      if (typeof value === 'string' && value) {
        req.headers['x-webworks-session'] = value;
        break;
      }
    }
  }

  next();
}

// The 0.3 server accepted WEBEAM_* by rewriting it into WEBWORKS_* before reading anything.
// Deployments that still carry those names (or only the old Accounts Worker URL) keep working.
function applyLegacyEnv(env = process.env) {
  for (const key of Object.keys(env)) {
    if (!key.startsWith('WEBEAM_')) continue;
    const canonical = 'WEBWORKS_' + key.slice('WEBEAM_'.length);
    if (env[canonical] === undefined) env[canonical] = env[key];
  }

  // 0.3 called the state folder WEBWORKS_CACHE_DIR. Without this a deployment that upgrades
  // would silently start from an empty ./data directory.
  if (!env.WEBWORKS_DATA_DIR && env.WEBWORKS_CACHE_DIR) env.WEBWORKS_DATA_DIR = env.WEBWORKS_CACHE_DIR;

  if (env.WEBWORKS_ACCOUNTS_WORKER_URL) {
    // The old accounts worker became GWAS; a deployment that only configured its URL was
    // running remote accounts, so keep that choice instead of silently falling back to local.
    if (!env.WEBWORKS_GWAS_URL) env.WEBWORKS_GWAS_URL = env.WEBWORKS_ACCOUNTS_WORKER_URL;
    if (!env.WEBWORKS_ACCOUNT_MODE) env.WEBWORKS_ACCOUNT_MODE = 'gwas';
  }

  return env;
}

module.exports = {
  ENGINE_MOUNTS,
  LEGACY_ENGINE_MOUNTS,
  ALL_ENGINE_MOUNTS,
  LEGACY_SESSION_HEADERS,
  CORS_ALLOW_HEADERS,
  CORS_EXPOSE_HEADERS,
  corsAllowHeaders,
  QUERY_SESSION_PARAMS,
  legacyHeaderAliases,
  applyLegacyEnv,
};
