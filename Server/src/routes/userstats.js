const express = require('express');
const { requireSession } = require('../authMiddleware');
const { state, ensureUserStats, statsForApp } = require('../state');
const leaderboards = require('../leaderboardStore');
const catalog = require('../achievementCatalog');
const statsStore = require('../statsStore');
const { accounts } = require('../accounts');
const gwas = require('../gwasService');
const log = require('../log');

const router = express.Router();

// GWAS deployments keep achievement state on the account service so progress follows
// the player between servers and games. If GWAS is unreachable we fall back to the
// local stores rather than dropping the player's unlocks on the floor.
let remoteWarningAt = 0;
function noteRemoteFailure(err) {
  if (Date.now() - remoteWarningAt < 60000) return;
  remoteWarningAt = Date.now();
  log.warn(`GWAS achievements unavailable (${err.code || err.message}); using local stats for now`);
}

const remoteToken = (req) => gwas.linkedToken(req);

router.get('/Stats', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const data = await gwas.stats.read(req.session.appId, token);
      return res.json({ stats: data.stats || {} });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  const record = ensureUserStats(req.session.appId, req.session.steamId);
  res.json({ stats: record.stats });
});

router.post('/Stats', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const data = await gwas.stats.write(req.session.appId, token, {
        stats: req.body.stats || {},
        unlocked: req.body.unlocked || [],
        cleared: req.body.cleared || [],
      });
      return res.json({ ok: true, stats: data.stats || {} });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  const record = ensureUserStats(req.session.appId, req.session.steamId);
  Object.assign(record.stats, req.body.stats || {});
  for (const name of req.body.unlocked || []) {
    setUnlocked(record, name, true);
    const def = catalog.byKey(req.session.appId, name);
    if (def) recordUnlock(req.session.appId, req.session.steamId, def, { unlocked: true });
  }
  for (const name of req.body.cleared || []) {
    setUnlocked(record, name, false);
    const def = catalog.byKey(req.session.appId, name);
    if (def) recordUnlock(req.session.appId, req.session.steamId, def, { unlocked: false });
  }
  statsStore.flush(req.session.appId, req.session.steamId);
  res.json({ ok: true });
});

router.get('/Stat', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const data = await gwas.stats.value(req.session.appId, token, String(req.query.name || ''));
      return res.json({ value: data.value === undefined ? 0 : data.value });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  const record = ensureUserStats(req.session.appId, req.session.steamId);
  res.json({ value: record.stats[req.query.name] ?? 0 });
});

router.post('/Stat', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const payload = { name: req.body.name };
      if (req.body.increment !== undefined) payload.increment = req.body.increment;
      else payload.value = req.body.value;
      const data = await gwas.stats.setValue(req.session.appId, token, payload);
      return res.json({ ok: true, value: data.value });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  const record = ensureUserStats(req.session.appId, req.session.steamId);
  const { name, value, increment } = req.body;

  if (increment !== undefined) {
    record.stats[name] = (record.stats[name] || 0) + increment;
  } else {
    record.stats[name] = value;
  }

  statsStore.flush(req.session.appId, req.session.steamId);
  res.json({ ok: true, value: record.stats[name] });
});

let schema = [];
try {
  const raw = require('../../achievements.json');
  schema = (Array.isArray(raw) ? raw : raw.achievements || []).map((a) => ({ ...a, id: String(a.id || a.key || '') }));
} catch {
}

function achievementList(appId, record) {
  const byKey = new Map(catalog.view(appId).achievements.map((a) => [a.key, a]));
  for (const a of schema) if (!byKey.has(a.id)) byKey.set(a.id, { id: 0, key: a.id, name: a.name, description: a.description });

  const ids = new Set([...byKey.keys(), ...Object.keys(record.achievements)]);
  return [...ids].map((id) => {
    const def = byKey.get(id) || { id: 0, key: id };
    const a = record.achievements[id] || {};
    return {
      id,
      key: def.key || id,
      numericId: def.id || 0,
      name: def.name || id,
      description: def.description || '',
      hidden: !!def.hidden,
      maxProgress: def.maxProgress || a.maxProgress || 0,
      unlocked: !!a.unlocked,
      unlockTime: a.unlockTime || 0,
      progress: a.progress || 0,
    };
  });
}

// In GWAS mode achievement state lives on the account service. A guest session in that
// mode has nowhere to store it, which is what the 501 reports.
function requireTrackable(req, res, next) {
  if (accounts.mode !== 'gwas' || remoteToken(req)) return next();
  return res.status(501).json({ error: 'achievements_local_only' });
}

function remoteAchievementList(remote) {
  return (remote.achievements || []).map((a) => ({
    id: a.key,
    key: a.key,
    numericId: a.achievementId,
    name: a.name || a.key,
    description: a.description || '',
    hidden: !!a.hidden,
    maxProgress: a.maxProgress || 0,
    unlocked: !!a.unlocked,
    unlockTime: a.unlockTime || 0,
    progress: a.progress || 0,
  }));
}

function recordUnlock(appId, steamId, entry, options) {
  return statsStore.unlock(appId, steamId, entry, options);
}

function setUnlocked(record, name, unlocked) {
  const a = (record.achievements[name] = record.achievements[name] || { unlocked: false, progress: 0, maxProgress: 0 });
  if (unlocked && !a.unlocked) a.unlockTime = Date.now();
  if (!unlocked) a.unlockTime = 0;
  a.unlocked = unlocked;
}

router.get('/Achievements', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const remote = await gwas.achievements.catalog(req.session.appId, token);
      return res.json({ achievements: remoteAchievementList(remote) });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  res.json({ achievements: achievementList(req.session.appId, ensureUserStats(req.session.appId, req.session.steamId)) });
});

router.post('/AchievementProgress', requireSession, async (req, res) => {
  const { name, current, max } = req.body;
  const token = remoteToken(req);
  if (token) {
    try {
      const remote = await gwas.achievements.progress(req.session.appId, token, { name, current, max });
      return res.json({ ok: true, achievement: remote.achievement });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  const record = ensureUserStats(req.session.appId, req.session.steamId);

  setUnlocked(record, name, (record.achievements[name] || {}).unlocked || false);
  record.achievements[name].progress = current;
  record.achievements[name].maxProgress = max;
  if (current >= max) setUnlocked(record, name, true);

  const def = catalog.byKey(req.session.appId, name);
  if (def) {
    recordUnlock(req.session.appId, req.session.steamId, def, {
      progress: current,
      max,
      unlocked: current >= max || !!record.achievements[name].unlocked,
    });
  } else {
    statsStore.flush(req.session.appId, req.session.steamId);
  }

  res.json({ ok: true });
});

// --- achievement catalog (numeric ids) -------------------------------------------

router.get('/Achievements/Catalog', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const remote = await gwas.achievements.catalog(req.session.appId, token);
      return res.json({ appId: remote.appId, nextId: remote.nextId, achievements: remoteAchievementList(remote).map((a) => ({
        id: a.numericId,
        key: a.key,
        name: a.name,
        description: a.description,
        hidden: a.hidden,
        maxProgress: a.maxProgress,
        createdAt: 0,
      })) });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  res.json(catalog.view(req.session.appId));
});

router.post('/Achievements/Define', requireSession, requireTrackable, async (req, res) => {
  const body = req.body || {};
  const entries = Array.isArray(body.achievements) ? body.achievements : [body];
  const token = remoteToken(req);
  if (token) {
    try {
      const remote = await gwas.achievements.define(req.session.appId, entries, token);
      return res.json({
        appId: remote.appId,
        nextId: remote.nextId,
        achievements: (remote.achievements || []).map((a) => ({
          id: a.achievementId,
          key: a.key,
          name: a.name || a.key,
          description: a.description || '',
          hidden: !!a.hidden,
          maxProgress: a.maxProgress || 0,
          createdAt: 0,
        })),
        added: remote.added || [],
        updated: remote.updated || [],
      });
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }
  const result = catalog.define(req.session.appId, entries);
  if (result.error) return res.status(400).json(result);
  res.json(result);
});

router.post('/Achievements/Unlock', requireSession, requireTrackable, async (req, res) => {
  const body = req.body || {};
  const token = remoteToken(req);

  if (token) {
    try {
      const remote = await gwas.achievements.unlock(req.session.appId, token, body);
      return res.json({ ok: true, achievement: remote.achievement });
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }

  const entry = catalog.resolve(req.session.appId, body);
  if (!entry) return res.status(404).json({ error: 'unknown_achievement' });

  const value = recordUnlock(req.session.appId, req.session.steamId, entry, {
    progress: body.progress,
    max: body.max,
  });
  res.json({ ok: true, achievement: value });
});

router.post('/Achievements/Clear', requireSession, requireTrackable, async (req, res) => {
  const body = req.body || {};
  const token = remoteToken(req);

  if (token) {
    try {
      const remote = await gwas.achievements.clear(req.session.appId, token, body);
      return res.json({ ok: true, achievement: remote.achievement });
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }

  const entry = catalog.resolve(req.session.appId, body);
  if (!entry) return res.status(404).json({ error: 'unknown_achievement' });
  const value = recordUnlock(req.session.appId, req.session.steamId, entry, { unlocked: false });
  res.json({ ok: true, achievement: value });
});

router.get('/Achievements/Earned', requireSession, async (req, res) => {
  const steamId = String(req.query.steamId || req.session.steamId);
  const token = remoteToken(req);

  if (token && steamId === String(req.session.steamId)) {
    try {
      const remote = await gwas.achievements.catalog(req.session.appId, token);
      return res.json({
        steamId,
        appId: req.session.appId,
        achievements: (remote.achievements || []).filter((a) => a.unlocked).map((a) => ({
          appId: String(req.session.appId),
          achievementId: a.achievementId,
          key: a.key,
          unlockTime: a.unlockTime,
          progress: a.progress || 0,
          maxProgress: a.maxProgress || 0,
        })),
      });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }

  const record = ensureUserStats(req.session.appId, steamId);
  res.json({
    steamId,
    appId: req.session.appId,
    achievements: Object.values(record.earned).filter((a) => a.unlockTime > 0),
  });
});

// Every game and achievement number this account has earned, newest first.
router.get('/Achievements/Profile', requireSession, async (req, res) => {
  const steamId = String(req.query.steamId || req.session.steamId);
  const token = remoteToken(req);

  if (token && steamId === String(req.session.steamId)) {
    try {
      const remote = await gwas.achievements.profile(token);
      const earned = (remote.earned || []).map((a) => ({
        appId: String(a.appId),
        achievementId: a.achievementId,
        key: a.key,
        unlockTime: a.unlockTime,
        progress: a.progress || 0,
        maxProgress: a.maxProgress || 0,
      })).filter((a) => a.unlockTime > 0);

      const games = new Map();
      for (const e of earned) {
        if (!games.has(e.appId)) games.set(e.appId, []);
        games.get(e.appId).push(e.achievementId);
      }

      return res.json({
        steamId,
        total: earned.length,
        games: [...games.entries()].map(([appId, ids]) => ({ appId, achievements: ids })),
        earned,
      });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }

  const earned = statsStore.earnedAcrossApps(steamId).filter((a) => a.unlockTime > 0);
  const games = new Map();
  for (const e of earned) {
    if (!games.has(e.appId)) games.set(e.appId, []);
    games.get(e.appId).push(e.achievementId);
  }
  res.json({
    steamId,
    total: earned.length,
    games: [...games.entries()].map(([appId, ids]) => ({ appId, achievements: ids })),
    earned,
  });
});

// Global unlock percentages, the data behind GetAchievementAchievedPercent.
router.get('/Achievements/Percentages', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const remote = await gwas.achievements.percentages(req.session.appId, token);
      return res.json(remote);
    } catch (err) {
      noteRemoteFailure(err);
    }
  }

  const defs = catalog.view(req.session.appId).achievements;
  const records = statsStore.recordsForApp(req.session.appId);
  const total = Math.max(1, records.size);
  const counts = new Map();
  for (const record of records.values()) {
    for (const entry of Object.values(record.earned || {})) {
      if (entry.unlockTime > 0) counts.set(Number(entry.achievementId), (counts.get(Number(entry.achievementId)) || 0) + 1);
    }
  }

  const achievements = defs.map((def) => ({
    achievementId: def.id,
    key: def.key,
    name: def.name,
    description: def.description,
    hidden: !!def.hidden,
    maxProgress: def.maxProgress || 0,
    percent: records.size ? Math.round((1000 * (counts.get(def.id) || 0)) / total) / 10 : 0,
  }));

  res.json({ appId: req.session.appId, players: records.size, achievements });
});


const bad = (res, error) => res.status(400).json({ error });

// Another player's stats, used by RequestUserStats/GetUserStat/GetUserAchievement.
router.get('/UserStats', requireSession, async (req, res) => {
  const steamId = String(req.query.steamId || '');
  if (!steamId) return res.status(400).json({ error: 'missing_steamId' });

  const token = remoteToken(req);
  const username = String(req.query.username || '');
  if (token && username) {
    try {
      const remote = await gwas.achievements.user(req.session.appId, token, username);
      return res.json({
        steamId,
        username,
        stats: remote.stats || {},
        achievements: Object.fromEntries((remote.achievements || []).map((a) => [a.key, {
          unlocked: !!a.unlocked,
          unlockTime: a.unlockTime || 0,
          progress: a.progress || 0,
          maxProgress: a.maxProgress || 0,
        }])),
        earned: remote.achievements || [],
      });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }

  const record = statsStore.record(req.session.appId, steamId);
  res.json({
    steamId,
    stats: record.stats,
    achievements: record.achievements,
    earned: Object.values(record.earned),
  });
});

// The UGC handle attached to a leaderboard, mirrored locally so AttachLeaderboardUGC
// has somewhere to report success from.
const attachedUgc = new Map();

router.post('/Leaderboard/AttachUGC', requireSession, (req, res) => {
  const { name, ugcHandle } = req.body || {};
  if (!leaderboards.validName(name)) return bad(res, 'invalid_name');
  if (!leaderboards.find(req.session.appId, name)) return res.status(404).json({ error: 'leaderboard_not_found' });
  attachedUgc.set(`${req.session.appId}|${name}`, String(ugcHandle || ''));
  res.json({ ok: true, ugcHandle: String(ugcHandle || '') });
});

// Entries for a specific set of players, for DownloadLeaderboardEntriesForUsers.
router.get('/Leaderboard/EntriesForUsers', requireSession, (req, res) => {
  const name = String(req.query.name || '');
  const users = new Set(String(req.query.users || '').split(',').filter(Boolean));
  const global = leaderboards.entries(req.session.appId, name, req.session.steamId, 'global', {});
  if (!global) return res.json({ found: false, entries: [] });
  res.json({
    found: true,
    entryCount: global.entryCount,
    entries: global.entries.filter((entry) => users.has(String(entry.steamId))),
  });
});

router.post('/Leaderboard/FindOrCreate', requireSession, (req, res) => {
  const { name, sort, display } = req.body || {};
  if (!leaderboards.validName(name)) return bad(res, 'invalid_name');
  const board = leaderboards.findOrCreate(req.session.appId, name, sort, display);
  res.json({ found: true, ...leaderboards.boardView(board, name) });
});

router.get('/Leaderboard/Find', requireSession, (req, res) => {
  const board = leaderboards.find(req.session.appId, String(req.query.name || ''));
  res.json(board ? { found: true, ...leaderboards.boardView(board, req.query.name) } : { found: false });
});

router.post('/Leaderboard/Submit', requireSession, (req, res) => {
  const { name, score, details, forceUpdate } = req.body || {};
  if (!leaderboards.validName(name)) return bad(res, 'invalid_name');
  const result = leaderboards.submit(req.session.appId, name, req.session, score, details, !!forceUpdate);
  if (result.error) return bad(res, result.error);
  res.json(result);
});

router.get('/Leaderboard/Entries', requireSession, (req, res) => {
  const q = req.query;
  const num = (v) => (v === undefined || v === '' || !Number.isFinite(Number(v)) ? undefined : Math.trunc(Number(v)));
  const result = leaderboards.entries(req.session.appId, String(q.name || ''), req.session.steamId, String(q.type || 'global'), {
    offset: num(q.offset), count: num(q.count), start: num(q.start), end: num(q.end),
  });
  if (!result) return res.json({ found: false, entryCount: 0, entries: [] });
  res.json({ found: true, ...result });
});

router.get('/Leaderboard', requireSession, (req, res) => {
  const name = String(req.query.name || '');
  const board = leaderboards.find(req.session.appId, name);
  if (!board) return res.json({ name, sort: 'descending', display: 'numeric', entries: [] });
  res.json({ ...leaderboards.boardView(board, name), entries: leaderboards.entries(req.session.appId, name, req.session.steamId, 'global', {}).entries });
});

router.post('/Leaderboard', requireSession, (req, res) => {
  const { name, sort, display, score, details, forceUpdate } = req.body || {};
  if (!leaderboards.validName(name)) return bad(res, 'invalid_name');
  const board = leaderboards.findOrCreate(req.session.appId, name, sort, display);
  if (score !== undefined) {
    const r = leaderboards.submit(req.session.appId, name, req.session, score, details, !!forceUpdate);
    if (r.error) return bad(res, r.error);
  }
  res.json({ ...leaderboards.boardView(board, name), entries: leaderboards.entries(req.session.appId, name, req.session.steamId, 'global', {}).entries });
});

router.get('/GlobalStats', requireSession, (req, res) => {
  const days = Math.max(1, Math.min(60, Number(req.query.days) || 1));
  const stats = {};
  for (const record of statsForApp(req.session.appId).values()) {
    for (const [name, value] of Object.entries(record.stats)) {
      if (typeof value === 'number') stats[name] = (stats[name] || 0) + value;
    }
  }
  const history = {};
  for (const [name, total] of Object.entries(stats)) {
    history[name] = [total, ...new Array(days - 1).fill(0)];
  }
  res.json({ days, stats, history });
});

router.get('/PlayerCount', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const remote = await gwas.achievements.players(req.session.appId, token);
      return res.json({ count: Number(remote.players) || 0 });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }

  let count = 0;
  for (const steamId of state.sockets.keys()) {
    if (state.appOf.get(steamId) === req.session.appId) count++;
  }
  res.json({ count });
});

router.post('/Reset', requireSession, (req, res) => {
  if (process.env.WEBWORKS_ALLOW_STATS_RESET !== 'true') {
    return res.status(403).json({ error: 'disabled' });
  }
  const record = ensureUserStats(req.session.appId, req.session.steamId);
  record.stats = {};
  record.achievements = {};
  record.earned = {};
  statsStore.flush(req.session.appId, req.session.steamId);
  res.json({ ok: true });
});

module.exports = router;
