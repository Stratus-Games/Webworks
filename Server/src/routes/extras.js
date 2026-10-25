const express = require('express');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { requireSession, requireServerSession } = require('../authMiddleware');
const inventory = require('../inventoryStore');
const { state } = require('../state');
const gwas = require('../gwasService');
const { CACHE_DIR } = require('../leaderboardStore');
const log = require('../log');

const router = express.Router();

// Inventory follows the same rule as achievements: with a GWAS account the items live
// on the account service so they are shared by every game and server.
let remoteWarningAt = 0;
function noteRemoteFailure(err) {
  if (Date.now() - remoteWarningAt < 60000) return;
  remoteWarningAt = Date.now();
  log.warn(`GWAS inventory unavailable (${err.code || err.message}); using the local store for now`);
}

const remoteToken = (req) => gwas.linkedToken(req);
const linkedFor = (steamId) => state.linkedAccounts.get(String(steamId)) || null;

function encodeItemToken(appId, itemDefId) {
  return `item:${Buffer.from(JSON.stringify({ appId, itemDefId })).toString('base64url')}`;
}

function decodeItemToken(token) {
  const text = String(token || '');
  try {
    return JSON.parse(Buffer.from(text.startsWith('item:') ? text.slice(5) : text, 'base64url').toString('utf8'));
  } catch {
    return null;
  }
}


router.get('/Apps/Info', requireSession, (req, res) => {
  res.json({
    appId: req.session.appId,
    language: 'english',
    availableLanguages: ['english'],
    buildId: 1,
    betaName: '',
    launchCommandLine: '',
    launchQueryParams: {},
    dlcs: [],
    owned: true,
    vacBanned: false,
    lowViolence: false,
    cybercafe: false,
  });
});


router.get('/Inventory/Definitions', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const data = await gwas.inventory.definitions(req.session.appId, token);
      return res.json({ definitions: data.definitions || [] });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  res.json({ definitions: inventory.definitions(req.session.appId) });
});

router.get('/Inventory/Items', requireSession, async (req, res) => {
  const token = remoteToken(req);
  if (token) {
    try {
      const data = await gwas.inventory.items(req.session.appId, token);
      return res.json({ items: data.items || [] });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  res.json({ items: inventory.items(req.session.appId, req.session.steamId) });
});

router.post('/Inventory/Define', requireServerSession, async (req, res) => {
  const body = req.body || {};
  if (gwas.enabled()) {
    try {
      const result = await gwas.inventory.define(req.session.appId, body);
      return res.json(result.definition || result);
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }
  const result = inventory.define(req.session.appId, body.itemDefId, body);
  if (result.error) return res.status(400).json(result);
  res.json(result);
});

router.post('/Inventory/Grant', requireServerSession, async (req, res) => {
  const body = req.body || {};
  const steamId = String(body.steamId || '');
  if (!steamId) return res.status(400).json({ error: 'missing_steam_id' });

  const linked = linkedFor(steamId);
  if (gwas.enabled() && linked) {
    try {
      const result = await gwas.inventory.grant(req.session.appId, linked.token, {
        itemDefId: body.itemDefId,
        quantity: body.quantity,
        username: linked.username,
      });
      return res.json(result);
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }
  if (gwas.enabled()) return res.status(409).json({ error: 'player_not_connected' });

  const result = inventory.grant(req.session.appId, steamId, body.itemDefId, body.quantity);
  if (result.error) return res.status(400).json(result);
  res.json(result);
});

router.post('/Inventory/GrantSelf', requireSession, async (req, res) => {
  const body = req.body || {};
  const itemDefId = String(body.itemDefId || '');
  const token = remoteToken(req);

  if (token) {
    try {
      const result = await gwas.inventory.grant(req.session.appId, token, {
        itemDefId,
        quantity: body.quantity,
        items: Array.isArray(body.items) ? body.items : undefined,
      });
      return res.json(result);
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }

  const requested = Array.isArray(body.items) && body.items.length
    ? body.items
    : [{ itemDefId, quantity: body.quantity }];

  const granted = [];
  for (const entry of requested) {
    const id = String((entry && entry.itemDefId) || '');
    if (!inventory.isClientGrantable(req.session.appId, id)) {
      return res.status(403).json({ error: 'not_client_grantable', itemDefId: id });
    }
    const result = inventory.grant(req.session.appId, req.session.steamId, id, entry.quantity);
    if (result.error) return res.status(400).json(result);
    granted.push(result);
  }
  res.json({ ok: true, granted });
});

router.post('/Inventory/Consume', requireSession, async (req, res) => {
  const body = req.body || {};
  const token = remoteToken(req);

  if (token) {
    try {
      const result = await gwas.inventory.consume(req.session.appId, token, body.itemDefId, body.quantity);
      return res.json(result);
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }

  const result = inventory.consume(req.session.appId, req.session.steamId, body.itemDefId, body.quantity);
  if (result.error) return res.status(400).json(result);
  res.json(result);
});

router.post('/Inventory/Exchange', requireSession, async (req, res) => {
  const body = req.body || {};
  const token = remoteToken(req);

  if (token) {
    try {
      const result = await gwas.inventory.exchange(req.session.appId, token, { generate: body.generate || [], destroy: body.destroy || [] });
      return res.json(result);
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }

  const result = inventory.exchange(req.session.appId, req.session.steamId, body.destroy || [], body.generate || []);
  if (result.error) return res.status(409).json(result);
  res.json(result);
});

router.post('/Inventory/Transfer', requireServerSession, async (req, res) => {
  const body = req.body || {};
  const steamId = String(body.steamId || body.toSteamId || '');
  if (!steamId) return res.status(400).json({ error: 'missing_steam_id' });

  const linked = linkedFor(steamId);
  if (gwas.enabled() && linked) {
    try {
      const result = await gwas.inventory.trade(req.session.appId, linked.token, {
        partner: linked.username,
        give: [{ itemDefId: body.itemDefId, quantity: body.quantity }],
        get: [],
      });
      return res.json(result);
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }
  if (gwas.enabled()) return res.status(409).json({ error: 'player_not_connected' });

  const result = inventory.transfer(req.session.appId, req.session.steamId, steamId, body.itemDefId, body.quantity);
  if (result.error) return res.status(400).json(result);
  res.json(result);
});

router.post('/Inventory/Trade', requireSession, async (req, res) => {
  const body = req.body || {};
  const token = remoteToken(req);
  let partner = String(body.partner || body.partnerSteamId || '');
  if (!partner) return res.status(400).json({ error: 'missing_partner' });

  // The shim only knows the partner's SteamID; the account service keys on usernames.
  if (/^\d+$/.test(partner)) {
    const linkedPartner = linkedFor(partner);
    if (linkedPartner) partner = linkedPartner.username;
  }

  if (token) {
    try {
      const result = await gwas.inventory.trade(req.session.appId, token, {
        partner,
        give: body.give || [],
        get: body.get || [],
      });
      return res.json(result);
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }

  const result = inventory.trade(req.session.appId, req.session.steamId, partner, body.give || [], body.get || []);
  if (result.error) return res.status(409).json(result);
  res.json(result);
});

router.post('/Inventory/Properties', requireSession, async (req, res) => {
  const body = req.body || {};
  const token = remoteToken(req);
  const itemDefId = String(body.itemDefId || '');

  if (token) {
    try {
      const result = await gwas.inventory.properties(req.session.appId, token, { itemDefId, properties: body.properties || {} });
      return res.json(result);
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }

  const result = inventory.setProperties(req.session.appId, req.session.steamId, itemDefId, body.properties || {});
  if (result.error) return res.status(404).json(result);
  res.json(result);
});

router.post('/Inventory/Inspect', requireSession, async (req, res) => {
  const body = req.body || {};
  const token = remoteToken(req);

  if (token) {
    try {
      const result = await gwas.inventory.inspect(req.session.appId, token, body.token);
      return res.json(result);
    } catch (err) {
      return res.status(err.status || 502).json({ error: err.code || 'gwas_error' });
    }
  }

  const parsed = decodeItemToken(body.token);
  if (!parsed || !parsed.itemDefId) return res.status(400).json({ error: 'invalid_item_token' });
  const def = inventory.definitions(req.session.appId).find((d) => d.itemDefId === String(parsed.itemDefId));
  if (!def) return res.status(404).json({ error: 'unknown_item_def' });
  res.json({ ok: true, item: { itemDefId: def.itemDefId, appId: req.session.appId, definition: def, token: encodeItemToken(req.session.appId, def.itemDefId) } });
});


const SHOT_LIMIT = 200;
const SHOT_MAX_BYTES = 4 * 1024 * 1024;

function shotDir(appId, steamId) {
  return path.join(CACHE_DIR, 'screenshots', String(appId), String(steamId));
}

function loadShots(appId, steamId) {
  try {
    return JSON.parse(fs.readFileSync(path.join(shotDir(appId, steamId), 'index.json'), 'utf8'));
  } catch {
    return [];
  }
}

function saveShots(appId, steamId, list) {
  const dir = shotDir(appId, steamId);
  fs.mkdirSync(dir, { recursive: true });
  const tmp = path.join(dir, 'index.json.tmp');
  fs.writeFileSync(tmp, JSON.stringify(list));
  fs.renameSync(tmp, path.join(dir, 'index.json'));
}

router.get('/Screenshots/List', requireSession, (req, res) => {
  res.json({ screenshots: loadShots(req.session.appId, req.session.steamId) });
});

router.post('/Screenshots/Add', requireSession, (req, res) => {
  const body = req.body || {};
  if (typeof body.data !== 'string' || !body.data) return res.status(400).json({ error: 'missing_image' });

  const raw = Buffer.from(String(body.data).replace(/^data:[^,]+,/, ''), 'base64');
  if (!raw.length) return res.status(400).json({ error: 'invalid_image' });
  if (raw.length > SHOT_MAX_BYTES) return res.status(413).json({ error: 'screenshot_too_large' });

  const isPng = raw[0] === 0x89 && raw[1] === 0x50 && raw[2] === 0x4e && raw[3] === 0x47;
  const isJpeg = raw[0] === 0xff && raw[1] === 0xd8;
  if (!isPng && !isJpeg) return res.status(400).json({ error: 'unsupported_image_format' });

  const list = loadShots(req.session.appId, req.session.steamId);
  if (list.length >= SHOT_LIMIT) return res.status(413).json({ error: 'screenshot_limit_reached' });

  const id = crypto.randomBytes(8).toString('hex');
  const ext = isPng ? 'png' : 'jpg';
  const dir = shotDir(req.session.appId, req.session.steamId);
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(path.join(dir, `${id}.${ext}`), raw);

  const entry = {
    id,
    width: Math.floor(Number(body.width)) || 0,
    height: Math.floor(Number(body.height)) || 0,
    caption: String(body.caption || '').slice(0, 256),
    location: String(body.location || '').slice(0, 256),
    file: `${id}.${ext}`,
    createdAt: Date.now(),
  };
  list.push(entry);
  saveShots(req.session.appId, req.session.steamId, list);
  res.json({ ok: true, screenshot: entry });
});

router.get('/Screenshots/Image', requireSession, (req, res) => {
  const list = loadShots(req.session.appId, req.session.steamId);
  const shot = list.find((s) => s.id === String(req.query.id));
  if (!shot) return res.status(404).json({ error: 'not_found' });
  const file = path.join(shotDir(req.session.appId, req.session.steamId), path.basename(shot.file));
  try {
    const bytes = fs.readFileSync(file);
    res.json({ mime: shot.file.endsWith('.png') ? 'image/png' : 'image/jpeg', data: bytes.toString('base64') });
  } catch {
    res.status(404).json({ error: 'not_found' });
  }
});

router.delete('/Screenshots/Delete', requireSession, (req, res) => {
  const list = loadShots(req.session.appId, req.session.steamId);
  const id = String(req.query.id || (req.body || {}).id || '');
  const shot = list.find((s) => s.id === id);
  if (!shot) return res.status(404).json({ error: 'not_found' });
  try { fs.unlinkSync(path.join(shotDir(req.session.appId, req.session.steamId), path.basename(shot.file))); } catch {  }
  saveShots(req.session.appId, req.session.steamId, list.filter((s) => s.id !== id));
  res.json({ ok: true });
});


const timeline = new Map();

router.get('/Timeline/Events', requireSession, (req, res) => {
  res.json({ events: timeline.get(`${req.session.appId}|${req.session.steamId}`) || [] });
});

router.post('/Timeline/Event', requireSession, (req, res) => {
  const body = req.body || {};
  const key = `${req.session.appId}|${req.session.steamId}`;
  const events = timeline.get(key) || [];
  const event = {
    id: events.length ? events[events.length - 1].id + 1 : 1,
    type: String(body.type || 'event').slice(0, 64),
    description: String(body.description || '').slice(0, 512),
    icon: String(body.icon || '').slice(0, 128),
    priority: Math.max(0, Math.min(10, Number(body.priority) || 0)),
    time: body.time ? Number(body.time) : Date.now(),
  };
  events.push(event);
  while (events.length > 128) events.shift();
  timeline.set(key, events);
  res.json({ ok: true, event });
});

router.delete('/Timeline/Events', requireSession, (req, res) => {
  timeline.delete(`${req.session.appId}|${req.session.steamId}`);
  res.json({ ok: true });
});


router.get('/Ping', requireSession, (req, res) => {
  res.json({ ok: true, serverTime: Date.now(), appId: req.session.appId });
});

router.post('/Log', requireSession, (req, res) => {
  const message = String((req.body || {}).message || '').slice(0, 500);
  if (message) log.event(`client[${req.session.appId}/${req.session.steamId}] ${message}`);
  res.json({ ok: true });
});

module.exports = router;
