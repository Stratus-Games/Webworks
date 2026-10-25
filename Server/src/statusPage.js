// The page a person lands on when they open the server address in a browser. Without it
// Express answers "Cannot GET /", which reads as "the server did not start" even when it is
// serving players normally.

const { networkingHint } = require('./gwasProbe');

const ESCAPES = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };

// Game ids and display names come from clients, so nothing from the status payload is
// interpolated without escaping.
function esc(value) {
  return String(value == null ? '' : value).replace(/[&<>"']/g, (c) => ESCAPES[c]);
}

function list(items, emptyText) {
  if (!items || !items.length) return `<p class="muted">${esc(emptyText)}</p>`;
  return `<ul>${items.map((item) => `<li>${item}</li>`).join('')}</ul>`;
}

function renderStatusPage(status = {}) {
  const urls = status.urls || [];
  const games = (status.games || []).map(esc);
  const accounts = status.accounts === 'gwas' ? 'GWAS (remote accounts)' : 'local accounts (this server)';
  const accountsDetail = status.accountsDetail || {};
  const accountsExtra = accountsDetail.mode !== 'gwas'
    ? ''
    : accountsDetail.reachable === false
      ? ` &mdash; <strong>not reachable</strong> (${esc(accountsDetail.error || 'no answer')})`
      : accountsDetail.reachable === true
        ? ` &mdash; reachable${accountsDetail.storage ? ` (${esc(accountsDetail.storage)} storage)` : ''}`
        : '';
  const players = Number.isFinite(status.online) ? status.online : 0;
  const lobbies = Number.isFinite(status.lobbies) ? status.lobbies : 0;
  const networking = status.networking || {};

  const addressList = list(urls.map((url) => `<a href="${esc(url)}">${esc(url)}</a>`), 'no addresses yet');
  const gameList = list(games, 'no game has connected yet');
  const engineList = list((status.engines || []).map((mount) => `<code>${esc(mount)}</code>`), 'none');

  const bridgeLine = networking.publishing
    ? `GWAS networking is on${networking.authMode === 'claim' ? ' (signed in with the secret this machine generated)' : ''}${
        networking.serverId ? `, this server is <code>${esc(networking.serverId)}</code>` : ''
      }, bridging at <code>${esc(networking.publicUrl || '')}</code>.`
    : networking.enabled
      ? 'GWAS networking is on but waiting for a public address, so lobbies stay here for now.'
      : 'GWAS networking is off, so every lobby is hosted by this server.';

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex">
<title>Webworks ${esc(status.version)}</title>
<style>
  :root { color-scheme: light dark; }
  body { margin: 0; padding: 2.5rem 1.25rem; font: 15px/1.55 ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif; }
  main { max-width: 44rem; margin: 0 auto; }
  h1 { margin: 0 0 .25rem; font-size: 1.5rem; letter-spacing: .01em; }
  .tag { margin: 0 0 1.75rem; opacity: .7; }
  section { border: 1px solid rgba(128,128,128,.32); border-radius: 10px; padding: .85rem 1rem; margin-bottom: .85rem; }
  h2 { margin: 0 0 .4rem; font-size: .78rem; text-transform: uppercase; letter-spacing: .08em; opacity: .65; font-weight: 600; }
  ul { margin: .2rem 0 0; padding-left: 1.1rem; }
  li { margin: .12rem 0; }
  code, a { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: .92em; }
  .muted { opacity: .6; margin: .2rem 0 0; }
  .big { font-size: 1.35rem; font-weight: 600; }
  footer { margin-top: 1.5rem; font-size: .82rem; opacity: .6; }
</style>
</head>
<body>
<main>
  <h1>Webworks <span class="muted">${esc(status.version)}</span></h1>
  <p class="tag">The server is running. This page is the human view; game clients talk to the API mounts below.</p>

  <section>
    <h2>Addresses</h2>
    ${addressList}
    ${status.publicUrl ? `<p>Public (tunnel): <a href="${esc(status.publicUrl)}">${esc(status.publicUrl)}</a></p>` : ''}
  </section>

  <section>
    <h2>Live now</h2>
    <p class="big">${esc(players)} online &middot; ${esc(lobbies)} ${lobbies === 1 ? 'lobby' : 'lobbies'}</p>
    <h2>Connected games</h2>
    ${gameList}
  </section>

  <section>
    <h2>Accounts</h2>
    <p>${esc(accounts)}${accountsDetail.url ? ` &middot; <code>${esc(accountsDetail.url)}</code>` : ''}${accountsExtra}</p>
  </section>

  <section>
    <h2>Networking</h2>
    <p>${bridgeLine}</p>
    ${networking.enabled && networking.publishing ? '' : (() => {
      const hint = status.networkingHint || networkingHint(networking.disabledReason);
      return hint ? `<p class="muted">Why: ${esc(hint)}</p>` : '';
    })()}
  </section>

  <section>
    <h2>Client mounts</h2>
    ${engineList}
    <p class="muted">Machine-readable status: <a href="/health">/health</a> &middot; <a href="/status">/status</a></p>
    <p class="muted">Try every route from a browser: <a href="/harness/">/harness/</a> (sign in, guest, the API console and the multiplayer arena).</p>
  </section>

  <footer>
    Health check: <code>curl ${esc(urls[0] || 'http://localhost:3000')}/health</code><br>
    Another port: <code>npm start -- --port 3001</code> &middot; set <code>WEBWORKS_PORT</code> &middot; config: <code>webworks.config.json</code>
  </footer>
</main>
</body>
</html>
`;
}

module.exports = { renderStatusPage };
