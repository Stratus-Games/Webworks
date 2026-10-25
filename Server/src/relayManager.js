
const { fork } = require('child_process');
const path = require('path');

const IDLE_TIMEOUT_MS = Math.max(10000, Number(process.env.WEBWORKS_RELAY_IDLE_TIMEOUT_MS) || 120000);
const SPAWN_TIMEOUT_MS = 8000;

const relays = new Map();
let nextReqId = 1;

function spawn(lobbyId, appId) {
  return new Promise((resolve, reject) => {
    const child = fork(path.join(__dirname, 'relayWorker.js'), [], {
      env: { ...process.env, WEBWORKS_RELAY_LOBBY_ID: lobbyId },
      silent: false,
    });

    const timeout = setTimeout(() => {
      child.kill();
      reject(new Error('relay_spawn_timeout'));
    }, SPAWN_TIMEOUT_MS);

    const record = { child, wsPort: null, udpPort: null, lastActivity: Date.now(), appId, resolvers: new Map() };

    child.once('message', (msg) => {
      if (msg && msg.type === 'listening') {
        clearTimeout(timeout);
        record.wsPort = msg.wsPort;
        record.udpPort = msg.udpPort;
        relays.set(lobbyId, record);
        resolve(record);
      }
    });

    child.on('message', (msg) => {
      if (!msg || typeof msg !== 'object') return;
      if (msg.type === 'heartbeat') {
        record.lastActivity = Date.now();
      } else if (msg.type === 'resolve') {
        const gameServers = require('./gameServerStore');
        const server = gameServers.find(msg.steamId);
        child.send({
          type: 'resolved',
          reqId: msg.reqId,
          found: !!server,
          address: server ? server.ip : null,
          port: server ? server.gamePort : null,
        });
      }
    });

    child.once('error', (err) => {
      clearTimeout(timeout);
      relays.delete(lobbyId);
      reject(err);
    });

    child.once('exit', () => {
      relays.delete(lobbyId);
    });
  });
}

async function ensureRelay(lobbyId, appId, udpHost) {
  const existing = relays.get(lobbyId);
  if (existing) {
    existing.lastActivity = Date.now();
    return { wsPort: existing.wsPort, udpPort: existing.udpPort, udpHost };
  }
  const record = await spawn(lobbyId, appId);
  return { wsPort: record.wsPort, udpPort: record.udpPort, udpHost };
}

function sweep() {
  const cutoff = Date.now() - IDLE_TIMEOUT_MS;
  for (const [lobbyId, record] of relays) {
    if (record.lastActivity < cutoff) {
      record.child.kill();
      relays.delete(lobbyId);
    }
  }
}

function shutdownAll() {
  for (const record of relays.values()) record.child.kill();
  relays.clear();
}

module.exports = { ensureRelay, sweep, shutdownAll, count: () => relays.size, IDLE_TIMEOUT_MS };
