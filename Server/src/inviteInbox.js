const { state } = require('./state');

// Lobby invites that have been pushed to a player but not answered yet. The realtime push
// reaches a client with an open socket; this inbox is what makes the same invite visible
// on the bootstrap's friends page, which has no socket of its own.
//
// Nothing is persisted and nothing is authoritative: a lobby invite is only useful while
// its one-time token is alive, so entries are pruned against the token store on read.

const MAX_PER_PLAYER = 20;

function inboxFor(steamId) {
  const id = String(steamId);
  if (!state.inviteInbox.has(id)) state.inviteInbox.set(id, []);
  return state.inviteInbox.get(id);
}

function add(steamId, invite) {
  if (!steamId || !invite || !invite.token) return null;
  const list = inboxFor(steamId);
  // One entry per lobby per sender: re-inviting the same friend refreshes the token
  // instead of stacking duplicate rows on their friends page.
  const index = list.findIndex((entry) => entry.lobbyId === invite.lobbyId && entry.fromSteamId === invite.fromSteamId);
  const entry = {
    lobbyId: String(invite.lobbyId),
    inviteToken: String(invite.token),
    fromSteamId: String(invite.fromSteamId || ''),
    fromName: invite.fromName || '',
    appId: invite.appId || null,
    expiresAt: invite.expiresAt || 0,
    createdAt: Date.now(),
  };
  if (index >= 0) list[index] = entry;
  else list.push(entry);
  if (list.length > MAX_PER_PLAYER) list.splice(0, list.length - MAX_PER_PLAYER);
  return entry;
}

function live(steamId, now = Date.now()) {
  const list = state.inviteInbox.get(String(steamId));
  if (!list || !list.length) return [];
  const kept = list.filter((entry) => entry.expiresAt > now && state.lobbies.has(entry.lobbyId));
  if (kept.length) state.inviteInbox.set(String(steamId), kept);
  else state.inviteInbox.delete(String(steamId));
  return kept;
}

function dismiss(steamId, { lobbyId, inviteToken } = {}) {
  const list = live(steamId);
  const kept = list.filter((entry) => {
    if (lobbyId && entry.lobbyId === String(lobbyId)) return false;
    if (inviteToken && entry.inviteToken === String(inviteToken)) return false;
    return true;
  });
  if (kept.length) state.inviteInbox.set(String(steamId), kept);
  else state.inviteInbox.delete(String(steamId));
  return list.length - kept.length;
}

function forget(steamId) {
  state.inviteInbox.delete(String(steamId));
}

module.exports = { add, live, dismiss, forget };
