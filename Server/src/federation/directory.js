// GWAS directory client.
//
// GWAS never connects to a node server. Node servers publish a small census of their
// live lobbies to GWAS while they have any, and read back the other servers' censuses
// from the same heartbeat response. That keeps the cost to one request per heartbeat
// per *busy* server, which is the only way this fits a 100k/day worker budget:
//
//   1 heartbeat / 120s = 720 requests/day per server with lobbies open
//   an idle server sends nothing at all
//   a cross-server join costs one extra rendezvous request
//
// Everything the directory returns is untrusted input: the URL is validated by
// targets.js before it is dialled, and tickets are verified locally.

const log = require('../log');
const census = require('./census');
const { parseBridgeUrl } = require('./targets');

const DEFAULT_HEARTBEAT_MS = 120 * 1000;
const MIN_HEARTBEAT_MS = 30 * 1000;
const REQUEST_TIMEOUT_MS = 8000;
const RENDEZVOUS_MIN_INTERVAL_MS = 2000;

class DirectoryError extends Error {
  constructor(code, status) {
    super(code);
    this.name = 'DirectoryError';
    this.code = code;
    this.status = status || 502;
  }
}

function normalizeBase(url) {
  return String(url || '').replace(/\/+$/, '');
}

class DirectoryClient {
  constructor(options = {}) {
    this.url = normalizeBase(options.url);
    this.key = options.key || '';
    // The token bought with this machine's generated server secret. Preferred over the
    // operator-wide key:
    // it is bound to this server's own id, so it cannot be used to act as another one.
    this.token = options.token || '';
    this.serverId = options.serverId || '';
    this.secret = options.secret || '';
    this.publicKey = options.publicKey || '';
    this.bridgeUrl = options.bridgeUrl || '';
    this.heartbeatMs = Math.max(MIN_HEARTBEAT_MS, Number(options.heartbeatMs) || DEFAULT_HEARTBEAT_MS);
    this.allowInsecure = Boolean(options.allowInsecure);
    this.allowedHosts = options.allowedHosts || [];
    this.fetchImpl = options.fetchImpl || fetch;
    this.requestTimeoutMs = Number(options.requestTimeoutMs) || REQUEST_TIMEOUT_MS;

    this.servers = new Map(); // serverId -> { serverId, bridgeUrl, lobbies, updatedAt }
    this.ttlMs = 0;
    this.lastHeartbeatAt = 0;
    this.lastHeartbeatOkAt = 0;
    this.lastError = null;
    this.disabledReason = null;
    this.published = false;
    this.failures = 0;
    this.requests = 0;
    this.timer = null;
    this.soonTimer = null;
    this.inFlight = null;
    this._lastRendezvousAt = 0;
  }

  get enabled() {
    const auth = this.key || this.token;
    return Boolean(this.url && auth && this.serverId && this.secret && this.publicKey && this.bridgeUrl) && !this.disabledReason;
  }

  start() {
    if (this.timer) return;
    if (!this.enabled) return;
    const tick = () => {
      this.heartbeat().catch(() => {  });
    };
    this.timer = setInterval(tick, this.heartbeatMs);
    if (this.timer.unref) this.timer.unref();
    tick();
  }

  stop() {
    if (this.timer) clearInterval(this.timer);
    this.timer = null;
    if (this.soonTimer) clearTimeout(this.soonTimer);
    this.soonTimer = null;
  }

  disable(reason) {
    if (!this.disabledReason) log.warn(`GWAS networking disabled: ${reason}`);
    this.disabledReason = reason;
    this.stop();
  }

  async call(pathname, body) {
    this.requests++;
    const headers = { 'Content-Type': 'application/json', Accept: 'application/json' };
    if (this.key) headers['x-gwas-server-key'] = this.key;
    else if (this.token) headers['x-gwas-server-token'] = this.token;

    let resp;
    try {
      resp = await this.fetchImpl(this.url + pathname, {
        method: 'POST',
        headers,
        body: JSON.stringify(body),
        signal: AbortSignal.timeout(this.requestTimeoutMs),
      });
    } catch (err) {
      this.lastError = 'unreachable';
      throw new DirectoryError('directory_unreachable', 503);
    }

    const text = await resp.text();
    let data = null;
    if (text) {
      try {
        data = JSON.parse(text);
      } catch {
        data = null;
      }
    }

    if (resp.status === 404 || resp.status === 501) {
      // An older GWAS without the directory routes. Degrade: this server keeps working
      // exactly as before, it just cannot hear about lobbies hosted elsewhere.
      this.disable('this GWAS does not serve the federation directory');
      throw new DirectoryError('directory_unsupported', 501);
    }
    if (resp.status === 401) {
      const code = (data && data.error) || '';
      this.disable(code === 'invalid_server_token' || code === 'server_claim_required'
        ? 'GWAS rejected this server\'s saved token; delete webworks.config.json to claim it again'
        : 'GWAS rejected the server key for the federation directory');
      throw new DirectoryError('directory_unauthorized', resp.status);
    }
    if (resp.status === 403 && data && data.error === 'server_id_mismatch') {
      // A token only speaks for its own server id. Reaching this means the id and the
      // token disagree, which is a configuration mistake, never something to retry.
      this.disable('GWAS says this server\'s token belongs to a different id');
      throw new DirectoryError('directory_forbidden', resp.status);
    }
    if (resp.status === 429) {
      throw new DirectoryError('directory_rate_limited', 429);
    }
    if (!resp.ok) {
      throw new DirectoryError((data && data.error) || `directory_${resp.status}`, resp.status);
    }
    return data || {};
  }

  // Something changed (a lobby opened, closed or moved): publish within a second or so
  // instead of waiting for the next scheduled heartbeat. Debounced so a burst of
  // lobby churn still costs one request.
  publishSoon() {
    if (!this.enabled || this.soonTimer) return;
    const wait = Math.max(0, 1500 - (Date.now() - this.lastHeartbeatAt));
    this.soonTimer = setTimeout(() => {
      this.soonTimer = null;
      this.heartbeat().catch(() => {  });
    }, wait);
    if (this.soonTimer.unref) this.soonTimer.unref();
  }

  // Make sure the cached census is not stale, without waiting for the next tick. Used
  // by the lobby browser, which has no reason to poll on its own.
  refresh() {
    if (!this.enabled) return;
    if (Date.now() - this.lastHeartbeatOkAt < 30000) return;
    this.heartbeat(true).catch(() => {  });
  }

  async heartbeat(force = false) {
    if (!this.enabled) return null;
    if (this.inFlight) {
      if (!force) return this.inFlight;
      try { await this.inFlight; } catch {  }
    }

    const promise = this._heartbeat(force);
    this.inFlight = promise;
    const settle = () => { if (this.inFlight === promise) this.inFlight = null; };
    promise.then(settle, settle);
    return promise;
  }

  async _heartbeat(force = false) {
    const lobbies = census.build();
    const now = Date.now();

    // Nothing to publish: tell the directory once, then stay silent until a lobby
    // shows up again. Idle servers cost zero requests. A forced heartbeat still
    // registers (with an empty census) so this server may dial other peers.
    if (lobbies.length === 0 && !force) {
      if (this.published) {
        await this.leave().catch(() => {  });
        this.published = false;
      }
      return null;
    }

    let data;
    try {
      data = await this.call('/directory/heartbeat', {
        v: 1,
        serverId: this.serverId,
        secret: this.secret,
        publicKey: this.publicKey,
        bridgeUrl: this.bridgeUrl,
        engine: 'webworks',
        appIds: [...new Set(lobbies.map((l) => l.appId))],
        lobbies,
        now,
      });
    } catch (err) {
      this.failures++;
      this.lastError = err.code || 'heartbeat_failed';
      // Back off rather than hammer a directory that is down or rate limiting us.
      if (this.failures >= 3 && this.timer) {
        this.stop();
        const delay = Math.min(this.heartbeatMs * 4, 15 * 60 * 1000);
        this.timer = setTimeout(() => {
          this.timer = null;
          this.start();
        }, delay);
        if (this.timer.unref) this.timer.unref();
      }
      log.warn(`GWAS directory heartbeat failed: ${this.lastError}`);
      return null;
    }

    this.failures = 0;
    this.lastError = null;
    this.lastHeartbeatAt = now;
    this.lastHeartbeatOkAt = now;
    this.published = true;
    this.ttlMs = Number(data.ttlMs) || this.ttlMs;

    const incoming = new Map();
    for (const entry of Array.isArray(data.servers) ? data.servers : []) {
      if (!entry || typeof entry.serverId !== 'string' || entry.serverId === this.serverId) continue;
      const valid = parseBridgeUrl(entry.bridgeUrl, { allowInsecure: this.allowInsecure, allowedHosts: this.allowedHosts });
      if (!valid.ok) {
        log.warn(`ignoring directory entry for ${entry.serverId}: ${valid.error}`);
        continue;
      }
      incoming.set(entry.serverId, {
        serverId: entry.serverId,
        bridgeUrl: valid.url.toString(),
        updatedAt: Number(entry.updatedAt) || now,
        lobbies: Array.isArray(entry.lobbies) ? entry.lobbies.filter((l) => l && typeof l.id === 'string') : [],
      });
    }
    this.servers = incoming;
    return data;
  }

  async leave() {
    if (!this.url || !(this.key || this.token)) return;
    try {
      await this.call('/directory/leave', { serverId: this.serverId, secret: this.secret });
    } catch {
      // Expiry in the worker covers a failed leave.
    }
  }

  // Where does this lobby live? Local knowledge first, and null when we have never
  // heard of it (the caller can still try a rendezvous, which is authoritative).
  findLobby(appId, lobbyId) {
    const id = String(lobbyId);
    const app = String(appId);
    for (const server of this.servers.values()) {
      for (const lobby of server.lobbies) {
        if (String(lobby.id) === id && String(lobby.appId) === app) {
          return { serverId: server.serverId, bridgeUrl: server.bridgeUrl, lobby };
        }
      }
    }
    return null;
  }

  // Ask GWAS to introduce us to the server hosting a lobby. GWAS mints a ticket for
  // that server; it never sees or relays a single game packet.
  async rendezvous({ appId, lobbyId, toServerId }) {
    if (!this.enabled) throw new DirectoryError('directory_disabled', 503);

    const now = Date.now();
    if (now - this._lastRendezvousAt < RENDEZVOUS_MIN_INTERVAL_MS) {
      throw new DirectoryError('directory_throttled', 429);
    }
    this._lastRendezvousAt = now;

    const payload = {
      from: this.serverId,
      secret: this.secret,
      to: toServerId || undefined,
      appId: String(appId),
      lobbyId: String(lobbyId),
    };

    let data;
    try {
      data = await this.call('/directory/rendezvous', payload);
    } catch (err) {
      // This server has been idle long enough to fall out of the directory (it only
      // publishes while it hosts lobbies). Register once and try again.
      if (!err || err.code !== 'not_registered') throw err;
      await this.heartbeat(true);
      data = await this.call('/directory/rendezvous', payload);
    }

    if (!data || typeof data.ticket !== 'string' || typeof data.peerId !== 'string') {
      throw new DirectoryError('directory_bad_response', 502);
    }
    const target = parseBridgeUrl(data.bridgeUrl, { allowInsecure: this.allowInsecure, allowedHosts: this.allowedHosts });
    if (!target.ok) throw new DirectoryError(target.error, 403);

    return {
      peerId: data.peerId,
      bridgeUrl: target.url.toString(),
      publicKey: typeof data.publicKey === 'string' ? data.publicKey : '',
      ticket: data.ticket,
      expiresAt: Number(data.expiresAt) || 0,
    };
  }

  status() {
    return {
      enabled: this.enabled,
      url: this.url || null,
      serverId: this.serverId || null,
      bridgeUrl: this.bridgeUrl || null,
      published: this.published,
      authMode: this.key ? 'shared-key' : this.token ? 'claim' : null,
      heartbeatMs: this.heartbeatMs,
      lastHeartbeatAt: this.lastHeartbeatOkAt || null,
      servers: this.servers.size,
      lobbies: [...this.servers.values()].reduce((sum, s) => sum + s.lobbies.length, 0),
      requests: this.requests,
      disabledReason: this.disabledReason,
      lastError: this.lastError,
    };
  }
}

let singleton = null;

function init(options) {
  singleton = new DirectoryClient(options);
  return singleton;
}

function current() {
  return singleton;
}

async function stop() {
  if (!singleton) return;
  try {
    if (singleton.published) await singleton.leave();
  } catch {
    // best effort
  }
  singleton.stop();
}

module.exports = {
  DirectoryClient,
  DirectoryError,
  init,
  current,
  stop,
  DEFAULT_HEARTBEAT_MS,
  MIN_HEARTBEAT_MS,
};
