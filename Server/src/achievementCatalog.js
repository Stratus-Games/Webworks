const fs = require('fs');
const path = require('path');

// Per-game achievement definitions. Every achievement gets a stable numeric id the
// first time it is seen, so a user's unlocks can be stored as { appId, achievementId }
// and still resolve after the catalog grows.
const CACHE_DIR = process.env.WEBWORKS_CACHE_DIR
  ? path.resolve(process.env.WEBWORKS_CACHE_DIR)
  : path.join(__dirname, '..', 'cache');
const DIR = path.join(CACHE_DIR, 'achievements');
const SEED_FILE = path.join(__dirname, '..', 'achievements.json');

const KEY_RE = /^[A-Za-z0-9_.:-]{1,128}$/;
const MAX_NAME = 256;
const MAX_DESCRIPTION = 1024;
const MAX_ACHIEVEMENTS = 1000;

const catalogs = new Map();

function fileFor(appId) {
  return path.join(DIR, `${String(appId).replace(/[^0-9A-Za-z_-]/g, '_')}.json`);
}

function blank(appId) {
  return { version: 1, appId: String(appId), nextId: 1, achievements: [] };
}

function cleanText(value, max) {
  return typeof value === 'string' ? value.slice(0, max) : '';
}

function normalizeEntry(raw) {
  if (!raw || typeof raw !== 'object') return null;
  const key = String(raw.key || raw.id || '').trim();
  if (!KEY_RE.test(key)) return null;
  return {
    key,
    name: cleanText(raw.name, MAX_NAME) || key,
    description: cleanText(raw.description, MAX_DESCRIPTION),
    hidden: Boolean(raw.hidden),
    maxProgress: Math.max(0, Math.trunc(Number(raw.maxProgress) || 0)),
  };
}

function load(appId) {
  const id = String(appId);
  if (catalogs.has(id)) return catalogs.get(id);

  let catalog = blank(id);
  try {
    const parsed = JSON.parse(fs.readFileSync(fileFor(id), 'utf8'));
    if (parsed && Array.isArray(parsed.achievements)) {
      catalog = {
        version: 1,
        appId: id,
        nextId: Math.max(1, Math.trunc(Number(parsed.nextId) || 1)),
        achievements: parsed.achievements
          .filter((a) => a && Number.isFinite(Number(a.id)) && KEY_RE.test(String(a.key || '')))
          .map((a) => ({
            id: Math.trunc(Number(a.id)),
            key: String(a.key),
            name: cleanText(a.name, MAX_NAME) || String(a.key),
            description: cleanText(a.description, MAX_DESCRIPTION),
            hidden: Boolean(a.hidden),
            maxProgress: Math.max(0, Math.trunc(Number(a.maxProgress) || 0)),
            createdAt: Number(a.createdAt) || Date.now(),
          })),
      };
      const highest = catalog.achievements.reduce((n, a) => Math.max(n, a.id), 0);
      catalog.nextId = Math.max(catalog.nextId, highest + 1);
    }
  } catch {
    catalog = blank(id);
  }

  if (!catalog.achievements.length) seed(catalog);
  catalogs.set(id, catalog);
  return catalog;
}

// Optional hand-written list at Server/achievements.json; ids become 1..n on first run.
function seed(catalog) {
  let seedList = [];
  try {
    const raw = JSON.parse(fs.readFileSync(SEED_FILE, 'utf8'));
    seedList = Array.isArray(raw) ? raw : Array.isArray(raw && raw.achievements) ? raw.achievements : [];
  } catch {
    return;
  }
  for (const raw of seedList) {
    const entry = normalizeEntry(raw);
    if (!entry || catalog.achievements.some((a) => a.key === entry.key)) continue;
    if (catalog.achievements.length >= MAX_ACHIEVEMENTS) break;
    catalog.achievements.push({ id: catalog.nextId++, ...entry, createdAt: Date.now() });
  }
  if (catalog.achievements.length) save(catalog);
}

function save(catalog) {
  try {
    fs.mkdirSync(DIR, { recursive: true });
    const file = fileFor(catalog.appId);
    const tmp = `${file}.tmp`;
    fs.writeFileSync(tmp, JSON.stringify(catalog, null, 2));
    fs.renameSync(tmp, file);
  } catch (err) {
    console.error(`[webworks] could not write achievement catalog for ${catalog.appId}: ${err.message}`);
  }
}

function view(appId) {
  const catalog = load(appId);
  return {
    appId: catalog.appId,
    nextId: catalog.nextId,
    achievements: catalog.achievements.map((a) => ({ ...a })),
  };
}

function define(appId, entries) {
  const catalog = load(appId);
  if (!Array.isArray(entries)) return { error: 'invalid_achievements' };

  const added = [];
  const updated = [];
  for (const raw of entries) {
    const entry = normalizeEntry(raw);
    if (!entry) return { error: 'invalid_achievement', key: raw && raw.key };
    const existing = catalog.achievements.find((a) => a.key === entry.key);
    if (existing) {
      existing.name = entry.name;
      existing.description = entry.description;
      existing.hidden = entry.hidden;
      existing.maxProgress = entry.maxProgress;
      updated.push(existing.id);
      continue;
    }
    if (catalog.achievements.length >= MAX_ACHIEVEMENTS) return { error: 'achievement_limit_reached' };
    const item = { id: catalog.nextId++, ...entry, createdAt: Date.now() };
    catalog.achievements.push(item);
    added.push(item.id);
  }

  save(catalog);
  return { ...view(appId), added, updated };
}

function byKey(appId, key) {
  return load(appId).achievements.find((a) => a.key === key) || null;
}

function byId(appId, id) {
  const numeric = Math.trunc(Number(id));
  if (!Number.isFinite(numeric)) return null;
  return load(appId).achievements.find((a) => a.id === numeric) || null;
}

function resolve(appId, { id, key }) {
  if (key !== undefined && key !== null && key !== '') return byKey(appId, String(key));
  if (id !== undefined && id !== null && id !== '') return byId(appId, id);
  return null;
}

function init() {
  fs.mkdirSync(DIR, { recursive: true });
  return { dir: DIR };
}

module.exports = { init, load, view, define, byKey, byId, resolve, DIR, KEY_RE, MAX_ACHIEVEMENTS };
