const crypto = require('crypto');
const { state } = require('./state');
const { pushToLobby } = require('./realtime');
const { isOnline } = require('./presence');

function snapshot(lobby) {
  return {
    id: lobby.id,
    ownerId: lobby.ownerId,
    maxMembers: lobby.maxMembers,
    memberCount: lobby.members.size,
    visibility: lobby.visibility,
    joinable: lobby.joinable,
    data: lobby.data,
    members: [...lobby.members].map((steamId) => ({ steamId, name: state.names.get(steamId) || '' })),
    memberData: lobby.memberData,
    gameServer: lobby.gameServer || { ip: 0, port: 0, serverId: '0' },
  };
}

function removeMember(lobby, steamId, type) {
  if (!lobby.members.delete(steamId)) return;
  delete lobby.memberData[steamId];

  if (lobby.members.size === 0) {
    state.lobbies.delete(lobby.id);
    try { require('./federation').lobbiesChanged(); } catch {  }
    return;
  }

  if (lobby.ownerId === steamId) lobby.ownerId = [...lobby.members][0];

  pushToLobby(lobby, { type, lobbyId: lobby.id, steamId, name: state.names.get(steamId) || '', ownerId: lobby.ownerId });
}

function dropPlayer(steamId) {
  for (const lobby of [...state.lobbies.values()]) {
    if (lobby.members.has(steamId)) removeMember(lobby, steamId, 'LobbyMemberDisconnected');
  }
}

const SWEEP_MS = 30 * 1000;
const STALE_MS = 2 * 60 * 1000;

function sweepLobbies(now = Date.now()) {
  let removed = 0;
  for (const lobby of [...state.lobbies.values()]) {
    const alive = [...lobby.members].some((id) => isOnline(id));
    if (alive) lobby.lastSeen = now;
    else if (now - (lobby.lastSeen || lobby.createdAt || now) > STALE_MS) { state.lobbies.delete(lobby.id); removed++; }
  }
  if (removed > 0) {
    try { require('./federation').lobbiesChanged(); } catch {  }
  }
  return removed;
}

const DEFAULT_INVITE_TTL_MS = 60 * 60 * 1000;
const MIN_INVITE_TTL_MS = 60 * 1000;
const MAX_INVITE_TTL_MS = 7 * 24 * 60 * 60 * 1000;

function createInvite(lobby, createdBy, ttlMs) {
  const clamped = Math.max(MIN_INVITE_TTL_MS, Math.min(Number(ttlMs) || DEFAULT_INVITE_TTL_MS, MAX_INVITE_TTL_MS));
  const token = crypto.randomBytes(16).toString('hex');
  const expiresAt = Date.now() + clamped;
  state.invites.set(token, { lobbyId: lobby.id, appId: lobby.appId, createdBy, createdAt: Date.now(), expiresAt });
  return { token, expiresAt };
}

function resolveInvite(token) {
  const invite = state.invites.get(token);
  if (!invite) return null;
  if (Date.now() > invite.expiresAt) {
    state.invites.delete(token);
    return null;
  }
  return invite;
}

function sweepInvites(now = Date.now()) {
  let removed = 0;
  for (const [token, invite] of state.invites) {
    if (now > invite.expiresAt) {
      state.invites.delete(token);
      removed++;
    }
  }
  return removed;
}

function startLobbySweeper() {
  const timer = setInterval(() => {
    sweepLobbies();
    sweepInvites();
  }, SWEEP_MS);
  if (timer.unref) timer.unref();
}

module.exports = {
  snapshot,
  removeMember,
  dropPlayer,
  sweepLobbies,
  startLobbySweeper,
  createInvite,
  resolveInvite,
  sweepInvites,
};
