const MAX_INPUT_QUEUE = 256;
const BROADCAST_TTL_MS = 12 * 60 * 60 * 1000;
const PARTY_TTL_MS = 6 * 60 * 60 * 1000;
const SESSION_TTL_MS = 6 * 60 * 60 * 1000;

const music = new Map();
const broadcasts = new Map();
const remoteSessions = new Map();
const parties = new Map();
let nextId = 1;

function idFor(prefix) {
  return prefix + '-' + (nextId++).toString(36) + '-' + Date.now().toString(36);
}

function musicFor(steamId) {
  return music.get(String(steamId)) || { status: 'Stopped', track: '', artist: '', volume: 1, updatedAt: 0 };
}

function setMusic(steamId, patch) {
  const id = String(steamId);
  const current = musicFor(id);
  const next = {
    status: typeof patch.status === 'string' ? patch.status.slice(0, 24) : current.status,
    track: typeof patch.track === 'string' ? patch.track.slice(0, 256) : current.track,
    artist: typeof patch.artist === 'string' ? patch.artist.slice(0, 128) : current.artist,
    volume: typeof patch.volume === 'number' ? Math.max(0, Math.min(1, patch.volume)) : current.volume,
    updatedAt: Date.now(),
  };
  music.set(id, next);
  return next;
}

function startBroadcast(steamId, appId, body) {
  const existing = broadcasts.get(String(steamId));
  if (existing) stopBroadcast(steamId);
  const record = {
    id: idFor('bc'),
    steamId: String(steamId),
    appId,
    url: typeof body.url === 'string' ? body.url.slice(0, 1024) : '',
    title: typeof body.title === 'string' ? body.title.slice(0, 128) : '',
    viewers: new Set(),
    startedAt: Date.now(),
  };
  broadcasts.set(String(steamId), record);
  return record;
}

function stopBroadcast(steamId) {
  const record = broadcasts.get(String(steamId));
  if (!record) return null;
  broadcasts.delete(String(steamId));
  return record;
}

function broadcastOf(steamId) {
  return broadcasts.get(String(steamId)) || null;
}

function broadcastsForApp(appId) {
  return [...broadcasts.values()].filter((b) => b.appId === appId);
}

function watch(steamId, broadcasterSteamId) {
  const record = broadcastOf(broadcasterSteamId);
  if (!record) return null;
  record.viewers.add(String(steamId));
  return record;
}

function unwatch(steamId, broadcasterSteamId) {
  const record = broadcastOf(broadcasterSteamId);
  if (!record) return null;
  record.viewers.delete(String(steamId));
  return record;
}

function startRemoteSession(steamId, appId, body) {
  const record = {
    id: idFor('rp'),
    hostSteamId: String(steamId),
    hostName: typeof body.name === 'string' ? body.name.slice(0, 64) : '',
    appId,
    title: typeof body.title === 'string' ? body.title.slice(0, 128) : '',
    maxGuests: Math.max(1, Math.min(8, Number(body.maxGuests) || 4)),
    resolutionX: Math.max(0, Number(body.resolutionX) || 1920),
    resolutionY: Math.max(0, Number(body.resolutionY) || 1080),
    guests: new Set(),
    input: new Map(),
    createdAt: Date.now(),
  };
  remoteSessions.set(record.id, record);
  return record;
}

function remoteSession(id) {
  return remoteSessions.get(String(id)) || null;
}

function remoteSessionsForApp(appId) {
  return [...remoteSessions.values()].filter((s) => s.appId === appId);
}

function joinRemoteSession(sessionId, steamId) {
  const record = remoteSession(sessionId);
  if (!record) return { error: 'not_found' };
  if (record.guests.size >= record.maxGuests) return { error: 'session_full' };
  if (record.hostSteamId === String(steamId)) return { error: 'host_cannot_join' };
  record.guests.add(String(steamId));
  return { ok: true, session: record };
}

function leaveRemoteSession(sessionId, steamId) {
  const record = remoteSession(sessionId);
  if (!record) return { ok: true };
  record.guests.delete(String(steamId));
  record.input.delete(String(steamId));
  if (record.guests.size === 0 && record.hostSteamId === String(steamId)) remoteSessions.delete(record.id);
  return { ok: true, session: record };
}

function stopRemoteSession(sessionId, steamId) {
  const record = remoteSession(sessionId);
  if (!record) return { ok: true };
  if (record.hostSteamId !== String(steamId)) return { error: 'not_host' };
  remoteSessions.delete(record.id);
  return { ok: true, session: record };
}

function pushRemoteInput(sessionId, fromSteamId, event) {
  const record = remoteSession(sessionId);
  if (!record) return { error: 'not_found' };
  if (!record.guests.has(String(fromSteamId))) return { error: 'not_a_guest' };
  const queue = record.input.get(String(fromSteamId)) || [];
  queue.push(event);
  while (queue.length > MAX_INPUT_QUEUE) queue.shift();
  record.input.set(String(fromSteamId), queue);
  return { ok: true };
}

function drainRemoteInput(sessionId, guestSteamId) {
  const record = remoteSession(sessionId);
  if (!record) return { error: 'not_found' };
  if (guestSteamId) {
    const list = record.input.get(String(guestSteamId)) || [];
    record.input.set(String(guestSteamId), []);
    return { ok: true, events: list };
  }
  const all = [];
  for (const [guest, list] of record.input) {
    for (const event of list) all.push({ ...event, guestSteamId: guest });
    record.input.set(guest, []);
  }
  return { ok: true, events: all };
}

function startParty(steamId, appId, body) {
  for (const party of parties.values()) {
    if (party.ownerSteamId === String(steamId)) parties.delete(party.id);
  }
  const record = {
    id: idFor('party'),
    ownerSteamId: String(steamId),
    appId,
    metadata: typeof body.metadata === 'string' ? body.metadata.slice(0, 512) : '',
    ownerName: typeof body.name === 'string' ? body.name.slice(0, 64) : '',
    maxMembers: Math.max(1, Math.min(16, Number(body.maxMembers) || 8)),
    members: new Set([String(steamId)]),
    createdAt: Date.now(),
  };
  parties.set(record.id, record);
  return record;
}

function party(id) { return parties.get(String(id)) || null; }

function joinParty(id, steamId) {
  const record = party(id);
  if (!record) return { error: 'not_found' };
  if (record.members.size >= record.maxMembers && !record.members.has(String(steamId))) return { error: 'party_full' };
  record.members.add(String(steamId));
  return { ok: true, party: record };
}

function leaveParty(id, steamId) {
  const record = party(id);
  if (!record) return { ok: true };
  record.members.delete(String(steamId));
  if (record.members.size === 0) parties.delete(record.id);
  return { ok: true, party: record };
}

function destroyParty(id, steamId) {
  const record = party(id);
  if (!record) return { ok: true };
  if (record.ownerSteamId !== String(steamId)) return { error: 'not_owner' };
  parties.delete(record.id);
  return { ok: true, party: record };
}

function partiesForApp(appId) {
  return [...parties.values()].filter((p) => p.appId === appId);
}

function sweep() {
  const now = Date.now();
  let removed = 0;
  for (const [id, record] of broadcasts) {
    if (now - record.startedAt > BROADCAST_TTL_MS) { broadcasts.delete(id); removed++; }
  }
  for (const [id, record] of remoteSessions) {
    if (now - record.createdAt > SESSION_TTL_MS) { remoteSessions.delete(id); removed++; }
  }
  for (const [id, record] of parties) {
    if (now - record.createdAt > PARTY_TTL_MS) { parties.delete(id); removed++; }
  }
  return removed;
}

module.exports = {
  musicFor, setMusic,
  startBroadcast, stopBroadcast, broadcastOf, broadcastsForApp, watch, unwatch,
  startRemoteSession, remoteSession, remoteSessionsForApp, joinRemoteSession, leaveRemoteSession, stopRemoteSession,
  pushRemoteInput, drainRemoteInput,
  startParty, party, joinParty, leaveParty, destroyParty, partiesForApp,
  sweep,
  count: () => ({ broadcasts: broadcasts.size, remotePlay: remoteSessions.size, parties: parties.size }),
};
