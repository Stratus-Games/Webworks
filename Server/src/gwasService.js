const { accounts } = require('./accounts');
const { state } = require('./state');
const log = require('./log');

// Thin client for the GWAS game services: achievements, stats, inventory and cloud
// saves. Everything here is authenticated with the player's own GWAS token, so a
// game on one server sees the same progress as the same account anywhere else.
//
// When GWAS is unreachable the callers fall back to the local stores, which keeps a
// game playable offline instead of failing every achievement write.

const REQUEST_TIMEOUT_MS = Math.max(2000, Number(process.env.WEBWORKS_GWAS_TIMEOUT_MS) || 12000);

class GwasError extends Error {
  constructor(code, status) {
    super(code);
    this.name = 'GwasError';
    this.code = code;
    this.status = status || 502;
  }
}

function enabled() {
  return accounts.mode === 'gwas' && Boolean(accounts.url);
}

function linked(req) {
  if (!req || !req.session) return null;
  const entry = state.linkedAccounts.get(String(req.session.steamId));
  return entry && entry.token ? entry : null;
}

function linkedToken(req) {
  if (!enabled()) return null;
  const entry = linked(req);
  return entry ? entry.token : null;
}

function isRemote(req) {
  return Boolean(linkedToken(req));
}

async function request(method, pathname, { token, body, server = false } = {}) {
  if (!enabled()) throw new GwasError('gwas_disabled', 501);

  const headers = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers.Authorization = `Bearer ${token}`;

  const serverKey = process.env.WEBWORKS_GWAS_SERVER_KEY || '';
  if (server && serverKey) headers['x-gwas-server-key'] = serverKey;

  let resp;
  try {
    resp = await fetch(accounts.url + pathname, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    });
  } catch (err) {
    log.warn(`GWAS ${method} ${pathname} failed: ${err.message}`);
    throw new GwasError('gwas_unreachable', 503);
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

  if (!resp.ok) {
    const code = (data && data.error) || `gwas_${resp.status}`;
    throw new GwasError(code, resp.status);
  }
  return data || {};
}

// --- achievements and stats ---------------------------------------------------

const achievements = {
  catalog: (appId, token) => request('GET', `/achievements?appId=${encodeURIComponent(appId)}`, { token }),
  profile: (token) => request('GET', '/achievements/profile', { token }),
  percentages: (appId, token) => request('GET', `/achievements/percentages?appId=${encodeURIComponent(appId)}`, { token }),
  players: (appId, token) => request('GET', `/achievements/players?appId=${encodeURIComponent(appId)}`, { token }),
  user: (appId, token, username) => request('GET', `/achievements/user?appId=${encodeURIComponent(appId)}&username=${encodeURIComponent(username)}`, { token }),
  define: (appId, entries, token) => request('POST', '/achievements/define', { token, body: { appId, achievements: entries }, server: true }),
  unlock: (appId, token, entry) => request('POST', '/achievements/unlock', { token, body: { appId, ...entry } }),
  clear: (appId, token, entry) => request('POST', '/achievements/clear', { token, body: { appId, ...entry } }),
  progress: (appId, token, entry) => request('POST', '/achievements/progress', { token, body: { appId, ...entry } }),
};

const stats = {
  read: (appId, token) => request('GET', `/stats?appId=${encodeURIComponent(appId)}`, { token }),
  write: (appId, token, payload) => request('POST', '/stats', { token, body: { appId, ...payload } }),
  value: (appId, token, name) => request('GET', `/stats/value?appId=${encodeURIComponent(appId)}&name=${encodeURIComponent(name)}`, { token }),
  setValue: (appId, token, payload) => request('POST', '/stats/value', { token, body: { appId, ...payload } }),
};

// --- inventory ----------------------------------------------------------------

const inventory = {
  definitions: (appId, token) => request('GET', `/inventory/definitions?appId=${encodeURIComponent(appId)}`, { token }),
  items: (appId, token) => request('GET', `/inventory/items?appId=${encodeURIComponent(appId)}`, { token }),
  define: (appId, def) => request('POST', '/inventory/define', { body: { appId, ...def }, server: true }),
  grant: (appId, token, payload) => request('POST', '/inventory/grant', { token, body: { appId, ...payload } }),
  consume: (appId, token, itemDefId, quantity) => request('POST', '/inventory/consume', { token, body: { appId, itemDefId, quantity } }),
  exchange: (appId, token, payload) => request('POST', '/inventory/exchange', { token, body: { appId, ...payload } }),
  trade: (appId, token, payload) => request('POST', '/inventory/trade', { token, body: { appId, ...payload } }),
  properties: (appId, token, payload) => request('POST', '/inventory/properties', { token, body: { appId, ...payload } }),
  inspect: (appId, token, itemToken) => request('POST', '/inventory/inspect', { token, body: { appId, token: itemToken } }),
};

// --- cloud saves --------------------------------------------------------------

const storage = {
  list: (appId, token) => request('GET', `/storage/files?appId=${encodeURIComponent(appId)}`, { token }),
  write: (appId, token, name, data) => request('POST', '/storage/write', { token, body: { appId, name, data } }),
  remove: (appId, token, name) => request('POST', '/storage/delete', { token, body: { appId, name } }),
};

module.exports = {
  GwasError,
  enabled,
  isRemote,
  linked,
  linkedToken,
  request,
  achievements,
  stats,
  inventory,
  storage,
};
