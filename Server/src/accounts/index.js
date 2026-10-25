const { LocalAccountStore, sanitizeDisplayName, USERNAME_RE } = require('./localStore');
const { GwasAccountStore } = require('./gwas');
const { AccountError } = require('./errors');
const log = require('../log');

class Accounts {
  constructor() {
    this.provider = null;
    this.mode = 'local';
    this.detail = null;
  }

  init(config) {
    this.mode = config.accountMode;
    this.provider = config.accountMode === 'gwas' ? new GwasAccountStore() : new LocalAccountStore();
    this.detail = this.provider.init({
      dataDir: config.dataDir,
      sessionDays: config.sessionDays,
      gwasUrl: config.gwasUrl,
      gwasApiKey: config.gwasApiKey,
    });
    log.info(`account backend: ${this.label}`);
    return this;
  }

  get label() {
    return this.mode === 'gwas' ? 'GWAS (remote)' : 'Local Accounts';
  }

  get enabled() { return Boolean(this.provider); }

  // Base URL of the remote account service (GWAS), or '' for local accounts.
  get url() { return (this.provider && this.provider.url) || ''; }

  steamIdFor(username) { return this.provider.steamIdFor(username); }


  register(input) { return this.provider.register(input); }
  login(input) { return this.provider.login(input); }
  logout(token) { return this.provider.logout(token); }
  verifyToken(token) { return this.provider.verifyToken(token); }
  me(token) { return this.provider.me(token); }

  changePassword(input) { return this.provider.changePassword(input); }
  setPersona(username, personaState, personaName, token) {
    if (this.provider.setPersona) return this.provider.setPersona(username, personaState, personaName, token);
    return Promise.resolve(null);
  }
  updateProfile(username, update) { return this.provider.updateProfile(username, update); }
  deleteAccount(input) { return this.provider.deleteAccount(input); }

  avatarFor(username) { return this.provider.avatarFor(username); }


  getFriends(username, token) { return this.provider.getFriends(username, token); }
  addFriend(username, friendUsername, token) { return this.provider.addFriend(username, friendUsername, token); }
  acceptFriend(username, friendUsername, token) { return this.provider.acceptFriend(username, friendUsername, token); }
  removeFriend(username, friendUsername, token) { return this.provider.removeFriend(username, friendUsername, token); }
  blockFriend(username, friendUsername, token) { return this.provider.blockFriend(username, friendUsername, token); }
  unblockFriend(username, friendUsername, token) { return this.provider.unblockFriend(username, friendUsername, token); }
  search(query, token) { return this.provider.search(query, token); }

  follow(username, targetUsername, token) {
    if (this.provider.follow) return this.provider.follow(username, targetUsername, token);
    return Promise.resolve({ ok: true });
  }
  unfollow(username, targetUsername, token) {
    if (this.provider.unfollow) return this.provider.unfollow(username, targetUsername, token);
    return Promise.resolve({ ok: true });
  }
  followers(username, token) {
    if (this.provider.followers) return this.provider.followers(username, token);
    return Promise.resolve([]);
  }
  following(username, token) {
    if (this.provider.following) return this.provider.following(username, token);
    return Promise.resolve([]);
  }

  publicProfile(account) {
    if (!account) return null;
    if (this.provider.publicProfile) return this.provider.publicProfile(account);
    return account;
  }
}

const accounts = new Accounts();

function sendAccountError(res, err) {
  if (err instanceof AccountError) {
    if (err.status >= 500) log.warn(`account backend error: ${err.code}`);
    return res.status(err.status || 400).json({ error: err.code });
  }
  log.error('account request failed:', err && err.stack ? err.stack : err);
  return res.status(500).json({ error: 'internal_error' });
}

module.exports = { accounts, AccountError, sendAccountError, sanitizeDisplayName, USERNAME_RE };
