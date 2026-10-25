// The part of a lobby that is safe to publish to the GWAS directory.
//
// The directory is a phone book, not a game database: it only needs enough to say
// "lobby L exists on server S, for game G, with N/M seats and these filter values".
// Member SteamIDs, chat and member data stay on the hosting node server. The values
// are capped so one busy lobby cannot push a heartbeat over the size limit.

const { state } = require('../state');

// 200 keeps the heartbeat well under the directory's 128 KiB body limit while still
// covering the lobbies a single node server realistically hosts at once.
const MAX_LOBBIES = 200;
const MAX_DATA_KEYS = 24;
const MAX_DATA_CHARS = 160;

function sanitizeData(data) {
  const out = {};
  if (!data || typeof data !== 'object') return out;
  let keys = 0;
  for (const [key, value] of Object.entries(data)) {
    if (keys >= MAX_DATA_KEYS) break;
    if (typeof key !== 'string' || !key || key.length > 64) continue;
    if (value === null || value === undefined) continue;
    if (typeof value !== 'string' && typeof value !== 'number' && typeof value !== 'boolean') continue;
    out[key] = String(value).slice(0, MAX_DATA_CHARS);
    keys++;
  }
  return out;
}

function entryFor(lobby) {
  return {
    id: String(lobby.id),
    appId: String(lobby.appId),
    ownerId: String(lobby.ownerId),
    ownerName: state.names.get(String(lobby.ownerId)) || '',
    createdAt: lobby.createdAt || 0,
    maxMembers: lobby.maxMembers,
    members: lobby.members.size,
    visibility: lobby.visibility || 'public',
    joinable: lobby.joinable !== false,
    data: sanitizeData(lobby.data),
  };
}

// Every lobby this server currently hosts, newest first, capped.
function build(appId = null) {
  const out = [];
  for (const lobby of state.lobbies.values()) {
    if (appId !== null && String(lobby.appId) !== String(appId)) continue;
    out.push(entryFor(lobby));
  }
  out.sort((a, b) => (b.createdAt || 0) - (a.createdAt || 0));
  return out.slice(0, MAX_LOBBIES);
}

module.exports = { build, sanitizeData, MAX_LOBBIES };
