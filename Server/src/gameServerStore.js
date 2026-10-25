
const HEARTBEAT_TIMEOUT_MS = Math.max(5000, Number(process.env.WEBWORKS_SERVER_HEARTBEAT_TIMEOUT_MS) || 45000);
const MAX_KEYS_PER_SERVER = 64;

const servers = new Map();

function logOn(appId, session, body, fallbackIp) {
  const { gamePort, queryPort, ip, versionString, dedicated } = body || {};
  const record = {
    id: session.steamId,
    appId,
    ip: typeof ip === 'string' && ip ? ip : (fallbackIp || '0.0.0.0'),
    gamePort: Number(gamePort) || 0,
    queryPort: Number(queryPort) || 0,
    versionString: typeof versionString === 'string' ? versionString : '1.0.0.0',
    dedicated: !!dedicated,
    name: 'A Webworks Server',
    map: '',
    maxPlayers: 0,
    botCount: 0,
    tags: '',
    passworded: false,
    secure: true,
    rules: {},
    players: [],
    lastHeartbeat: Date.now(),
    createdAt: Date.now(),
  };
  servers.set(record.id, record);
  return record;
}

function find(serverId) { return servers.get(serverId) || null; }

function findByAddress(host, port) {
  const h = String(host || '').toLowerCase();
  const p = Number(port) || 0;
  for (const record of servers.values()) {
    const ip = String(record.ip || '').toLowerCase();
    if (ip !== h) continue;
    if (!p) return record;
    if (record.gamePort === p || record.queryPort === p) return record;
  }
  return null;
}

function heartbeat(serverId, patch) {
  const record = find(serverId);
  if (!record) return { error: 'not_logged_on' };

  const { name, map, maxPlayers, botCount, tags, passworded, gamePort, queryPort } = patch || {};
  if (name !== undefined) record.name = String(name).slice(0, 64);
  if (map !== undefined) record.map = String(map).slice(0, 64);
  if (maxPlayers !== undefined) record.maxPlayers = Math.max(0, Number(maxPlayers) || 0);
  if (botCount !== undefined) record.botCount = Math.max(0, Number(botCount) || 0);
  if (tags !== undefined) record.tags = String(tags).slice(0, 128);
  if (passworded !== undefined) record.passworded = !!passworded;
  if (gamePort !== undefined) record.gamePort = Number(gamePort) || record.gamePort;
  if (queryPort !== undefined) record.queryPort = Number(queryPort) || record.queryPort;

  record.lastHeartbeat = Date.now();
  return { ok: true };
}

function setKeys(serverId, sets, clearAll) {
  const record = find(serverId);
  if (!record) return { error: 'not_logged_on' };
  if (clearAll) record.rules = {};
  if (sets && typeof sets === 'object') {
    for (const [k, v] of Object.entries(sets)) {
      if (Object.keys(record.rules).length >= MAX_KEYS_PER_SERVER && !(k in record.rules)) continue;
      record.rules[String(k).slice(0, 64)] = String(v).slice(0, 256);
    }
  }
  return { ok: true };
}

function updatePlayer(serverId, steamId, name, score) {
  const record = find(serverId);
  if (!record) return { error: 'not_logged_on' };
  let p = record.players.find((p) => p.steamId === steamId);
  if (!p) { p = { steamId, name: '', score: 0 }; record.players.push(p); }
  if (name !== undefined) p.name = String(name).slice(0, 32);
  if (score !== undefined) p.score = Number(score) || 0;
  return { ok: true };
}

function endPlayerSession(serverId, steamId) {
  const record = find(serverId);
  if (!record) return { error: 'not_logged_on' };
  record.players = record.players.filter((p) => p.steamId !== steamId);
  return { ok: true };
}

function logOff(serverId) {
  return { ok: true, existed: servers.delete(serverId) };
}

function view(record) {
  return {
    id: record.id,
    ip: record.ip,
    gamePort: record.gamePort,
    queryPort: record.queryPort,
    name: record.name,
    map: record.map,
    maxPlayers: record.maxPlayers,
    playerCount: record.players.length,
    botCount: record.botCount,
    tags: record.tags,
    passworded: record.passworded,
    secure: record.secure,
    dedicated: record.dedicated,
    versionString: record.versionString,
    rules: record.rules,
  };
}

function list(appId, opts) {
  let entries = [...servers.values()].filter((r) => r.appId === appId);

  if (opts.map) entries = entries.filter((r) => r.map === opts.map);
  if (opts.notFull) entries = entries.filter((r) => r.maxPlayers === 0 || r.players.length < r.maxPlayers);
  if (opts.noPassword) entries = entries.filter((r) => !r.passworded);
  if (opts.secureOnly) entries = entries.filter((r) => r.secure);
  if (Array.isArray(opts.tags) && opts.tags.length) {
    entries = entries.filter((r) => opts.tags.every((t) => (r.tags || '').split(',').includes(t)));
  }
  if (opts.text) {
    const needle = String(opts.text).toLowerCase();
    entries = entries.filter((r) => r.name.toLowerCase().includes(needle));
  }

  return entries.map(view);
}

function sweep() {
  const cutoff = Date.now() - HEARTBEAT_TIMEOUT_MS;
  let removed = 0;
  for (const [id, record] of servers) {
    if (record.lastHeartbeat < cutoff) { servers.delete(id); removed++; }
  }
  return removed;
}

module.exports = {
  logOn, find, findByAddress, heartbeat, setKeys, updatePlayer, endPlayerSession, logOff, list, view, sweep,
  HEARTBEAT_TIMEOUT_MS,
  count: () => servers.size,
};
