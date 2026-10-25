const test = require('node:test');
const assert = require('node:assert');
const { renderStatusPage } = require('../src/statusPage');

const base = {
  version: '1.0.0',
  urls: ['http://localhost:3000', 'http://192.168.1.5:3000'],
  publicUrl: null,
  accounts: 'local',
  engines: ['/api/Unity/SteamworksNET'],
  online: 3,
  games: ['730'],
  lobbies: 1,
  networking: { enabled: false, mode: 'local' },
};

test('the status page shows the addresses, counters and client mounts', () => {
  const html = renderStatusPage(base);
  assert.ok(html.startsWith('<!doctype html>'));
  assert.ok(html.includes('http://localhost:3000'));
  assert.ok(html.includes('http://192.168.1.5:3000'));
  assert.ok(html.includes('3 online'));
  assert.ok(html.includes('/api/Unity/SteamworksNET'));
  assert.ok(html.includes('local accounts (this server)'));
  assert.ok(html.includes('GWAS networking is off'));
  assert.ok(!html.includes('Public (tunnel)'));
});

test('the status page reflects GWAS accounts, a tunnel and a bridged server', () => {
  const html = renderStatusPage(Object.assign({}, base, {
    accounts: 'gwas',
    publicUrl: 'https://brave-tiger-lamp.trycloudflare.com',
    networking: {
      enabled: true,
      mode: 'gwas',
      publishing: true,
      authMode: 'claim',
      serverId: 'srv_abc',
      publicUrl: 'wss://brave-tiger-lamp.trycloudflare.com/federation/bridge',
    },
  }));
  assert.ok(html.includes('GWAS (remote accounts)'));
  assert.ok(html.includes('https://brave-tiger-lamp.trycloudflare.com'));
  assert.ok(html.includes('srv_abc'));
  assert.ok(html.includes('the secret this machine generated'));
});

// A server that has a claimed token but no public address yet used to read as "networking
// is off", which is what sent people looking for a key to set. It now says what it waits
// for.
test('the status page explains a server that is waiting for an address', () => {
  const html = renderStatusPage(Object.assign({}, base, {
    accounts: 'gwas',
    networking: { enabled: true, mode: 'gwas', publishing: false, serverId: 'srv_abc', disabledReason: 'waiting_public_url' },
    networkingHint: 'waiting for a public address: accept the Cloudflare tunnel prompt or set WEBWORKS_PUBLIC_URL',
  }));
  assert.ok(html.includes('waiting for a public address'));
  assert.ok(html.includes('WEBWORKS_PUBLIC_URL'));
});

test('the status page says so when nothing has connected yet', () => {
  const html = renderStatusPage(Object.assign({}, base, { games: [], lobbies: 0, online: 0, urls: [] }));
  assert.ok(html.includes('no game has connected yet'));
  assert.ok(html.includes('no addresses yet'));
  assert.ok(html.includes('0 online'));
});

test('the status page escapes client-supplied values', () => {
  const html = renderStatusPage(Object.assign({}, base, {
    games: ['<script>alert(1)</script>'],
    urls: ['http://localhost:3000/?x="><img src=x onerror=alert(1)>'],
  }));
  assert.ok(!html.includes('<script>alert(1)</script>'));
  assert.ok(!html.includes('onerror=alert(1)>'));
  assert.ok(html.includes('&lt;script&gt;alert(1)&lt;/script&gt;'));
});
