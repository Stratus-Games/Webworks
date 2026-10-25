const { state } = require('./state');

// A session token is what the shims hold once a game session exists, but the bootstrap
// screens only have the account token they got from /accounts/login. Both are accepted:
// an account token resolves to the game session that linked it, so the friends page and
// its heartbeat work before a game ever opens a socket.
function resolveSessionToken(token) {
  if (!token) return null;
  if (state.sessions.has(token)) return { session: state.sessions.get(token), token };

  const mapped = state.accountTokenToSession.get(String(token));
  if (mapped && state.sessions.has(mapped)) return { session: state.sessions.get(mapped), token: mapped };

  for (const session of state.sessions.values()) {
    const linked = state.linkedAccounts.get(String(session.steamId));
    if (linked && linked.token === token) {
      if (session.sessionToken) state.accountTokenToSession.set(String(token), String(session.sessionToken));
      return { session, token: session.sessionToken || token };
    }
  }
  return null;
}

function requireSession(req, res, next) {
  const token =
    req.headers['x-webworks-session'] ||
    (req.headers.authorization || '').replace(/^Bearer\s+/i, '');

  const resolved = resolveSessionToken(token);
  if (!resolved) {
    return res.status(401).json({ error: 'invalid_or_missing_session' });
  }

  req.session = resolved.session;
  req.sessionToken = resolved.token;
  next();
}

function requireServerSession(req, res, next) {
  requireSession(req, res, () => {
    if (req.session.kind !== 'server') return res.status(403).json({ error: 'not_a_game_server' });
    next();
  });
}

module.exports = { requireSession, requireServerSession, resolveSessionToken };
