const { state } = require('./state');
const { pushToLobby } = require('./realtime');

const MAX_CHAT_BYTES = 4096;
const ENTRY_TYPE_CHAT_MSG = 1;

const BUCKET_CAPACITY = 200;
const REFILL_PER_SEC = 120;
const buckets = new Map();

function allow(steamId, now = Date.now()) {
  let b = buckets.get(steamId);
  if (!b) { b = { tokens: BUCKET_CAPACITY, at: now }; buckets.set(steamId, b); }
  b.tokens = Math.min(BUCKET_CAPACITY, b.tokens + ((now - b.at) / 1000) * REFILL_PER_SEC);
  b.at = now;
  if (b.tokens < 1) return false;
  b.tokens -= 1;
  return true;
}

function forget(steamId) { buckets.delete(steamId); }

function decodePayload(data, message) {
  let buf;
  if (typeof data === 'string' && data.length > 0) {
    if (data.length > Math.ceil(MAX_CHAT_BYTES / 3) * 4) return { error: 'message_too_large' };
    if (!/^[A-Za-z0-9+/]*={0,2}$/.test(data)) return { error: 'bad_data' };
    buf = Buffer.from(data, 'base64');
  } else if (typeof message === 'string') {
    buf = Buffer.from(message, 'utf8');
  } else {
    return { error: 'empty_message' };
  }
  if (buf.length === 0) return { error: 'empty_message' };
  if (buf.length > MAX_CHAT_BYTES) return { error: 'message_too_large' };
  return { buf };
}

function send(session, lobbyId, data, message) {
  const lobby = state.lobbies.get(String(lobbyId));
  if (!lobby || lobby.appId !== session.appId) return { status: 404, error: 'lobby_not_found' };
  if (!lobby.members.has(session.steamId)) return { status: 403, error: 'not_a_lobby_member' };
  if (!allow(session.steamId)) return { status: 429, error: 'rate_limited' };

  const decoded = decodePayload(data, message);
  if (decoded.error) return { status: 400, error: decoded.error };

  lobby.chatSeq = (lobby.chatSeq || 0) + 1;
  const buf = decoded.buf;
  pushToLobby(lobby, {
    type: 'LobbyChat',
    lobbyId: lobby.id,
    steamId: session.steamId,
    name: session.name,
    chatId: lobby.chatSeq,
    entryType: ENTRY_TYPE_CHAT_MSG,
    data: buf.toString('base64'),
    message: buf.toString('utf8').replace(/\0+$/, ''),
  });
  return { ok: true, chatId: lobby.chatSeq };
}

module.exports = { send, forget, decodePayload, allow, MAX_CHAT_BYTES };
