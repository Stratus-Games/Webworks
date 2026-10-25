const fs = require('fs');
const path = require('path');
const { globalAccountIdentity } = require('../identity');
const { AccountError } = require('./errors');
const log = require('../log');

const REQUEST_TIMEOUT_MS = Math.max(2000, Number(process.env.WEBWORKS_GWAS_TIMEOUT_MS) || 15000);

const ERROR_STATUS = {
  bad_credentials: 401,
  unauthorized: 401,
  invalid_token: 401,
  account_banned: 403,
  username_taken: 409,
  no_such_account: 404,
  not_found: 404,
  rate_limited: 429,
  invalid_username: 400,
  weak_password: 400,
  invalid_password: 400,
  missing_username: 400,
  invalid_avatar: 400,
  avatar_too_large: 413,
  forbidden: 403,
  bad_request: 400,
};

// The account service rejecting a request (a short password, a name with a space, a
// name someone already has) is not a broken gateway. Those answers used to be
// relabelled 502, so the browser reported "Bad Gateway" and the real reason never
// reached the player. An unmapped 4xx keeps the upstream status; only a 5xx or an
// unreadable answer is a genuine 502.
function statusFor(code, upstreamStatus) {
  if (ERROR_STATUS[code]) return ERROR_STATUS[code];
  if (upstreamStatus >= 400 && upstreamStatus < 500) return upstreamStatus;
  return 502;
}

class GwasAccountStore {
  constructor() {
    this.mode = 'gwas';
    this.url = '';
    this.apiKey = '';
    this.available = false;
    this.dataDir = null;
    this.overlayFile = null;
    this.overlay = { version: 1, blocks: {} };
    this.avatarCache = new Map();
  }

  init({ gwasUrl, gwasApiKey, dataDir }) {
    this.url = String(gwasUrl || '').replace(/\/+$/, '');
    this.apiKey = gwasApiKey || '';
    this.available = Boolean(this.url);
    this.dataDir = dataDir;

    if (dataDir) {
      this.overlayFile = path.join(dataDir, 'gwas-social.json');
      try {
        if (fs.existsSync(this.overlayFile)) {
          const parsed = JSON.parse(fs.readFileSync(this.overlayFile, 'utf8'));
          this.overlay = { version: 1, blocks: parsed.blocks || {} };
        }
      } catch (err) {
        log.warn(`gwas-social.json was not readable (${err.message}); starting empty`);
      }
    }
    return { url: this.url, accounts: 'remote' };
  }

  _saveOverlay() {
    if (!this.overlayFile) return;
    const tmp = `${this.overlayFile}.tmp`;
    fs.writeFileSync(tmp, JSON.stringify(this.overlay, null, 2));
    fs.renameSync(tmp, this.overlayFile);
  }

  steamIdFor(username) {
    return globalAccountIdentity(username).steamId;
  }

  async _request(method, pathname, { token, body } = {}) {
    const headers = { Accept: 'application/json' };
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    if (token) headers.Authorization = `Bearer ${token}`;
    if (this.apiKey) headers['x-webworks-api-key'] = this.apiKey;

    let resp;
    try {
      resp = await fetch(this.url + pathname, {
        method,
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
        signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
      });
    } catch (err) {
      log.warn(`GWAS unreachable: ${err.message}`);
      throw new AccountError('accounts_unavailable', 503);
    }

    const text = await resp.text();
    let data = null;
    if (text) {
      try { data = JSON.parse(text); } catch { data = null; }
    }

    if (!resp.ok) {
      const code = (data && (data.error || data.code)) || `upstream_${resp.status}`;
      const status = statusFor(code, resp.status);
      if (status >= 500) log.warn(`GWAS ${method} ${pathname} failed: ${resp.status} ${code}`);
      throw new AccountError(code, status);
    }
    return data || {};
  }

  _rememberAvatar(username, base64) {
    if (!username || !base64) return;
    this.avatarCache.set(String(username).toLowerCase(), {
      username,
      width: 0,
      height: 0,
      mime: 'image/png',
      data: String(base64).replace(/^data:[^,]+,/, ''),
    });
  }

  _profile(account) {
    if (!account) return null;
    const username = account.username;
    if (!username) return null;
    if (account.avatarBase64) this._rememberAvatar(username, account.avatarBase64);
    return {
      username,
      displayName: account.displayName || username,
      personaState: account.personaState || 'Online',
      steamId: this.steamIdFor(username),
      hasAvatar: Boolean(account.avatarBase64 || this.avatarCache.has(String(username).toLowerCase())),
      avatarBase64: account.avatarBase64 || null,
      createdAt: account.createdAt || 0,
    };
  }

  async register({ username, password, displayName, avatarBase64 }) {
    const body = { username, password };
    if (displayName) body.displayName = displayName;
    await this._request('POST', '/accounts/register', { body });

    const { token, account } = await this.login({ username, password });

    if (avatarBase64) {
      try {
        await this._request('POST', '/accounts/avatar', { token, body: { avatarBase64: stripDataUrl(avatarBase64) } });
        this._rememberAvatar(username, stripDataUrl(avatarBase64));
        account.hasAvatar = true;
        account.avatarBase64 = stripDataUrl(avatarBase64);
      } catch (err) {
        log.warn(`GWAS avatar upload failed: ${err.code}`);
      }
    }
    return { token, account };
  }

  async login({ username, password }) {
    const data = await this._request('POST', '/accounts/login', { body: { username, password } });
    const account = this._profile(data.account);
    if (!account || !data.token) throw new AccountError('invalid_upstream_response', 502);
    return { token: data.token, account };
  }

  async logout(token) {
    if (token) {
      try { await this._request('POST', '/accounts/logout', { token, body: {} }); } catch {  }
    }
    return { ok: true };
  }

  async verifyToken(token) {
    if (!token) return null;
    try {
      const data = await this._request('GET', '/accounts/me', { token });
      return this._profile(data.account || data);
    } catch (err) {
      if (err instanceof AccountError && (err.status === 401 || err.status === 404)) return null;
      log.warn(`GWAS token verification failed: ${err.code}`);
      return null;
    }
  }

  async me(token) {
    const profile = await this.verifyToken(token);
    if (!profile) throw new AccountError('invalid_token', 401);
    return profile;
  }

  async changePassword({ currentPassword, newPassword, token }) {
    if (!token) throw new AccountError('invalid_token', 401);
    const data = await this._request('POST', '/accounts/password', { token, body: { currentPassword, newPassword } });
    return { token: data.token, account: this._profile(data.account) };
  }

  // Persona state lives on the account so it follows the player between devices.
  async setPersona(_username, personaState, personaName, token) {
    if (!token) return null;
    const body = { personaState };
    if (personaName) body.personaName = personaName;
    const data = await this._request('POST', '/accounts/persona', { token, body });
    return this._profile(data.account);
  }

  async updateProfile(username, update = {}) {
    if (!update.token) throw new AccountError('invalid_token', 401);
    if (update.avatarBase64) {
      const b64 = stripDataUrl(update.avatarBase64);
      await this._request('POST', '/accounts/avatar', { token: update.token, body: { avatarBase64: b64 } });
      this._rememberAvatar(username, b64);
    }
    if (update.removeAvatar) {
      this.avatarCache.delete(String(username).toLowerCase());
    }
    return this.me(update.token);
  }

  async deleteAccount({ username, token }) {
    const sessionToken = token || null;
    if (!sessionToken) throw new AccountError('invalid_token', 401);
    await this._request('POST', '/accounts/delete', { token: sessionToken, body: {} });
    this.avatarCache.delete(String(username).toLowerCase());
    return { ok: true };
  }

  async avatarFor(username) {
    const cached = this.avatarCache.get(String(username).toLowerCase());
    if (cached) return cached;

    try {
      const resp = await fetch(`${this.url}/accounts/avatar?username=${encodeURIComponent(username)}`, {
        headers: this.apiKey ? { 'x-webworks-api-key': this.apiKey } : {},
        signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
      });
      if (!resp.ok) return null;
      const type = resp.headers.get('content-type') || '';
      if (type.includes('application/json')) {
        const data = await resp.json();
        const b64 = data.avatarBase64 || (data.account && data.account.avatarBase64);
        if (!b64) return null;
        this._rememberAvatar(username, b64);
        return this.avatarCache.get(String(username).toLowerCase());
      }
      if (type.startsWith('image/')) {
        const bytes = Buffer.from(await resp.arrayBuffer());
        const entry = { username, width: 0, height: 0, mime: type.split(';')[0], data: bytes.toString('base64') };
        this.avatarCache.set(String(username).toLowerCase(), entry);
        return entry;
      }
    } catch {  }
    return null;
  }

  async getFriends(username, token) {
    const empty = { accepted: [], incoming: [], outgoing: [], blocked: [] };
    if (!token) return empty;

    let data;
    try {
      data = await this._request('GET', '/accounts/friends', { token });
    } catch (err) {
      log.warn(`GWAS friends fetch failed: ${err.code}`);
      return empty;
    }

    const blocked = new Set((this.overlay.blocks[String(username).toLowerCase()] || []).map((b) => b.toLowerCase()));

    const accepted = (data.accepted || [])
      .map((f) => (typeof f === 'string' ? this._profile({ username: f }) : this._profile(f)))
      .filter((f) => f && !blocked.has(String(f.username).toLowerCase()));

    const incoming = (data.incoming || [])
      .map((f) => (typeof f === 'string' ? f : f.username))
      .filter((u) => u && !blocked.has(String(u).toLowerCase()));

    const outgoing = (data.outgoing || [])
      .map((f) => (typeof f === 'string' ? f : f.username))
      .filter((u) => u && !blocked.has(String(u).toLowerCase()));

    return { accepted, incoming, outgoing, blocked: [...blocked] };
  }

  async addFriend(_username, friendUsername, token) {
    if (!token) throw new AccountError('invalid_token', 401);
    await this._request('POST', '/accounts/friends/request', { token, body: { targetUsername: friendUsername } });
    return { ok: true, status: 'requested' };
  }

  async acceptFriend(_username, friendUsername, token) {
    if (!token) throw new AccountError('invalid_token', 401);
    await this._request('POST', '/accounts/friends/accept', { token, body: { fromUsername: friendUsername } });
    return { ok: true, status: 'friends' };
  }

  async removeFriend(_username, friendUsername, token) {
    if (!token) throw new AccountError('invalid_token', 401);
    await this._request('POST', '/accounts/friends/remove', { token, body: { username: friendUsername } });
    return { ok: true };
  }

  async blockFriend(username, targetUsername, token) {
    const key = String(username).toLowerCase();
    const target = String(targetUsername).toLowerCase();
    const list = new Set(this.overlay.blocks[key] || []);
    list.add(target);
    this.overlay.blocks[key] = [...list];
    this._saveOverlay();
    try { await this.removeFriend(username, targetUsername, token); } catch {  }
    return { ok: true };
  }

  async unblockFriend(username, targetUsername, token) {
    const key = String(username).toLowerCase();
    this.overlay.blocks[key] = (this.overlay.blocks[key] || []).filter((b) => b !== String(targetUsername).toLowerCase());
    this._saveOverlay();
    return { ok: true };
  }

  async follow(_username, targetUsername, token) {
    if (!token) throw new AccountError('invalid_token', 401);
    await this._request('POST', '/accounts/follow', { token, body: { username: targetUsername } });
    return { ok: true };
  }

  async unfollow(_username, targetUsername, token) {
    if (!token) throw new AccountError('invalid_token', 401);
    await this._request('POST', '/accounts/unfollow', { token, body: { username: targetUsername } });
    return { ok: true };
  }

  async followers(username, token) {
    if (!token) return [];
    const data = await this._request('GET', `/accounts/followers?username=${encodeURIComponent(username)}`, { token });
    return (data.accounts || []).map((account) => this._profile(account)).filter(Boolean);
  }

  async following(_username, token) {
    if (!token) return [];
    const data = await this._request('GET', '/accounts/following', { token });
    return (data.accounts || []).map((account) => this._profile(account)).filter(Boolean);
  }

  async search(query, token) {
    if (!token) return [];
    const data = await this._request('GET', `/accounts/search?q=${encodeURIComponent(String(query || ''))}`, { token });
    return (data.accounts || []).map((account) => this._profile(account)).filter(Boolean);
  }

  async friendProfiles(username, token) {
    return (await this.getFriends(username, token)).accepted;
  }
}

function stripDataUrl(raw) {
  const s = String(raw || '');
  const comma = s.indexOf(',');
  return s.startsWith('data:') && comma > 0 ? s.slice(comma + 1) : s;
}

module.exports = { GwasAccountStore };
