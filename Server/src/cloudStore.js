
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { CACHE_DIR } = require('./leaderboardStore');

const QUOTA = Math.max(1024, Math.floor(Number(process.env.WEBWORKS_CLOUD_QUOTA_BYTES)) || 5 * 1024 * 1024);
const MAX_FILES = 200;

function dirFor(appId, steamId) {
  return path.join(CACHE_DIR, 'cloud', appId, steamId);
}

function loadIndex(dir) {
  try {
    return JSON.parse(fs.readFileSync(path.join(dir, 'index.json'), 'utf8')) || {};
  } catch {
    return {};
  }
}

function saveIndex(dir, index) {
  fs.mkdirSync(dir, { recursive: true });
  const tmp = path.join(dir, 'index.json.tmp');
  fs.writeFileSync(tmp, JSON.stringify(index));
  fs.renameSync(tmp, path.join(dir, 'index.json'));
}

const hashName = (name) => crypto.createHash('sha256').update(name).digest('hex').slice(0, 40);

function validName(name) {
  if (typeof name !== 'string' || name.length === 0 || name.length > 260) return false;
  if (/[\u0000-\u001f\\]/.test(name) || name.startsWith('/')) return false;
  return !name.split('/').some((part) => part === '' || part === '.' || part === '..');
}

function list(appId, steamId) {
  const dir = dirFor(appId, steamId);
  const index = loadIndex(dir);
  return Object.entries(index).map(([hash, e]) => {
    let data = null;
    try { data = fs.readFileSync(path.join(dir, hash + '.bin')); } catch {  }
    return data ? { name: e.name, size: e.size, time: e.time, data: data.toString('base64') } : null;
  }).filter(Boolean);
}

function used(index) {
  return Object.values(index).reduce((sum, e) => sum + e.size, 0);
}

function write(appId, steamId, name, buffer) {
  if (!validName(name)) return { error: 'invalid_name' };
  const dir = dirFor(appId, steamId);
  const index = loadIndex(dir);
  const hash = hashName(name);
  const old = index[hash] ? index[hash].size : 0;
  if (!index[hash] && Object.keys(index).length >= MAX_FILES) return { error: 'too_many_files' };
  if (used(index) - old + buffer.length > QUOTA) return { error: 'quota_exceeded', quota: QUOTA };

  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(path.join(dir, hash + '.bin'), buffer);
  index[hash] = { name, size: buffer.length, time: Date.now() };
  saveIndex(dir, index);
  return { ok: true, size: buffer.length, quota: QUOTA, used: used(index) };
}

function remove(appId, steamId, name) {
  if (!validName(name)) return { error: 'invalid_name' };
  const dir = dirFor(appId, steamId);
  const index = loadIndex(dir);
  const hash = hashName(name);
  if (!index[hash]) return { ok: true, existed: false };
  try { fs.unlinkSync(path.join(dir, hash + '.bin')); } catch {  }
  delete index[hash];
  saveIndex(dir, index);
  return { ok: true, existed: true };
}

function usage(appId, steamId) {
  return { quota: QUOTA, used: used(loadIndex(dirFor(appId, steamId))) };
}

module.exports = { list, write, remove, usage, validName, QUOTA };
