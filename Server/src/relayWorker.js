
const dgram = require('dgram');
const crypto = require('crypto');
const WebSocket = require('ws');

const LOBBY_ID = process.env.WEBWORKS_RELAY_LOBBY_ID;
const TICKET_SECRET = process.env.WEBWORKS_TICKET_SECRET || 'dev-secret-change-me';
const SELF_IDLE_MS = Math.max(10000, Number(process.env.WEBWORKS_RELAY_IDLE_TIMEOUT_MS) || 120000);

const peers = new Map();
let lastActivity = Date.now();

function verifyTicket(ticket) {
  try {
    const decoded = Buffer.from(ticket, 'base64').toString('utf8');
    const parts = decoded.split('.');
    if (parts.length !== 4) return null;
    const [steamId, lobbyId, expiresAt, signature] = parts;
    if (lobbyId !== LOBBY_ID) return null;
    const expected = crypto.createHmac('sha256', TICKET_SECRET).update(`${steamId}.${lobbyId}.${expiresAt}`).digest('hex');
    if (signature !== expected) return null;
    if (Date.now() > Number(expiresAt)) return null;
    return steamId;
  } catch {
    return null;
  }
}

function markActive() {
  lastActivity = Date.now();
  if (process.send) process.send({ type: 'heartbeat' });
}

const pendingResolves = new Map();
let nextReqId = 1;
function resolveViaMaster(steamId) {
  return new Promise((resolve) => {
    const reqId = nextReqId++;
    pendingResolves.set(reqId, resolve);
    setTimeout(() => {
      if (pendingResolves.delete(reqId)) resolve(null);
    }, 2000);
    process.send({ type: 'resolve', reqId, steamId });
  });
}

process.on('message', (msg) => {
  if (!msg || msg.type !== 'resolved') return;
  const resolve = pendingResolves.get(msg.reqId);
  if (!resolve) return;
  pendingResolves.delete(msg.reqId);
  resolve(msg.found ? { address: msg.address, port: msg.port } : null);
});

async function deliver(fromSteamId, to, channel, reliability, dataB64) {
  markActive();
  const frame = JSON.stringify({ type: 'packet', from: fromSteamId, channel, reliability, data: dataB64 });

  let dest = peers.get(to);
  if (!dest) {
    const resolved = await resolveViaMaster(to);
    if (!resolved) return;
    dest = { kind: 'udp', address: resolved.address, port: resolved.port };
  }

  if (dest.kind === 'ws') {
    if (dest.socket.readyState === WebSocket.OPEN) dest.socket.send(frame);
  } else {
    udpSocket.send(Buffer.from(frame), dest.port, dest.address);
  }
}

const udpSocket = dgram.createSocket('udp4');

udpSocket.on('message', (msg, rinfo) => {
  let parsed;
  try { parsed = JSON.parse(msg.toString('utf8')); } catch { return; }

  if (parsed.type === 'hello') {
    const steamId = verifyTicket(parsed.ticket);
    if (!steamId) return;
    peers.set(steamId, { kind: 'udp', address: rinfo.address, port: rinfo.port });
    markActive();
    udpSocket.send(Buffer.from(JSON.stringify({ type: 'welcome' })), rinfo.port, rinfo.address);
    return;
  }

  if (parsed.type === 'send') {
    let fromSteamId = null;
    for (const [id, p] of peers) {
      if (p.kind === 'udp' && p.address === rinfo.address && p.port === rinfo.port) { fromSteamId = id; break; }
    }
    if (!fromSteamId) return;
    deliver(fromSteamId, parsed.to, parsed.channel, parsed.reliability, parsed.data);
  }
});

udpSocket.on('error', (err) => {
  console.error(`[relay ${LOBBY_ID}] udp error: ${err.message}`);
});

const wss = new WebSocket.Server({ port: 0 });

wss.on('connection', (socket) => {
  let steamId = null;

  socket.on('message', (raw) => {
    let parsed;
    try { parsed = JSON.parse(raw.toString('utf8')); } catch { return; }

    if (parsed.type === 'hello') {
      const verified = verifyTicket(parsed.ticket);
      if (!verified) { socket.close(4001, 'bad_ticket'); return; }
      steamId = verified;
      peers.set(steamId, { kind: 'ws', socket });
      markActive();
      socket.send(JSON.stringify({ type: 'welcome' }));
      return;
    }

    if (parsed.type === 'send' && steamId) {
      deliver(steamId, parsed.to, parsed.channel, parsed.reliability, parsed.data);
    }
  });

  socket.on('close', () => {
    if (steamId && peers.get(steamId) && peers.get(steamId).kind === 'ws' && peers.get(steamId).socket === socket) {
      peers.delete(steamId);
    }
  });
});

setInterval(() => {
  if (Date.now() - lastActivity > SELF_IDLE_MS) process.exit(0);
}, 30000).unref();

Promise.all([
  new Promise((resolve) => udpSocket.bind(0, resolve)),
  new Promise((resolve) => wss.once('listening', resolve)),
]).then(() => {
  process.send({ type: 'listening', wsPort: wss.address().port, udpPort: udpSocket.address().port });
});
