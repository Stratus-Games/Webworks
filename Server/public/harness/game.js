/* Arena: a small 3D HTML5 multiplayer game.
 *
 * It exists to prove the Webworks pieces end to end in a browser: a lobby created through
 * /Matchmaking, a realtime socket authenticated with the session token, lobby chat carrying
 * player transforms, coin pickups and chat, and member joins/leaves. Nothing here is
 * game-engine specific -- the same calls are what the Unity shims make.
 *
 * The renderer is deliberately hand-rolled: a perspective camera and flat-shaded boxes,
 * cylinders and an octahedron, drawn with canvas paths. That keeps the page a single file
 * with no dependency, while still being real 3D (a view matrix, a near plane, depth
 * sorting) rather than a top-down sprite board.
 *
 * Smoothness rules this file follows:
 *   - movement is integrated from real elapsed time, never "per frame"
 *   - remote players are interpolated toward their last known position
 *   - the canvas is sized to its container in device pixels, so it is never upscaled
 *   - the DOM (player list, chat) changes only when something actually changes
 */
(function () {
  'use strict';

  var SEND_MS = 50;          // 20 transform messages a second
  var LATENCY_MS = 2000;
  // A background tab stops drawing, and browsers throttle its timers, so a player who is
  // still in the room can go quiet for a while. Their token stays for a long time; the
  // roster (member list) keeps them in the room even before their next transform arrives.
  var STALE_MS = 90000;
  var FORGET_MS = 300000;
  var NEAR = 0.35;

  var canvas, ctx;
  var api = null;
  var shared = null;
  var wired = false;
  var socket = null;
  var reconnectTimer = null;
  var reconnectDelay = 800;
  var reconnectAttempts = 0;
  var lobbyId = null;
  var me = null;
  var connected = false;
  var started = false;
  var keys = {};
  var lastSent = 0;
  var lastLatency = 0;
  var latency = null;
  var dirtyList = false;
  var listQueued = false;
  var viewW = 720;
  var viewH = 460;
  var dpr = 1;
  var lastFrame = 0;
  var arenaHalf = 21;

  // Local player: position in the x/z plane, y is up.
  var self = { x: 0, z: 6, vx: 0, vz: 0, facing: Math.PI, speed: 7 };
  var camera = { x: 0, y: 12, z: -9, cx: 0, cy: 0, cz: 0 };
  var players = {};          // steamId -> remote transform + target
  var scores = {};
  var coin = { id: 'seed', x: 6, z: -4, spin: 0 };
  var lastCoinAt = 0;

  function $(id) { return document.getElementById(id); }

  function status(message, isError) {
    var node = $('game-status');
    if (!node) return;
    node.textContent = message || '';
    node.classList.toggle('error', Boolean(isError));
  }

  function setSocketState(text, on) {
    var label = $('game-socket');
    if (label) { label.textContent = text; label.dataset.state = on ? 'on' : 'off'; }
    var dot = $('game-socket-dot');
    if (dot) dot.classList.toggle('on', Boolean(on));
  }

  function hashHue(text) {
    var hash = 0;
    var value = String(text || '');
    for (var i = 0; i < value.length; i++) hash = (hash * 31 + value.charCodeAt(i)) & 0xffffffff;
    return Math.abs(hash) % 360;
  }

  function colorFor(id) {
    return 'hsl(' + hashHue(id) + ', 66%, 58%)';
  }

  // ------------------------------------------------------------------ lifecycle

  function attach(sharedState) {
    api = sharedState.request;
    shared = sharedState.state;
    me = shared.session;
    canvas = $('arena');
    if (canvas && !ctx) {
      ctx = canvas.getContext('2d', { alpha: false });
      resize();
      if (window.ResizeObserver) {
        var observer = new ResizeObserver(function () { resize(); });
        observer.observe(canvas.parentElement || canvas);
      }
      window.addEventListener('resize', resize);
    }

    if (!wired) {
      wired = true;
      wire();
      requestAnimationFrame(frame);
    }

    // ?lobby=<id> is the shareable link the join message prints: land straight in that room.
    var wanted = null;
    try { wanted = new URLSearchParams(location.search).get('lobby'); } catch (err) { wanted = null; }

    if (wanted && wanted !== lobbyId) join(wanted);
    else if (shared.lobbyId && shared.lobbyId !== lobbyId) join(shared.lobbyId);
    else if (lobbyId) renderLobby();
    else draw();
  }

  // Coming back to the tab: a socket that died in the background is reconnected at once
  // instead of waiting for the next backoff step.
  function wake() {
    if (document.visibilityState === 'hidden') return;
    if (started && !connected) {
      reconnectAttempts = 0;
      reconnectDelay = 400;
      connect();
    }
    lastFrame = 0;
  }

  function focus() {
    if (!started) draw();
  }

  function stop() {
    if (reconnectTimer) { clearTimeout(reconnectTimer); reconnectTimer = null; }
    if (socket) { try { socket.close(); } catch (err) { /* ignore */ } socket = null; }
    if (lobbyId && api) {
      var id = lobbyId;
      api('POST', '/Matchmaking/LeaveLobby', { lobbyId: id });
    }
    lobbyId = null;
    players = {};
    scores = {};
    connected = false;
    started = false;
    latency = null;
    if (shared) shared.lobbyId = null;
    var log = $('chat-log');
    if (log) log.innerHTML = '';
    var code = $('join-code');
    if (code) code.value = '';
    setSocketState('not connected', false);
    status('');
    renderLobby();
    draw();
  }

  function wire() {
    $('create-room').addEventListener('click', function () {
      status('Creating a room\u2026');
      api('POST', '/Matchmaking/CreateLobby', { maxMembers: 8 }).then(function (reply) {
        if (!reply.ok || !reply.data) { status('Could not create a room (' + reply.status + ').', true); return; }
        join(reply.data.id);
      });
    });

    $('join-room').addEventListener('click', function () {
      var id = $('join-code').value.trim();
      if (!id) { status('Paste a lobby id first.', true); return; }
      join(id);
    });

    $('leave-room').addEventListener('click', stop);

    $('chat-send').addEventListener('click', sendChat);
    $('chat-input').addEventListener('keydown', function (event) {
      if (event.key === 'Enter') sendChat();
    });

    window.addEventListener('keydown', function (event) {
      if (typing()) return;
      var key = event.key.toLowerCase();
      keys[key] = true;
      if (['arrowup', 'arrowdown', 'arrowleft', 'arrowright', ' '].indexOf(key) !== -1) event.preventDefault();
    });
    window.addEventListener('keyup', function (event) { keys[event.key.toLowerCase()] = false; });
    window.addEventListener('blur', function () { keys = {}; });
    document.addEventListener('visibilitychange', wake);

    // Timers, not animation frames: these keep running (throttled) when the tab is hidden,
    // so a player sitting in another tab stays in the room and stays online.
    setInterval(function () {
      if (!connected || !started) return;
      send({ t: 'pos', x: round2(self.x), z: round2(self.z), f: round2(self.facing), name: me ? me.name : '' });
    }, 1500);
    setInterval(function () {
      if (!connected || !started) return;
      pingServer();
      if (Date.now() - lastRosterAt > 9000) syncRoster();
    }, 2000);

    if (canvas) {
      canvas.addEventListener('click', function () { canvas.focus(); });
      canvas.setAttribute('tabindex', '0');
    }
  }

  function typing() {
    var node = document.activeElement;
    if (!node) return false;
    var tag = node.tagName;
    return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
  }

  function sendChat() {
    var input = $('chat-input');
    var text = input.value.trim();
    if (!text || !connected) return;
    send({ t: 'chat', text: text });
    input.value = '';
  }

  // ------------------------------------------------------------------ networking

  function join(id) {
    if (!api) return;
    started = false;
    players = {};
    scores = {};
    status('Joining ' + id + '\u2026');
    api('POST', '/Matchmaking/JoinLobby', { lobbyId: id }).then(function (reply) {
      if (!reply.ok || !reply.data) {
        status(reply.status === 404 ? 'No lobby with that id.' : 'Could not join (' + reply.status + ').', true);
        return;
      }
      lobbyId = reply.data.id;
      if (shared) shared.lobbyId = lobbyId;
      started = true;
      self.x = 0;
      self.z = 6;
      self.vx = 0;
      self.vz = 0;
      camera.x = self.x;
      camera.z = self.z - 9;
      coin = { id: 'seed', x: 6, z: -4, spin: 0 };
      lastCoinAt = performance.now();
      status('In room ' + lobbyId + ' \u00b7 share the id, or open this page with ?lobby=' + lobbyId);
      $('join-code').value = lobbyId;
      connect();
      renderLobby();
      draw();
    });
  }

  function connect() {
    if (!me || !me.token) return;
    if (reconnectTimer) { clearTimeout(reconnectTimer); reconnectTimer = null; }
    if (socket) { try { socket.close(); } catch (err) { /* ignore */ } }

    var scheme = location.protocol === 'https:' ? 'wss' : 'ws';
    socket = new WebSocket(scheme + '://' + location.host + '/realtime?session=' + encodeURIComponent(me.token));
    var thisSocket = socket;

    thisSocket.onopen = function () {
      if (socket !== thisSocket) return;
      connected = true;
      reconnectAttempts = 0;
      reconnectDelay = 800;
      setSocketState('connected', true);
      send({ t: 'hello', name: me.name, x: self.x, z: self.z, f: self.facing });
      send({ t: 'pos', x: self.x, z: self.z, f: self.facing, name: me.name });
      pingServer();
      lastRosterAt = Date.now();
      syncRoster();
    };

    thisSocket.onclose = function () {
      if (socket !== thisSocket) return;
      connected = false;
      setSocketState('reconnecting', false);
      // Keep the room: the lobby is still there, so retry instead of dropping the player.
      if (started && reconnectAttempts < 8) {
        reconnectAttempts++;
        reconnectTimer = setTimeout(function () {
          reconnectTimer = null;
          connect();
        }, reconnectDelay);
        reconnectDelay = Math.min(8000, reconnectDelay * 1.8);
      } else if (started) {
        setSocketState('disconnected', false);
      }
    };

    thisSocket.onerror = function () {
      if (socket !== thisSocket) return;
      connected = false;
      setSocketState('error', false);
    };

    thisSocket.onmessage = function (event) {
      var message;
      try { message = JSON.parse(event.data); } catch (err) { return; }
      if (!message || !message.type) return;

      if (message.type === 'LobbyChat' && String(message.lobbyId) === String(lobbyId)) {
        handlePayload(message);
        return;
      }

      if (message.type === 'LobbyMemberJoined' && String(message.lobbyId) === String(lobbyId)) {
        if (message.steamId && message.steamId !== (me && me.steamId)) {
          addChat('system', (message.name || 'Someone') + ' joined the room');
          if (!players[message.steamId]) {
            players[message.steamId] = {
              id: message.steamId,
              name: message.name || 'Player',
              x: self.x, z: self.z, tx: self.x, tz: self.z,
              f: 0, at: Date.now(), color: colorFor(message.steamId),
            };
          }
        }
        markLobbyDirty();
        return;
      }

      if (message.type === 'LobbyMemberLeave' || message.type === 'LobbyMemberDisconnected') {
        if (String(message.lobbyId) !== String(lobbyId)) return;
        delete players[message.steamId];
        addChat('system', (message.name || 'Someone') + ' left the room');
        markLobbyDirty();
      }
    };
  }

  function handlePayload(message) {
    var payload;
    try { payload = JSON.parse(message.message || '{}'); } catch (err) { payload = { t: 'chat', text: message.message }; }
    if (!payload.t) return;
    var fromMe = message.steamId === (me && me.steamId);

    if (payload.t === 'pos' && message.steamId && !fromMe) {
      var known = players[message.steamId];
      if (!known) {
        known = players[message.steamId] = {
          id: message.steamId,
          name: message.name || payload.name || 'Player',
          x: clampArena(Number(payload.x) || 0),
          z: clampArena(Number(payload.z) || 0),
          tx: clampArena(Number(payload.x) || 0),
          tz: clampArena(Number(payload.z) || 0),
          f: Number(payload.f) || 0,
          at: Date.now(),
          color: colorFor(message.steamId),
        };
        markLobbyDirty();
      } else {
        known.name = message.name || payload.name || known.name;
        known.tx = clampArena(Number(payload.x) || 0);
        known.tz = clampArena(Number(payload.z) || 0);
        known.f = Number(payload.f) || known.f;
        known.at = Date.now();
      }
      return;
    }

    if (payload.t === 'hello' && message.steamId && !fromMe) {
      var entry = players[message.steamId];
      if (!entry) {
        entry = players[message.steamId] = {
          id: message.steamId,
          name: message.name || payload.name || 'Player',
          x: clampArena(Number(payload.x) || 0),
          z: clampArena(Number(payload.z) || 0),
          tx: clampArena(Number(payload.x) || 0),
          tz: clampArena(Number(payload.z) || 0),
          f: 0,
          at: Date.now(),
          color: colorFor(message.steamId),
        };
        markLobbyDirty();
      }
      entry.at = Date.now();
      entry.name = message.name || payload.name || entry.name;
      return;
    }

    // The coin is identified by id, so two players grabbing it at once cannot score the
    // same coin twice: the second message carries a different starting coin than the
    // one this client already moved on from.
    if (payload.t === 'coin' && payload.id && payload.id !== coin.id) {
      coin.id = String(payload.id);
      coin.x = clampArena(Number(payload.x) || coin.x);
      coin.z = clampArena(Number(payload.z) || coin.z);
      if (payload.by && payload.by !== (me && me.steamId)) {
        scores[payload.by] = (scores[payload.by] || 0) + 1;
        addChat('system', (message.name || 'Someone') + ' grabbed the coin');
        markLobbyDirty();
      }
      return;
    }

    if (payload.t === 'chat' && payload.text) {
      addChat(message.name || 'Player', payload.text);
      return;
    }

    if (payload.t === 'ping' && payload.at) {
      lastLatency = Date.now() - Number(payload.at);
      latency = lastLatency;
      var pingNode = $('game-ping');
      if (pingNode) pingNode.textContent = lastLatency + ' ms';
    }
  }

  function send(payload) {
    if (!socket || socket.readyState !== 1 || !lobbyId) return;
    socket.send(JSON.stringify({
      type: 'LobbyChat',
      lobbyId: lobbyId,
      message: JSON.stringify(payload),
    }));
  }

  function pingServer() {
    if (!connected) return;
    send({ t: 'ping', at: Date.now() });
  }

  // The roster is authoritative for membership; transforms are only what people are doing.
  // Fetching it keeps a backgrounded player's avatar on screen at their last position.
  function syncRoster() {
    if (!api || !lobbyId) return;
    api('GET', '/Matchmaking/LobbyMembers?lobbyId=' + encodeURIComponent(lobbyId)).then(function (reply) {
      if (!reply.ok || !reply.data || !reply.data.members) return;
      var seen = {};
      reply.data.members.forEach(function (member) {
        var id = String(member.steamId);
        seen[id] = true;
        if (id === String(me && me.steamId)) return;
        var entry = players[id];
        if (!entry) {
          players[id] = {
            id: id,
            name: member.name || 'Player',
            x: self.x, z: self.z, tx: self.x, tz: self.z,
            f: 0, at: Date.now(), color: colorFor(id),
          };
          markLobbyDirty();
        } else if (member.name) {
          entry.name = member.name;
        }
      });
      Object.keys(players).forEach(function (id) {
        if (seen[id]) return;
        delete players[id];
        markLobbyDirty();
      });
    });
  }

  // ------------------------------------------------------------------ simulation

  function desiredInput() {
    var ix = 0;
    var iz = 0;
    if (keys.a || keys.arrowleft) ix -= 1;
    if (keys.d || keys.arrowright) ix += 1;
    if (keys.w || keys.arrowup) iz -= 1;
    if (keys.s || keys.arrowdown) iz += 1;
    if (ix && iz) { ix *= Math.SQRT1_2; iz *= Math.SQRT1_2; }
    return { x: ix, z: iz };
  }

  function step(dt, now) {
    if (!started || typing()) {
      self.vx = 0;
      self.vz = 0;
      return;
    }

    var input = desiredInput();
    var accel = 46;
    var damping = 12;
    self.vx += (input.x * self.speed - self.vx) * Math.min(1, accel * dt / self.speed);
    self.vz += (input.z * self.speed - self.vz) * Math.min(1, accel * dt / self.speed);
    if (!input.x) self.vx -= self.vx * Math.min(1, damping * dt);
    if (!input.z) self.vz -= self.vz * Math.min(1, damping * dt);

    var beforeX = self.x;
    var beforeZ = self.z;
    self.x += self.vx * dt;
    self.z += self.vz * dt;

    var limit = arenaHalf - 1.2;
    if (self.x > limit) { self.x = limit; self.vx = 0; }
    if (self.x < -limit) { self.x = -limit; self.vx = 0; }
    if (self.z > limit) { self.z = limit; self.vz = 0; }
    if (self.z < -limit) { self.z = -limit; self.vz = 0; }

    if (Math.abs(self.vx) + Math.abs(self.vz) > 0.4) {
      self.facing = Math.atan2(self.vx, self.vz);
    }

    var moved = beforeX !== self.x || beforeZ !== self.z;
    if (connected && (moved || now - lastSent > 900) && now - lastSent >= SEND_MS) {
      lastSent = now;
      send({ t: 'pos', x: round2(self.x), z: round2(self.z), f: round2(self.facing), name: me ? me.name : '' });
    }

    if (connected && now - lastLatencyCheck > LATENCY_MS) {
      lastLatencyCheck = now;
      pingServer();
    }

    // Coin pickup: whoever gets there first claims it, and the claim carries the next
    // coin so every client agrees on where it reappeared.
    var dx = self.x - coin.x;
    var dz = self.z - coin.z;
    if (connected && dx * dx + dz * dz < 1.6 * 1.6 && now - lastCoinAt > 400) {
      lastCoinAt = now;
      var next = { id: 'c' + Math.random().toString(36).slice(2, 9), x: round2(rand(-arenaHalf + 3, arenaHalf - 3)), z: round2(rand(-arenaHalf + 3, arenaHalf - 3)) };
      coin.id = next.id;
      coin.x = next.x;
      coin.z = next.z;
      scores[me.steamId] = (scores[me.steamId] || 0) + 1;
      send({ t: 'coin', id: next.id, x: next.x, z: next.z, by: me.steamId, name: me.name });
      addChat('system', (me.name || 'You') + ' grabbed the coin');
      markLobbyDirty();
      bumpScore();
    }

    // Remote players glide toward their last transform instead of teleporting on each packet.
    var smoothing = 1 - Math.exp(-12 * dt);
    var nowMs = Date.now();
    Object.keys(players).forEach(function (id) {
      var player = players[id];
      if (!player) return;
      player.x += (player.tx - player.x) * smoothing;
      player.z += (player.tz - player.z) * smoothing;
      if (nowMs - player.at > FORGET_MS) { delete players[id]; markLobbyDirty(); }
    });
  }
  var lastLatencyCheck = 0;
  var lastRosterAt = 0;

  function rand(min, max) { return min + Math.random() * (max - min); }
  function round2(value) { return Math.round(value * 100) / 100; }
  function clampArena(value) { return Math.max(-arenaHalf, Math.min(arenaHalf, value)); }

  // ------------------------------------------------------------------ camera + projection

  function updateCamera(dt) {
    // A slow chase camera: it lags behind the player slightly, which reads as weight
    // without making the controls feel floaty.
    var targetX = self.x;
    var targetZ = self.z - 9.5;
    var follow = 1 - Math.exp(-6 * dt);
    camera.x += (targetX - camera.x) * follow;
    camera.z += (targetZ - camera.z) * follow;
    camera.y = 11.5;

    camera.cx = self.x;
    camera.cy = 1.4;
    camera.cz = self.z;
  }

  function makeView() {
    var fx = camera.cx - camera.x;
    var fy = camera.cy - camera.y;
    var fz = camera.cz - camera.z;
    var flen = Math.hypot(fx, fy, fz) || 1;
    fx /= flen; fy /= flen; fz /= flen;

    // right = forward x worldUp  (worldUp = 0,1,0)
    var rx = -fz;
    var rz = fx;
    var rlen = Math.hypot(rx, rz) || 1;
    rx /= rlen; rz /= rlen;

    // up = right x forward
    var ux = -rz * fy;
    var uy = rz * fx - rx * fz;
    var uz = rx * fy;

    return { ex: camera.x, ey: camera.y, ez: camera.z, fx: fx, fy: fy, fz: fz, rx: rx, ry: 0, rz: rz, ux: ux, uy: uy, uz: uz };
  }

  function project(view, x, y, z) {
    var dx = x - view.ex;
    var dy = y - view.ey;
    var dz = z - view.ez;

    var depth = dx * view.fx + dy * view.fy + dz * view.fz;
    if (depth < NEAR) return null;

    var right = dx * view.rx + dy * view.ry + dz * view.rz;
    var up = dx * view.ux + dy * view.uy + dz * view.uz;

    var focal = (viewH * 0.5) / Math.tan((58 * Math.PI / 180) / 2);
    var scale = focal / depth;
    return { x: viewW * 0.5 + right * scale, y: viewH * 0.5 - up * scale, scale: scale, depth: depth };
  }

  // ------------------------------------------------------------------ rendering

  function resize() {
    if (!canvas) return;
    var rect = canvas.getBoundingClientRect();
    var width = Math.max(240, Math.round(rect.width || 720));
    var ratio = Math.min(2, window.devicePixelRatio || 1);
    var height = Math.round(width * (rect.height && rect.width ? rect.height / rect.width : 0.64));

    viewW = width;
    viewH = height;
    dpr = ratio;
    canvas.width = Math.round(width * ratio);
    canvas.height = Math.round(height * ratio);
    canvas.style.height = height + 'px';
    if (ctx) ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
    draw();
  }

  function frame(timestamp) {
    advance(timestamp || performance.now());
    requestAnimationFrame(frame);
  }

  // One frame without scheduling the next. The loop uses it, and so does a scripted check
  // (a tab that is not being drawn never gets animation frames of its own).
  function advance(timestamp) {
    if (!ctx) return;
    if (canvas && Math.abs(canvas.clientWidth - viewW) > 1) resize();
    var now = timestamp || performance.now();
    var dt = lastFrame ? Math.min(0.05, (now - lastFrame) / 1000) : 0.016;
    lastFrame = now;

    coin.spin += dt * 2.2;
    step(dt, now);
    updateCamera(dt);
    draw();
    if (dirtyList) flushLobby();
  }

  function draw() {
    if (!ctx) return;
    var view = makeView();

    // sky / floor wash
    var backdrop = ctx.createLinearGradient(0, 0, 0, viewH);
    backdrop.addColorStop(0, '#10090c');
    backdrop.addColorStop(0.55, '#0a0608');
    backdrop.addColorStop(1, '#050304');
    ctx.fillStyle = backdrop;
    ctx.fillRect(0, 0, viewW, viewH);

    drawFloor(view);
    drawArenaEdge(view);

    var drawables = [];
    Object.keys(players).forEach(function (id) {
      var player = players[id];
      if (!player) return;
      if (Date.now() - player.at > STALE_MS) return;
      drawables.push({ kind: 'player', depth: depthOf(view, player.x, player.z), data: player });
    });
    drawables.push({ kind: 'player', depth: depthOf(view, self.x, self.z), data: { id: me && me.steamId, name: me ? me.name : 'You', x: self.x, z: self.z, f: self.facing, mine: true } });
    drawables.push({ kind: 'coin', depth: depthOf(view, coin.x, coin.z), data: coin });
    drawables.sort(function (a, b) { return b.depth - a.depth; });

    for (var i = 0; i < drawables.length; i++) {
      var item = drawables[i];
      if (item.kind === 'coin') drawCoin(view, item.data);
      else drawPlayer(view, item.data);
    }

    drawVignette();
    drawOverlay();
  }

  function depthOf(view, x, z) {
    return (x - view.ex) * view.fx + (1 - view.ey) * view.fy + (z - view.ez) * view.fz;
  }

  // A 3D line can have an end behind the camera. Subdividing and drawing only the visible
  // pieces is what makes the near half of the floor grid show up at all.
  function strokeSegment3d(view, ax, ay, az, bx, by, bz, color, width) {
    var steps = 16;
    var previous = null;
    ctx.strokeStyle = color;
    ctx.lineWidth = width || 1;
    ctx.beginPath();
    var drawing = false;
    for (var i = 0; i <= steps; i++) {
      var t = i / steps;
      var point = project(view, ax + (bx - ax) * t, ay + (by - ay) * t, az + (bz - az) * t);
      if (!point) { drawing = false; previous = null; continue; }
      if (!drawing) { ctx.moveTo(point.x, point.y); drawing = true; }
      else ctx.lineTo(point.x, point.y);
      previous = point;
    }
    ctx.stroke();
    return previous;
  }

  function drawFloor(view) {
    // stage floor: a soft pool of light under the arena, so the grid sits on a surface
    var centre = project(view, 0, 0, 0);
    if (centre) {
      var radius = Math.max(80, arenaHalf * centre.scale * 1.25);
      var pool = ctx.createRadialGradient(centre.x, centre.y, radius * 0.05, centre.x, centre.y, radius);
      pool.addColorStop(0, 'rgba(74, 22, 30, 0.55)');
      pool.addColorStop(0.55, 'rgba(40, 12, 16, 0.32)');
      pool.addColorStop(1, 'rgba(10, 6, 7, 0)');
      ctx.save();
      ctx.beginPath();
      ctx.ellipse(centre.x, centre.y, radius, radius * 0.5, 0, 0, Math.PI * 2);
      ctx.fillStyle = pool;
      ctx.fill();
      ctx.restore();
    }

    var stepSize = 2;
    var line = 0;
    for (var x = -arenaHalf; x <= arenaHalf + 0.001; x += stepSize) {
      var major = Math.abs(x) < 0.001;
      strokeSegment3d(view, x, 0, -arenaHalf, x, 0, arenaHalf, major ? 'rgba(235, 41, 51, 0.5)' : 'rgba(204, 20, 36, 0.22)', major ? 1.6 : 1);
      line++;
    }
    for (var z = -arenaHalf; z <= arenaHalf + 0.001; z += stepSize) {
      var centreLine = Math.abs(z) < 0.001;
      strokeSegment3d(view, -arenaHalf, 0, z, arenaHalf, 0, z, centreLine ? 'rgba(235, 41, 51, 0.5)' : 'rgba(204, 20, 36, 0.22)', centreLine ? 1.6 : 1);
    }

    if (centre) {
      ctx.save();
      ctx.beginPath();
      ctx.ellipse(centre.x, centre.y, 3.3 * centre.scale, 1.5 * centre.scale, 0, 0, Math.PI * 2);
      ctx.strokeStyle = 'rgba(235, 41, 51, 0.45)';
      ctx.lineWidth = 2;
      ctx.stroke();
      ctx.restore();
    }
  }

  function drawArenaEdge(view) {
    var corners = [
      [-arenaHalf, -arenaHalf], [arenaHalf, -arenaHalf],
      [arenaHalf, arenaHalf], [-arenaHalf, arenaHalf],
    ];
    ctx.save();
    for (var i = 0; i < corners.length; i++) {
      var a = corners[i];
      var b = corners[(i + 1) % corners.length];
      strokeSegment3d(view, a[0], 0, a[1], b[0], 0, b[1], 'rgba(235, 41, 51, 0.62)', 2);
      // glass wall: only the top edge is drawn, which reads as a low barrier
      strokeSegment3d(view, a[0], 2.4, a[1], b[0], 2.4, b[1], 'rgba(204, 20, 36, 0.22)', 1.5);
    }
    ctx.restore();
  }

  function drawPlayer(view, player) {
    var base = project(view, player.x, 0, player.z);
    if (!base) return;
    var head = project(view, player.x, 1.35, player.z);
    if (!head) return;

    var radius = 0.52 * base.scale;
    var bodyTop = head.y;
    var bodyBottom = base.y;
    var color = player.mine ? '#eb2933' : player.color;
    var far = Math.max(0, Math.min(1, (base.depth - 8) / 26));

    // contact shadow
    ctx.save();
    ctx.globalAlpha = 0.45 * (1 - far * 0.6);
    ctx.beginPath();
    ctx.ellipse(base.x, base.y, radius * 1.15, radius * 0.42, 0, 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(0, 0, 0, 0.65)';
    ctx.fill();
    ctx.restore();

    // body: a cylinder in screen space between the projected base and head
    var bodyGradient = ctx.createLinearGradient(base.x - radius, 0, base.x + radius, 0);
    bodyGradient.addColorStop(0, shade(color, -0.34));
    bodyGradient.addColorStop(0.45, color);
    bodyGradient.addColorStop(1, shade(color, -0.2));

    ctx.beginPath();
    ctx.moveTo(base.x - radius, bodyBottom);
    ctx.lineTo(base.x - radius, bodyTop + radius * 0.42);
    ctx.ellipse(base.x, bodyTop + radius * 0.42, radius, radius * 0.42, 0, Math.PI, Math.PI * 2);
    ctx.lineTo(base.x + radius, bodyBottom);
    ctx.closePath();
    ctx.fillStyle = bodyGradient;
    ctx.fill();

    // feet
    ctx.beginPath();
    ctx.ellipse(base.x, bodyBottom, radius, radius * 0.42, 0, 0, Math.PI, false);
    ctx.fillStyle = shade(color, -0.55);
    ctx.fill();

    // shoulders: a highlight across the top edge
    ctx.beginPath();
    ctx.ellipse(base.x, bodyTop + radius * 0.42, radius, radius * 0.42, 0, 0, Math.PI * 2);
    ctx.fillStyle = shade(color, 0.16);
    ctx.fill();

    // head
    var headRadius = Math.max(2, radius * 0.72);
    var headGradient = ctx.createRadialGradient(base.x - headRadius * 0.35, head.y - headRadius * 0.4, headRadius * 0.2, base.x, head.y, headRadius);
    headGradient.addColorStop(0, '#fdf1f1');
    headGradient.addColorStop(0.35, shade(color, 0.3));
    headGradient.addColorStop(1, shade(color, -0.32));
    ctx.beginPath();
    ctx.arc(base.x, head.y, headRadius, 0, Math.PI * 2);
    ctx.fillStyle = headGradient;
    ctx.fill();

    // facing notch: a small wedge that points where the player is heading
    var notchLength = radius * 1.45;
    var facingX = base.x + Math.sin(player.f || 0) * notchLength;
    var facingY = bodyBottom + Math.cos(player.f || 0) * notchLength * 0.35;
    ctx.beginPath();
    ctx.arc(facingX, facingY + radius * 0.06, Math.max(1.5, radius * 0.22), 0, Math.PI * 2);
    ctx.fillStyle = 'rgba(255, 255, 255, 0.85)';
    ctx.fill();

    if (player.mine) {
      ctx.beginPath();
      ctx.ellipse(base.x, bodyBottom + 1, radius * 1.5, radius * 0.5, 0, 0, Math.PI * 2);
      ctx.strokeStyle = 'rgba(255, 255, 255, 0.5)';
      ctx.lineWidth = 1.5;
      ctx.stroke();
    }

    drawLabel(player.name || 'Player', base.x, head.y - headRadius - 12, player.mine);
  }

  function drawLabel(text, x, y, isMe) {
    ctx.save();
    ctx.font = '600 12px ui-sans-serif, system-ui, -apple-system, "Segoe UI", sans-serif';
    var width = ctx.measureText(text).width;
    ctx.fillStyle = 'rgba(8, 5, 6, 0.72)';
    roundRect(x - width / 2 - 7, y - 11, width + 14, 18, 9);
    ctx.fill();
    ctx.fillStyle = isMe ? '#ffffff' : 'rgba(242, 242, 242, 0.92)';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(text, x, y - 1);
    ctx.restore();
  }

  function drawCoin(view, state) {
    var centre = project(view, state.x, 1.05, state.z);
    if (!centre) return;
    var spin = state.spin;
    var radius = 0.42;
    var top = project(view, state.x, 1.05 + radius * 1.25, state.z);
    var bottom = project(view, state.x, 1.05 - radius * 1.25, state.z);
    if (!top || !bottom) return;

    var ring = [];
    for (var i = 0; i < 4; i++) {
      var angle = spin + (i * Math.PI) / 2;
      ring.push(project(view, state.x + Math.cos(angle) * radius * 1.35, 1.05, state.z + Math.sin(angle) * radius * 1.35));
    }
    if (ring.some(function (p) { return !p; })) return;

    var glow = ctx.createRadialGradient(centre.x, centre.y, 1, centre.x, centre.y, Math.max(12, centre.scale * 0.9));
    glow.addColorStop(0, 'rgba(255, 226, 128, 0.5)');
    glow.addColorStop(1, 'rgba(255, 200, 80, 0)');
    ctx.fillStyle = glow;
    ctx.beginPath();
    ctx.arc(centre.x, centre.y, Math.max(12, centre.scale * 0.9), 0, Math.PI * 2);
    ctx.fill();

    var faces = [];
    for (var f = 0; f < 4; f++) {
      var a = ring[f];
      var b = ring[(f + 1) % 4];
      faces.push({ pts: [top, a, b], depth: (top.depth + a.depth + b.depth) / 3, light: f % 2 === 0 ? 0.28 : 0.06 });
      faces.push({ pts: [bottom, a, b], depth: (bottom.depth + a.depth + b.depth) / 3, light: f % 2 === 0 ? -0.25 : -0.45 });
    }
    faces.sort(function (p, q) { return q.depth - p.depth; });
    faces.forEach(function (face) {
      ctx.beginPath();
      ctx.moveTo(face.pts[0].x, face.pts[0].y);
      ctx.lineTo(face.pts[1].x, face.pts[1].y);
      ctx.lineTo(face.pts[2].x, face.pts[2].y);
      ctx.closePath();
      ctx.fillStyle = shade('#f3c74a', face.light);
      ctx.fill();
      ctx.strokeStyle = 'rgba(120, 78, 8, 0.35)';
      ctx.lineWidth = 1;
      ctx.stroke();
    });

    // floor marker so the coin is easy to find when it sits behind a player
    var mark = project(view, state.x, 0, state.z);
    if (mark) {
      ctx.save();
      ctx.globalAlpha = 0.4;
      ctx.beginPath();
      ctx.ellipse(mark.x, mark.y, 0.4 * mark.scale, 0.18 * mark.scale, 0, 0, Math.PI * 2);
      ctx.strokeStyle = '#f3c74a';
      ctx.lineWidth = 1.5;
      ctx.stroke();
      ctx.restore();
    }
  }

  function drawVignette() {
    var gradient = ctx.createRadialGradient(viewW / 2, viewH * 0.55, viewH * 0.25, viewW / 2, viewH * 0.55, viewH * 0.95);
    gradient.addColorStop(0, 'rgba(0, 0, 0, 0)');
    gradient.addColorStop(1, 'rgba(0, 0, 0, 0.55)');
    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, viewW, viewH);
  }

  function drawOverlay() {
    ctx.save();
    ctx.textAlign = 'center';
    if (!started) {
      ctx.fillStyle = 'rgba(240, 232, 232, 0.82)';
      ctx.font = '600 15px ui-sans-serif, system-ui, -apple-system, "Segoe UI", sans-serif';
      ctx.fillText('Create or join a room to step into the arena', viewW / 2, viewH / 2 - 6);
      ctx.font = '13px ui-sans-serif, system-ui, -apple-system, "Segoe UI", sans-serif';
      ctx.fillStyle = 'rgba(160, 148, 148, 0.9)';
      ctx.fillText('WASD or the arrow keys to move', viewW / 2, viewH / 2 + 18);
    } else if (!connected) {
      ctx.fillStyle = 'rgba(255, 150, 150, 0.92)';
      ctx.font = '600 13px ui-sans-serif, system-ui, -apple-system, "Segoe UI", sans-serif';
      ctx.fillText('Realtime socket reconnecting\u2026', viewW / 2, viewH - 18);
    }
    ctx.restore();
  }

  function shade(color, amount) {
    if (color.charAt(0) === '#') {
      var hex = color.slice(1);
      if (hex.length === 3) hex = hex.charAt(0) + hex.charAt(0) + hex.charAt(1) + hex.charAt(1) + hex.charAt(2) + hex.charAt(2);
      var r = parseInt(hex.slice(0, 2), 16);
      var g = parseInt(hex.slice(2, 4), 16);
      var b = parseInt(hex.slice(4, 6), 16);
      return 'rgb(' + adjust(r, amount) + ', ' + adjust(g, amount) + ', ' + adjust(b, amount) + ')';
    }
    var match = /hsl\(\s*([\d.]+)\s*,\s*([\d.]+)%\s*,\s*([\d.]+)%\s*\)/.exec(color);
    if (!match) return color;
    var lightness = Math.max(0, Math.min(100, Number(match[3]) + amount * 100));
    return 'hsl(' + match[1] + ', ' + match[2] + '%, ' + lightness.toFixed(1) + '%)';
  }

  function adjust(channel, amount) {
    var value = amount >= 0
      ? channel + (255 - channel) * amount
      : channel * (1 + amount);
    return Math.max(0, Math.min(255, Math.round(value)));
  }

  function roundRect(x, y, width, height, radius) {
    ctx.beginPath();
    ctx.moveTo(x + radius, y);
    ctx.lineTo(x + width - radius, y);
    ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
    ctx.lineTo(x + width, y + height - radius);
    ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
    ctx.lineTo(x + radius, y + height);
    ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
    ctx.lineTo(x, y + radius);
    ctx.quadraticCurveTo(x, y, x + radius, y);
  }

  // ------------------------------------------------------------------ DOM

  function markLobbyDirty() {
    dirtyList = true;
    if (listQueued) return;
    listQueued = true;
    requestAnimationFrame(flushLobby);
  }

  function flushLobby() {
    listQueued = false;
    dirtyList = false;
    renderLobby();
  }

  function bumpScore() {
    var node = $('game-score');
    if (node) node.textContent = String(scores[me && me.steamId] || 0);
  }

  function renderLobby() {
    var room = $('game-room');
    if (room) room.textContent = lobbyId ? 'room ' + lobbyId : 'not in a room';

    bumpScore();

    var list = $('player-list');
    if (!list) return;
    var ids = Object.keys(players).filter(function (id) { return Date.now() - players[id].at <= STALE_MS; });
    if (me && me.steamId) ids.push(me.steamId);

    var count = $('game-count');
    if (count) count.textContent = started ? '(' + ids.length + ')' : '';

    list.innerHTML = '';
    if (!ids.length) {
      var empty = document.createElement('li');
      empty.className = 'muted';
      empty.textContent = 'Nobody here yet';
      list.appendChild(empty);
      return;
    }

    ids.sort(function (a, b) {
      if (a === (me && me.steamId)) return -1;
      if (b === (me && me.steamId)) return 1;
      return (scores[b] || 0) - (scores[a] || 0);
    });

    ids.slice(0, 24).forEach(function (id) {
      var item = document.createElement('li');
      var isMe = id === (me && me.steamId);
      var name = isMe ? (me.name || 'You') : ((players[id] && players[id].name) || 'Player');
      item.className = isMe ? 'you' : '';
      var dot = document.createElement('span');
      dot.className = 'player-dot';
      dot.style.background = isMe ? 'var(--accent-bright)' : colorFor(id);
      item.appendChild(dot);
      item.appendChild(document.createTextNode(name));
      var score = document.createElement('span');
      score.className = 'player-score';
      score.textContent = String(scores[id] || 0);
      item.appendChild(score);
      list.appendChild(item);
    });
  }

  function addChat(who, text) {
    var log = $('chat-log');
    if (!log) return;
    var line = document.createElement('p');
    if (who === 'system') {
      line.className = 'muted';
      line.textContent = text;
    } else {
      var name = document.createElement('span');
      name.className = 'who';
      name.textContent = who + ': ';
      line.appendChild(name);
      line.appendChild(document.createTextNode(text));
    }
    log.appendChild(line);
    while (log.childNodes.length > 80) log.removeChild(log.firstChild);
    log.scrollTop = log.scrollHeight;
  }

  // A small read-only view for tests and for anyone poking at the page from the console.
  function probe() {
    return {
      lobbyId: lobbyId,
      started: started,
      connected: connected,
      socket: socket ? socket.readyState : null,
      me: me ? me.steamId : null,
      players: Object.keys(players),
      remote: Object.keys(players).map(function (id) {
        var player = players[id];
        return { id: id, name: player.name, x: round2(player.x), z: round2(player.z), tx: round2(player.tx), tz: round2(player.tz), at: player.at };
      }),
      scores: scores,
      coin: { id: coin.id, x: coin.x, z: coin.z },
      self: { x: round2(self.x), z: round2(self.z) },
      latency: latency,
      frames: lastFrame,
    };
  }

  window.WebworksArena = {
    attach: attach,
    focus: focus,
    stop: stop,
    probe: probe,
    send: send,
    tick: advance,
    draw: draw,
    resize: resize,
  };
})();
