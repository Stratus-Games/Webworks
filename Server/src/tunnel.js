const net = require('net');
const dgram = require('dgram');
const { WebSocketServer } = require('ws');
const { state } = require('./state');
const log = require('./log');

const DEFAULTS = {
  maxStreams: 32,
  maxFrameBytes: 256 * 1024,
  idleMs: 120 * 1000,
  allowAny: false,
  allowPrivate: false,
  allow: [],
};

function normalizeOptions(options) {
  const o = Object.assign({}, DEFAULTS, options || {});
  o.maxStreams = Math.max(1, Math.min(256, Number(o.maxStreams) || DEFAULTS.maxStreams));
  o.maxFrameBytes = Math.max(1024, Math.min(1024 * 1024, Number(o.maxFrameBytes) || DEFAULTS.maxFrameBytes));
  o.idleMs = Math.max(5000, Number(o.idleMs) || DEFAULTS.idleMs);
  o.allow = Array.isArray(o.allow) ? o.allow.map((v) => String(v).trim().toLowerCase()).filter(Boolean) : [];
  return o;
}

function isLoopback(host) {
  const h = String(host || '').toLowerCase();
  return h === 'localhost' || h === '127.0.0.1' || h === '::1' || h === '[::1]' || h === '0.0.0.0';
}

function isPrivateHost(host) {
  const h = String(host || '').toLowerCase();
  if (isLoopback(h)) return true;
  if (/^10\./.test(h) || /^192\.168\./.test(h) || /^172\.(1[6-9]|2\d|3[01])\./.test(h)) return true;
  if (/^169\.254\./.test(h)) return true;
  if (/^f[cd][0-9a-f]{2}:/.test(h) || /^fe80:/.test(h)) return true;
  return false;
}

function allowEntryMatches(entry, host, port) {
  if (!entry) return false;
  if (entry === host) return true;
  if (entry === `${host}:${port}`) return true;
  if (entry.startsWith('*.')) {
    const suffix = entry.slice(1);
    if (host.endsWith(suffix)) return true;
    if (`${host}:${port}`.endsWith(suffix)) return true;
  }
  return false;
}

function registeredServerMatches(host, port) {
  try {
    const gameServers = require('./gameServerStore');
    if (typeof gameServers.findByAddress !== 'function') return false;
    return !!gameServers.findByAddress(host, port);
  } catch {
    return false;
  }
}

function targetAllowed(host, port, options) {
  if (!host || !Number(port) || Number(port) < 1 || Number(port) > 65535) return false;
  if (options.allowAny) return true;
  if (isLoopback(host)) return true;
  if (registeredServerMatches(host, port)) return true;
  for (const entry of options.allow) {
    if (allowEntryMatches(entry, String(host).toLowerCase(), Number(port))) return true;
  }
  if (options.allowPrivate && isPrivateHost(host)) return true;
  return false;
}

function attachTunnel(httpServer, rawOptions) {
  const options = normalizeOptions(rawOptions);
  const wss = new WebSocketServer({ noServer: true, maxPayload: options.maxFrameBytes * 2 });

  const onUpgrade = (req, socket, head) => {
    let pathname = '';
    try { pathname = new URL(req.url, 'http://localhost').pathname; } catch { return; }
    if (pathname !== '/tunnel') return;
    wss.handleUpgrade(req, socket, head, (ws) => wss.emit('connection', ws, req));
  };
  httpServer.on('upgrade', onUpgrade);
  wss.on('close', () => httpServer.off('upgrade', onUpgrade));

  wss.on('connection', (ws, req) => {
    const url = new URL(req.url, 'http://localhost');
    const token = url.searchParams.get('session');
    const session = token && state.sessions.get(token);
    if (!session) {
      ws.close(4001, 'invalid_session');
      return;
    }

    const streams = new Map();
    let nextId = 1;
    let closed = false;

    const send = (message) => {
      if (closed || ws.readyState !== ws.OPEN) return;
      try { ws.send(JSON.stringify(message)); } catch {  }
    };

    const drop = (id, error) => {
      const stream = streams.get(id);
      if (!stream) return;
      streams.delete(id);
      try {
        if (stream.timer) clearTimeout(stream.timer);
        if (stream.kind === 'tcp' || stream.kind === 'accept') stream.socket.destroy();
        if (stream.kind === 'udp') stream.socket.close();
        if (stream.kind === 'listen') stream.server.close();
      } catch {  }
      send({ op: 'close', id, error: error || null });
    };

    const touch = (stream) => {
      if (!stream) return;
      if (stream.timer) clearTimeout(stream.timer);
      stream.timer = setTimeout(() => drop(stream.id, 'idle_timeout'), options.idleMs);
      if (stream.timer.unref) stream.timer.unref();
    };

    const register = (stream) => {
      if (streams.size >= options.maxStreams) return false;
      streams.set(stream.id, stream);
      touch(stream);
      return true;
    };

    const forwardData = (id, chunk) => {
      if (chunk.length > options.maxFrameBytes) {
        drop(id, 'frame_too_large');
        return;
      }
      send({ op: 'data', id, data: chunk.toString('base64') });
    };

    const openTcp = (id, host, port) => {
      const socket = new net.Socket();
      const stream = { id, kind: 'tcp', socket };
      if (!register(stream)) { send({ op: 'close', id, error: 'too_many_streams' }); return; }

      socket.setNoDelay(true);
      socket.connect(port, host, () => {
        send({ op: 'open', id, ok: true, protocol: 'tcp', host, port, localPort: socket.localPort });
      });
      socket.on('data', (chunk) => { touch(stream); forwardData(id, chunk); });
      socket.on('error', (err) => drop(id, err.code || 'socket_error'));
      socket.on('close', () => drop(id, null));
    };

    const openUdp = (id, host, port) => {
      const socket = dgram.createSocket(net.isIPv6(host) ? 'udp6' : 'udp4');
      const stream = { id, kind: 'udp', socket, host, port };
      if (!register(stream)) { send({ op: 'close', id, error: 'too_many_streams' }); return; }

      socket.on('message', (msg) => { touch(stream); forwardData(id, msg); });
      socket.on('error', (err) => drop(id, err.message || 'socket_error'));
      socket.bind(0, () => {
        send({ op: 'open', id, ok: true, protocol: 'udp', host, port, localPort: socket.address().port });
      });
    };

    const openListen = (id, port) => {
      const own = httpServer.address();
      const ownPort = own && typeof own === 'object' ? own.port : 0;
      if (port && (port < 1024 || port === ownPort)) {
        send({ op: 'close', id, error: 'port_not_allowed' });
        return;
      }

      const server = net.createServer();
      const stream = { id, kind: 'listen', server, connections: new Set() };
      if (!register(stream)) { send({ op: 'close', id, error: 'too_many_streams' }); return; }

      server.on('connection', (socket) => {
        const connId = nextId++;
        const conn = { id: connId, kind: 'accept', socket };
        if (!register(conn)) { socket.destroy(); return; }

        socket.setNoDelay(true);
        stream.connections.add(connId);
        send({ op: 'accept', id: connId, listenId: id, host: socket.remoteAddress, port: socket.remotePort });
        socket.on('data', (chunk) => { touch(conn); forwardData(connId, chunk); });
        socket.on('error', () => drop(connId, 'socket_error'));
        socket.on('close', () => drop(connId, null));
      });

      server.on('error', (err) => drop(id, err.code || 'listen_failed'));
      server.listen(port || 0, '0.0.0.0', () => {
        const bound = server.address();
        send({ op: 'listening', id, port: bound ? bound.port : 0 });
      });
    };

    ws.on('message', (raw) => {
      let msg;
      try { msg = JSON.parse(raw.toString()); } catch { return; }
      if (!msg || typeof msg !== 'object') return;

      const id = Number(msg.id) || 0;

      if (msg.op === 'open') {
        if (!id) return;
        if (streams.has(id)) { send({ op: 'close', id, error: 'duplicate_stream' }); return; }
        const host = String(msg.host || '');
        const port = Number(msg.port) || 0;
        if (!targetAllowed(host, port, options)) {
          send({ op: 'close', id, error: 'target_not_allowed' });
          return;
        }
        if (msg.protocol === 'udp') openUdp(id, host, port);
        else openTcp(id, host, port);
        return;
      }

      if (msg.op === 'listen') {
        if (!id) return;
        if (streams.has(id)) { send({ op: 'close', id, error: 'duplicate_stream' }); return; }
        openListen(id, Number(msg.port) || 0);
        return;
      }

      if (msg.op === 'data') {
        const stream = streams.get(id);
        if (!stream || typeof msg.data !== 'string') return;
        const buffer = Buffer.from(msg.data, 'base64');
        if (buffer.length > options.maxFrameBytes) { drop(id, 'frame_too_large'); return; }
        touch(stream);
        try {
          if (stream.kind === 'udp') stream.socket.send(buffer, stream.port, stream.host);
          else stream.socket.write(buffer);
        } catch {
          drop(id, 'write_failed');
        }
        return;
      }

      if (msg.op === 'close') { drop(id, null); return; }
      if (msg.op === 'ping') { send({ op: 'pong' }); return; }
    });

    ws.on('close', () => {
      closed = true;
      for (const id of [...streams.keys()]) {
        const stream = streams.get(id);
        streams.delete(id);
        try {
          if (stream.timer) clearTimeout(stream.timer);
          if (stream.socket) stream.socket.destroy();
          if (stream.server) stream.server.close();
        } catch {  }
      }
    });

    send({ op: 'hello', maxStreams: options.maxStreams, maxFrameBytes: options.maxFrameBytes, idleMs: options.idleMs });
    log.info(`tunnel opened for ${session.name} (${session.steamId})`);
  });

  return wss;
}

module.exports = { attachTunnel, targetAllowed, normalizeOptions, isPrivateHost, DEFAULTS };
