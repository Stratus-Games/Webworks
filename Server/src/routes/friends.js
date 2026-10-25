const express = require('express');
const { state } = require('../state');
const { requireSession } = require('../authMiddleware');
const { accounts } = require('../accounts');
const {
  resolveFriendsFor,
  decorateFriends,
  isOnline,
  onlineCount,
  notifyFriendGraphChanged,
} = require('../presence');
const { pushToPeer } = require('../realtime');
const { identicon } = require('../avatars');

const router = express.Router();

function myAccount(req) {
  return state.linkedAccounts.get(String(req.session.steamId)) || null;
}

// Friends carry the lobby they are sitting in so the client can fill FriendGameInfo_t
// without a second round trip -- that is how a game sees "my friend is in a lobby I
// can join" as soon as it refreshes its friend list.
function lobbyIdFor(appId, steamId) {
  for (const lobby of state.lobbies.values()) {
    if (lobby.appId === appId && lobby.members.has(String(steamId))) return lobby.id;
  }
  return null;
}

router.get('/List', requireSession, async (req, res) => {
  const friends = (await resolveFriendsFor(req.session.steamId)).map((f) => ({
    ...f,
    lobbyId: lobbyIdFor(req.session.appId, f.steamId),
  }));
  res.json({ friends, onlineCount: friends.filter((f) => f.personaState === 'Online').length });
});

router.get('/Graph', requireSession, async (req, res) => {
  const linked = myAccount(req);
  if (!linked) return res.json({ accepted: [], incoming: [], outgoing: [], blocked: [], onlineCount: 0 });
  try {
    const graph = await accounts.getFriends(linked.username, linked.token);
    res.json(await decorateFriends(graph));
  } catch {
    res.json({ accepted: [], incoming: [], outgoing: [], blocked: [], onlineCount: 0 });
  }
});

router.post('/Add', requireSession, async (req, res) => {
  const linked = myAccount(req);
  const target = String((req.body || {}).username || '');
  if (!linked) return res.status(409).json({ error: 'not_signed_in' });
  if (!target) return res.status(400).json({ error: 'missing_username' });
  try {
    const result = await accounts.addFriend(linked.username, target, linked.token);
    notifyFriendGraphChanged(linked.username);
    notifyFriendGraphChanged(target);
    res.json(result);
  } catch (err) {
    res.status(err.status || 400).json({ error: err.code || 'friend_request_failed' });
  }
});

router.post('/Accept', requireSession, async (req, res) => {
  const linked = myAccount(req);
  const target = String((req.body || {}).username || '');
  if (!linked) return res.status(409).json({ error: 'not_signed_in' });
  try {
    const result = await accounts.acceptFriend(linked.username, target, linked.token);
    notifyFriendGraphChanged(linked.username);
    notifyFriendGraphChanged(target);
    res.json(result);
  } catch (err) {
    res.status(err.status || 400).json({ error: err.code || 'friend_accept_failed' });
  }
});

router.post('/Remove', requireSession, async (req, res) => {
  const linked = myAccount(req);
  const target = String((req.body || {}).username || '');
  if (!linked) return res.status(409).json({ error: 'not_signed_in' });
  try {
    const result = await accounts.removeFriend(linked.username, target, linked.token);
    notifyFriendGraphChanged(linked.username);
    notifyFriendGraphChanged(target);
    res.json(result);
  } catch (err) {
    res.status(err.status || 400).json({ error: err.code || 'friend_remove_failed' });
  }
});

router.post('/Block', requireSession, async (req, res) => {
  const linked = myAccount(req);
  const target = String((req.body || {}).username || '');
  if (!linked) return res.status(409).json({ error: 'not_signed_in' });
  try {
    const result = await accounts.blockFriend(linked.username, target, linked.token);
    notifyFriendGraphChanged(linked.username);
    notifyFriendGraphChanged(target);
    res.json(result);
  } catch (err) {
    res.status(err.status || 400).json({ error: err.code || 'friend_block_failed' });
  }
});

router.get('/Search', requireSession, async (req, res) => {
  const linked = myAccount(req);
  if (!linked) return res.json({ accounts: [] });
  try {
    res.json({ accounts: await accounts.search(String(req.query.q || ''), linked.token) });
  } catch {
    res.json({ accounts: [] });
  }
});

router.get('/Online', requireSession, (req, res) => {
  const players = [];
  for (const steamId of state.sockets.keys()) {
    players.push({
      steamId,
      name: state.names.get(steamId) || '',
      appId: state.appOf.get(steamId) || null,
    });
  }
  res.json({ count: players.length, players });
});

router.get('/PlayedWith', requireSession, (req, res) => {
  res.json({ friends: [] });
});


router.get('/Avatar', requireSession, async (req, res) => {
  const size = req.query.size === 'small' ? 32 : req.query.size === 'large' ? 128 : 64;
  const linkedTarget = req.query.steamid ? state.linkedAccounts.get(String(req.query.steamid)) : null;
  const username = String(req.query.username || (linkedTarget && linkedTarget.username) || '');

  if (username) {
    try {
      const custom = await accounts.avatarFor(username);
      if (custom) {
        let decoded = null;
        try { decoded = require('../avatars').toRgba(Buffer.from(custom.data, 'base64')); } catch { decoded = null; }
        return res.json({
          width: decoded ? decoded.width : custom.width,
          height: decoded ? decoded.height : custom.height,
          mime: custom.mime,
          data: custom.data,
          rgba: decoded ? decoded.rgba.toString('base64') : null,
          custom: true,
        });
      }
    } catch {  }
  }

  const seed = username || String(req.query.steamid || req.session.steamId);
  const img = identicon(seed, size);
  res.json({
    width: img.width,
    height: img.height,
    mime: 'image/png',
    data: img.png,
    rgba: img.rgba.toString('base64'),
    custom: false,
  });
});


router.get('/RichPresence', requireSession, (req, res) => {
  res.json({ value: (req.session.richPresence || {})[req.query.key] || '' });
});

router.post('/RichPresence', requireSession, (req, res) => {
  req.session.richPresence = req.session.richPresence || {};
  req.session.richPresence[req.body.key] = req.body.value;
  res.json({ ok: true });
});

router.delete('/RichPresence', requireSession, (req, res) => {
  req.session.richPresence = {};
  res.json({ ok: true });
});

router.get('/RichPresence/Other', requireSession, async (req, res) => {
  const targetId = String(req.query.steamid || '');
  const key = String(req.query.key || '');
  const linked = myAccount(req);
  if (!linked || !targetId || !key) return res.json({ value: '' });

  const target = state.linkedAccounts.get(targetId);
  if (!target) return res.json({ value: '' });

  const graph = await accounts.getFriends(linked.username, linked.token);
  if (!(graph.accepted || []).some((f) => String(f.username).toLowerCase() === String(target.username).toLowerCase())) {
    return res.json({ value: '' });
  }

  for (const session of state.sessions.values()) {
    if (String(session.steamId) === targetId) {
      return res.json({ value: (session.richPresence || {})[key] || '' });
    }
  }
  res.json({ value: '' });
});

router.post('/RequestUserInfo', requireSession, (req, res) => {
  res.json({ ok: true, note: 'always_fresh_no_op' });
});

router.post('/Message', requireSession, (req, res) => {
  const { to, message } = req.body || {};
  if (!to || typeof message !== 'string') return res.status(400).json({ error: 'to_and_message_required' });
  const delivered = pushToPeer(req.session, to, {
    type: 'FriendMessage',
    from: req.session.steamId,
    fromName: req.session.name,
    message: message.slice(0, 2000),
  });
  res.json({ ok: true, delivered, online: isOnline(to) });
});

router.post('/GameInvite', requireSession, (req, res) => {
  const { to, connect } = req.body || {};
  if (!to || typeof connect !== 'string') return res.status(400).json({ error: 'to_and_connect_required' });
  const delivered = pushToPeer(req.session, to, {
    type: 'GameInvite',
    from: req.session.steamId,
    fromName: req.session.name,
    connect: connect.slice(0, 256),
  });
  res.json({ ok: true, delivered });
});

// --- follows ------------------------------------------------------------------
// Following is an account-level relation (GWAS stores it), so it survives devices and
// powers GetFollowerCount / IsFollowing / EnumerateFollowingList.

router.post('/Follow', requireSession, async (req, res) => {
  const linked = myAccount(req);
  const target = String((req.body || {}).username || '');
  if (!linked) return res.status(409).json({ error: 'not_signed_in' });
  if (!target) return res.status(400).json({ error: 'missing_username' });
  try {
    res.json(await accounts.follow(linked.username, target, linked.token));
  } catch (err) {
    res.status(err.status || 400).json({ error: err.code || 'follow_failed' });
  }
});

router.post('/Unfollow', requireSession, async (req, res) => {
  const linked = myAccount(req);
  const target = String((req.body || {}).username || '');
  if (!linked) return res.status(409).json({ error: 'not_signed_in' });
  if (!target) return res.status(400).json({ error: 'missing_username' });
  try {
    res.json(await accounts.unfollow(linked.username, target, linked.token));
  } catch (err) {
    res.status(err.status || 400).json({ error: err.code || 'unfollow_failed' });
  }
});

async function followList(req, res, kind) {
  const linked = myAccount(req);
  if (!linked) return res.json({ accounts: [] });

  // steamId is what the Steam shim has; the account service keys on usernames, so
  // resolve through the live session directory.
  const steamId = String(req.query.steamId || '');
  const linkedTarget = steamId ? state.linkedAccounts.get(steamId) : null;
  const username = String(req.query.username || (linkedTarget && linkedTarget.username) || linked.username);

  try {
    const list = kind === 'followers'
      ? await accounts.followers(username, linked.token)
      : await accounts.following(linked.username, linked.token);
    res.json({
      username,
      accounts: list.map((account) => ({ ...account, steamId: accounts.steamIdFor(account.username) })),
    });
  } catch {
    res.json({ username, accounts: [] });
  }
}

router.get('/Followers', requireSession, (req, res) => followList(req, res, 'followers'));
router.get('/Following', requireSession, (req, res) => followList(req, res, 'following'));

router.get('/IsFollowing', requireSession, async (req, res) => {
  const linked = myAccount(req);
  const steamId = String(req.query.steamId || '');
  const linkedTarget = steamId ? state.linkedAccounts.get(steamId) : null;
  const target = String(req.query.username || (linkedTarget && linkedTarget.username) || '');
  if (!linked || !target) return res.json({ following: false });
  try {
    const list = await accounts.following(linked.username, linked.token);
    res.json({ following: list.some((a) => String(a.username).toLowerCase() === target.toLowerCase()) });
  } catch {
    res.json({ following: false });
  }
});

module.exports = router;
