const express = require('express');
const dns = require('dns').promises;
const net = require('net');
const { requireSession } = require('../authMiddleware');
const { push } = require('../realtime');
const { state } = require('../state');
const { accounts } = require('../accounts');
const media = require('../mediaStore');

const router = express.Router();

const HTML_MAX_BYTES = 1024 * 1024;
const HTML_TIMEOUT_MS = 8000;

function isPrivateAddress(address) {
  const ip = String(address || '').toLowerCase();
  if (!ip) return true;
  if (ip === '::1' || ip === '127.0.0.1' || ip === '0.0.0.0') return true;
  if (net.isIPv4(ip)) {
    if (/^10\./.test(ip) || /^192\.168\./.test(ip) || /^169\.254\./.test(ip)) return true;
    if (/^172\.(1[6-9]|2\d|3[01])\./.test(ip)) return true;
    return false;
  }
  if (/^f[cd][0-9a-f]{2}:/.test(ip) || /^fe80:/.test(ip) || ip === '::') return true;
  return false;
}

async function assertFetchable(url) {
  if (url.protocol !== 'http:' && url.protocol !== 'https:') throw new Error('unsupported_scheme');
  if (url.username || url.password) throw new Error('credentials_in_url');
  const host = url.hostname.replace(/^\[|\]$/g, '');
  if (isPrivateAddress(host)) throw new Error('private_target');
  const addresses = await dns.lookup(host, { all: true });
  if (!addresses.length) throw new Error('dns_failed');
  for (const entry of addresses) {
    if (isPrivateAddress(entry.address)) throw new Error('private_target');
  }
}

async function fetchPage(rawUrl) {
  let url;
  try { url = new URL(rawUrl); } catch { return { error: 'invalid_url' }; }

  for (let hop = 0; hop < 4; hop++) {
    try {
      await assertFetchable(url);
    } catch (err) {
      return { error: err.message };
    }

    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), HTML_TIMEOUT_MS);
    try {
      const response = await fetch(url, { redirect: 'manual', signal: controller.signal });
      if (response.status >= 300 && response.status < 400) {
        const location = response.headers.get('location');
        if (!location) return { error: 'redirect_without_location' };
        url = new URL(location, url);
        continue;
      }

      const buffer = Buffer.from(await response.arrayBuffer());
      if (buffer.length > HTML_MAX_BYTES) return { error: 'page_too_large' };

      const contentType = response.headers.get('content-type') || 'text/html';
      const text = contentType.includes('charset=')
        ? buffer.toString('utf8')
        : buffer.toString('utf8');

      return {
        ok: response.ok,
        status: response.status,
        url: url.toString(),
        contentType,
        title: extractTitle(text),
        text: htmlToText(text),
        links: extractLinks(text, url).slice(0, 200),
        html: text.slice(0, HTML_MAX_BYTES),
      };
    } catch (err) {
      if (err.name === 'AbortError') return { error: 'timeout' };
      return { error: err.code || 'fetch_failed', message: err.message };
    } finally {
      clearTimeout(timer);
    }
  }

  return { error: 'too_many_redirects' };
}

function decodeEntities(text) {
  return String(text)
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&#39;/g, "'")
    .replace(/&nbsp;/g, ' ')
    .replace(/&amp;/g, '&');
}

function extractTitle(html) {
  const match = /<title[^>]*>([\s\S]*?)<\/title>/i.exec(html);
  return match ? decodeEntities(match[1]).trim().slice(0, 300) : '';
}

function htmlToText(html) {
  return decodeEntities(
    String(html)
      .replace(/<script[\s\S]*?<\/script>/gi, ' ')
      .replace(/<style[\s\S]*?<\/style>/gi, ' ')
      .replace(/<!--[\s\S]*?-->/g, ' ')
      .replace(/<\/(p|div|li|tr|h[1-6]|section|article)>/gi, '\n')
      .replace(/<br\s*\/?>/gi, '\n')
      .replace(/<[^>]+>/g, ' ')
  )
    .replace(/[ \t\f\v]+/g, ' ')
    .replace(/\n\s*\n\s*\n+/g, '\n\n')
    .trim()
    .slice(0, 200000);
}

function extractLinks(html, baseUrl) {
  const links = [];
  const re = /<a\b[^>]*href\s*=\s*["']([^"']+)["'][^>]*>([\s\S]*?)<\/a>/gi;
  let match;
  while ((match = re.exec(html))) {
    let href = decodeEntities(match[1]).trim();
    if (!href || href.startsWith('#') || href.startsWith('javascript:') || href.startsWith('mailto:')) continue;
    try { href = new URL(href, baseUrl).toString(); } catch { continue; }
    if (!href.startsWith('http://') && !href.startsWith('https://')) continue;
    links.push({ url: href, text: htmlToText(match[2]).slice(0, 200) });
    if (links.length >= 400) break;
  }
  return links;
}

async function friendsOf(steamId) {
  const linked = state.linkedAccounts.get(String(steamId));
  if (!linked) return [];
  try {
    const graph = await accounts.getFriends(linked.username, linked.token);
    return (graph.accepted || []).map((f) => String(f.steamId || accounts.steamIdFor(f.username)));
  } catch {
    return [];
  }
}

async function pushToFriends(steamId, message) {
  const friends = await friendsOf(steamId);
  for (const friendId of friends) push(friendId, message);
}

function broadcastView(record) {
  return {
    broadcasting: !!record,
    viewers: record ? record.viewers.size : 0,
    url: record ? record.url : '',
    title: record ? record.title : '',
    broadcastId: record ? record.id : null,
    steamId: record ? record.steamId : null,
  };
}

router.get('/Music/State', requireSession, async (req, res) => {
  const target = req.query.steamId ? String(req.query.steamId) : req.session.steamId;
  if (target !== String(req.session.steamId)) {
    const friends = await friendsOf(req.session.steamId);
    if (!friends.includes(target)) return res.status(403).json({ error: 'not_a_friend' });
  }
  res.json({ steamId: target, ...media.musicFor(target) });
});

router.post('/Music/State', requireSession, async (req, res) => {
  const body = req.body || {};
  const next = media.setMusic(req.session.steamId, {
    status: body.status,
    track: body.track,
    artist: body.artist,
    volume: typeof body.volume === 'number' ? body.volume : undefined,
  });

  const message = { type: 'MusicStateChanged', steamId: String(req.session.steamId), name: req.session.name, ...next };
  await pushToFriends(req.session.steamId, message);
  res.json({ ok: true, ...next });
});

router.post('/Video/Broadcast/Start', requireSession, (req, res) => {
  const record = media.startBroadcast(req.session.steamId, req.session.appId, req.body || {});
  pushToFriends(req.session.steamId, {
    type: 'BroadcastStarted',
    steamId: String(req.session.steamId),
    name: req.session.name,
    broadcastId: record.id,
    url: record.url,
    title: record.title,
  }).catch(() => {});
  res.json({ ok: true, broadcast: broadcastView(record) });
});

router.post('/Video/Broadcast/Stop', requireSession, async (req, res) => {
  const record = media.stopBroadcast(req.session.steamId);
  if (record) {
    await pushToFriends(req.session.steamId, {
      type: 'BroadcastStopped',
      steamId: String(req.session.steamId),
      broadcastId: record.id,
    });
  }
  res.json({ ok: true, stopped: !!record });
});

router.get('/Video/Status', requireSession, (req, res) => {
  const target = req.query.steamId ? String(req.query.steamId) : req.session.steamId;
  if (target !== String(req.session.steamId)) {
    const record = media.broadcastOf(target);
    if (!record || record.appId !== req.session.appId) return res.json(broadcastView(null));
    return res.json(broadcastView(record));
  }
  res.json(broadcastView(media.broadcastOf(target)));
});

router.get('/Video/URL', requireSession, (req, res) => {
  const appId = req.query.appId ? String(req.query.appId) : req.session.appId;
  const active = media.broadcastsForApp(appId);
  const own = active.find((b) => b.steamId === String(req.session.steamId)) || active[0] || null;
  res.json({ url: own ? own.url : '' });
});

router.post('/Video/Watch', requireSession, (req, res) => {
  const record = media.watch(req.session.steamId, (req.body || {}).steamId);
  if (!record) return res.status(404).json({ error: 'not_broadcasting' });
  push(record.steamId, {
    type: 'BroadcastViewers',
    broadcastId: record.id,
    viewers: record.viewers.size,
  });
  res.json({ ok: true, ...broadcastView(record) });
});

router.post('/Video/Unwatch', requireSession, (req, res) => {
  const record = media.unwatch(req.session.steamId, (req.body || {}).steamId);
  if (!record) return res.json({ ok: true });
  push(record.steamId, {
    type: 'BroadcastViewers',
    broadcastId: record.id,
    viewers: record.viewers.size,
  });
  res.json({ ok: true, ...broadcastView(record) });
});

function sessionView(record, forSteamId) {
  return {
    id: record.id,
    hostSteamId: record.hostSteamId,
    hostName: record.hostName,
    title: record.title,
    maxGuests: record.maxGuests,
    guestCount: record.guests.size,
    isHost: record.hostSteamId === String(forSteamId),
    isGuest: record.guests.has(String(forSteamId)),
    resolutionX: record.resolutionX,
    resolutionY: record.resolutionY,
  };
}

router.post('/RemotePlay/Start', requireSession, (req, res) => {
  const record = media.startRemoteSession(req.session.steamId, req.session.appId, {
    ...(req.body || {}),
    name: req.session.name,
  });
  res.json({ ok: true, session: sessionView(record, req.session.steamId) });
});

router.get('/RemotePlay/Sessions', requireSession, (req, res) => {
  const sessions = media.remoteSessionsForApp(req.session.appId).map((record) => sessionView(record, req.session.steamId));
  res.json({ sessions });
});

router.get('/RemotePlay/Session', requireSession, (req, res) => {
  const record = media.remoteSession(req.query.id);
  if (!record) return res.status(404).json({ error: 'not_found' });
  res.json({ session: sessionView(record, req.session.steamId) });
});

router.post('/RemotePlay/Join', requireSession, (req, res) => {
  const result = media.joinRemoteSession((req.body || {}).sessionId, req.session.steamId);
  if (result.error) return res.status(400).json(result);
  push(result.session.hostSteamId, {
    type: 'RemotePlaySessionStarted',
    session: sessionView(result.session, result.session.hostSteamId),
    guestSteamId: String(req.session.steamId),
  });
  push(req.session.steamId, {
    type: 'RemotePlaySessionStarted',
    session: sessionView(result.session, req.session.steamId),
  });
  res.json({ ok: true, session: sessionView(result.session, req.session.steamId) });
});

router.post('/RemotePlay/Leave', requireSession, (req, res) => {
  const { sessionId } = req.body || {};
  const result = media.leaveRemoteSession(sessionId, req.session.steamId);
  if (result.session) {
    push(result.session.hostSteamId, { type: 'RemotePlaySessionEnded', sessionId: String(sessionId), steamId: String(req.session.steamId) });
  }
  res.json({ ok: true });
});

router.post('/RemotePlay/Stop', requireSession, (req, res) => {
  const { sessionId } = req.body || {};
  const result = media.stopRemoteSession(sessionId, req.session.steamId);
  if (result.error) return res.status(403).json(result);
  for (const guest of result.session ? result.session.guests : []) {
    push(guest, { type: 'RemotePlaySessionEnded', sessionId: String(sessionId), steamId: result.session.hostSteamId });
  }
  res.json({ ok: true });
});

router.post('/RemotePlay/Invite', requireSession, (req, res) => {
  const { steamId, sessionId, message } = req.body || {};
  if (!steamId) return res.status(400).json({ error: 'missing_steamId' });
  push(String(steamId), {
    type: 'RemotePlayInvite',
    from: String(req.session.steamId),
    fromName: req.session.name,
    sessionId: sessionId ? String(sessionId) : null,
    message: typeof message === 'string' ? message.slice(0, 200) : '',
  });
  res.json({ ok: true });
});

router.post('/RemotePlay/Input', requireSession, (req, res) => {
  const { sessionId, events } = req.body || {};
  const list = Array.isArray(events) ? events.slice(0, 64) : [req.body || {}];
  for (const event of list) {
    const result = media.pushRemoteInput(sessionId, req.session.steamId, {
      type: typeof event.type === 'string' ? event.type.slice(0, 24) : 'unknown',
      x: Number(event.x) || 0,
      y: Number(event.y) || 0,
      value: typeof event.value === 'number' ? event.value : 0,
      key: typeof event.key === 'string' ? event.key.slice(0, 16) : null,
      at: Date.now(),
    });
    if (result.error) return res.status(400).json(result);
  }
  const record = media.remoteSession(sessionId);
  if (record) push(record.hostSteamId, { type: 'RemotePlayInput', sessionId: String(sessionId), from: String(req.session.steamId) });
  res.json({ ok: true });
});

router.get('/RemotePlay/Input', requireSession, (req, res) => {
  const result = media.drainRemoteInput(req.query.sessionId, req.query.guestSteamId);
  if (result.error) return res.status(404).json(result);
  res.json(result);
});

router.post('/Party/Create', requireSession, (req, res) => {
  const record = media.startParty(req.session.steamId, req.session.appId, { ...(req.body || {}), name: req.session.name });
  res.json({ ok: true, beaconId: record.id, party: partyView(record, req.session.steamId) });
});

function partyView(record, steamId) {
  return {
    beaconId: record.id,
    ownerSteamId: record.ownerSteamId,
    ownerName: record.ownerName,
    metadata: record.metadata,
    maxMembers: record.maxMembers,
    members: [...record.members],
    isOwner: record.ownerSteamId === String(steamId),
  };
}

router.get('/Party/Beacons', requireSession, (req, res) => {
  res.json({ beacons: media.partiesForApp(req.session.appId).map((p) => partyView(p, req.session.steamId)) });
});

router.get('/Party/Beacon', requireSession, (req, res) => {
  const record = media.party(req.query.id);
  if (!record) return res.status(404).json({ error: 'not_found' });
  res.json({ party: partyView(record, req.session.steamId) });
});

router.post('/Party/Join', requireSession, (req, res) => {
  const result = media.joinParty((req.body || {}).beaconId, req.session.steamId);
  if (result.error) return res.status(400).json(result);
  for (const member of result.party.members) {
    push(member, {
      type: 'PartyChanged',
      party: partyView(result.party, member),
      joined: String(req.session.steamId),
      name: req.session.name,
    });
  }
  res.json({ ok: true, party: partyView(result.party, req.session.steamId) });
});

router.post('/Party/Leave', requireSession, (req, res) => {
  const { beaconId } = req.body || {};
  const result = media.leaveParty(beaconId, req.session.steamId);
  const record = result.party;
  if (record) {
    for (const member of record.members) {
      push(member, { type: 'PartyChanged', party: partyView(record, member), left: String(req.session.steamId) });
    }
  }
  res.json({ ok: true });
});

router.delete('/Party/Beacon', requireSession, (req, res) => {
  const result = media.destroyParty(req.query.id, req.session.steamId);
  if (result.error) return res.status(403).json(result);
  if (result.party) {
    for (const member of result.party.members) push(member, { type: 'PartyChanged', party: null, destroyed: true });
  }
  res.json({ ok: true });
});

router.post('/Party/Invite', requireSession, (req, res) => {
  const { steamId, beaconId, message } = req.body || {};
  if (!steamId) return res.status(400).json({ error: 'missing_steamId' });
  push(String(steamId), {
    type: 'PartyInvite',
    from: String(req.session.steamId),
    fromName: req.session.name,
    beaconId: beaconId || null,
    message: typeof message === 'string' ? message.slice(0, 200) : '',
  });
  res.json({ ok: true });
});

router.get('/HTML/Fetch', requireSession, async (req, res) => {
  const target = String(req.query.url || '');
  if (!target) return res.status(400).json({ error: 'missing_url' });
  const page = await fetchPage(target);
  if (page.error) return res.status(400).json(page);
  res.json(page);
});

// --- HTTP proxy (SteamHTTP) ------------------------------------------------------

const HTTP_MAX_REQUEST = 4 * 1024 * 1024;
const HTTP_MAX_RESPONSE = 4 * 1024 * 1024;
const HTTP_METHODS = new Set(['GET', 'HEAD', 'POST', 'PUT', 'DELETE', 'OPTIONS', 'PATCH']);
const ALLOW_PRIVATE_HTTP = process.env.WEBWORKS_ALLOW_PRIVATE_HTTP === 'true';

router.post('/HTTP/Request', requireSession, async (req, res) => {
  const body = req.body || {};
  const method = String(body.method || 'GET').toUpperCase();
  if (!HTTP_METHODS.has(method)) return res.status(400).json({ ok: false, error: 'unsupported_method' });

  let url;
  try { url = new URL(String(body.url || '')); } catch { return res.status(400).json({ ok: false, error: 'invalid_url' }); }

  if (!ALLOW_PRIVATE_HTTP) {
    try { await assertFetchable(url); } catch (err) { return res.status(400).json({ ok: false, error: err.message }); }
  }

  const headers = {};
  for (const [key, value] of Object.entries(body.headers || {})) {
    if (!/^[A-Za-z0-9-]{1,64}$/.test(key) || typeof value !== 'string') continue;
    if (/^(host|content-length|connection)$/i.test(key)) continue;
    headers[key] = value.slice(0, 2048);
  }

  let payload;
  if (typeof body.bodyBase64 === 'string' && body.bodyBase64) {
    payload = Buffer.from(body.bodyBase64, 'base64');
    if (payload.length > HTTP_MAX_REQUEST) return res.status(413).json({ ok: false, error: 'request_too_large' });
  }

  const timeout = Math.max(1000, Math.min(60000, Number(body.timeoutMs) || 15000));
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeout);
  try {
    const response = await fetch(url, { method, headers, body: payload, redirect: 'follow', signal: controller.signal });
    const buffer = Buffer.from(await response.arrayBuffer());
    if (buffer.length > HTTP_MAX_RESPONSE) return res.status(413).json({ ok: false, error: 'response_too_large' });

    const out = {};
    response.headers.forEach((value, key) => { out[key] = value; });
    res.json({
      ok: response.ok,
      status: response.status,
      headers: out,
      bodyBase64: buffer.toString('base64'),
      timedOut: false,
    });
  } catch (err) {
    if (err.name === 'AbortError') return res.json({ ok: false, status: 0, headers: {}, bodyBase64: '', timedOut: true });
    res.status(502).json({ ok: false, status: 0, headers: {}, bodyBase64: '', error: err.code || 'fetch_failed' });
  } finally {
    clearTimeout(timer);
  }
});

module.exports = router;
