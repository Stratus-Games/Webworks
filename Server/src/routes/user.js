const express = require('express');
const crypto = require('crypto');
const { state, normalizeAppId, linkAccountToSession, forgetSteamId } = require('../state');
const { requireSession } = require('../authMiddleware');
const {
  generateGuestSteamId,
  generateGuestName,
  generateSessionToken,
  stableIdentity,
} = require('../identity');
const { accounts } = require('../accounts');
const { isOnline, onlineCount, onlineByGame, broadcastPersonaChange, notePing, noteOffline, PING_TTL_MS } = require('../presence');
const log = require('../log');

const router = express.Router();

const MAX_NAME_LENGTH = 32;
const CONTROL_CHARS = /[\x00-\x1f\x7f]/g;

function sanitizeName(raw) {
  if (typeof raw !== 'string') return null;
  const cleaned = raw.replace(CONTROL_CHARS, '').trim().slice(0, MAX_NAME_LENGTH);
  return cleaned.length > 0 ? cleaned : null;
}

function registerIdentity(token, { steamId, name, appId, account }) {
  const session = {
    steamId,
    name,
    appId,
    accountUsername: account ? account.username : null,
    createdAt: Date.now(),
    sessionToken: token,
  };
  state.sessions.set(token, session);
  state.appOf.set(steamId, appId);
  state.names.set(steamId, name);
  if (account) linkAccountToSession(session, { username: account.username, token: account.token, provider: accounts.mode });

  if (!state.knownApps.has(appId)) {
    state.knownApps.add(appId);
    log.info(`game connected for the first time: app id ${appId}`);
  }
  return session;
}

router.post('/GuestSession', async (req, res) => {
  const body = req.body || {};
  const appId = normalizeAppId(body.appId);
  if (appId === null) return res.status(400).json({ error: 'invalid_app_id' });

  let steamId = null;
  let name = null;
  let account = null;
  let personaState = null;

  if (body.accountToken) {
    try {
      const profile = await accounts.verifyToken(body.accountToken);
      if (profile) {
        steamId = profile.steamId || accounts.steamIdFor(profile.username);
        name = profile.displayName || profile.username;
        account = { username: profile.username, token: body.accountToken };
        personaState = profile.personaState || null;
      }
    } catch (err) {
      log.warn(`account token check failed: ${err.code || err.message}`);
    }
  }

  if (!steamId) {
    const deviceKey = String(body.deviceKey || '').toLowerCase();
    if (/^[0-9a-f]{16,64}$/.test(deviceKey)) {
      ({ steamId, name } = stableIdentity(appId, deviceKey));
    } else {
      steamId = generateGuestSteamId();
      name = generateGuestName();
    }
    const desiredName = sanitizeName(body.name);
    if (desiredName) name = desiredName;
  }

  const token = generateSessionToken();
  registerIdentity(token, { steamId, name, appId, account });
  if (personaState) state.personaStates.set(String(steamId), personaState);

  let hasAvatar = false;
  if (account) {
    try { hasAvatar = Boolean(await accounts.avatarFor(account.username)); } catch { hasAvatar = false; }
  }

  res.json({
    sessionToken: token,
    steamId,
    name,
    appId,
    accountSystem: accounts.mode,
    linkedAccount: account ? account.username : null,
    hasAvatar,
  });
});

router.post('/LinkAccount', requireSession, async (req, res) => {
  const token = (req.body || {}).accountToken;
  if (!token) return res.status(400).json({ error: 'missing_account_token' });

  let profile;
  try {
    profile = await accounts.verifyToken(token);
  } catch (err) {
    return res.status(401).json({ error: 'invalid_account_token' });
  }
  if (!profile) return res.status(401).json({ error: 'invalid_account_token' });

  const steamId = profile.steamId || accounts.steamIdFor(profile.username);
  const name = profile.displayName || profile.username;
  const session = req.session;
  const oldSteamId = String(session.steamId);

  const oldSockets = state.sockets.get(oldSteamId);
  require('../lobbies').dropPlayer(oldSteamId);
  forgetSteamId(oldSteamId);

  session.steamId = steamId;
  session.name = name;
  session.accountUsername = profile.username;
  state.appOf.set(steamId, session.appId);
  state.names.set(steamId, name);
  if (profile.personaState) state.personaStates.set(steamId, profile.personaState);
  linkAccountToSession(session, { username: profile.username, token, provider: accounts.mode });

  if (oldSockets) {
    state.sockets.set(steamId, oldSockets);
    for (const socket of oldSockets) socket.sessionToken = req.sessionToken;
  }

  res.json({ steamId, name, linkedAccount: profile.username });
});

router.get('/Profile', requireSession, async (req, res) => {
  const linked = state.linkedAccounts.get(String(req.session.steamId));
  let hasAvatar = false;
  if (linked) {
    const avatar = await accounts.avatarFor(linked.username);
    hasAvatar = Boolean(avatar);
  }
  const steamId = String(req.session.steamId);
  res.json({
    steamId,
    name: state.personaNames.get(steamId) || req.session.name,
    username: linked ? linked.username : null,
    hasAvatar,
    online: isOnline(steamId),
    isBehindNAT: false,
    steamLevel: 0,
    personaState: state.personaStates.get(steamId) || 'Online',
    accountSystem: accounts.mode,
  });
});

const PERSONA_STATES = ['Offline', 'Online', 'Away', 'Busy', 'Snooze', 'LookingToPlay', 'LookingToTrade', 'Invisible'];

// Persona state is part of the account, so changing it here updates every server the
// account is signed into and every friend that asks for it.
router.post('/PersonaState', requireSession, async (req, res) => {
  const body = req.body || {};
  const personaState = String(body.state || body.personaState || '');
  if (!PERSONA_STATES.includes(personaState)) return res.status(400).json({ error: 'invalid_persona_state' });

  const steamId = String(req.session.steamId);
  state.personaStates.set(steamId, personaState);

  const personaName = sanitizeName(body.name);
  if (personaName) {
    state.personaNames.set(steamId, personaName);
    state.names.set(steamId, personaName);
    req.session.name = personaName;
  }

  const linked = state.linkedAccounts.get(steamId);
  if (linked && accounts.mode === 'gwas') {
    try {
      await accounts.setPersona(linked.username, personaState, personaName, linked.token);
    } catch (err) {
      log.warn(`could not store persona state on GWAS: ${err.code || err.message}`);
    }
  }

  await broadcastPersonaChange(steamId, personaState);
  res.json({
    ok: true,
    personaState,
    personaName: state.personaNames.get(steamId) || req.session.name,
  });
});

router.get('/PersonaState', requireSession, (req, res) => {
  const steamId = String(req.session.steamId);
  res.json({
    personaState: state.personaStates.get(steamId) || 'Online',
    personaName: state.personaNames.get(steamId) || req.session.name,
  });
});

router.get('/Status', requireSession, (req, res) => {
  res.json({
    online: onlineCount(),
    byGame: onlineByGame(),
    games: [...state.knownApps],
    lobbies: state.lobbies.size,
    accountSystem: accounts.mode,
  });
});

router.post('/AuthTicket', requireSession, (req, res) => {
  const expiresAt = Date.now() + 10 * 60 * 1000;
  const payload = `${req.session.steamId}.${expiresAt}`;
  const signature = crypto
    .createHmac('sha256', process.env.WEBWORKS_TICKET_SECRET || 'dev-secret-change-me')
    .update(payload)
    .digest('hex');

  const ticket = Buffer.from(`${payload}.${signature}`).toString('base64');
  res.json({ ticket, expiresAt });
});

router.post('/BeginAuthSession', requireSession, (req, res) => {
  const { ticket } = req.body || {};
  if (!ticket) return res.status(400).json({ error: 'missing_ticket' });

  try {
    const decoded = Buffer.from(ticket, 'base64').toString('utf8');
    const [steamId, expiresAt, signature] = decoded.split('.');
    const expected = crypto
      .createHmac('sha256', process.env.WEBWORKS_TICKET_SECRET || 'dev-secret-change-me')
      .update(`${steamId}.${expiresAt}`)
      .digest('hex');

    if (signature !== expected || Date.now() > Number(expiresAt)) return res.json({ valid: false });
    res.json({ valid: true, steamId });
  } catch {
    res.status(400).json({ error: 'malformed_ticket' });
  }
});

router.post('/EndAuthSession', requireSession, (req, res) => {
  res.json({ ok: true });
});

// Encrypted app ticket: the same HMAC envelope as the auth ticket, with the caller's
// extra data folded in so a backend can confirm it came from this account/app pair.
router.post('/EncryptedAppTicket', requireSession, (req, res) => {
  const data = String((req.body || {}).data || '').slice(0, 2048);
  const expiresAt = Date.now() + 5 * 60 * 1000;
  const payload = `${req.session.steamId}.${req.session.appId}.${expiresAt}.${data}`;
  const signature = crypto
    .createHmac('sha256', process.env.WEBWORKS_TICKET_SECRET || 'dev-secret-change-me')
    .update(payload)
    .digest('hex');
  const ticket = Buffer.from(`${payload}.${signature}`).toString('base64');
  res.json({ ticket, expiresAt });
});

router.get('/DurationControl', requireSession, (req, res) => {
  res.json({ progress: 'full', remainingSeconds: null, notifyMinutes: null });
});

// --- presence heartbeat -------------------------------------------------------
// The bootstrap (and any client without a game socket) calls this every ~20 seconds.
// A fresh heartbeat is presence: friends see Online even while the player sits on the
// name/friends screen or has moved the browser to another tab. Closing the tab either
// sends /Offline or lets the heartbeat window lapse.
function heartbeat(req, res) {
  const cameOnline = notePing(req.session);
  res.json({
    ok: true,
    steamId: String(req.session.steamId),
    appId: req.session.appId,
    online: true,
    cameOnline,
    onlineCount: onlineCount(),
    ttlMs: PING_TTL_MS,
  });
}

router.post('/Heartbeat', requireSession, heartbeat);
router.get('/Heartbeat', requireSession, heartbeat);
router.post('/Ping', requireSession, heartbeat);

router.post('/Offline', requireSession, (req, res) => {
  noteOffline(req.session);
  res.json({ ok: true, onlineCount: onlineCount() });
});

module.exports = router;
