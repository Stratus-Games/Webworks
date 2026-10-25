const { state } = require('./state');
const { accounts } = require('./accounts');

// A player counts as online while a realtime socket is open OR while their client keeps
// sending heartbeats. The heartbeat is what keeps the bootstrap's friends page honest:
// someone sitting on the name/friends screen has no game socket yet, but they are still
// in the game and their friends should see it. It also keeps a player online when the
// browser moves their tab to the background, because the tab stays open.
//
// Hidden tabs have their timers throttled to roughly once a minute, so the window is
// deliberately longer than that: a tab that is still open never flickers offline.
const PING_TTL_MS = Math.max(200, Number(process.env.WEBWORKS_PING_TTL_MS) || 150000);

const offlineTimers = new Map();

function lastPingAt(steamId) {
  return state.pings.get(String(steamId)) || 0;
}

function isOnline(steamId) {
  const id = String(steamId);
  // A player connected through another node server holds their lobby here, so the
  // lobby sweeper must treat them as present too.
  if (state.sockets.has(id) || state.federatedPlayers.has(id)) return true;
  const at = state.pings.get(id) || 0;
  return at > 0 && Date.now() - at < PING_TTL_MS;
}

function gameOf(steamId) {
  return state.appOf.get(String(steamId)) || null;
}

function onlineCount() {
  const ids = new Set(state.sockets.keys());
  const now = Date.now();
  for (const [id, at] of state.pings) {
    if (now - at < PING_TTL_MS) ids.add(id);
  }
  return ids.size;
}

function onlineByGame() {
  const counts = {};
  const ids = new Set(state.sockets.keys());
  const now = Date.now();
  for (const [id, at] of state.pings) {
    if (now - at < PING_TTL_MS) ids.add(id);
  }
  for (const id of ids) {
    const app = state.appOf.get(id) || '0';
    counts[app] = (counts[app] || 0) + 1;
  }
  return counts;
}

function cancelOfflineCheck(steamId) {
  const id = String(steamId);
  const timer = offlineTimers.get(id);
  if (timer) {
    clearTimeout(timer);
    offlineTimers.delete(id);
  }
}

// Every heartbeat refreshes the last-seen stamp. Coming back from offline publishes the
// persona change once, so a friend's list flips to Online without waiting for a refresh.
function notePing(session) {
  if (!session || !session.steamId) return false;
  const id = String(session.steamId);
  const wasOnline = isOnline(id);
  state.pings.set(id, Date.now());
  session.lastSeenAt = Date.now();
  cancelOfflineCheck(id);
  if (!wasOnline) {
    broadcastPersonaChange(id, state.personaStates.get(id) || 'Online');
    return true;
  }
  return false;
}

// The explicit "this tab is going away" signal. Closing the tab should drop a player
// offline immediately instead of waiting for the heartbeat window to lapse.
function noteOffline(session) {
  if (!session || !session.steamId) return;
  const id = String(session.steamId);
  const wasOnline = isOnline(id);
  state.pings.delete(id);
  cancelOfflineCheck(id);
  if (state.sockets.has(id) || state.federatedPlayers.has(id)) return;
  if (wasOnline) broadcastPersonaChange(id, 'Offline');
}

// A socket closing is not the same as a player leaving when they still have a live
// heartbeat (for example the tab is open but the socket dropped). Wait out whatever is
// left of the heartbeat window before publishing Offline.
function noteSocketClosed(steamId, appId) {
  const id = String(steamId);
  if (state.sockets.has(id)) return;

  const at = lastPingAt(id);
  const remaining = at ? PING_TTL_MS - (Date.now() - at) : 0;
  if (remaining > 0) {
    cancelOfflineCheck(id);
    const timer = setTimeout(() => {
      offlineTimers.delete(id);
      if (isOnline(id)) return;
      broadcastPersonaChange(id, 'Offline');
    }, remaining + 50);
    if (timer.unref) timer.unref();
    offlineTimers.set(id, timer);
    return;
  }

  broadcastPersonaChange(id, 'Offline');
}

async function decorateFriends(graph) {
  const safe = graph || { accepted: [], incoming: [], outgoing: [], blocked: [] };
  const accepted = (safe.accepted || []).map((profile) => {
    const steamId = String(profile.steamId || accounts.steamIdFor(profile.username));
    const online = isOnline(steamId);
    // A live session's own persona wins over the one stored on the account, because
    // the account is the durable copy and the session is the current device.
    const personaState = online
      ? (state.personaStates.get(steamId) || profile.personaState || 'Online')
      : 'Offline';
    return {
      username: profile.username,
      displayName: profile.displayName || profile.username,
      name: state.personaNames.get(steamId) || profile.displayName || profile.username,
      steamId,
      hasAvatar: Boolean(profile.hasAvatar || profile.avatarBase64),
      avatarBase64: profile.avatarBase64 || null,
      personaState,
      game: personaState === 'Offline' || personaState === 'Invisible' ? null : gameOf(steamId),
    };
  });

  return {
    accepted,
    incoming: safe.incoming || [],
    outgoing: safe.outgoing || [],
    blocked: safe.blocked || [],
    onlineCount: accepted.filter((f) => f.personaState === 'Online').length,
  };
}

async function resolveFriendsFor(steamId) {
  const linked = state.linkedAccounts.get(String(steamId));
  if (!linked) return [];

  try {
    const graph = await accounts.getFriends(linked.username, linked.token);
    const decorated = await decorateFriends(graph);
    return decorated.accepted.map((friend) => ({
      steamId: friend.steamId,
      username: friend.username,
      name: friend.name,
      displayName: friend.displayName,
      personaState: friend.personaState,
      game: friend.game,
      hasAvatar: friend.hasAvatar,
      avatarBase64: friend.avatarBase64 || null,
    }));
  } catch {
    return [];
  }
}

function sessionsForAccount(username) {
  const key = String(username || '').toLowerCase();
  const out = [];
  for (const session of state.sessions.values()) {
    if (session.accountUsername && String(session.accountUsername).toLowerCase() === key) out.push(session);
  }
  return out;
}

function notifyFriendGraphChanged(username) {
  const { push } = require('./realtime');
  for (const session of sessionsForAccount(username)) {
    push(session.steamId, { type: 'FriendListChanged' });
  }
}

async function broadcastPersonaChange(steamId, personaState) {
  const linked = state.linkedAccounts.get(String(steamId));
  if (!linked) return;
  const name = state.personaNames.get(String(steamId)) || state.names.get(String(steamId)) || linked.username;
  const game = personaState === 'Online' ? gameOf(steamId) : null;

  try {
    const graph = await accounts.getFriends(linked.username, linked.token);
    const { push } = require('./realtime');
    for (const friend of graph.accepted || []) {
      const friendId = String(friend.steamId || accounts.steamIdFor(friend.username));
      push(friendId, {
        type: 'PersonaStateChange',
        steamId: String(steamId),
        name,
        personaState,
        game,
      });
    }
  } catch {
  }
}

module.exports = {
  PING_TTL_MS,
  isOnline,
  gameOf,
  onlineCount,
  onlineByGame,
  notePing,
  noteOffline,
  noteSocketClosed,
  decorateFriends,
  resolveFriendsFor,
  sessionsForAccount,
  notifyFriendGraphChanged,
  broadcastPersonaChange,
};
