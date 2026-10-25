// GWAS networking: cross-server lobbies over direct server-to-server links.
//
// The directory (GWAS) is a phone book and an introduction service. It lists which
// node server hosts which live lobby and hands out a one-time ticket when a player on
// server B wants to join a lobby that lives on server A. A verifies that ticket with
// its own secret, B dials A's bridge itself, and from then on lobby traffic, chat and
// pushes travel over that link. No game data and no packets pass through a worker.
//
// Direction is one way on purpose: the server that hosts the lobby never dials anybody.
// It only accepts a websocket that carries a ticket GWAS minted for exactly this
// server, for exactly one lobby, for a couple of minutes, once.

const crypto = require('crypto');
const { WebSocketServer, WebSocket } = require('ws');

const { state } = require('../state');
const log = require('../log');
const compat = require('../compat');
const directory = require('./directory');
const identity = require('./identity');
const { verify, NonceCache } = require('./tickets');
const { assertSafeBridgeUrl } = require('./targets');

const MAX_FRAME_BYTES = 64 * 1024;
const MAX_RESPONSE_BYTES = 48 * 1024;
const MAX_BODY_CHARS = 32 * 1024;
const REQUEST_TIMEOUT_MS = 8000;
const HANDSHAKE_TIMEOUT_MS = 8000;
const PING_MS = 20 * 1000;
const PONG_TIMEOUT_MS = 60 * 1000;
const DEFAULT_IDLE_MS = 3 * 60 * 1000;
const DEFAULT_MAX_LINKS = 64;
const MAX_REQUESTS_PER_SECOND = 60;
const MAX_PLAYERS_PER_LINK = 256;

// Only lobby operations are forwarded. Anything account-shaped, filesystem-shaped or
// server-shaped would be a privilege problem, so it simply is not on this list.
const ALLOWED_OPS = new Set([
  'JoinLobby',
  'LeaveLobby',
  'Lobby',
  'LobbyMembers',
  'LobbyData',
  'MemberData',
  'ChatSend',
  'LobbyVisibility',
  'LobbyJoinable',
  'LobbyMaxMembers',
  'LobbyOwner',
  'LobbyLinked',
  'LobbyGameServer',
  'CreateInvite',
  'InviteFriend',
]);

class FederationError extends Error {
  constructor(code, status) {
    super(code);
    this.name = 'FederationError';
    this.code = code;
    this.status = status || 502;
  }
}

let settings = null; // { bridgePath, maxLinks, idleMs, allowInsecure, allowedHosts, requestTimeoutMs }
let httpServer = null;
let loopbackBase = null;
let wss = null;
let enabled = false;
let disabledReason = null;
let canonicalMount = null;
let gateway = null; // { url, heartbeatMs } -- the GWAS the directory lives on
let auth = null; // { token, key }
let publishedUrl = null;

const nonces = new NonceCache();
const links = new Map(); // linkId -> link
const linksByPeer = new Map(); // peerId -> Set<link>
const linksByLobby = new Map(); // `${appId}/${lobbyId}` -> link (dialer side)
const linksBySteam = new Map(); // steamId -> link (host side, for pushes)
const fedSessions = new Map(); // `${linkId}|${steamId}` -> session token (host side)
const pending = new Map(); // request id -> { link, resolve }
const dialing = new Map(); // `${appId}/${lobbyId}` -> Promise<link>
const upgradeAttempts = new Map(); // ip -> [timestamps]

function nowMs() {
  return Date.now();
}

function isOpen(link) {
  return Boolean(link && link.open && link.socket && link.socket.readyState === WebSocket.OPEN);
}

function send(link, message) {
  if (!isOpen(link)) return false;
  try {
    link.socket.send(JSON.stringify(message));
    link.lastActivity = nowMs();
    return true;
  } catch {
    return false;
  }
}

// --- lifecycle ---------------------------------------------------------------

function init(cfg) {
  if (!cfg || String(cfg.networkingMode || 'gwas').toLowerCase() !== 'gwas') {
    disabledReason = 'networking_mode_local';
    return status();
  }

  const self = identity.init({ dataDir: cfg.dataDir });
  const bridgePath = normalizePath(cfg.federationBridgePath || '/federation/bridge');

  auth = {
    token: String(cfg.serverToken || '').trim(),
    // Only for operators running a whole fleet under the worker's shared secret.
    key: String(cfg.federationServerKey || process.env.WEBWORKS_GWAS_SERVER_KEY || '').trim(),
  };
  if (!auth.token && !auth.key) {
    disabledReason = 'missing_server_token';
    log.warn('GWAS networking needs this server\'s claim token; cross-server lobbies stay local.');
    return status();
  }

  settings = {
    bridgePath,
    maxLinks: Math.max(2, Number(cfg.federationMaxLinks) || DEFAULT_MAX_LINKS),
    idleMs: Math.max(30000, Number(cfg.federationLinkIdleMs) || DEFAULT_IDLE_MS),
    allowInsecure: Boolean(cfg.federationAllowInsecure),
    allowedHosts: cfg.federationAllowedHosts || [],
    requestTimeoutMs: Math.max(2000, Number(cfg.federationRequestTimeoutMs) || REQUEST_TIMEOUT_MS),
  };

  gateway = {
    url: cfg.federationUrl || cfg.gwasUrl,
    heartbeatMs: cfg.federationHeartbeatMs,
  };

  enabled = true;
  disabledReason = null;

  if (settings.allowInsecure) {
    log.warn('GWAS networking: insecure targets allowed (ws:// and private addresses). Development only -- production needs wss://.');
  }

  // A real public address may already be known (WEBWORKS_PUBLIC_URL, or a saved one). If
  // not, publishing waits for the Cloudflare tunnel and starts the moment its URL lands.
  const publicUrl = String(cfg.federationPublicUrl || '').trim();
  if (publicUrl) publish(publicUrl);
  else {
    disabledReason = 'waiting_public_url';
    log.info(`GWAS networking: ${self.serverId} is ready and publishes as soon as it has a public address`);
  }
  return status();
}

// Turns a public address into the bridge URL and starts heartbeating. Called at startup when
// the address is configured, and again the moment a tunnel reports one.
function publish(publicUrl) {
  if (!enabled || publishedUrl) return status();

  let bridgeUrl;
  try {
    const url = new URL(String(publicUrl).trim());
    url.protocol = url.protocol === 'https:' ? 'wss:' : url.protocol === 'http:' ? 'ws:' : url.protocol;
    url.pathname = settings.bridgePath;
    url.search = '';
    url.hash = '';
    bridgeUrl = url.toString();
  } catch {
    disabledReason = 'invalid_public_url';
    log.warn(`GWAS networking: ${publicUrl} is not a URL; cross-server lobbies stay local.`);
    return status();
  }

  const targetCheck = require('./targets').parseBridgeUrl(bridgeUrl, {
    allowInsecure: settings.allowInsecure,
    allowedHosts: settings.allowedHosts,
  });
  if (!targetCheck.ok) {
    disabledReason = targetCheck.error;
    log.warn(`GWAS networking: bridge URL ${bridgeUrl} is not usable (${targetCheck.error}); cross-server lobbies stay local.`);
    return status();
  }

  const self = identity.current();
  const dir = directory.init({
    url: gateway.url,
    key: auth.key,
    token: auth.token,
    serverId: self.serverId,
    secret: self.secret,
    publicKey: self.publicKey,
    bridgeUrl,
    heartbeatMs: gateway.heartbeatMs,
    allowInsecure: settings.allowInsecure,
    allowedHosts: settings.allowedHosts,
  });
  dir.start();

  publishedUrl = bridgeUrl;
  disabledReason = null;
  log.info(`GWAS networking: ${self.serverId} publishes at ${bridgeUrl}`);
  return status();
}

// A tunnel just came up: start publishing on its address without a restart.
function setPublicUrl(publicUrl) {
  if (!enabled) return status();
  if (publishedUrl) return status();
  const result = publish(publicUrl);
  if (publishedUrl) log.info(`GWAS networking: publishing at ${publishedUrl}`);
  return result;
}

function attach(server) {
  if (!enabled) return null;
  httpServer = server;
  const address = server.address();
  if (!address || typeof address === 'string') {
    disabledReason = 'no_loopback';
    log.warn('GWAS networking cannot route forwarded lobby calls without a TCP listener; cross-server lobbies stay off.');
    enabled = false;
    return null;
  }
  loopbackBase = `http://127.0.0.1:${address.port}`;
  canonicalMount = compat.ENGINE_MOUNTS[0];

  wss = new WebSocketServer({ noServer: true, maxPayload: MAX_FRAME_BYTES, perMessageDeflate: false });
  wss.on('connection', onIncoming);
  server.on('upgrade', onUpgrade);

  const timer = setInterval(sweep, 30000);
  if (timer.unref) timer.unref();

  // Half-open TCP is common after a laptop sleeps; a websocket ping every 20s with a
  // missing pong is what actually tears the link (and its federated players) down.
  const pinger = setInterval(() => {
    for (const link of [...links.values()]) {
      if (!isOpen(link)) continue;
      if (link.socket.isAlive === false || nowMs() - (link.lastPongAt || 0) > PONG_TIMEOUT_MS) {
        destroyLink(link, 'unresponsive');
        continue;
      }
      link.socket.isAlive = false;
      try {
        link.socket.ping();
      } catch {
        destroyLink(link, 'unresponsive');
      }
    }
  }, PING_MS);
  if (pinger.unref) pinger.unref();

  return wss;
}

async function shutdown() {
  for (const link of [...links.values()]) destroyLink(link, 'shutdown');
  await directory.stop();
  enabled = false;
}

function normalizePath(value) {
  const path = '/' + String(value || '').replace(/^\/+/, '');
  return path.replace(/\/+$/, '') || '/federation/bridge';
}

// --- inbound bridge ----------------------------------------------------------

function allowUpgradeAttempt(ip) {
  const now = nowMs();
  const list = (upgradeAttempts.get(ip) || []).filter((t) => now - t < 60000);
  if (list.length >= 30) {
    upgradeAttempts.set(ip, list);
    return false;
  }
  list.push(now);
  upgradeAttempts.set(ip, list);
  return true;
}

function refuse(socket, status, code) {
  try {
    socket.write(`HTTP/1.1 ${status} ${code}\r\nConnection: close\r\nContent-Length: 0\r\n\r\n`);
  } catch {
    // socket already gone
  }
  socket.destroy();
}

function onUpgrade(req, socket, head) {
  let url;
  try {
    url = new URL(req.url, 'http://localhost');
  } catch {
    return;
  }
  if (url.pathname !== settings.bridgePath) return;

  const ip = req.socket.remoteAddress || 'unknown';
  if (!allowUpgradeAttempt(ip)) return refuse(socket, 429, 'Too Many Requests');
  if (links.size >= settings.maxLinks) return refuse(socket, 503, 'Too Many Links');

  const ticket = url.searchParams.get('ticket') || '';
  const claims = verify(identity.current().secret, ticket);
  if (!claims || claims.to !== identity.current().serverId) return refuse(socket, 403, 'Forbidden');
  if (!nonces.take(claims.nonce, claims.exp)) return refuse(socket, 409, 'Ticket Already Used');

  wss.handleUpgrade(req, socket, head, (ws) => wss.emit('connection', ws, req, claims));
}

function onIncoming(socket, req, claims) {
  const peerLinks = linksByPeer.get(claims.from);
  if (peerLinks && peerLinks.size > 4) {
    socket.close(4005, 'too_many_peer_links');
    return;
  }

  const link = createLink({
    role: 'host',
    peerId: claims.from,
    appId: claims.appId,
    lobbyId: claims.lobbyId,
    socket,
  });

  socket.isAlive = true;
  socket.on('pong', () => {
    socket.isAlive = true;
    link.lastPongAt = nowMs();
    link.lastActivity = nowMs();
  });

  const signature = identity.signHandshake(link.id, claims.from);
  send(link, { t: 'welcome', linkId: link.id, sig: signature });
  log.info(`federation link up: ${claims.from} -> ${claims.appId}/${claims.lobbyId} (${link.id})`);

  socket.on('message', (raw) => onMessage(link, raw));
  socket.on('error', () => {  });
  socket.on('close', () => destroyLink(link, 'peer_closed'));
}

// --- outbound dial -----------------------------------------------------------

async function ensureLink(appId, lobbyId) {
  const key = `${appId}/${lobbyId}`;
  const existing = linksByLobby.get(key);
  if (isOpen(existing)) return existing;
  if (dialing.has(key)) return dialing.get(key);

  const promise = dial(appId, lobbyId).finally(() => dialing.delete(key));
  dialing.set(key, promise);
  return promise;
}

async function dial(appId, lobbyId) {
  const dir = directory.current();
  if (!dir || !dir.enabled) throw new FederationError('directory_disabled', 503);
  if (links.size >= settings.maxLinks) throw new FederationError('too_many_links', 503);

  const known = dir.findLobby(appId, lobbyId);
  const intro = await dir.rendezvous({ appId, lobbyId, toServerId: known ? known.serverId : undefined });

  const safe = await assertSafeBridgeUrl(intro.bridgeUrl, {
    allowInsecure: settings.allowInsecure,
    allowedHosts: settings.allowedHosts,
  });
  if (!safe.ok) throw new FederationError(safe.error, 403);
  if (!intro.publicKey) throw new FederationError('peer_identity_missing', 503);

  const url = new URL(safe.url.toString());
  url.searchParams.set('ticket', intro.ticket);

  const socket = new WebSocket(url.toString(), {
    handshakeTimeout: HANDSHAKE_TIMEOUT_MS,
    maxPayload: MAX_FRAME_BYTES,
    perMessageDeflate: false,
  });

  const link = createLink({ role: 'dialer', peerId: intro.peerId, appId, lobbyId, socket, publicKey: intro.publicKey });
  // Attach before the handshake: the host's signed welcome is the very first frame and
  // would otherwise be emitted before anyone is listening.
  socket.on('message', (raw) => onMessage(link, raw));

  await new Promise((resolve, reject) => {
    const timer = setTimeout(() => fail(new FederationError('federation_handshake_timeout', 504)), HANDSHAKE_TIMEOUT_MS);
    const onError = () => fail(new FederationError('federation_unreachable', 503));
    const onClose = () => fail(new FederationError('federation_closed', 503));
    function fail(err) {
      clearTimeout(timer);
      socket.removeListener('error', onError);
      socket.removeListener('close', onClose);
      link.ready = false;
      destroyLink(link, 'handshake_failed');
      reject(err);
    }
    link.onWelcome = (message) => {
      // The host signs its own link id and our server id, so a certificate for the
      // right hostname is not the only thing standing between us and an impostor.
      const verified = identity.verifyHandshake(String(message.linkId || ''), identity.current().serverId, message.sig, intro.publicKey);
      if (!verified) return fail(new FederationError('peer_identity_invalid', 403));
      clearTimeout(timer);
      socket.removeListener('error', onError);
      socket.removeListener('close', onClose);
      link.ready = true;
      resolve();
    };
    socket.once('error', onError);
    socket.once('close', onClose);
  });

  socket.on('error', () => {  });
  socket.on('close', () => destroyLink(link, 'closed'));
  socket.isAlive = true;
  socket.on('pong', () => {
    socket.isAlive = true;
    link.lastPongAt = nowMs();
    link.lastActivity = nowMs();
  });

  log.info(`federation link up: ${identity.current().serverId} -> ${intro.peerId} (${appId}/${lobbyId})`);
  return link;
}

// --- link bookkeeping --------------------------------------------------------

function createLink({ role, peerId, appId, lobbyId, socket, publicKey }) {
  const link = {
    id: 'lnk_' + crypto.randomBytes(8).toString('hex'),
    role,
    peerId: String(peerId),
    appId: String(appId),
    lobbyId: String(lobbyId),
    socket,
    publicKey: publicKey || null,
    open: true,
    ready: role === 'host',
    players: new Set(),
    lastActivity: nowMs(),
    lastPongAt: nowMs(),
    windowStart: nowMs(),
    windowCount: 0,
    onWelcome: null,
  };

  links.set(link.id, link);
  if (!linksByPeer.has(link.peerId)) linksByPeer.set(link.peerId, new Set());
  linksByPeer.get(link.peerId).add(link);
  if (role === 'dialer') linksByLobby.set(`${link.appId}/${link.lobbyId}`, link);
  return link;
}

function destroyLink(link, reason) {
  if (!link.open) return;
  link.open = false;
  links.delete(link.id);

  const peers = linksByPeer.get(link.peerId);
  if (peers) {
    peers.delete(link);
    if (peers.size === 0) linksByPeer.delete(link.peerId);
  }

  const lobbyKey = `${link.appId}/${link.lobbyId}`;
  if (linksByLobby.get(lobbyKey) === link) linksByLobby.delete(lobbyKey);

  for (const steamId of [...link.players]) dropFederated(link, steamId);

  for (const [id, waiter] of pending) {
    if (waiter.link !== link) continue;
    pending.delete(id);
    waiter.resolve({ error: new FederationError('federation_linked_closed', 503) });
  }

  try {
    link.socket.terminate();
  } catch {
    // already closed
  }
  if (reason !== 'shutdown') log.info(`federation link down: ${link.id} (${reason})`);
}

function sweep() {
  const cutoff = nowMs() - settings.idleMs;
  for (const link of [...links.values()]) {
    if (!isOpen(link)) {
      destroyLink(link, 'stale');
      continue;
    }
    // A hold on players is an active game session: keep the link. Otherwise a link
    // nobody has used for a few minutes is closed and re-established on demand.
    if (link.players.size === 0 && link.lastActivity < cutoff) destroyLink(link, 'idle');
  }

  const now = nowMs();
  for (const [ip, list] of upgradeAttempts) {
    const kept = list.filter((t) => now - t < 60000);
    if (kept.length === 0) upgradeAttempts.delete(ip);
    else upgradeAttempts.set(ip, kept);
  }
  nonces.prune(now);
}

// --- frames ------------------------------------------------------------------

function onMessage(link, raw) {
  if (!link.open) return;
  let message;
  try {
    message = JSON.parse(raw.toString());
  } catch {
    return;
  }
  if (!message || typeof message !== 'object' || typeof message.t !== 'string') return;
  link.lastActivity = nowMs();

  if (message.t === 'welcome') {
    if (link.onWelcome) link.onWelcome(message);
    return;
  }
  if (message.t === 'response') {
    const waiter = pending.get(message.id);
    if (!waiter || waiter.link !== link) return;
    pending.delete(message.id);
    waiter.resolve({ status: Number(message.status) || 502, body: message.body });
    return;
  }
  if (message.t === 'push') {
    // Only a hosting server may push to the players of the server that dialled it. The
    // other direction would let a peer inject arbitrary realtime events into clients.
    if (link.role !== 'dialer') return;
    if (typeof message.to !== 'string' || !message.message || typeof message.message !== 'object') return;
    // Delivered to a local socket only: a push must never bounce back over a link.
    const { pushLocal } = require('../realtime');
    pushLocal(message.to, message.message);
    return;
  }
  if (message.t === 'leave') {
    if (link.role !== 'host' || typeof message.steamId !== 'string') return;
    dropFederated(link, message.steamId);
    return;
  }
  if (message.t === 'request') {
    if (link.role !== 'host') return;
    handleRequest(link, message);
  }
}

function withinRate(link) {
  const now = nowMs();
  if (now - link.windowStart >= 1000) {
    link.windowStart = now;
    link.windowCount = 0;
  }
  link.windowCount++;
  return link.windowCount <= MAX_REQUESTS_PER_SECOND;
}

const REQUEST_ERROR_STATUS = {
  bad_request: 400,
  bad_path: 400,
  bad_player: 400,
  operation_not_allowed: 403,
  lobby_not_federated: 403,
  app_mismatch: 403,
};

// Everything a peer is allowed to ask for, checked against the single lobby its ticket
// granted. Kept pure so it can be tested without a live link.
function validateRequest(grant, message) {
  if (!message || typeof message !== 'object') return { error: 'bad_request' };
  if (typeof message.id !== 'string' || message.id.length === 0 || message.id.length > 64) return { error: 'bad_request' };
  if (typeof message.path !== 'string' || message.path.length > 120) return { error: 'bad_path' };

  const op = message.path.replace(/^\/+/, '').split('?')[0];
  if (op.includes('/') || !ALLOWED_OPS.has(op)) return { error: 'operation_not_allowed' };

  const body = message.body && typeof message.body === 'object' ? message.body : {};
  const query = message.query && typeof message.query === 'object' ? message.query : {};
  const lobbyId = String(body.lobbyId || query.lobbyId || '');
  if (lobbyId !== String(grant.lobbyId)) return { error: 'lobby_not_federated' };

  const player = message.player;
  if (!player || typeof player.steamId !== 'string' || !/^[A-Za-z0-9_-]{1,32}$/.test(player.steamId)) {
    return { error: 'bad_player' };
  }
  if (String(player.appId) !== String(grant.appId)) return { error: 'app_mismatch' };

  return { op, body, query, player };
}

async function handleRequest(link, message) {
  const fail = (status, error, id = message.id) => send(link, { t: 'response', id, status, body: { error } });

  if (!withinRate(link)) return fail(429, 'rate_limited');

  const request = validateRequest(link, message);
  if (request.error) return fail(REQUEST_ERROR_STATUS[request.error] || 400, request.error);
  const { op, body, query, player } = request;

  // A SteamID that is genuinely connected here must not be shadowed by a peer.
  if (state.sockets.has(player.steamId)) return fail(409, 'player_local');
  const claimed = state.federatedPlayers.get(String(player.steamId));
  if (claimed && claimed.linkId !== link.id) return fail(409, 'player_conflict');

  if (link.players.size >= MAX_PLAYERS_PER_LINK && !link.players.has(player.steamId)) {
    return fail(429, 'too_many_players');
  }

  const token = federatedToken(link, player);
  if (!token) return fail(409, 'player_conflict');

  const result = await dispatch(op, message.method, body, query, token);
  if (result.error) return fail(result.status || 502, result.error);
  send(link, { t: 'response', id: message.id, status: result.status, body: result.body });
}

function federatedToken(link, player) {
  const key = `${link.id}|${player.steamId}`;
  const existing = fedSessions.get(key);
  if (existing && state.sessions.has(existing)) return existing;

  const token = 'fed_' + crypto.randomBytes(24).toString('hex');
  const session = {
    steamId: String(player.steamId),
    name: String(player.name || '').slice(0, 64) || 'Player',
    appId: link.appId,
    kind: 'player',
    createdAt: nowMs(),
    federated: { peerId: link.peerId, linkId: link.id, lobbyId: link.lobbyId },
  };

  fedSessions.set(key, token);
  state.sessions.set(token, session);
  // So lobby snapshots and chat lines show the player's name on this server too.
  if (!state.names.get(session.steamId)) state.names.set(session.steamId, session.name);
  state.federatedPlayers.set(session.steamId, { linkId: link.id, peerId: link.peerId, appId: link.appId });
  link.players.add(session.steamId);
  linksBySteam.set(session.steamId, link);
  return token;
}

function dropFederated(link, steamId) {
  // On the dialing side "players" only records who to send a leave for; those
  // SteamIDs are connected to *that* server, so its lobby state is not ours to touch.
  if (link.role !== 'host') {
    link.players.delete(steamId);
    return;
  }

  const key = `${link.id}|${steamId}`;
  const token = fedSessions.get(key);
  if (token) {
    fedSessions.delete(key);
    state.sessions.delete(token);
  }
  link.players.delete(steamId);
  if (linksBySteam.get(steamId) === link) linksBySteam.delete(steamId);
  if (state.federatedPlayers.get(steamId) && state.federatedPlayers.get(steamId).linkId === link.id) {
    state.federatedPlayers.delete(steamId);
  }
  try {
    require('../lobbies').dropPlayer(steamId);
  } catch {
    // lobby module not loaded yet
  }
}

// Run the real route on this server, over loopback, with the federated session. That
// keeps every ownership, membership and seat-count check in one place.
async function dispatch(op, method, body, query, token) {
  const verb = ['GET', 'POST', 'DELETE'].includes(String(method || '').toUpperCase()) ? String(method).toUpperCase() : 'POST';
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (typeof value !== 'string' && typeof value !== 'number') continue;
    if (String(key).length > 40 || String(value).length > 200) continue;
    params.set(String(key), String(value));
  }
  const suffix = params.toString() ? `?${params}` : '';
  const url = `${loopbackBase}/api/${canonicalMount.replace(/^\/api\//, '')}/Matchmaking/${op}${suffix}`;

  const init = {
    method: verb,
    headers: {
      'Content-Type': 'application/json',
      'x-webworks-session': token,
      'x-webworks-federated': '1',
    },
    signal: AbortSignal.timeout(settings.requestTimeoutMs),
  };
  if (verb !== 'GET') {
    const payload = JSON.stringify(body);
    if (payload.length > MAX_BODY_CHARS) return { status: 413, error: 'body_too_large' };
    init.body = payload;
  }

  let resp;
  try {
    resp = await fetch(url, init);
  } catch (err) {
    return { status: 502, error: 'upstream_failed' };
  }

  const text = await resp.text();
  if (text.length > MAX_RESPONSE_BYTES) return { status: 502, error: 'response_too_large' };
  let payload = null;
  if (text) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = text;
    }
  }
  return { status: resp.status, body: payload };
}

// --- public API used by the server ------------------------------------------

function canForward() {
  return enabled;
}

function remoteLobbies(appId) {
  if (!enabled) return [];
  const dir = directory.current();
  if (!dir) return [];
  // A server only heartbeats while it hosts lobbies, so an idle one has no census yet.
  // Refresh in the background; the browser shows remote lobbies from the next call on.
  dir.refresh();

  const out = [];
  for (const server of dir.servers.values()) {
    for (const lobby of server.lobbies) {
      if (String(lobby.appId) !== String(appId)) continue;
      out.push({ ...lobby, serverId: server.serverId });
    }
  }
  return out;
}

async function forward(req, { path, method, lobbyId }) {
  if (!enabled) throw new FederationError('federation_disabled', 503);
  const appId = String(req.session.appId);
  const id = String(lobbyId);
  const link = await ensureLink(appId, id);
  if (!isOpen(link) || !link.ready) throw new FederationError('federation_not_ready', 503);

  link.players.add(String(req.session.steamId));
  const requestId = crypto.randomBytes(10).toString('hex');

  const response = await new Promise((resolve) => {
    const timer = setTimeout(() => {
      pending.delete(requestId);
      resolve({ error: new FederationError('federation_timeout', 504) });
    }, settings.requestTimeoutMs);
    if (timer.unref) timer.unref();
    pending.set(requestId, {
      link,
      resolve: (value) => {
        clearTimeout(timer);
        resolve(value);
      },
    });

    const sent = send(link, {
      t: 'request',
      id: requestId,
      method: String(method || 'POST').toUpperCase(),
      path,
      body: req.body && typeof req.body === 'object' ? req.body : {},
      query: req.query && typeof req.query === 'object' ? req.query : {},
      player: {
        steamId: String(req.session.steamId),
        name: String(req.session.name || ''),
        appId,
      },
    });
    if (!sent) {
      clearTimeout(timer);
      pending.delete(requestId);
      resolve({ error: new FederationError('federation_send_failed', 503) });
    }
  });

  if (response.error) {
    link.players.delete(String(req.session.steamId));
    throw response.error;
  }
  if (/\/LeaveLobby$/.test(String(path)) && response.status < 400) link.players.delete(String(req.session.steamId));
  return response;
}

// Called by realtime.push when the player has no socket on this server.
function deliver(steamId, message) {
  if (!enabled) return false;
  const link = linksBySteam.get(String(steamId));
  if (!isOpen(link)) return false;
  return send(link, { t: 'push', to: String(steamId), message });
}

// A lobby was created or removed on this server: advertise it now, not in two minutes.
function lobbiesChanged() {
  const dir = directory.current();
  if (dir && enabled) dir.publishSoon();
}

// The player's own server saw the websocket close: leave every remote lobby.
function playerDisconnected(steamId) {
  if (!enabled) return;
  for (const link of [...links.values()]) {
    if (link.role !== 'dialer' || !link.players.has(String(steamId))) continue;
    send(link, { t: 'leave', steamId: String(steamId) });
    link.players.delete(String(steamId));
  }
}

function status() {
  const dir = directory.current();
  return {
    enabled,
    mode: enabled ? 'gwas' : 'local',
    publishing: Boolean(enabled && publishedUrl),
    publicUrl: publishedUrl,
    authMode: auth ? (auth.token ? 'claim' : auth.key ? 'shared-key' : null) : null,
    serverId: identity.loaded() ? identity.current().serverId : null,
    disabledReason,
    links: links.size,
    peers: linksByPeer.size,
    federatedPlayers: state.federatedPlayers.size,
    directory: dir ? dir.status() : null,
  };
}

module.exports = {
  FederationError,
  init,
  setPublicUrl,
  attach,
  shutdown,
  forward,
  deliver,
  remoteLobbies,
  canForward,
  validateRequest,
  lobbiesChanged,
  playerDisconnected,
  status,
  ALLOWED_OPS,
};
