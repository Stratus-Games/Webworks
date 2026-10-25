// One-time introduction tickets for server-to-server links.
//
// A node server never shares its bridge secret with anyone. GWAS mints a ticket for a
// pair of servers (dialer -> host) using the *host's* secret, so only the host can
// verify it and only the dialer ever sees it. The ticket is single use and lives for
// a minute: it authorises one websocket handshake, nothing else.
//
// Wire format (kept identical in the GWAS worker, see GWAS/src/tickets.js):
//
//   base64url(JSON claims) "." base64url(hmac-sha256(secret, first-part))
//
// This module is deliberately dependency-free so it can be unit tested in isolation.

const crypto = require('crypto');

const VERSION = 1;
const MAX_TICKET_TTL_MS = 2 * 60 * 1000;
const MAX_TICKET_CHARS = 4096;

function base64url(buffer) {
  return Buffer.from(buffer).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fromBase64url(value) {
  const padded = value.replace(/-/g, '+').replace(/_/g, '/');
  return Buffer.from(padded + '='.repeat((4 - (padded.length % 4)) % 4), 'base64');
}

function sign(secret, claims) {
  const payload = base64url(JSON.stringify(claims));
  const mac = crypto.createHmac('sha256', secret).update(payload).digest();
  return payload + '.' + base64url(mac);
}

function issue(secret, { from, to, appId, lobbyId, ttlMs = 60 * 1000, nonce }) {
  if (!secret) throw new Error('missing_ticket_secret');
  for (const [name, value] of Object.entries({ from, to, appId, lobbyId })) {
    if (typeof value !== 'string' || !value) throw new Error('missing_ticket_' + name);
  }
  const ttl = Math.max(1000, Math.min(Number(ttlMs) || 0, MAX_TICKET_TTL_MS));
  const claims = {
    v: VERSION,
    from: String(from),
    to: String(to),
    appId: String(appId),
    lobbyId: String(lobbyId),
    exp: Date.now() + ttl,
    n: nonce || crypto.randomBytes(12).toString('hex'),
  };
  return { ticket: sign(secret, claims), expiresAt: claims.exp, nonce: claims.n };
}

// Returns the claims of a valid ticket or null. Never throws: a malformed ticket from
// the network is just a rejected connection.
function verify(secret, ticket, now = Date.now()) {
  if (!secret || typeof ticket !== 'string' || ticket.length === 0 || ticket.length > MAX_TICKET_CHARS) return null;

  const dot = ticket.indexOf('.');
  if (dot <= 0 || ticket.indexOf('.', dot + 1) !== -1) return null;

  const payload = ticket.slice(0, dot);
  let mac;
  try {
    mac = fromBase64url(ticket.slice(dot + 1));
  } catch {
    return null;
  }
  if (mac.length !== 32) return null;

  const expected = crypto.createHmac('sha256', secret).update(payload).digest();
  if (!crypto.timingSafeEqual(mac, expected)) return null;

  let claims;
  try {
    claims = JSON.parse(fromBase64url(payload).toString('utf8'));
  } catch {
    return null;
  }
  if (!claims || typeof claims !== 'object') return null;
  if (claims.v !== VERSION) return null;
  for (const key of ['from', 'to', 'appId', 'lobbyId', 'n']) {
    if (typeof claims[key] !== 'string' || !claims[key]) return null;
  }
  const exp = Number(claims.exp);
  if (!Number.isFinite(exp)) return null;
  if (exp <= now) return null;
  // A ticket that lives longer than the protocol allows was not minted by us.
  if (exp - now > MAX_TICKET_TTL_MS + 1000) return null;

  return { from: claims.from, to: claims.to, appId: claims.appId, lobbyId: claims.lobbyId, exp, nonce: claims.n };
}

// Nonces already spent, so a captured handshake URL cannot be replayed while the
// ticket is still valid.
class NonceCache {
  constructor(max = 4096) {
    this.max = max;
    this.seen = new Map();
  }

  // True when the nonce was free and is now reserved.
  take(nonce, expiresAt, now = Date.now()) {
    this.prune(now);
    if (this.seen.has(nonce)) return false;
    if (this.seen.size >= this.max) return false;
    this.seen.set(nonce, expiresAt);
    return true;
  }

  prune(now = Date.now()) {
    for (const [nonce, exp] of this.seen) if (exp <= now) this.seen.delete(nonce);
  }

  clear() {
    this.seen.clear();
  }
}

module.exports = { issue, verify, sign, NonceCache, MAX_TICKET_TTL_MS };
