const fs = require('fs');
const path = require('path');
const readline = require('readline');
const log = require('./log');
const serverClaim = require('./serverClaim');

const SERVER_ROOT = path.resolve(__dirname, '..');

// There is exactly one GWAS address in the setup story. WEBWORKS_GWAS_URL is the escape
// hatch for a self-hosted worker; nobody is asked for it during setup.
const DEFAULT_GWAS_URL = 'https://gwas.webeam.workers.dev';

const DEFAULTS = {
  accountMode: 'gwas',
  gwasUrl: DEFAULT_GWAS_URL,
  gwasApiKey: '',
  host: '0.0.0.0',
  port: 3000,
  dataDir: path.join(SERVER_ROOT, 'data'),
  corsOrigin: '*',
  log: 'normal',
  sessionDays: 30,
  tunnelAllowAny: false,
  tunnelAllowPrivate: false,
  tunnelAllow: [],
  tunnelMaxStreams: 32,
  // Cloudflare quick tunnel: '' asks on the first interactive start, 'quick' always starts
  // one, 'off' never does. The answer is remembered in webworks.config.json.
  publicTunnel: '',
  // GWAS networking: cross-server lobbies. On by default; it needs a public address, which
  // arrives from the Cloudflare tunnel or from WEBWORKS_PUBLIC_URL.
  networkingMode: 'gwas',
  // The token bought with this machine's generated server secret. The secret itself lives
  // in data/federation.json, next to the id it protects.
  serverToken: '',
  serverId: '',
  federationPublicUrl: '',
  federationUrl: '',
  // Only for operators running a whole fleet under the worker's shared GWAS_SERVER_KEY.
  federationServerKey: '',
  federationBridgePath: '/federation/bridge',
  federationHeartbeatMs: 120 * 1000,
  federationLinkIdleMs: 3 * 60 * 1000,
  federationRequestTimeoutMs: 8000,
  federationMaxLinks: 64,
  federationAllowInsecure: false,
  federationAllowedHosts: [],
};

function configPath() {
  return process.env.WEBWORKS_CONFIG || path.join(SERVER_ROOT, 'webworks.config.json');
}

function envOverrides() {
  const env = process.env;
  const out = {};
  if (env.WEBWORKS_ACCOUNT_MODE) out.accountMode = String(env.WEBWORKS_ACCOUNT_MODE).toLowerCase();
  if (env.WEBWORKS_SERVER_TOKEN) out.serverToken = env.WEBWORKS_SERVER_TOKEN;
  if (env.WEBWORKS_GWAS_URL) out.gwasUrl = env.WEBWORKS_GWAS_URL;
  if (env.WEBWORKS_GWAS_API_KEY) out.gwasApiKey = env.WEBWORKS_GWAS_API_KEY;
  if (env.WEBWORKS_HOST) out.host = env.WEBWORKS_HOST;
  // Only a real port is taken from the environment. Hosting platforms often export PORT=0
  // (meaning "any port"), which used to bind an ephemeral port nobody could guess and looked
  // exactly like "localhost:3000 does not work". Ports are 1-65535; anything else is ignored.
  const envPort = Number(env.WEBWORKS_PORT || env.PORT);
  if (Number.isInteger(envPort) && envPort > 0 && envPort <= 65535) out.port = envPort;
  if (env.WEBWORKS_DATA_DIR) out.dataDir = path.resolve(env.WEBWORKS_DATA_DIR);
  if (env.WEBWORKS_CORS_ORIGIN) out.corsOrigin = env.WEBWORKS_CORS_ORIGIN;
  if (env.WEBWORKS_LOG) out.log = env.WEBWORKS_LOG;
  if (env.WEBWORKS_TUNNEL_ALLOW_ANY) out.tunnelAllowAny = env.WEBWORKS_TUNNEL_ALLOW_ANY === '1' || env.WEBWORKS_TUNNEL_ALLOW_ANY.toLowerCase() === 'true';
  if (env.WEBWORKS_TUNNEL_ALLOW_PRIVATE) out.tunnelAllowPrivate = env.WEBWORKS_TUNNEL_ALLOW_PRIVATE === '1' || env.WEBWORKS_TUNNEL_ALLOW_PRIVATE.toLowerCase() === 'true';
  if (env.WEBWORKS_TUNNEL_ALLOW) out.tunnelAllow = env.WEBWORKS_TUNNEL_ALLOW.split(',');
  if (env.WEBWORKS_TUNNEL_MAX_STREAMS) out.tunnelMaxStreams = Number(env.WEBWORKS_TUNNEL_MAX_STREAMS);
  if (env.WEBWORKS_PUBLIC_TUNNEL) out.publicTunnel = env.WEBWORKS_PUBLIC_TUNNEL;

  if (env.WEBWORKS_NETWORKING) out.networkingMode = String(env.WEBWORKS_NETWORKING).toLowerCase();
  if (env.WEBWORKS_PUBLIC_URL) out.federationPublicUrl = env.WEBWORKS_PUBLIC_URL;
  if (env.WEBWORKS_FEDERATION_URL) out.federationUrl = env.WEBWORKS_FEDERATION_URL;
  if (env.WEBWORKS_FEDERATION_SERVER_KEY) out.federationServerKey = env.WEBWORKS_FEDERATION_SERVER_KEY;
  else if (env.WEBWORKS_GWAS_SERVER_KEY) out.federationServerKey = env.WEBWORKS_GWAS_SERVER_KEY;
  if (env.WEBWORKS_FEDERATION_BRIDGE_PATH) out.federationBridgePath = env.WEBWORKS_FEDERATION_BRIDGE_PATH;
  if (env.WEBWORKS_FEDERATION_HEARTBEAT_MS) out.federationHeartbeatMs = Number(env.WEBWORKS_FEDERATION_HEARTBEAT_MS);
  if (env.WEBWORKS_FEDERATION_LINK_IDLE_MS) out.federationLinkIdleMs = Number(env.WEBWORKS_FEDERATION_LINK_IDLE_MS);
  if (env.WEBWORKS_FEDERATION_REQUEST_TIMEOUT_MS) out.federationRequestTimeoutMs = Number(env.WEBWORKS_FEDERATION_REQUEST_TIMEOUT_MS);
  if (env.WEBWORKS_FEDERATION_MAX_LINKS) out.federationMaxLinks = Number(env.WEBWORKS_FEDERATION_MAX_LINKS);
  if (env.WEBWORKS_FEDERATION_ALLOW_INSECURE) out.federationAllowInsecure = env.WEBWORKS_FEDERATION_ALLOW_INSECURE === '1' || env.WEBWORKS_FEDERATION_ALLOW_INSECURE.toLowerCase() === 'true';
  if (env.WEBWORKS_FEDERATION_ALLOWED_HOSTS) out.federationAllowedHosts = env.WEBWORKS_FEDERATION_ALLOWED_HOSTS.split(',');
  return out;
}

function normalize(cfg) {
  const out = Object.assign({}, DEFAULTS, cfg);
  // Setup does not ask which account system to use: this server always uses GWAS. `local`
  // stays for offline development, but only when the environment asks for it, so a config
  // file written by an older version cannot quietly keep a server off the network.
  out.accountMode = String(process.env.WEBWORKS_ACCOUNT_MODE || '').toLowerCase() === 'local' ? 'local' : 'gwas';
  out.gwasUrl = String(out.gwasUrl || DEFAULTS.gwasUrl).replace(/\/+$/, '');
  out.serverToken = String(out.serverToken || '');
  out.serverId = String(out.serverId || '');
  out.port = Number(out.port);
  if (!Number.isInteger(out.port) || out.port < 1 || out.port > 65535) out.port = DEFAULTS.port;
  out.dataDir = path.resolve(SERVER_ROOT, out.dataDir || DEFAULTS.dataDir);
  out.tunnelAllowAny = out.tunnelAllowAny === true || out.tunnelAllowAny === 'true' || out.tunnelAllowAny === 1;
  out.tunnelAllowPrivate = out.tunnelAllowPrivate === true || out.tunnelAllowPrivate === 'true' || out.tunnelAllowPrivate === 1;
  out.tunnelAllow = Array.isArray(out.tunnelAllow)
    ? out.tunnelAllow.map((v) => String(v).trim().toLowerCase()).filter(Boolean)
    : String(out.tunnelAllow || '').split(',').map((v) => v.trim().toLowerCase()).filter(Boolean);
  out.tunnelMaxStreams = Math.max(1, Math.min(256, Number(out.tunnelMaxStreams) || DEFAULTS.tunnelMaxStreams));
  const tunnelChoice = String(out.publicTunnel || '').toLowerCase();
  out.publicTunnel = ['quick', 'yes', '1', 'true', 'on'].includes(tunnelChoice) ? 'quick'
    : ['off', 'no', '0', 'false'].includes(tunnelChoice) ? 'off'
    : '';

  out.networkingMode = String(process.env.WEBWORKS_NETWORKING || out.networkingMode || 'gwas').toLowerCase() === 'local' ? 'local' : 'gwas';
  out.federationUrl = String(out.federationUrl || out.gwasUrl || '').replace(/\/+$/, '');
  out.federationPublicUrl = String(out.federationPublicUrl || '').trim().replace(/\/+$/, '');
  out.federationBridgePath = '/' + String(out.federationBridgePath || 'federation/bridge').replace(/^\/+|\/+$/g, '');
  out.federationHeartbeatMs = Math.max(30000, Number(out.federationHeartbeatMs) || DEFAULTS.federationHeartbeatMs);
  out.federationLinkIdleMs = Math.max(30000, Number(out.federationLinkIdleMs) || DEFAULTS.federationLinkIdleMs);
  out.federationRequestTimeoutMs = Math.max(2000, Number(out.federationRequestTimeoutMs) || DEFAULTS.federationRequestTimeoutMs);
  out.federationMaxLinks = Math.max(2, Math.min(512, Number(out.federationMaxLinks) || DEFAULTS.federationMaxLinks));
  out.federationAllowInsecure = out.federationAllowInsecure === true || out.federationAllowInsecure === 'true' || out.federationAllowInsecure === 1;
  out.federationAllowedHosts = Array.isArray(out.federationAllowedHosts)
    ? out.federationAllowedHosts.map((v) => String(v).trim().toLowerCase()).filter(Boolean)
    : String(out.federationAllowedHosts || '').split(',').map((v) => v.trim().toLowerCase()).filter(Boolean);
  return out;
}

function loadFile() {
  try {
    const file = configPath();
    if (!fs.existsSync(file)) return {};
    return JSON.parse(fs.readFileSync(file, 'utf8'));
  } catch (err) {
    log.warn(`could not read ${configPath()}: ${err.message} -- using defaults`);
    return {};
  }
}

function saveFile(cfg) {
  const persisted = {
    gwasUrl: cfg.gwasUrl || '',
    // The token from the server secret. The secret itself is never written down here.
    serverToken: cfg.serverToken || '',
    serverId: cfg.serverId || '',
    host: cfg.host,
    port: cfg.port,
    dataDir: relativeToRoot(cfg.dataDir),
    corsOrigin: cfg.corsOrigin,
    log: cfg.log,
    sessionDays: cfg.sessionDays,
    tunnelAllowAny: cfg.tunnelAllowAny,
    tunnelAllowPrivate: cfg.tunnelAllowPrivate,
    tunnelAllow: cfg.tunnelAllow,
    tunnelMaxStreams: cfg.tunnelMaxStreams,
    publicTunnel: cfg.publicTunnel,
    federationPublicUrl: cfg.federationPublicUrl,
    // federationUrl is deliberately not saved: it is derived from gwasUrl, so a server that
    // changes its GWAS address never keeps pointing the directory at the old one.
    // Stored locally so a chosen GWAS networking setup survives a restart; the same key
    // can come from WEBWORKS_GWAS_SERVER_KEY instead, which keeps it out of the file.
    federationServerKey: cfg.federationServerKey || '',
    federationBridgePath: cfg.federationBridgePath,
    federationHeartbeatMs: cfg.federationHeartbeatMs,
    federationLinkIdleMs: cfg.federationLinkIdleMs,
    federationMaxLinks: cfg.federationMaxLinks,
    federationAllowInsecure: cfg.federationAllowInsecure,
    federationAllowedHosts: cfg.federationAllowedHosts,
  };
  try {
    fs.writeFileSync(configPath(), JSON.stringify(persisted, null, 2) + '\n');
    return true;
  } catch (err) {
    log.warn(`could not save ${configPath()}: ${err.message}`);
    return false;
  }
}

function relativeToRoot(target) {
  const relative = path.relative(SERVER_ROOT, target);
  if (!relative || relative.startsWith('..') || path.isAbsolute(relative)) return target;
  return './' + relative.split(path.sep).join('/');
}

// Resolves with the typed answer, or with '' when stdin closes. Without the 'close' case a
// prompt at end-of-input (piped answers, a closed terminal) would wait forever and the
// server would never reach listen().
function ask(rl, question) {
  return new Promise((resolve) => {
    let answered = false;
    const finish = (value) => {
      if (answered) return;
      answered = true;
      rl.off('close', onClose);
      resolve(String(value || '').trim());
    };
    const onClose = () => finish('');
    rl.once('close', onClose);
    rl.question(question, (answer) => finish(answer));
  });
}

// This server's own id, and the secret it was born with, from the identity file
// federation uses. Created on demand: the secret is what claims the id at GWAS, and it is
// generated here rather than typed, so setup asks nothing at all.
function currentIdentity(cfg) {
  const identity = require('./federation/identity');
  return identity.init({ dataDir: cfg.dataDir });
}

// Asked once, right after the server is listening, so the answer sits next to the URLs it
// affects. Only the first run asks; after that the saved answer is used until it is changed
// in webworks.config.json or with WEBWORKS_PUBLIC_TUNNEL.
async function askPublicTunnel(current, { interactive = process.stdin.isTTY } = {}) {
  if (!interactive) return current.publicTunnel || 'off';
  const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
  try {
    log.banner('  Cloudflare tunnel');
    log.banner('  -----------------');
    log.banner('  A quick tunnel gives this server a temporary public https:// address, so people');
    log.banner('  outside your network can connect. Cloudflare carries the traffic and anyone with');
    log.banner('  the link can reach this server. wrangler is installed automatically on the first');
    log.banner('  yes, and the answer is remembered in webworks.config.json.');
    log.banner('');
    const answer = (await ask(rl, '  Create a Cloudflare tunnel for this run? [y/N] ')).toLowerCase();
    current.publicTunnel = answer === 'y' || answer === 'yes' ? 'quick' : 'off';
  } finally {
    rl.close();
  }
  return current.publicTunnel;
}

async function resolve() {
  const fromFile = loadFile();
  let cfg = normalize(Object.assign({}, DEFAULTS, fromFile, envOverrides()));

  // Nothing is asked here. The machine generated its own server secret on first start, so
  // claiming the id (and renewing the token when it ages out) is just another request. The
  // only question left anywhere is the tunnel, and that is asked after the server listens.
  //
  // Local accounts are the offline opt-out, so they never reach for the network: a machine
  // with no connection starts immediately instead of waiting on a claim it cannot make.
  if (cfg.accountMode !== 'local' && !serverClaim.isUsable(cfg.serverToken)) {
    const identity = currentIdentity(cfg);
    try {
      const result = await serverClaim.claim({
        url: cfg.gwasUrl,
        serverId: identity.serverId,
        secret: identity.claimSecret,
      });
      cfg.serverToken = result.token;
      cfg.serverId = result.serverId || identity.serverId;
      log.banner(`  server ${cfg.serverId} claimed with GWAS -- cross-server lobbies are on`);
      saveFile(cfg);
    } catch (err) {
      for (const line of serverClaim.explain(err)) log.banner(`  ${line}`);
      if (!fromFile.gwasUrl) saveFile(cfg);
    }
  } else if (!fromFile.gwasUrl) {
    // First run that needs no questions: write the defaults down once so the config file
    // says the same thing the running server does.
    saveFile(cfg);
  }

  cfg = normalize(Object.assign({}, cfg, envOverrides()));
  if (!cfg.gwasUrl) {
    log.error('no GWAS URL is configured. Set WEBWORKS_GWAS_URL.');
    throw new Error('missing_gwas_url');
  }

  log.setLevel(cfg.log);
  return cfg;
}

function resolveSync(overrides = {}) {
  const cfg = normalize(Object.assign({}, DEFAULTS, loadFile(), envOverrides(), overrides));
  log.setLevel(cfg.log);
  return cfg;
}

module.exports = {
  resolve, resolveSync, normalize, saveFile, askPublicTunnel, ask,
  currentIdentity, configPath, SERVER_ROOT, DEFAULTS, DEFAULT_GWAS_URL,
};
