const { WebSocketServer } = require('ws');
const { state } = require('./state');
const { broadcastPersonaChange, noteSocketClosed } = require('./presence');
const log = require('./log');


function socketsOf(steamId) {
  return state.sockets.get(String(steamId)) || null;
}

// Deliver to a socket held by this process only. Federation pushes arrive here so they
// can never bounce back over a link.
function pushLocal(steamId, message) {
  const set = socketsOf(steamId);
  if (!set) return false;
  const json = JSON.stringify(message);
  let delivered = false;
  for (const socket of set) {
    if (socket.readyState === socket.OPEN) {
      try { socket.send(json); delivered = true; } catch {  }
    }
  }
  return delivered;
}

function push(steamId, message) {
  if (pushLocal(steamId, message)) return true;
  // The player is connected to another node server but sits in a lobby here: the
  // event goes over the peer link instead (GWAS networking).
  try {
    return require('./federation').deliver(steamId, message);
  } catch {
    return false;
  }
}

function addSocket(steamId, socket) {
  const id = String(steamId);
  if (!state.sockets.has(id)) state.sockets.set(id, new Set());
  state.sockets.get(id).add(socket);
}

function removeSocket(steamId, socket) {
  const id = String(steamId);
  const set = state.sockets.get(id);
  if (!set) return true;
  set.delete(socket);
  if (set.size === 0) {
    state.sockets.delete(id);
    return true;
  }
  return false;
}

function pushToPeer(fromSession, toSteamId, message) {
  const to = String(toSteamId);
  if (state.appOf.get(to) !== fromSession.appId) return false;
  return push(to, message);
}

function pushToLobby(lobby, message, excludeSteamId = null) {
  for (const memberId of lobby.members) {
    if (memberId === excludeSteamId) continue;
    push(memberId, message);
  }
}

function broadcastPeerGone(steamId, appId) {
  for (const otherId of state.sockets.keys()) {
    if (otherId === steamId) continue;
    if (state.appOf.get(otherId) !== appId) continue;
    push(otherId, { type: 'PeerDisconnected', from: steamId });
  }
}

const HEARTBEAT_MS = 15 * 1000;

function attachRealtime(httpServer) {
  const wss = new WebSocketServer({ noServer: true });

  const onUpgrade = (req, socket, head) => {
    let pathname = '';
    try { pathname = new URL(req.url, 'http://localhost').pathname; } catch { return; }
    if (pathname !== '/realtime') return;
    wss.handleUpgrade(req, socket, head, (ws) => wss.emit('connection', ws, req));
  };
  httpServer.on('upgrade', onUpgrade);

  wss.on('connection', (socket, req) => {
    const url = new URL(req.url, 'http://localhost');
    const token = url.searchParams.get('session');
    const session = state.sessions.get(token);

    if (!session) {
      socket.close(4001, 'invalid_session');
      return;
    }

    addSocket(session.steamId, socket);
    socket.isAlive = true;
    socket.sessionToken = token;
    broadcastPersonaChange(session.steamId, 'Online');

    socket.on('pong', () => { socket.isAlive = true; });

    socket.on('message', (raw) => {
      let msg;
      try {
        msg = JSON.parse(raw.toString());
      } catch {
        return;
      }
      if (!msg || typeof msg !== 'object') return;

      if (msg.type === 'LobbyChat' && msg.lobbyId) {
        const result = require('./chat').send(session, msg.lobbyId, msg.data, msg.message);
        if (result.error) push(session.steamId, { type: 'LobbyChatError', lobbyId: String(msg.lobbyId), error: result.error });
        return;
      }

      if (msg.type === 'FriendMessage' && msg.to && typeof msg.message === 'string') {
        pushToPeer(session, msg.to, {
          type: 'FriendMessage',
          from: session.steamId,
          fromName: session.name,
          message: msg.message.slice(0, 2000),
        });
        return;
      }

      if (msg.type === 'Relay' && msg.to && msg.payload && typeof msg.payload === 'object') {
        pushToPeer(session, msg.to, {
          type: 'Relay',
          from: session.steamId,
          fromName: session.name,
          payload: msg.payload,
        });
        return;
      }

      if (msg.type === 'P2PPacket' && msg.to) {
        pushToPeer(session, msg.to, {
          type: 'P2PPacket',
          from: session.steamId,
          channel: msg.channel ?? 0,
          data: msg.data,
        });
      }
    });

    socket.on('close', () => {
      const last = removeSocket(session.steamId, socket);
      if (!last) return;

      const lobbies = require('./lobbies');
      lobbies.dropPlayer(session.steamId);
      require('./chat').forget(session.steamId);
      // Any lobby this player joined on another node server has to let go too.
      try { require('./federation').playerDisconnected(session.steamId); } catch {  }
      broadcastPeerGone(session.steamId, session.appId);
      // Still online while the tab keeps heartbeating; offline once it stops.
      noteSocketClosed(session.steamId, session.appId);
      log.info(`offline: ${session.name} (${session.steamId})`);
    });
  });

  const heartbeat = setInterval(() => {
    for (const socket of wss.clients) {
      if (socket.isAlive === false) {
        socket.terminate();
        continue;
      }
      socket.isAlive = false;
      socket.ping();
    }
  }, HEARTBEAT_MS);
  if (heartbeat.unref) heartbeat.unref();

  wss.on('close', () => {
    clearInterval(heartbeat);
    httpServer.off('upgrade', onUpgrade);
  });
  return wss;
}

module.exports = { attachRealtime, push, pushLocal, pushToPeer, pushToLobby, socketsOf, addSocket, removeSocket };
