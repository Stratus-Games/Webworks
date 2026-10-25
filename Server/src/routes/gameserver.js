const express = require('express');
const { requireSession, requireServerSession } = require('../authMiddleware');
const { generateServerId } = require('../identity');
const { state, ensureUserStats } = require('../state');
const gameServers = require('../gameServerStore');
const catalog = require('../achievementCatalog');
const statsStore = require('../statsStore');

const router = express.Router();

router.post('/LogOn', requireSession, (req, res) => {
  if (req.session.kind === 'server') return res.status(400).json({ error: 'already_a_server_session' });

  const serverId = generateServerId();
  const token = require('crypto').randomBytes(24).toString('hex');
  const session = { steamId: serverId, name: 'GameServer', appId: req.session.appId, kind: 'server', createdAt: Date.now() };
  state.sessions.set(token, session);

  gameServers.logOn(req.session.appId, session, req.body || {}, req.ip);
  res.json({ sessionToken: token, serverId });
});

router.post('/LogOff', requireServerSession, (req, res) => {
  const result = gameServers.logOff(req.session.steamId);
  state.sessions.delete(req.sessionToken);
  res.json(result);
});

router.post('/Heartbeat', requireServerSession, (req, res) => {
  const result = gameServers.heartbeat(req.session.steamId, req.body || {});
  if (result.error) return res.status(404).json(result);
  res.json(result);
});

router.post('/Keys', requireServerSession, (req, res) => {
  const { set, clearAll } = req.body || {};
  const result = gameServers.setKeys(req.session.steamId, set, !!clearAll);
  if (result.error) return res.status(404).json(result);
  res.json(result);
});

router.post('/Player', requireServerSession, (req, res) => {
  const { steamId, name, score, end } = req.body || {};
  if (!steamId) return res.status(400).json({ error: 'missing_steamId' });
  const result = end
    ? gameServers.endPlayerSession(req.session.steamId, steamId)
    : gameServers.updatePlayer(req.session.steamId, steamId, name, score);
  if (result.error) return res.status(404).json(result);
  res.json(result);
});

router.get('/List', requireSession, (req, res) => {
  const q = req.query || {};
  const entries = gameServers.list(req.session.appId, {
    map: q.map || undefined,
    notFull: q.notFull === '1',
    noPassword: q.noPassword === '1',
    secureOnly: q.secureOnly === '1',
    tags: q.tags ? String(q.tags).split(',').filter(Boolean) : undefined,
    text: q.text || undefined,
  });
  res.json({ entries });
});


router.get('/Stats', requireServerSession, (req, res) => {
  if (!req.query.steamId) return res.status(400).json({ error: 'missing_steamId' });
  const record = ensureUserStats(req.session.appId, req.query.steamId);
  res.json({ stats: record.stats, achievements: record.achievements });
});

router.post('/Stats', requireServerSession, (req, res) => {
  const { steamId, stats, unlocked, cleared } = req.body || {};
  if (!steamId) return res.status(400).json({ error: 'missing_steamId' });
  const record = ensureUserStats(req.session.appId, steamId);
  Object.assign(record.stats, stats || {});
  for (const name of unlocked || []) {
    record.achievements[name] = { ...(record.achievements[name] || {}), unlocked: true, unlockTime: (record.achievements[name] || {}).unlockTime || Date.now() };
    const def = catalog.byKey(req.session.appId, name);
    if (def) statsStore.unlock(req.session.appId, steamId, def, { unlocked: true });
  }
  for (const name of cleared || []) {
    record.achievements[name] = { ...(record.achievements[name] || {}), unlocked: false, unlockTime: 0 };
    const def = catalog.byKey(req.session.appId, name);
    if (def) statsStore.unlock(req.session.appId, steamId, def, { unlocked: false });
  }
  statsStore.flush(req.session.appId, steamId);
  res.json({ ok: true });
});

module.exports = router;
