const state = {
  sessions: new Map(),


  appOf: new Map(),

  knownApps: new Set(),

  lobbies: new Map(),

  userStats: new Map(),

  names: new Map(),

  sockets: new Map(),

  linkedAccounts: new Map(),

  steamIdsOfAccount: new Map(),

  invites: new Map(),

  // Lobby invites waiting on a player, so the bootstrap friends page can show them.
  inviteInbox: new Map(),

  // Persona state/name per online session, mirrored to the GWAS account when linked.
  personaStates: new Map(),
  personaNames: new Map(),

  // Last heartbeat per player. A player with a fresh heartbeat is online even when they
  // have no realtime socket yet (the name/friends screen before a game starts).
  pings: new Map(),

  // Account token -> game session token, so the bootstrap can call session routes with
  // the token it actually holds.
  accountTokenToSession: new Map(),

  // Players whose session lives on another node server but whose lobby is hosted here
  // (GWAS networking). Key: steamId, value: { linkId, peerId, appId }.
  federatedPlayers: new Map(),
};

function normalizeAppId(raw) {
  if (raw === undefined || raw === null || raw === '') return '0';
  const id = String(raw).trim();
  return /^[0-9A-Za-z][0-9A-Za-z_-]{0,31}$/.test(id) ? id : null;
}

const statsStore = require('./statsStore');

// Stats are persisted (data/userstats/<app>/<user>.json); state.userStats stays as an
// in-process view for anything that wants to enumerate a game's players.
function statsForApp(appId) {
  return statsStore.recordsForApp(appId);
}

function ensureUserStats(appId, steamId) {
  return statsStore.record(appId, steamId);
}

function linkAccountToSession(session, { username, token, provider }) {
  const steamId = String(session.steamId);
  state.linkedAccounts.set(steamId, { username, token, provider: provider || null });
  if (token && session.sessionToken) state.accountTokenToSession.set(String(token), String(session.sessionToken));

  const key = String(username).toLowerCase();
  if (!state.steamIdsOfAccount.has(key)) state.steamIdsOfAccount.set(key, new Set());
  state.steamIdsOfAccount.get(key).add(steamId);
}

function unlinkAccountFromSession(session) {
  const steamId = String(session.steamId);
  const linked = state.linkedAccounts.get(steamId);
  state.linkedAccounts.delete(steamId);
  if (!linked) return;
  if (linked.token) state.accountTokenToSession.delete(String(linked.token));
  const key = String(linked.username).toLowerCase();
  const set = state.steamIdsOfAccount.get(key);
  if (set) {
    set.delete(steamId);
    if (set.size === 0) state.steamIdsOfAccount.delete(key);
  }
}

function forgetSteamId(steamId) {
  const id = String(steamId);
  state.appOf.delete(id);
  state.names.delete(id);
  state.linkedAccounts.delete(id);
  state.sockets.delete(id);
  state.personaStates.delete(id);
  state.personaNames.delete(id);
  state.pings.delete(id);
  state.inviteInbox.delete(id);
  state.federatedPlayers.delete(id);
  for (const [key, set] of state.steamIdsOfAccount) {
    set.delete(id);
    if (set.size === 0) state.steamIdsOfAccount.delete(key);
  }
}

module.exports = {
  state,
  ensureUserStats,
  statsForApp,
  normalizeAppId,
  linkAccountToSession,
  unlinkAccountFromSession,
  forgetSteamId,
};
