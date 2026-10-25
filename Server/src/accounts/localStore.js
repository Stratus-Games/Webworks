const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const avatars = require('./avatars');
const { globalAccountIdentity } = require('../identity');
const { AccountError } = require('./errors');
const log = require('../log');

const USERNAME_RE = /^[A-Za-z0-9_]{3,20}$/;
const DISPLAY_NAME_MAX = 32;
const PASSWORD_MIN = 6;
const PASSWORD_MAX = 256;

function sanitizeDisplayName(raw) {
  if (typeof raw !== 'string') return null;
  const cleaned = raw.replace(/[\x00-\x1f\x7f]/g, '').trim().slice(0, DISPLAY_NAME_MAX);
  return cleaned.length ? cleaned : null;
}

function hashPassword(password) {
  const salt = crypto.randomBytes(16);
  const hash = crypto.scryptSync(password, salt, 64, { N: 16384, r: 8, p: 1 });
  return `scrypt$16384$${salt.toString('base64')}$${hash.toString('base64')}`;
}

function verifyPassword(password, stored) {
  try {
    const [scheme, cost, saltB64, hashB64] = String(stored).split('$');
    if (scheme !== 'scrypt') return false;
    const salt = Buffer.from(saltB64, 'base64');
    const expected = Buffer.from(hashB64, 'base64');
    const actual = crypto.scryptSync(password, salt, expected.length, { N: Number(cost) || 16384, r: 8, p: 1 });
    return actual.length === expected.length && crypto.timingSafeEqual(actual, expected);
  } catch {
    return false;
  }
}

class LocalAccountStore {
  constructor() {
    this.mode = 'local';
    this.dataDir = null;
    this.file = null;
    this.db = { version: 1, accounts: {}, sessions: {} };
    this.sessionDays = 30;
    this._attempts = new Map();
  }

  init({ dataDir, sessionDays = 30 }) {
    this.dataDir = dataDir;
    this.sessionDays = sessionDays;
    this.file = path.join(dataDir, 'accounts.json');
    fs.mkdirSync(dataDir, { recursive: true });

    if (fs.existsSync(this.file)) {
      try {
        const parsed = JSON.parse(fs.readFileSync(this.file, 'utf8'));
        this.db = {
          version: 1,
          accounts: parsed.accounts && typeof parsed.accounts === 'object' ? parsed.accounts : {},
          sessions: parsed.sessions && typeof parsed.sessions === 'object' ? parsed.sessions : {},
        };
      } catch (err) {
        const backup = `${this.file}.corrupt-${Date.now()}`;
        try { fs.copyFileSync(this.file, backup); } catch {  }
        log.warn(`accounts.json was not valid JSON (${err.message}); moved to ${path.basename(backup)} and starting empty`);
      }
    }

    this._pruneSessions();
    this._save();
    return { file: this.file, accounts: Object.keys(this.db.accounts).length };
  }


  _save() {
    const tmp = `${this.file}.tmp`;
    fs.writeFileSync(tmp, JSON.stringify(this.db, null, 2));
    fs.renameSync(tmp, this.file);
  }

  _pruneSessions(now = Date.now()) {
    let removed = 0;
    for (const [token, s] of Object.entries(this.db.sessions)) {
      if (!s || !s.expiresAt || s.expiresAt <= now) { delete this.db.sessions[token]; removed++; }
    }
    return removed;
  }

  _account(username) {
    if (!username) return null;
    return this.db.accounts[String(username).toLowerCase()] || null;
  }

  _requireAccount(username) {
    const acct = this._account(username);
    if (!acct) throw new AccountError('no_such_account', 404);
    return acct;
  }

  _issueToken(acct) {
    const token = crypto.randomBytes(32).toString('hex');
    const now = Date.now();
    this.db.sessions[token] = {
      username: acct.username,
      createdAt: now,
      expiresAt: now + this.sessionDays * 24 * 60 * 60 * 1000,
    };
    return token;
  }

  _rateLimit(key) {
    const now = Date.now();
    const windowStart = now - 5 * 60 * 1000;
    const entry = this._attempts.get(key) || [];
    const recent = entry.filter((t) => t > windowStart);
    if (recent.length >= 10) throw new AccountError('rate_limited', 429);
    recent.push(now);
    this._attempts.set(key, recent);
  }


  steamIdFor(username) {
    return globalAccountIdentity(username).steamId;
  }

  publicProfile(acct) {
    if (!acct) return null;
    return {
      username: acct.username,
      displayName: acct.displayName || acct.username,
      steamId: this.steamIdFor(acct.username),
      hasAvatar: Boolean(acct.avatar),
      createdAt: acct.createdAt || 0,
    };
  }


  register({ username, password, displayName, avatarBase64 }) {
    if (typeof username !== 'string' || !USERNAME_RE.test(username)) throw new AccountError('invalid_username');
    if (typeof password !== 'string' || password.length < PASSWORD_MIN || password.length > PASSWORD_MAX) {
      throw new AccountError('invalid_password');
    }
    const key = username.toLowerCase();
    if (this.db.accounts[key]) throw new AccountError('username_taken', 409);

    let avatar = null;
    if (avatarBase64) {
      try {
        avatar = avatars.save(this.dataDir, key, avatars.inspect(avatarBase64));
      } catch (err) {
        if (err instanceof avatars.AvatarError) throw new AccountError(err.code);
        throw err;
      }
    }

    const acct = {
      username,
      displayName: sanitizeDisplayName(displayName) || username,
      password: hashPassword(password),
      createdAt: Date.now(),
      avatar,
      friends: [],
      incoming: [],
      outgoing: [],
      blocked: [],
      banned: false,
    };
    this.db.accounts[key] = acct;
    const token = this._issueToken(acct);
    this._save();
    log.info(`local account created: ${acct.username}`);
    return { token, account: this.publicProfile(acct) };
  }

  login({ username, password, ip = '' }) {
    const key = String(username || '').toLowerCase();
    this._rateLimit(`${key}|${ip}`);
    const acct = this._account(key);
    if (!acct || !verifyPassword(String(password || ''), acct.password)) throw new AccountError('bad_credentials', 401);
    if (acct.banned) throw new AccountError('account_banned', 403);

    const token = this._issueToken(acct);
    this._save();
    return { token, account: this.publicProfile(acct) };
  }

  logout(token) {
    if (token && this.db.sessions[token]) {
      delete this.db.sessions[token];
      this._save();
    }
    return { ok: true };
  }

  verifyToken(token) {
    if (!token) return null;
    const s = this.db.sessions[token];
    if (!s) return null;
    if (!s.expiresAt || s.expiresAt <= Date.now()) {
      delete this.db.sessions[token];
      this._save();
      return null;
    }
    const acct = this._account(s.username);
    if (!acct || acct.banned) return null;
    return this.publicProfile(acct);
  }

  me(token) {
    const profile = this.verifyToken(token);
    if (!profile) throw new AccountError('invalid_token', 401);
    return profile;
  }

  changePassword({ username, currentPassword, newPassword }) {
    const acct = this._requireAccount(username);
    if (!verifyPassword(String(currentPassword || ''), acct.password)) throw new AccountError('bad_credentials', 401);
    if (typeof newPassword !== 'string' || newPassword.length < PASSWORD_MIN || newPassword.length > PASSWORD_MAX) {
      throw new AccountError('invalid_password');
    }
    acct.password = hashPassword(newPassword);
    for (const [token, s] of Object.entries(this.db.sessions)) {
      if (String(s.username).toLowerCase() === String(username).toLowerCase()) delete this.db.sessions[token];
    }
    this._save();
    return { ok: true };
  }

  updateProfile(username, { displayName, avatarBase64, removeAvatar } = {}) {
    const acct = this._requireAccount(username);

    if (displayName !== undefined) {
      const clean = sanitizeDisplayName(displayName);
      if (!clean) throw new AccountError('invalid_display_name');
      acct.displayName = clean;
    }

    if (removeAvatar) {
      avatars.remove(this.dataDir, acct.avatar);
      acct.avatar = null;
    } else if (avatarBase64) {
      try {
        const next = avatars.save(this.dataDir, String(username).toLowerCase(), avatars.inspect(avatarBase64));
        avatars.remove(this.dataDir, acct.avatar);
        acct.avatar = next;
      } catch (err) {
        if (err instanceof avatars.AvatarError) throw new AccountError(err.code);
        throw err;
      }
    }

    this._save();
    return this.publicProfile(acct);
  }

  deleteAccount({ username, password }) {
    const acct = this._requireAccount(username);
    if (!verifyPassword(String(password || ''), acct.password)) throw new AccountError('bad_credentials', 401);
    const key = String(username).toLowerCase();
    avatars.remove(this.dataDir, acct.avatar);
    delete this.db.accounts[key];
    for (const [token, s] of Object.entries(this.db.sessions)) {
      if (String(s.username).toLowerCase() === key) delete this.db.sessions[token];
    }
    for (const other of Object.values(this.db.accounts)) {
      other.friends = (other.friends || []).filter((f) => f !== key);
      other.incoming = (other.incoming || []).filter((f) => f !== key);
      other.outgoing = (other.outgoing || []).filter((f) => f !== key);
    }
    this._save();
    return { ok: true };
  }


  avatarFor(username) {
    const acct = this._account(username);
    if (!acct || !acct.avatar) return null;
    const file = avatars.read(this.dataDir, acct.avatar);
    if (!file) return null;
    return {
      username: acct.username,
      width: file.width,
      height: file.height,
      mime: file.mime,
      data: file.bytes.toString('base64'),
    };
  }


  _unique(list, key) {
    if (!Array.isArray(list)) return [];
    const seen = new Set();
    return list.filter((v) => typeof v === 'string' && v && !seen.has(v) && seen.add(v) !== false).map((v) => v.toLowerCase());
  }

  _sanitizeGraph(acct) {
    acct.friends = Array.from(new Set((acct.friends || []).map((s) => String(s).toLowerCase()))).filter((k) => k !== String(acct.username).toLowerCase());
    acct.incoming = Array.from(new Set((acct.incoming || []).map((s) => String(s).toLowerCase())));
    acct.outgoing = Array.from(new Set((acct.outgoing || []).map((s) => String(s).toLowerCase())));
    acct.blocked = Array.from(new Set((acct.blocked || []).map((s) => String(s).toLowerCase())));
  }

  getFriends(username) {
    const acct = this._account(username);
    if (!acct) return { accepted: [], incoming: [], outgoing: [], blocked: [] };
    this._sanitizeGraph(acct);
    return {
      accepted: acct.friends.map((key) => this.publicProfile(this.db.accounts[key])).filter(Boolean),
      incoming: acct.incoming.map((key) => (this.db.accounts[key] ? this.db.accounts[key].username : key)).filter(Boolean),
      outgoing: acct.outgoing.map((key) => (this.db.accounts[key] ? this.db.accounts[key].username : key)).filter(Boolean),
      blocked: acct.blocked.map((key) => (this.db.accounts[key] ? this.db.accounts[key].username : key)).filter(Boolean),
    };
  }

  _removeFrom(list, key) {
    if (!Array.isArray(list)) return;
    const i = list.indexOf(key);
    if (i >= 0) list.splice(i, 1);
  }

  addFriend(username, friendUsername) {
    const from = this._requireAccount(username);
    const to = this._account(friendUsername);
    if (!to) throw new AccountError('no_such_account', 404);
    if (to.username.toLowerCase() === from.username.toLowerCase()) throw new AccountError('cannot_friend_self');

    this._sanitizeGraph(from);
    this._sanitizeGraph(to);
    const fromKey = from.username.toLowerCase();
    const toKey = to.username.toLowerCase();

    if (from.blocked.includes(toKey)) throw new AccountError('you_blocked_this_account', 409);
    if (to.blocked.includes(fromKey)) throw new AccountError('account_unavailable', 403);

    if (from.friends.includes(toKey)) return { ok: true, status: 'friends', account: this.publicProfile(to) };

    if (from.incoming.includes(toKey)) return this._accept(from, to);

    if (!from.outgoing.includes(toKey)) from.outgoing.push(toKey);
    if (!to.incoming.includes(fromKey)) to.incoming.push(fromKey);
    this._save();
    return { ok: true, status: 'requested', account: this.publicProfile(to) };
  }

  _accept(from, to) {
    const fromKey = from.username.toLowerCase();
    const toKey = to.username.toLowerCase();
    this._removeFrom(from.incoming, toKey);
    this._removeFrom(from.outgoing, toKey);
    this._removeFrom(to.incoming, fromKey);
    this._removeFrom(to.outgoing, fromKey);
    if (!from.friends.includes(toKey)) from.friends.push(toKey);
    if (!to.friends.includes(fromKey)) to.friends.push(fromKey);
    this._save();
    return { ok: true, status: 'friends', account: this.publicProfile(to) };
  }

  acceptFriend(username, friendUsername) {
    const from = this._requireAccount(username);
    const to = this._account(friendUsername);
    if (!to) throw new AccountError('no_such_account', 404);
    this._sanitizeGraph(from);
    this._sanitizeGraph(to);
    if (!from.incoming.includes(to.username.toLowerCase())) throw new AccountError('no_pending_request', 409);
    return this._accept(from, to);
  }

  removeFriend(username, friendUsername) {
    const from = this._requireAccount(username);
    const to = this._account(friendUsername);
    if (!to) throw new AccountError('no_such_account', 404);
    this._sanitizeGraph(from);
    this._sanitizeGraph(to);
    const fromKey = from.username.toLowerCase();
    const toKey = to.username.toLowerCase();
    this._removeFrom(from.friends, toKey);
    this._removeFrom(to.friends, fromKey);
    this._removeFrom(from.incoming, toKey);
    this._removeFrom(to.incoming, fromKey);
    this._removeFrom(from.outgoing, toKey);
    this._removeFrom(to.outgoing, fromKey);
    this._save();
    return { ok: true };
  }

  blockFriend(username, targetUsername) {
    const from = this._requireAccount(username);
    const to = this._account(targetUsername);
    if (!to) throw new AccountError('no_such_account', 404);
    this._sanitizeGraph(from);
    this._sanitizeGraph(to);
    const fromKey = from.username.toLowerCase();
    const toKey = to.username.toLowerCase();
    if (fromKey === toKey) throw new AccountError('cannot_block_self');
    this.removeFriend(username, targetUsername);
    if (!from.blocked.includes(toKey)) from.blocked.push(toKey);
    this._save();
    return { ok: true };
  }

  unblockFriend(username, targetUsername) {
    const acct = this._requireAccount(username);
    this._sanitizeGraph(acct);
    this._removeFrom(acct.blocked, String(targetUsername).toLowerCase());
    this._save();
    return { ok: true };
  }

  search(query, limit = 20) {
    const q = String(query || '').trim().toLowerCase();
    if (q.length < 2) return [];
    const results = [];
    for (const [key, acct] of Object.entries(this.db.accounts)) {
      if (results.length >= limit) break;
      if (key.includes(q) || String(acct.displayName || '').toLowerCase().includes(q)) results.push(this.publicProfile(acct));
    }
    return results;
  }

  friendProfiles(username) {
    return this.getFriends(username).accepted;
  }
}

module.exports = { LocalAccountStore, sanitizeDisplayName, USERNAME_RE };
