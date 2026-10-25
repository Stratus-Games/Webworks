const express = require('express');
const http = require('http');
const https = require('https');
const fs = require('fs');
const path = require('path');

const compat = require('./src/compat');

// Must happen before anything reads process.env: WEBEAM_* names and the old Accounts Worker
// URL keep working on upgraded deployments.
compat.applyLegacyEnv();

const config = require('./src/config');
const log = require('./src/log');
const gwasProbe = require('./src/gwasProbe');
const { renderStatusPage } = require('./src/statusPage');
// The banner, /health and /status all read this one place so a version bump cannot
// leave the server claiming to be an older release than its package.
const VERSION = require('./package.json').version;

// --port/--host win over the config file and the environment, so "the port is taken" has a
// one-line answer: `npm start -- --port 3001`.
function applyCliOverrides() {
  const args = process.argv.slice(2);
  const valueOf = (name) => {
    const index = args.indexOf(name);
    return index >= 0 && args[index + 1] ? args[index + 1] : null;
  };
  const port = valueOf('--port');
  const host = valueOf('--host');
  if (port) process.env.WEBWORKS_PORT = port;
  if (host) process.env.WEBWORKS_HOST = host;
}

async function listen(server, port, host) {
  await new Promise((resolve, reject) => {
    const onError = (err) => { server.off('listening', onListening); reject(err); };
    const onListening = () => { server.off('error', onError); resolve(); };
    server.once('error', onError);
    server.once('listening', onListening);
    server.listen(port, host);
  });
}

// Turn a failed bind into something a person can act on. The common case is a Webworks
// server left running from an earlier terminal.
function explainListenFailure(err, cfg) {
  const port = cfg.port;
  const finder = process.platform === 'win32'
    ? `netstat -ano | findstr :${port}`
    : `lsof -nP -iTCP:${port} -sTCP:LISTEN`;
  const alternatives = `pick another port:            npm start -- --port ${port === 65535 ? 3000 : port + 1}   (or set WEBWORKS_PORT)`;

  if (!err) return [`the server could not start on port ${port}.`];

  if (err.code === 'EADDRINUSE') {
    return [
      `port ${port} is already in use, so the server did not start.`,
      `find the process holding it:  ${finder}`,
      alternatives,
    ];
  }

  // Windows reserves chunks of the ephemeral range for Hyper-V/WSL; binding inside one of
  // them fails with EACCES and looks like a mystery failure without this hint.
  if (err.code === 'EACCES' || err.code === 'EPERM') {
    return [
      `port ${port} cannot be opened on this machine (${err.code}), so the server did not start.`,
      'on Windows this is usually a port reserved by Hyper-V or WSL; see the reserved ranges:',
      '  netsh interface ipv4 show excludedportrange protocol=tcp',
      alternatives,
    ];
  }

  if (err.code === 'EADDRNOTAVAIL') {
    return [
      `${cfg.host} is not an address on this machine, so the server did not start.`,
      'listen on every interface instead:  npm start -- --host 0.0.0.0',
      alternatives,
    ];
  }

  return [`the server could not listen on ${cfg.host}:${port}: ${err.message}`, alternatives];
}

async function main() {
  applyCliOverrides();
  const cfg = await config.resolve();

  // Remote accounts only work if the GWAS deployment answers, and its lobby directory only
  // works if both sides hold the same server key. Ask once, up front, so "it stays local"
  // always comes with the reason instead of being discovered at the first sign-in.
  let gwas = null;
  if (cfg.accountMode === 'gwas') {
    log.banner(`  checking GWAS at ${cfg.gwasUrl} ...`);
    gwas = await gwasProbe.probeGwas(cfg.gwasUrl, {
      serverKey: cfg.federationServerKey,
      serverToken: cfg.serverToken,
      timeoutMs: 5000,
    });
  }

  log.banner('  starting the server...');
  if (!process.env.WEBWORKS_CACHE_DIR) process.env.WEBWORKS_CACHE_DIR = cfg.dataDir;

  const { accounts } = require('./src/accounts');
  accounts.init(cfg);

  const federation = require('./src/federation');
  federation.init(cfg);

  const { state } = require('./src/state');
  const { attachRealtime } = require('./src/realtime');
  const { attachTunnel } = require('./src/tunnel');
  const { startLobbySweeper } = require('./src/lobbies');
  const cloudflareTunnel = require('./src/cloudflareTunnel');
  const leaderboardStore = require('./src/leaderboardStore');
  const workshopStore = require('./src/workshopStore');
  const gameServerStore = require('./src/gameServerStore');
  const relayManager = require('./src/relayManager');
  const cloudStore = require('./src/cloudStore');
  const achievementCatalog = require('./src/achievementCatalog');
  const statsStore = require('./src/statsStore');

  const userRoutes = require('./src/routes/user');
  const accountRoutes = require('./src/routes/accounts');
  const matchmakingRoutes = require('./src/routes/matchmaking');
  const networkingRoutes = require('./src/routes/networking');
  const friendsRoutes = require('./src/routes/friends');
  const userstatsRoutes = require('./src/routes/userstats');
  const remoteStorageRoutes = require('./src/routes/remotestorage');
  const workshopRoutes = require('./src/routes/workshop');
  const gameserverRoutes = require('./src/routes/gameserver');
  const extrasRoutes = require('./src/routes/extras');
  const mediaRoutes = require('./src/routes/media');
  const mediaStore = require('./src/mediaStore');

  const app = express();
  app.disable('x-powered-by');

  // Any origin by default: a server on localhost, a LAN address, a tunnel URL or a custom
  // domain all reach the same routes. A browser preflight that asks for headers this build
  // does not know yet gets its own list echoed back, so a newer client is never blocked.
  app.use((req, res, next) => {
    const requested = req.headers['access-control-request-headers'];
    res.setHeader('Access-Control-Allow-Origin', cfg.corsOrigin);
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PATCH, DELETE, OPTIONS');
    res.setHeader(
      'Access-Control-Allow-Headers',
      compat.corsAllowHeaders(cfg.corsOrigin === '*' ? requested : ''),
    );
    res.setHeader('Access-Control-Expose-Headers', compat.CORS_EXPOSE_HEADERS);
    res.setHeader('Access-Control-Max-Age', '600');
    if (cfg.corsOrigin !== '*') res.setHeader('Vary', 'Origin');
    if (req.method === 'OPTIONS') return res.status(204).end();
    next();
  });

  app.use(express.json({ limit: '8mb' }));

  // Old clients send x-webeam-* (or ?session=) instead of the current header names.
  app.use(compat.legacyHeaderAliases);

  const engineMounts = compat.ENGINE_MOUNTS;

  // Canonical mounts first, then every historical spelling without duplicating handlers.
  for (const base of compat.ALL_ENGINE_MOUNTS) {
    app.use(`${base}/Accounts`, accountRoutes);
    app.use(`${base}/User`, userRoutes);
    app.use(`${base}/Matchmaking`, matchmakingRoutes);
    app.use(`${base}/Networking`, networkingRoutes);
    app.use(`${base}/Friends`, friendsRoutes);
    app.use(`${base}/UserStats`, userstatsRoutes);
    app.use(`${base}/RemoteStorage`, remoteStorageRoutes);
    app.use(`${base}/UGC`, workshopRoutes);
    app.use(`${base}/GameServer`, gameserverRoutes);
    app.use(`${base}`, mediaRoutes);
    app.use(`${base}`, extrasRoutes);
  }

  app.use('/accounts', accountRoutes);

  // The function tester: one static page (plus the little arena game it hosts) that drives
  // every route this server mounts from a browser. Served from the server itself so it is
  // same-origin and needs no CORS or config.
  const harnessDir = path.join(__dirname, 'public', 'harness');
  app.use('/harness', express.static(harnessDir, { index: 'index.html', extensions: ['html'] }));
  app.get('/test', (req, res) => res.redirect(302, '/harness/'));

  // Filled in once the listener is up: the addresses the server is reachable on, locally
  // and through an optional Cloudflare tunnel.
  const runtime = { urls: [], publicUrl: null, tunnel: null };

  const statusPayload = () => {
    const fed = federation.status();
    return {
      ok: true,
      service: 'webworks-server',
      version: VERSION,
      urls: runtime.urls,
      publicUrl: runtime.publicUrl,
      accounts: accounts.mode,
      accountsDetail: {
        mode: accounts.mode,
        url: accounts.mode === 'gwas' ? cfg.gwasUrl : null,
        reachable: gwas ? gwas.reachable : null,
        error: gwas ? gwas.error : null,
        storage: gwas ? gwas.storage : null,
        directory: gwas ? gwas.directory : null,
      },
      engines: compat.ENGINE_MOUNTS,
      legacyEngines: compat.LEGACY_ENGINE_MOUNTS,
      online: state.sockets.size,
      onlineByGame: require('./src/presence').onlineByGame(),
      games: [...state.knownApps],
      lobbies: state.lobbies.size,
      networking: fed,
      networkingHint: fed.enabled && fed.publishing ? null : gwasProbe.networkingHint(fed.disabledReason),
    };
  };

  // A browser opening the server address gets a status page instead of Express's
  // "Cannot GET /", which looked like the server had failed to start.
  app.get('/', (req, res) => {
    res.setHeader('Content-Type', 'text/html; charset=utf-8');
    res.setHeader('Cache-Control', 'no-store');
    res.send(renderStatusPage(statusPayload()));
  });

  app.get('/health', (req, res) => res.json(statusPayload()));
  app.get('/status', (req, res) => res.json(statusPayload()));

  app.use((err, req, res, next) => {
    if (res.headersSent) return next(err);
    if (err && err.type === 'entity.too.large') return res.status(413).json({ error: 'body_too_large' });
    log.error('unhandled request error:', err && err.stack ? err.stack : err);
    res.status(500).json({ error: 'internal_error' });
  });

  const useTls = !!(process.env.WEBWORKS_TLS_CERT && process.env.WEBWORKS_TLS_KEY);
  const server = useTls
    ? https.createServer({ cert: fs.readFileSync(process.env.WEBWORKS_TLS_CERT), key: fs.readFileSync(process.env.WEBWORKS_TLS_KEY) }, app)
    : http.createServer(app);

  attachRealtime(server);
  attachTunnel(server, {
    allowAny: cfg.tunnelAllowAny,
    allowPrivate: cfg.tunnelAllowPrivate,
    allow: cfg.tunnelAllow,
    maxStreams: cfg.tunnelMaxStreams,
  });

  const cache = leaderboardStore.init();
  const workshop = workshopStore.init();
  achievementCatalog.init();
  statsStore.init();
  startLobbySweeper();
  setInterval(() => gameServerStore.sweep(), gameServerStore.HEARTBEAT_TIMEOUT_MS).unref?.();
  setInterval(() => relayManager.sweep(), Math.min(relayManager.IDLE_TIMEOUT_MS, 30000)).unref?.();
  setInterval(() => mediaStore.sweep(), 60000).unref?.();

  const shutdown = () => {
    try { leaderboardStore.saveNow(); } catch {  }
    try { statsStore.saveNow(); } catch {  }
    try { relayManager.shutdownAll(); } catch {  }
    try { federation.shutdown(); } catch {  }
    try { if (runtime.tunnel) runtime.tunnel.stop(); } catch {  }
    process.exit(0);
  };
  for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, shutdown);
  process.on('exit', () => {
    leaderboardStore.saveNow(); statsStore.saveNow(); relayManager.shutdownAll();
    try { if (runtime.tunnel) runtime.tunnel.stop(); } catch {  }
  });

  try {
    await listen(server, cfg.port, cfg.host);
  } catch (err) {
    for (const line of explainListenFailure(err, cfg)) log.error(line);
    process.exit(1);
  }

  const actualPort = server.address().port;
  // The peer bridge shares this listener, so tickets are verified by the same process
  // that owns the lobbies. It has to come after listen() to know its loopback port.
  federation.attach(server);
  const scheme = useTls ? 'https' : 'http';
  const wsScheme = useTls ? 'wss' : 'ws';
  const line = '='.repeat(64);
  log.banner('');
  log.banner(line);
  log.banner(`  WEBWORKS ${VERSION}  --  one server, every game`);
  log.banner(line);
  log.banner(`  accounts   ${accounts.label}`);
  if (accounts.mode === 'local') {
    log.banner(`             ${path.dirname(cache.file)}/accounts.json`);
  } else if (gwas) {
    const described = gwasProbe.describeGwas(gwas);
    log.banner(`             ${described.line}`);
    for (const warning of described.warnings) log.banner(`             ${warning}`);
  }
  runtime.urls = cloudflareTunnel.localUrls(cfg.host, actualPort, scheme);
  log.banner(`  listening  ${runtime.urls.join('   ')}   (health: /health, status: /status)`);
  log.banner(`  realtime   ${wsScheme}://localhost:${actualPort}/realtime`);
  log.banner(`  tunnel     ${wsScheme}://localhost:${actualPort}/tunnel   (raw tcp/udp relay${cfg.tunnelAllowAny ? ', any target' : ''})`);
  if (cfg.networkingMode === 'gwas') {
    const fed = federation.status();
    if (fed.publishing) {
      log.banner(`  networking gwas   ${fed.serverId} publishes at ${fed.directory.bridgeUrl}`);
      log.banner(`             ${fed.authMode === 'claim' ? 'signed in with the secret this machine generated' : 'using the shared server key'} -- directory every ${Math.round(fed.directory.heartbeatMs / 1000)}s while lobbies are live`);
    } else {
      log.banner(`  networking gwas   waiting for a public address (${fed.disabledReason || 'none yet'})`);
      const hint = gwasProbe.networkingHint(fed.disabledReason);
      if (hint) log.banner(`             ${hint}`);
    }
  } else {
    log.banner(`  networking local  every lobby is hosted by this server (unset WEBWORKS_NETWORKING=local to bridge)`);
  }
  log.banner(`  clients    ${engineMounts.join('  ')}`);
  log.banner(`  legacy     ${compat.LEGACY_ENGINE_MOUNTS.length} historical mounts, x-webeam-* headers, WEBEAM_* env`);
  log.banner(`  storage    ${cfg.dataDir}`);
  log.banner(`             leaderboards top ${cache.max} per board per game, cloud ${cloudStore.QUOTA} bytes per user per game`);
  log.banner(`             workshop ${workshop.items} item${workshop.items === 1 ? '' : 's'} loaded`);
  log.banner(`  logs       ${cfg.log === 'verbose' ? 'verbose' : 'minimal'} -- start with WEBWORKS_LOG=verbose for detail`);
  log.banner(`  exit       Ctrl+C to stop (state is flushed)`);
  log.banner(line);

  // Optional Cloudflare Quick Tunnel: asked once on the first interactive start, then
  // remembered. `--tunnel` / `--no-tunnel` override it for a single run.
  const noPrompt = process.argv.includes('--no-prompt') || process.env.WEBWORKS_NO_PROMPT === '1';
  const forced = process.argv.includes('--tunnel') ? 'quick'
    : process.argv.includes('--no-tunnel') ? 'off'
    : cfg.publicTunnel;

  let tunnelChoice = forced;
  if (!tunnelChoice) {
    tunnelChoice = await config.askPublicTunnel(cfg, { interactive: process.stdin.isTTY && !noPrompt });
    if (process.stdin.isTTY && !noPrompt) config.saveFile(cfg);
  }

  if (tunnelChoice === 'quick') {
    // Not awaited: the server is already serving, and the URL arrives when Cloudflare
    // assigns it (wrangler may spend a while downloading cloudflared on the first run).
    cloudflareTunnel.startQuickTunnel({ port: actualPort, scheme, log })
      .then((started) => {
        if (!started) return;
        runtime.publicUrl = started.url;
        runtime.tunnel = started;
        log.banner('');
        log.banner(`  tunnel     ${started.url}`);
        log.banner('             public and temporary -- share it only with people you trust');

        // That address is also what other node servers dial, so the lobby bridge starts
        // publishing on it right away instead of waiting for a restart.
        const fed = federation.setPublicUrl(started.url);
        if (fed && fed.publishing) {
          log.banner(`  networking gwas   ${fed.serverId} publishes at ${fed.directory.bridgeUrl}`);
        }
        log.banner('');
      })
      .catch((err) => log.warn(`Cloudflare tunnel failed: ${err.message}`));
  }

  log.banner('');

  return { app, server, cfg, runtime };
}

if (require.main === module) {
  main().catch((err) => {
    if (err && err.message === 'missing_gwas_url') process.exit(1);
    log.error(err && err.stack ? err.stack : err);
    process.exit(1);
  });
}

module.exports = { main, VERSION };
