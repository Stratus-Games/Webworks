const express = require('express');
const { accounts, sendAccountError, AccountError } = require('../accounts');
const { state } = require('../state');
const log = require('../log');

const router = express.Router();

function accountToken(req) {
  return req.headers['x-webworks-account']
    || (req.headers.authorization || '').replace(/^Bearer\s+/i, '')
    || (req.body && (req.body.accountToken || req.body.token))
    || req.query.accountToken
    || null;
}

function requireAccount(req, res, next) {
  const token = accountToken(req);
  Promise.resolve(accounts.verifyToken(token)).then((profile) => {
    if (!profile) return res.status(401).json({ error: 'invalid_token' });
    req.account = profile;
    req.accountToken = token;
    next();
  }).catch((err) => sendAccountError(res, err));
}

router.get('/Mode', (req, res) => {
  res.json({
    mode: accounts.mode,
    label: accounts.label,
    supportsAvatarUpload: true,
    supportsPasswordChange: accounts.mode === 'local',
    supportsRegistration: true,
  });
});

router.post('/Register', async (req, res) => {
  const body = req.body || {};
  try {
    const result = await accounts.register({
      username: body.username,
      password: body.password,
      displayName: body.displayName,
      avatarBase64: body.avatarBase64 || body.avatar || body.iconBase64,
    });
    log.info(`account registered: ${result.account && result.account.username}`);
    res.status(201).json({ ok: true, token: result.token, expiresAt: null, account: result.account });
  } catch (err) {
    sendAccountError(res, err);
  }
});

router.post('/Login', async (req, res) => {
  const body = req.body || {};
  try {
    const result = await accounts.login({
      username: body.username,
      password: body.password,
      ip: req.ip,
    });
    res.json({ ok: true, token: result.token, expiresAt: null, account: result.account });
  } catch (err) {
    sendAccountError(res, err);
  }
});

router.post('/Logout', async (req, res) => {
  try {
    await accounts.logout(accountToken(req));
    res.json({ ok: true });
  } catch (err) {
    sendAccountError(res, err);
  }
});

router.get('/Me', requireAccount, (req, res) => {
  res.json({ account: req.account });
});

router.patch('/Me', requireAccount, async (req, res) => {
  const body = req.body || {};
  try {
    const updated = await accounts.updateProfile(req.account.username, {
      displayName: body.displayName,
      avatarBase64: body.avatarBase64 || body.avatar || body.iconBase64,
      removeAvatar: Boolean(body.removeAvatar),
      token: req.accountToken,
    });
    for (const session of state.sessions.values()) {
      if (session.accountUsername && session.accountUsername.toLowerCase() === req.account.username.toLowerCase()) {
        session.name = updated.displayName;
        state.names.set(session.steamId, updated.displayName);
      }
    }
    res.json({ ok: true, account: updated });
  } catch (err) {
    sendAccountError(res, err);
  }
});

router.delete('/Me', requireAccount, async (req, res) => {
  try {
    await accounts.deleteAccount({ username: req.account.username, password: (req.body || {}).password, token: req.accountToken });
    res.json({ ok: true });
  } catch (err) {
    sendAccountError(res, err);
  }
});

router.post('/Password', requireAccount, async (req, res) => {
  const body = req.body || {};
  try {
    await accounts.changePassword({
      username: req.account.username,
      currentPassword: body.currentPassword,
      newPassword: body.newPassword,
      token: req.accountToken,
    });
    res.json({ ok: true });
  } catch (err) {
    sendAccountError(res, err);
  }
});

router.get('/Friends', requireAccount, async (req, res) => {
  try {
    const graph = await accounts.getFriends(req.account.username, req.accountToken);
    const { decorateFriends } = require('../presence');
    res.json(await decorateFriends(graph));
  } catch (err) {
    sendAccountError(res, err);
  }
});

router.post('/Friends/Add', requireAccount, async (req, res) => {
  try {
    const target = String((req.body || {}).username || '');
    if (!target) throw new AccountError('missing_username');
    res.json(await accounts.addFriend(req.account.username, target, req.accountToken));
  } catch (err) { sendAccountError(res, err); }
});

router.post('/Friends/Accept', requireAccount, async (req, res) => {
  try {
    const target = String((req.body || {}).username || '');
    if (!target) throw new AccountError('missing_username');
    res.json(await accounts.acceptFriend(req.account.username, target, req.accountToken));
  } catch (err) { sendAccountError(res, err); }
});

router.post('/Friends/Remove', requireAccount, async (req, res) => {
  try {
    const target = String((req.body || {}).username || '');
    if (!target) throw new AccountError('missing_username');
    res.json(await accounts.removeFriend(req.account.username, target, req.accountToken));
  } catch (err) { sendAccountError(res, err); }
});

router.post('/Friends/Block', requireAccount, async (req, res) => {
  try {
    const target = String((req.body || {}).username || '');
    if (!target) throw new AccountError('missing_username');
    res.json(await accounts.blockFriend(req.account.username, target, req.accountToken));
  } catch (err) { sendAccountError(res, err); }
});

router.post('/Friends/Unblock', requireAccount, async (req, res) => {
  try {
    const target = String((req.body || {}).username || '');
    if (!target) throw new AccountError('missing_username');
    res.json(await accounts.unblockFriend(req.account.username, target, req.accountToken));
  } catch (err) { sendAccountError(res, err); }
});

router.get('/Search', requireAccount, async (req, res) => {
  try {
    res.json({ accounts: await accounts.search(String(req.query.q || ''), req.accountToken) });
  } catch (err) { sendAccountError(res, err); }
});

router.get('/Avatar', async (req, res) => {
  const username = String(req.query.username || '');
  if (!username) return res.status(400).json({ error: 'missing_username' });

  const custom = await accounts.avatarFor(username);
  if (custom) {
    const avatars = require('../avatars');
    let decoded = null;
    try { decoded = avatars.toRgba(Buffer.from(custom.data, 'base64')); } catch { decoded = null; }
    return res.json({
      width: decoded ? decoded.width : custom.width,
      height: decoded ? decoded.height : custom.height,
      mime: custom.mime,
      data: custom.data,
      rgba: decoded ? decoded.rgba.toString('base64') : null,
      custom: true,
    });
  }

  const identicon = require('../avatars').identicon(username, sizeToPixels(req.query.size));
  res.json({ width: identicon.width, height: identicon.height, mime: 'image/png', data: identicon.png, rgba: identicon.rgba, custom: false });
});

function sizeToPixels(size) {
  return size === 'small' ? 32 : size === 'large' ? 128 : 64;
}

module.exports = router;
