const express = require('express');
const { state } = require('../state');
const { requireSession } = require('../authMiddleware');
const { push, pushToLobby } = require('../realtime');
const { generateLobbyId } = require('../identity');
const { snapshot, removeMember, createInvite, resolveInvite } = require('../lobbies');
const { resolveFriendsFor } = require('../presence');
const federation = require('../federation');
const chat = require('../chat');
const inviteInbox = require('../inviteInbox');

const router = express.Router();

// Cross-server lobbies (GWAS networking): a lobby that is not in this process may still
// be live on another node server. The request is forwarded over the peer link and the
// hosting server answers it, so the client keeps talking to its own server only.
router.use((req, res, next) => {
  // Already federated requests are executed here; forwarding one again would bounce it
  // between servers and cost directory calls.
  if (req.headers['x-webworks-federated']) return next();
  const lobbyId = (req.body && req.body.lobbyId) || (req.query && req.query.lobbyId);
  if (!lobbyId || !federation.canForward()) return next();

  requireSession(req, res, () => {
    const local = state.lobbies.get(String(lobbyId));
    if (local && local.appId === req.session.appId) return next();

    federation.forward(req, { path: req.path, method: req.method, lobbyId: String(lobbyId) })
      .then(({ status, body }) => {
        res.status(status);
        if (typeof body === 'string') res.type('text/plain').send(body);
        else if (body === undefined || body === null) res.end();
        else res.json(body);
      })
      .catch((err) => {
        const code = (err && err.code) || 'federation_failed';
        if (code === 'directory_unreachable' || code === 'federation_unreachable' || code === 'federation_timeout' || code === 'federation_linked_closed') {
          return res.status(503).json({ error: 'lobby_unreachable', detail: code });
        }
        // Not in the directory, or the lobby has gone since it was published.
        res.status(404).json({ error: 'lobby_not_found', detail: code });
      });
  });
});

function findLobby(id, req) {
  const lobby = state.lobbies.get(String(id));
  return lobby && lobby.appId === req.session.appId ? lobby : undefined;
}

router.post('/CreateLobby', requireSession, (req, res) => {
  const me = req.session.steamId;
  const id = generateLobbyId();
  const lobby = {
    id,
    appId: req.session.appId,
    createdAt: Date.now(),
    lastSeen: Date.now(),
    ownerId: me,
    maxMembers: Number(req.body.maxMembers) || 100,
    members: new Set([me]),
    data: {},
    memberData: { [me]: {} },
    visibility: 'public',
    joinable: true,
  };
  state.lobbies.set(id, lobby);
  // Publish it to the GWAS directory right away so players on other node servers can
  // find and join it without waiting for the next heartbeat.
  federation.lobbiesChanged();
  res.json(snapshot(lobby));
});

router.post('/JoinLobby', requireSession, async (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  const me = req.session.steamId;

  if (!lobby.members.has(me)) {
    // A private or invite-only lobby is still joinable by the owner's friends: that is
    // how friends-only multiplayer works without a dedicated invite round trip.
    if (!lobby.joinable) {
      const friendIds = await friendIdsOf(me);
      if (!friendIds.has(String(lobby.ownerId))) return res.status(403).json({ error: 'lobby_not_joinable' });
    }
    if (lobby.members.size >= lobby.maxMembers) return res.status(409).json({ error: 'lobby_full' });
    lobby.members.add(me);
    lobby.memberData[me] = {};
  }

  inviteInbox.dismiss(me, { lobbyId: lobby.id });
  pushToLobby(lobby, { type: 'LobbyMemberJoined', lobbyId: lobby.id, steamId: me, name: req.session.name });
  res.json(snapshot(lobby));
});

router.post('/LeaveLobby', requireSession, (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  removeMember(lobby, req.session.steamId, 'LobbyMemberLeave');
  res.json({ ok: true });
});

router.get('/Lobby', requireSession, (req, res) => {
  const lobby = findLobby(req.query.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  res.json(snapshot(lobby));
});

router.get('/LobbyMembers', requireSession, (req, res) => {
  const lobby = findLobby(req.query.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  res.json({ members: snapshot(lobby).members });
});

router.get('/LobbyData', requireSession, (req, res) => {
  const lobby = findLobby(req.query.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  res.json({ data: lobby.data });
});

router.post('/LobbyData', requireSession, (req, res) => {
  const { lobbyId, key, value } = req.body;
  const lobby = findLobby(lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  lobby.data[key] = value;
  pushToLobby(lobby, { type: 'LobbyDataChanged', lobbyId: lobby.id, key, value });
  res.json({ ok: true });
});

router.delete('/LobbyData', requireSession, (req, res) => {
  const { lobbyId, key } = req.body;
  const lobby = findLobby(lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  delete lobby.data[key];
  pushToLobby(lobby, { type: 'LobbyDataChanged', lobbyId: lobby.id, key, value: null });
  res.json({ ok: true });
});

router.get('/MemberData', requireSession, (req, res) => {
  const lobby = findLobby(req.query.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  res.json({ data: lobby.memberData[req.query.steamId] || {} });
});

router.post('/MemberData', requireSession, (req, res) => {
  const { lobbyId, key, value } = req.body;
  const lobby = findLobby(lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  const me = req.session.steamId;
  lobby.memberData[me] = lobby.memberData[me] || {};
  lobby.memberData[me][key] = value;
  pushToLobby(lobby, { type: 'LobbyMemberDataChanged', lobbyId: lobby.id, steamId: me, key, value });
  res.json({ ok: true });
});

router.post('/ChatSend', requireSession, (req, res) => {
  const { lobbyId, data, message } = req.body || {};
  const result = chat.send(req.session, lobbyId, data, message);
  if (result.error) return res.status(result.status).json({ error: result.error });
  res.json({ ok: true, chatId: result.chatId });
});

router.post('/LobbyVisibility', requireSession, (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  lobby.visibility = req.body.mode;
  res.json({ ok: true });
});

router.post('/LobbyJoinable', requireSession, (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  lobby.joinable = !!req.body.joinable;
  res.json({ ok: true });
});

router.post('/LobbyMaxMembers', requireSession, (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  lobby.maxMembers = Number(req.body.maxMembers) || lobby.maxMembers;
  res.json({ ok: true });
});

router.post('/LobbyOwner', requireSession, (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  if (lobby.ownerId !== req.session.steamId) return res.status(403).json({ error: 'not_owner' });
  if (!lobby.members.has(String(req.body.ownerId))) return res.status(400).json({ error: 'not_a_member' });
  lobby.ownerId = String(req.body.ownerId);
  pushToLobby(lobby, { type: 'LobbyDataChanged', lobbyId: lobby.id, ownerId: lobby.ownerId });
  res.json({ ok: true });
});

router.post('/LobbyLinked', requireSession, (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  if (lobby.ownerId !== req.session.steamId) return res.status(403).json({ error: 'not_owner' });
  lobby.linkedLobbyId = String(req.body.linkedLobbyId || '0');
  pushToLobby(lobby, { type: 'LobbyDataChanged', lobbyId: lobby.id, linkedLobbyId: lobby.linkedLobbyId });
  res.json({ ok: true });
});

router.post('/LobbyGameServer', requireSession, (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  if (lobby.ownerId !== req.session.steamId) return res.status(403).json({ error: 'not_owner' });

  lobby.gameServer = {
    ip: Number(req.body.ip) || 0,
    port: Number(req.body.port) || 0,
    serverId: String(req.body.serverId || '0'),
  };
  pushToLobby(lobby, { type: 'LobbyGameCreated', lobbyId: lobby.id, ...lobby.gameServer });
  res.json({ ok: true });
});

router.post('/CreateInvite', requireSession, (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  if (!lobby.members.has(req.session.steamId)) return res.status(403).json({ error: 'not_a_lobby_member' });

  const ttlMs = req.body.ttlSeconds ? Number(req.body.ttlSeconds) * 1000 : undefined;
  const { token, expiresAt } = createInvite(lobby, req.session.steamId, ttlMs);

  const base = process.env.WEBWORKS_INVITE_BASE_URL;
  const url = base ? `${base.replace(/\/+$/, '')}?webworksInvite=${token}` : null;

  res.json({ token, url, expiresAt });
});

router.get('/ResolveInvite', requireSession, (req, res) => {
  const invite = resolveInvite(String(req.query.token || ''));
  if (!invite) return res.status(410).json({ error: 'invite_expired_or_invalid' });
  if (invite.appId !== req.session.appId) return res.status(404).json({ error: 'invite_not_found' });
  res.json({ lobbyId: invite.lobbyId, expiresAt: invite.expiresAt });
});

router.post('/InviteFriend', requireSession, (req, res) => {
  const lobby = findLobby(req.body.lobbyId, req);
  if (!lobby) return res.status(404).json({ error: 'lobby_not_found' });
  if (!lobby.members.has(req.session.steamId)) return res.status(403).json({ error: 'not_a_lobby_member' });

  const friendSteamId = String(req.body.friendSteamId || '');
  if (!friendSteamId) return res.status(400).json({ error: 'missing_friend_steam_id' });
  if (friendSteamId === String(req.session.steamId)) return res.status(400).json({ error: 'cannot_invite_yourself' });

  const { token, expiresAt } = createInvite(lobby, req.session.steamId);
  // The inbox is what the bootstrap friends page polls; the push is what a client with an
  // open realtime socket sees instantly. Both carry the same one-time token.
  inviteInbox.add(friendSteamId, {
    lobbyId: lobby.id,
    token,
    fromSteamId: req.session.steamId,
    fromName: req.session.name,
    appId: lobby.appId,
    expiresAt,
  });
  push(friendSteamId, {
    type: 'LobbyInvite',
    lobbyId: lobby.id,
    inviteToken: token,
    expiresAt,
    fromSteamId: req.session.steamId,
    fromName: req.session.name,
  });
  res.json({ ok: true, token, expiresAt });
});

// Everything waiting on this player, newest first. Safe to poll: entries die with their
// invite token and disappear the moment the lobby does.
router.get('/Invites', requireSession, (req, res) => {
  const invites = inviteInbox.live(req.session.steamId)
    .slice()
    .sort((a, b) => b.createdAt - a.createdAt)
    .map((entry) => {
      const lobby = state.lobbies.get(entry.lobbyId);
      return {
        lobbyId: entry.lobbyId,
        inviteToken: entry.inviteToken,
        fromSteamId: entry.fromSteamId,
        fromName: entry.fromName,
        expiresAt: entry.expiresAt,
        memberCount: lobby ? lobby.members.size : 0,
        ownerName: lobby ? (state.names.get(lobby.ownerId) || '') : '',
      };
    });
  res.json({ invites });
});

router.post('/Invites/Dismiss', requireSession, (req, res) => {
  const { lobbyId, inviteToken } = req.body || {};
  if (!lobbyId && !inviteToken) return res.status(400).json({ error: 'missing_lobby_or_token' });
  const removed = inviteInbox.dismiss(req.session.steamId, { lobbyId, inviteToken });
  res.json({ ok: true, removed });
});


const NUMERIC_OPS = {
  eq: (a, b) => a === b,
  ne: (a, b) => a !== b,
  lt: (a, b) => a < b,
  lte: (a, b) => a <= b,
  gt: (a, b) => a > b,
  gte: (a, b) => a >= b,
};

// The friend list is the lobby browser: a player looking for a game should see the
// lobbies their friends are in, including private ones, which is what the launcher
// "join friends" flow relies on.
async function friendIdsOf(steamId) {
  const friends = await resolveFriendsFor(steamId);
  return new Set(friends.map((f) => String(f.steamId)));
}

function isFriendsLobby(lobby, friendIds) {
  if (!friendIds.size) return false;
  if (friendIds.has(String(lobby.ownerId))) return true;
  for (const member of lobby.members) if (friendIds.has(String(member))) return true;
  return false;
}

router.get('/FriendsActive', requireSession, async (req, res) => {
  const friends = await resolveFriendsFor(req.session.steamId);
  const active = friends
    .filter((f) => f.personaState !== 'Offline' && f.personaState !== 'Invisible')
    .map((f) => {
      const lobby = [...state.lobbies.values()].find((l) => l.appId === req.session.appId && l.members.has(String(f.steamId)));
      return {
        steamId: String(f.steamId),
        username: f.username,
        name: f.name,
        personaState: f.personaState,
        game: f.game || null,
        lobbyId: lobby ? lobby.id : null,
      };
    });

  res.json({ friends: active, count: active.length });
});

router.post('/LobbyList', requireSession, async (req, res) => {
  const { slots = 0, max = 50, filters = {}, numeric = [], near = [], compatible = null } = req.body;
  const num = (l, key) => (l.data[key] === undefined || l.data[key] === '' ? NaN : Number(l.data[key]));

  const friends = await resolveFriendsFor(req.session.steamId);
  const friendSteamIds = new Set(friends.map((f) => String(f.steamId)));

  let matches = [...state.lobbies.values()]
    .filter((l) => l.appId === req.session.appId)
    .filter((l) => l.visibility !== 'invisible')
    .filter((l) => (l.joinable && l.visibility === 'public') || isFriendsLobby(l, friendSteamIds))
    .filter((l) => l.maxMembers - l.members.size >= slots)
    .filter((l) => !compatible || l.members.has(String(compatible)))
    .filter((l) => Object.entries(filters).every(([k, v]) => l.data[k] === v))
    .filter((l) => (Array.isArray(numeric) ? numeric : []).every((f) => {
      const op = NUMERIC_OPS[f && f.op];
      const value = num(l, f && f.key);
      return op && Number.isFinite(value) && op(value, Number(f.value));
    }));

  // Friends' lobbies come first, then the newest public ones.
  const friendRank = (l) => (isFriendsLobby(l, friendSteamIds) ? 1 : 0);
  matches.sort((a, b) => friendRank(b) - friendRank(a) || (b.createdAt || 0) - (a.createdAt || 0));

  if (Array.isArray(near) && near.length) {
    const distance = (l) => near.reduce((sum, f) => {
      const value = num(l, f && f.key);
      return sum + (Number.isFinite(value) ? Math.abs(value - Number(f.value)) : Number.MAX_SAFE_INTEGER / 1e6);
    }, 0);
    matches = matches.map((l) => ({ l, d: distance(l) })).sort((a, b) => a.d - b.d).map((x) => x.l);
  }

  const active = friends
    .filter((f) => f.personaState !== 'Offline' && f.personaState !== 'Invisible')
    .map((f) => {
      const lobby = matches.find((l) => l.members.has(String(f.steamId)));
      return {
        steamId: String(f.steamId),
        username: f.username,
        name: f.name,
        personaState: f.personaState,
        lobbyId: lobby ? lobby.id : null,
      };
    });

  const limit = Math.max(1, Math.min(200, Number(max) || 50));
  const local = matches.slice(0, limit).map((lobby) => ({ ...snapshot(lobby), friend: isFriendsLobby(lobby, friendSteamIds) }));

  // Lobbies other node servers published to the GWAS directory. They are not resolved
  // until joined, so the member list stays on the hosting server.
  const remote = federation
    .remoteLobbies(req.session.appId)
    .filter((l) => l.visibility !== 'invisible')
    .filter((l) => l.joinable && l.visibility === 'public')
    .filter((l) => l.maxMembers - l.members >= slots)
    .filter((l) => Object.entries(filters).every(([k, v]) => l.data && l.data[k] === v))
    .filter((l) => (Array.isArray(numeric) ? numeric : []).every((f) => {
      const op = NUMERIC_OPS[f && f.op];
      const value = l.data && l.data[f && f.key] !== undefined && l.data[f.key] !== '' ? Number(l.data[f.key]) : NaN;
      return op && Number.isFinite(value) && op(value, Number(f.value));
    }))
    .map((l) => ({
      id: l.id,
      ownerId: l.ownerId,
      maxMembers: l.maxMembers,
      memberCount: l.members,
      visibility: l.visibility,
      joinable: l.joinable,
      data: l.data || {},
      members: [],
      memberData: {},
      gameServer: { ip: 0, port: 0, serverId: '0' },
      remote: true,
      serverId: l.serverId,
      friend: false,
    }));

  res.json({ lobbies: local.concat(remote).slice(0, limit), friends: active });
});

module.exports = router;
