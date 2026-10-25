const fs = require('fs');
const path = require('path');

// Per-game, per-user stats and achievements. Records live on disk so unlocks survive a
// restart, and achievements are keyed by the catalog's numeric id (see achievementCatalog).
const CACHE_DIR = process.env.WEBWORKS_CACHE_DIR
  ? path.resolve(process.env.WEBWORKS_CACHE_DIR)
  : path.join(__dirname, '..', 'cache');
const DIR = path.join(CACHE_DIR, 'userstats');

const cache = new Map();
const dirty = new Set();
let saveTimer = null;

function safeSegment(value) {
  return String(value).replace(/[^0-9A-Za-z_.-]/g, '_').slice(0, 64) || '0';
}

function fileFor(appId, steamId) {
  return path.join(DIR, safeSegment(appId), `${safeSegment(steamId)}.json`);
}

function blank() {
  return { stats: {}, achievements: {}, earned: {} };
}

function normalize(raw) {
  const record = blank();
  if (!raw || typeof raw !== 'object') return record;
  if (raw.stats && typeof raw.stats === 'object') {
    for (const [name, value] of Object.entries(raw.stats)) {
      if (typeof value === 'number' || typeof value === 'boolean' || typeof value === 'string') record.stats[name] = value;
    }
  }
  if (raw.achievements && typeof raw.achievements === 'object') {
    for (const [name, a] of Object.entries(raw.achievements)) {
      if (!a || typeof a !== 'object') continue;
      record.achievements[name] = {
        unlocked: Boolean(a.unlocked),
        unlockTime: Number(a.unlockTime) || 0,
        progress: Math.trunc(Number(a.progress) || 0),
        maxProgress: Math.trunc(Number(a.maxProgress) || 0),
      };
    }
  }
  if (raw.earned && typeof raw.earned === 'object') {
    for (const [id, e] of Object.entries(raw.earned)) {
      if (!e || typeof e !== 'object') continue;
      const numeric = Math.trunc(Number(e.achievementId !== undefined ? e.achievementId : id));
      if (!Number.isFinite(numeric) || numeric < 0) continue;
      record.earned[String(numeric)] = {
        appId: String(e.appId || ''),
        achievementId: numeric,
        key: String(e.key || ''),
        unlockTime: Number(e.unlockTime) || 0,
        progress: Math.trunc(Number(e.progress) || 0),
        maxProgress: Math.trunc(Number(e.maxProgress) || 0),
      };
    }
  }
  return record;
}

function key(appId, steamId) {
  return `${appId}\u0000${steamId}`;
}

function record(appId, steamId) {
  const k = key(appId, steamId);
  if (cache.has(k)) return cache.get(k);

  let parsed = null;
  try {
    parsed = JSON.parse(fs.readFileSync(fileFor(appId, steamId), 'utf8'));
  } catch {
  }
  const value = normalize(parsed);
  cache.set(k, value);
  return value;
}

function markDirty(appId, steamId) {
  dirty.add(key(appId, steamId));
  if (saveTimer) return;
  saveTimer = setTimeout(saveNow, 500);
  if (saveTimer.unref) saveTimer.unref();
}

function recordsForApp(appId) {
  const out = new Map();
  const dir = path.join(DIR, safeSegment(appId));
  let files = [];
  try {
    files = fs.readdirSync(dir).filter((f) => f.endsWith('.json'));
  } catch {
    return out;
  }
  for (const file of files) {
    const steamId = file.slice(0, -5);
    out.set(steamId, record(appId, steamId));
  }
  for (const [k, value] of cache) {
    const [app, steamId] = k.split('\u0000');
    if (app === String(appId)) out.set(steamId, value);
  }
  return out;
}

function save(appId, steamId) {
  const value = cache.get(key(appId, steamId));
  if (!value) return;
  try {
    const dir = path.join(DIR, safeSegment(appId));
    fs.mkdirSync(dir, { recursive: true });
    const file = fileFor(appId, steamId);
    const tmp = `${file}.tmp`;
    fs.writeFileSync(tmp, JSON.stringify(value, null, 2));
    fs.renameSync(tmp, file);
  } catch (err) {
    console.error(`[webworks] could not write stats for ${appId}/${steamId}: ${err.message}`);
  }
}

function saveNow() {
  if (saveTimer) {
    clearTimeout(saveTimer);
    saveTimer = null;
  }
  for (const k of [...dirty]) {
    const [appId, steamId] = k.split('\u0000');
    save(appId, steamId);
  }
  dirty.clear();
}

function flush(appId, steamId) {
  markDirty(appId, steamId);
  save(appId, steamId);
}

// Every game this user has an achievement in, as { appId, achievementId } pairs.
function earnedAcrossApps(steamId) {
  const wanted = safeSegment(steamId);
  const out = [];
  let apps = [];
  try {
    apps = fs.readdirSync(DIR);
  } catch {
    apps = [];
  }
  for (const appId of apps) {
    let exists = false;
    try {
      exists = fs.existsSync(fileFor(appId, wanted));
    } catch {
      exists = false;
    }
    if (!exists && !cache.has(key(appId, wanted))) continue;
    const value = record(appId, wanted);
    for (const entry of Object.values(value.earned)) {
      out.push({ appId: String(appId), ...entry });
    }
  }
  return out.sort((a, b) => b.unlockTime - a.unlockTime);
}

// Records an unlock against the numeric catalog id, keeping the legacy by-name view in
// sync so older clients keep working.
function unlock(appId, steamId, entry, opts = {}) {
  const record_ = record(appId, steamId);
  const unlocked = opts.unlocked !== false;
  const id = String(entry.id);
  const previous = record_.earned[id];
  const unlockTime = unlocked ? (previous && previous.unlockTime) || Date.now() : 0;
  const value = {
    appId: String(appId),
    achievementId: entry.id,
    key: entry.key,
    unlockTime,
    progress: Math.max(0, Math.trunc(Number(opts.progress) || 0)),
    maxProgress: Math.max(0, Math.trunc(Number(opts.max) || entry.maxProgress || 0)),
  };
  record_.earned[id] = value;
  record_.achievements[entry.key] = {
    unlocked,
    unlockTime,
    progress: value.progress,
    maxProgress: value.maxProgress,
  };
  flush(appId, steamId);
  return value;
}

function init() {
  fs.mkdirSync(DIR, { recursive: true });
  return { dir: DIR };
}

module.exports = { init, record, recordsForApp, markDirty, flush, saveNow, earnedAcrossApps, unlock, DIR, fileFor };
