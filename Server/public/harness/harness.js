/* Webworks function tester.
 *
 * Two jobs in one page:
 *   1. the bootstrap the Unity scene draws (sign in / register / guest, then a friends step for accounts)
 *   2. a console that calls every route family this server mounts, plus a tiny realtime arena
 *
 * It talks to the server it is served from, so there is no URL to configure. */
(function () {
  'use strict';

  var MOUNT = '/api/Unity/SteamworksNET';
  var APP_ID = '480';
  var SESSION_KEY = 'webworks_harness_session';
  var ACCOUNT_KEY = 'webworks_harness_account';

  var state = {
    health: null,
    session: null,
    account: null,
    entryMode: 'signin',
    avatarBase64: null,
    lobbyId: null,
    lastRealtime: [],
    ids: {},
    server: null,
  };

  function $(id) { return document.getElementById(id); }

  function el(tag, className, text) {
    var node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined && text !== null) node.textContent = text;
    return node;
  }

  function show(screen) {
    ['entry', 'friends', 'harness'].forEach(function (name) {
      $('screen-' + name).classList.toggle('hidden', name !== screen);
    });
  }

  function setStatus(id, message, isError) {
    var node = $(id);
    if (!node) return;
    node.textContent = message || '';
    node.classList.toggle('error', Boolean(isError));
  }

  // ---------------------------------------------------------------- transport

  function request(method, path, body, options) {
    options = options || {};
    var url = options.absolute ? path : MOUNT + path;
    var headers = {};
    var token = options.token !== undefined ? options.token : (state.session && state.session.token);
    if (token) headers['x-webworks-session'] = token;
    if (options.accountToken) headers['x-webworks-account'] = options.accountToken;
    if (body !== undefined && body !== null && method !== 'GET') headers['Content-Type'] = 'application/json';

    var started = performance.now();
    return fetch(url, {
      method: method,
      headers: headers,
      body: body === undefined || body === null || method === 'GET' ? undefined : JSON.stringify(body),
    }).then(function (resp) {
      return resp.text().then(function (text) {
        var data = text;
        try { data = text ? JSON.parse(text) : null; } catch (err) { /* keep text */ }
        return { ok: resp.ok, status: resp.status, ms: Math.round(performance.now() - started), data: data, url: url };
      });
    }).catch(function (err) {
      return { ok: false, status: 0, ms: Math.round(performance.now() - started), data: { error: String(err) }, url: url };
    });
  }

  // Accounts have always been mounted twice: under the engine prefix (what the Unity shim
  // uses) and under the bare /accounts prefix. Try the engine mount first, then the fallback.
  function accountCall(method, endpoint, body) {
    // Callers pass either a bare endpoint ('/Register') or the mounted one
    // ('/Accounts/Register'); both land on the same two mounts.
    var suffix = String(endpoint || '');
    if (suffix.indexOf('/Accounts') === 0 || suffix.indexOf('/accounts') === 0) suffix = suffix.slice('/Accounts'.length);
    // Account routes authenticate with the account token, not the game session token.
    var options = { accountToken: state.account && state.account.token };
    return request(method, '/Accounts' + suffix, body, options).then(function (reply) {
      if (reply.status !== 404 && reply.status !== 405) return reply;
      return request(method, '/accounts' + suffix, body, { absolute: true, accountToken: options.accountToken });
    });
  }

  // ---------------------------------------------------------------- bootstrap

  function loadCached() {
    try {
      var session = JSON.parse(localStorage.getItem(SESSION_KEY) || 'null');
      var account = JSON.parse(localStorage.getItem(ACCOUNT_KEY) || 'null');
      if (account) state.account = account;
      if (session) state.session = session;
    } catch (err) { /* ignore */ }
  }

  function saveCached() {
    try {
      if (state.session) localStorage.setItem(SESSION_KEY, JSON.stringify(state.session));
      else localStorage.removeItem(SESSION_KEY);
      if (state.account) localStorage.setItem(ACCOUNT_KEY, JSON.stringify(state.account));
      else localStorage.removeItem(ACCOUNT_KEY);
    } catch (err) { /* ignore */ }
  }

  function primeEntry() {
    stopPresence(false);
    state.session = null;
    state.account = null;
    saveCached();
    $('entry-title').textContent = state.entryMode === 'register' ? 'CREATE ACCOUNT' : 'SIGN IN';
    $('signin-btn').textContent = state.entryMode === 'register' ? 'Create Account' : 'Sign In';
    $('mode-toggle').textContent = state.entryMode === 'register'
      ? 'Already have an account? Sign In'
      : 'Need an account? Register';
    $('account-fields').classList.remove('hidden');
    $('display-name-field').classList.toggle('hidden', state.entryMode !== 'register');
    $('avatar-row').classList.toggle('hidden', state.entryMode !== 'register');
    $('password').placeholder = 'Password';
    setStatus('signin-status', '');
    setStatus('guest-status', '');
  }

  function startEntry() {
    show('entry');
    primeEntry();

    if (state.account && state.account.token) {
      $('entry-title').textContent = 'WELCOME BACK';
      $('username').value = state.account.username || '';
      $('password').placeholder = 'Password (leave blank to continue)';
      $('signin-btn').textContent = 'Continue';
      setStatus('signin-status', 'Signed in as ' + (state.account.displayName || state.account.username) + '. Press Continue, or type a password to switch.');
    }

    request('GET', '/health', null, { absolute: true }).then(function (reply) {
      if (!reply.ok || !reply.data) { $('entry-mode').textContent = 'unreachable'; return; }
      state.health = reply.data;
      $('entry-mode').textContent = reply.data.accounts === 'gwas'
        ? 'GWAS (remote) · ' + (reply.data.accountsDetail && reply.data.accountsDetail.url || '')
        : 'local accounts (this server)';
    });
  }

  function finishSession(payload) {
    state.session = {
      token: payload.sessionToken,
      steamId: payload.steamId,
      name: payload.name,
      appId: payload.appId,
      linkedAccount: payload.linkedAccount || null,
      accountSystem: payload.accountSystem || null,
    };
    saveCached();
    // Presence starts with the session, not with the game: people sitting on the friends
    // page, or in another tab, stay Online until the tab actually closes.
    startPresence();
  }

  function startGameSession(accountToken, name) {
    var body = { appId: APP_ID };
    if (accountToken) body.accountToken = accountToken;
    if (name) body.name = name;
    return request('POST', '/User/GuestSession', body).then(function (reply) {
      if (!reply.ok || !reply.data || !reply.data.sessionToken) {
        throw new Error((reply.data && (reply.data.error || reply.data.message)) || 'session failed (' + reply.status + ')');
      }
      finishSession(reply.data);
      return reply.data;
    });
  }

  function signIn() {
    var username = $('username').value.trim();
    var password = $('password').value;
    var hasPassword = Boolean(password);

    if (!hasPassword && state.account && state.account.token
        && (!username || username === state.account.username)) {
      setStatus('signin-status', 'Restoring your session…');
      startGameSession(state.account.token, state.account.displayName)
        .then(function () { showFriends(); })
        .catch(function (err) { setStatus('signin-status', err.message, true); });
      return;
    }

    if (!username || !password) {
      setStatus('signin-status', 'Enter your username and password.', true);
      return;
    }

    $('signin-btn').disabled = true;
    setStatus('signin-status', state.entryMode === 'register' ? 'Creating account…' : 'Signing in…');

    var call = state.entryMode === 'register'
      ? accountCall('POST', '/Register', {
          username: username,
          password: password,
          displayName: $('display-name').value.trim() || undefined,
          avatarBase64: state.avatarBase64 || undefined,
        })
      : accountCall('POST', '/Login', { username: username, password: password });

    call.then(function (reply) {
      if (!reply.ok || !reply.data || !reply.data.token) {
        throw new Error(friendlyAuthError(reply));
      }
      var account = reply.data.account || {};
      state.account = {
        token: reply.data.token,
        username: account.username || username,
        displayName: account.displayName || username,
      };
      saveCached();
      $('username').value = state.account.username;
      $('password').value = '';
      return startGameSession(state.account.token, state.account.displayName);
    }).then(function () {
      showFriends();
    }).catch(function (err) {
      setStatus('signin-status', err.message, true);
    }).finally(function () {
      $('signin-btn').disabled = false;
    });
  }

  function friendlyAuthError(reply) {
    if (!reply) return 'Could not reach the server.';
    var code = reply.data && reply.data.error;
    var map = {
      bad_credentials: 'Incorrect username or password.',
      no_such_account: 'No account with that username.',
      invalid_username: 'Use 3-32 letters, numbers, dots, dashes or underscores, starting with a letter or number.',
      weak_password: 'Password must be 6-200 characters.',
      invalid_password: 'Password must be 6-200 characters.',
      username_taken: 'That username is already taken.',
      avatar_too_large: 'That icon is too large.',
      invalid_avatar: 'That icon is not a PNG, JPEG or WebP image.',
      rate_limited: 'Too many attempts. Wait a minute.',
      accounts_unavailable: 'The accounts service is unavailable.',
    };
    // A 502 without a code is the tunnel or the server dying mid-request, not a rejected
    // sign-in. Say so, because "Bad Gateway" sends people looking at the wrong thing.
    if (!code && reply.status === 502) {
      return 'The server behind the tunnel did not answer (502). Check the server console, then try again.';
    }
    return map[code] || ('Something went wrong (' + reply.status + ').');
  }

  function continueAsGuest() {
    var name = $('guest-name').value.trim();
    if (!name) {
      setStatus('guest-status', 'Type a name first.', true);
      return;
    }
    $('guest-btn').disabled = true;
    setStatus('guest-status', 'Connecting…');
    startGameSession(null, name)
      .then(function () { enterHarness(); })
      .catch(function (err) { setStatus('guest-status', err.message, true); })
      .finally(function () { $('guest-btn').disabled = false; });
  }

  // ---------------------------------------------------------------- friends step
  //
  // The friends page is live: it polls while it is on screen, and a background socket
  // (the same one that carries presence) pushes FriendListChanged, PersonaStateChange and
  // LobbyInvite so both sides of a request update within a moment instead of on reload.

  var GRAPH_POLL_MS = 4000;
  var HEARTBEAT_MS = 20000;

  var onFriendsScreen = false;
  var graphSignature = null;
  var graphRefreshTimer = null;
  var friendsPoll = null;
  var invitesPoll = null;
  var knownFriends = {};
  var avatarCache = {};
  var invitedTo = {};
  var lastInvites = [];

  var presenceTimer = null;
  var presenceSocket = null;
  var presenceRetry = null;

  // ---------------------------------------------------------------- presence

  function heartbeat() {
    if (!state.session || !state.session.token) return;
    request('POST', '/User/Heartbeat', {});
  }

  function startPresence() {
    if (!state.session || !state.session.token) return;
    heartbeat();
    if (!presenceTimer) presenceTimer = setInterval(heartbeat, HEARTBEAT_MS);
    connectPresence();
  }

  function connectPresence() {
    if (!state.session || !state.session.token) return;
    if (presenceSocket && (presenceSocket.readyState === 0 || presenceSocket.readyState === 1)) return;

    var scheme = location.protocol === 'https:' ? 'wss' : 'ws';
    var socket = new WebSocket(scheme + '://' + location.host + '/realtime?session=' + encodeURIComponent(state.session.token));
    presenceSocket = socket;

    socket.onopen = function () {
      var live = $('friends-live');
      if (live) live.classList.add('live');
    };

    socket.onmessage = function (event) {
      var message;
      try { message = JSON.parse(event.data); } catch (err) { return; }
      if (!message || !message.type) return;

      if (message.type === 'LobbyInvite') {
        if (onFriendsScreen) {
          loadInvites({ flash: true });
          setStatus('friends-status', (message.fromName || 'A friend') + ' invited you to their room.');
        }
        return;
      }

      if (message.type === 'FriendListChanged' || message.type === 'PersonaStateChange' || message.type === 'FriendMessage') {
        if (onFriendsScreen) scheduleGraphRefresh();
        return;
      }

      if (message.type === 'FriendRequest' || message.type === 'FriendRequestAccepted') {
        if (onFriendsScreen) scheduleGraphRefresh();
      }
    };

    socket.onclose = function () {
      if (presenceSocket !== socket) return;
      presenceSocket = null;
      var live = $('friends-live');
      if (live) live.classList.remove('live');
      if (!state.session) return;
      clearTimeout(presenceRetry);
      presenceRetry = setTimeout(connectPresence, 3000);
    };

    socket.onerror = function () {
      var live = $('friends-live');
      if (live) live.classList.remove('live');
    };
  }

  function sendOffline() {
    if (!state.session || !state.session.token) return;
    try {
      fetch(MOUNT + '/User/Offline', {
        method: 'POST',
        headers: { 'x-webworks-session': state.session.token, 'Content-Type': 'application/json' },
        body: '{}',
        keepalive: true,
      });
    } catch (err) { /* the heartbeat window covers a failed beacon */ }
  }

  // offline=true is the tab going away; without it the session keeps its presence for the
  // next screen (for example a guest who signed out and signs back in).
  function stopPresence(offline) {
    if (presenceTimer) { clearInterval(presenceTimer); presenceTimer = null; }
    clearTimeout(presenceRetry);
    presenceRetry = null;
    if (presenceSocket) {
      var socket = presenceSocket;
      presenceSocket = null;
      try { socket.close(); } catch (err) { /* ignore */ }
    }
    if (offline) sendOffline();
  }

  // ---------------------------------------------------------------- friends screen

  function showFriends() {
    show('friends');
    onFriendsScreen = true;
    $('friends-who').textContent = (state.account && (state.account.displayName || state.account.username))
      || (state.session && state.session.name) || '';
    setStatus('friends-status', '');
    startFriendsRefresh();
    loadFriends({ soft: false });
    loadInvites();
  }

  function leaveFriendsScreen() {
    onFriendsScreen = false;
    if (friendsPoll) { clearInterval(friendsPoll); friendsPoll = null; }
    if (invitesPoll) { clearInterval(invitesPoll); invitesPoll = null; }
    clearTimeout(graphRefreshTimer);
    graphRefreshTimer = null;
  }

  function startFriendsRefresh() {
    if (!friendsPoll) {
      friendsPoll = setInterval(function () {
        if (!onFriendsScreen) return;
        loadFriends({ soft: true });
      }, GRAPH_POLL_MS);
    }
    if (!invitesPoll) {
      invitesPoll = setInterval(function () {
        if (!onFriendsScreen) return;
        loadInvites();
      }, GRAPH_POLL_MS);
    }
  }

  function scheduleGraphRefresh() {
    clearTimeout(graphRefreshTimer);
    graphRefreshTimer = setTimeout(function () {
      graphRefreshTimer = null;
      if (onFriendsScreen) loadFriends({ soft: true });
    }, 400);
  }

  function loadFriends(options) {
    var soft = Boolean(options && options.soft);
    if (!soft) {
      $('friend-list').innerHTML = '<p class="empty">Loading friends…</p>';
    }
    return request('GET', '/Friends/Graph').then(function (reply) {
      if (!reply.ok || !reply.data) {
        if (soft) return;
        $('friend-list').innerHTML = '';
        $('friend-list').appendChild(el('p', 'empty', 'Could not load friends (' + reply.status + '). You can still continue.'));
        $('friends-summary').textContent = 'You can still continue to the game.';
        return;
      }
      renderFriends(reply.data);
    });
  }

  function avatarFor(username, image) {
    if (!username) return;
    var cached = avatarCache[username];
    if (cached) { image.src = cached; return; }
    image.dataset.pending = username;
    request('GET', '/Friends/Avatar?username=' + encodeURIComponent(username)).then(function (reply) {
      if (!reply.ok || !reply.data || !reply.data.data) return;
      var mime = reply.data.mime || 'image/png';
      var url = 'data:' + mime + ';base64,' + reply.data.data;
      avatarCache[username] = url;
      if (image.dataset && image.dataset.pending === username) image.src = url;
    });
  }

  function friendRow(entry) {
    var item = el('div', 'friend-row' + (entry.online ? ' is-online' : ''));

    var avatar = el('div', 'friend-avatar');
    var image = document.createElement('img');
    image.alt = '';
    avatar.appendChild(image);
    item.appendChild(avatar);
    avatarFor(entry.username, image);

    var body = el('div', 'friend-body');
    var name = el('span', 'name' + (entry.online ? ' online' : ''));
    name.appendChild(el('span', 'dot'));
    name.appendChild(document.createTextNode(entry.name || entry.username || 'Player'));
    body.appendChild(name);
    if (entry.detail) body.appendChild(el('span', 'detail', entry.detail));
    item.appendChild(body);

    if (entry.action) {
      var button = el('button', 'row-btn' + (entry.actionStyle ? ' ' + entry.actionStyle : ''), entry.actionLabel);
      button.type = 'button';
      button.addEventListener('click', entry.action);
      item.appendChild(button);
    }

    return item;
  }

  function renderFriends(graph) {
    var signature = JSON.stringify({
      a: (graph.accepted || []).map(function (f) { return [f.username, f.personaState, f.game, f.name, f.displayName]; }),
      i: graph.incoming || [],
      o: graph.outgoing || [],
    });
    // Polling every few seconds must not rebuild an identical list: that is what made the
    // page flicker and drop clicks before.
    if (signature === graphSignature) return;
    graphSignature = signature;

    var accepted = (graph.accepted || []).slice().sort(function (a, b) {
      var ao = a.personaState === 'Online' ? 0 : 1;
      var bo = b.personaState === 'Online' ? 0 : 1;
      if (ao !== bo) return ao - bo;
      return String(a.displayName || a.name || a.username).localeCompare(String(b.displayName || b.name || b.username));
    });
    var incoming = graph.incoming || [];
    var outgoing = graph.outgoing || [];

    knownFriends = {};
    var list = $('friend-list');
    list.innerHTML = '';

    accepted.forEach(function (friend) {
      knownFriends[friend.username] = friend.steamId;
      var online = friend.personaState === 'Online';
      var detail = friend.game ? 'in game' : (online ? 'online' : (friend.personaState || 'offline').toLowerCase());
      list.appendChild(friendRow({
        username: friend.username,
        name: friend.displayName || friend.name || friend.username,
        detail: invitedTo[friend.username] ? 'invited · ' + detail : detail,
        online: online,
        action: online ? function () { inviteFriend(friend.username, friend.steamId); } : null,
        actionLabel: invitedTo[friend.username] ? 'Invited' : 'Invite',
        actionStyle: invitedTo[friend.username] ? 'quiet' : null,
      }));
    });

    incoming.forEach(function (raw) {
      var username = typeof raw === 'string' ? raw : raw.username;
      if (!username) return;
      list.appendChild(friendRow({
        username: username,
        name: username,
        detail: 'wants to be friends',
        online: false,
        action: function () { acceptFriend(username); },
        actionLabel: 'Accept',
      }));
    });

    outgoing.forEach(function (raw) {
      var username = typeof raw === 'string' ? raw : raw.username;
      if (!username) return;
      list.appendChild(friendRow({
        username: username,
        name: username,
        detail: 'request sent',
        online: false,
      }));
    });

    if (!accepted.length && !incoming.length && !outgoing.length) {
      list.appendChild(el('p', 'empty', 'No friends yet. Add someone by username below.'));
    }

    var online = accepted.filter(function (friend) { return friend.personaState === 'Online'; }).length;
    $('friends-summary').textContent = accepted.length + (accepted.length === 1 ? ' friend' : ' friends')
      + ' \u00b7 ' + online + ' online'
      + (incoming.length ? ' \u00b7 ' + incoming.length + ' waiting' : '');
  }

  function acceptFriend(username) {
    request('POST', '/Friends/Accept', { username: username }).then(function (reply) {
      setStatus('friends-status', reply.ok ? 'Accepted ' + username + '.' : 'Could not accept ' + username + ' (' + reply.status + ').', !reply.ok);
      graphSignature = null;
      loadFriends({ soft: true });
    });
  }

  function addFriend() {
    var username = $('add-friend').value.trim();
    if (!username) { setStatus('friends-status', 'Type the username you want to add.', true); return; }
    setStatus('friends-status', 'Sending a request to ' + username + '\u2026');
    request('POST', '/Friends/Add', { username: username }).then(function (reply) {
      if (!reply.ok) {
        var code = reply.data && reply.data.error;
        var map = {
          no_such_account: 'No account with that username.',
          cannot_friend_yourself: 'That is you.',
          rate_limited: 'Too many requests. Wait a moment.',
          not_signed_in: 'Sign in with an account first.',
        };
        setStatus('friends-status', map[code] || ('Could not add that player (' + reply.status + ').'), true);
        return;
      }
      $('add-friend').value = '';
      setStatus('friends-status', 'Friend request sent to ' + username + '.');
      graphSignature = null;
      loadFriends({ soft: true });
    });
  }

  // ---------------------------------------------------------------- invites

  function ensureLobby() {
    if (state.lobbyId) return Promise.resolve(state.lobbyId);
    return request('POST', '/Matchmaking/CreateLobby', { maxMembers: 8 }).then(function (reply) {
      if (!reply.ok || !reply.data || !reply.data.id) throw new Error('Could not open a room (' + reply.status + ').');
      state.lobbyId = reply.data.id;
      state.ids.lobbyId = reply.data.id;
      return reply.data.id;
    });
  }

  function inviteFriend(username, steamId) {
    var friendSteamId = steamId || knownFriends[username];
    if (!friendSteamId) {
      setStatus('friends-status', 'That friend has no steam id yet. Refresh and try again.', true);
      return;
    }
    setStatus('friends-status', 'Opening your room\u2026');
    ensureLobby().then(function (lobbyId) {
      return request('POST', '/Matchmaking/InviteFriend', { lobbyId: lobbyId, friendSteamId: friendSteamId });
    }).then(function (reply) {
      if (!reply.ok) {
        var code = reply.data && reply.data.error;
        setStatus('friends-status', code === 'cannot_invite_yourself' ? 'That is you.' : ('Could not invite ' + username + ' (' + reply.status + ').'), true);
        return;
      }
      invitedTo[username] = true;
      graphSignature = null;
      loadFriends({ soft: true });
      setStatus('friends-status', 'Invited ' + username + '. Room code ' + state.lobbyId + '.');
    }).catch(function (err) {
      setStatus('friends-status', err.message, true);
    });
  }

  function loadInvites(options) {
    return request('GET', '/Matchmaking/Invites').then(function (reply) {
      if (!reply.ok || !reply.data) return;
      lastInvites = reply.data.invites || [];
      renderInvites();
      if (options && options.flash && lastInvites.length) {
        var node = $('invite-list');
        if (node) {
          node.classList.remove('flash');
          // restart the animation even when the class was already there
          void node.offsetWidth;
          node.classList.add('flash');
        }
      }
    });
  }

  function renderInvites() {
    var node = $('invite-list');
    if (!node) return;
    node.innerHTML = '';
    node.classList.toggle('hidden', !lastInvites.length);
    lastInvites.forEach(function (invite) {
      var row = el('div', 'invite-row');
      var text = el('div', 'invite-text');
      text.appendChild(el('strong', null, invite.fromName || 'A friend'));
      text.appendChild(document.createTextNode(' invited you'));
      text.appendChild(el('span', 'invite-detail', invite.memberCount ? invite.memberCount + ' in room' : 'room ready'));
      row.appendChild(text);

      var join = el('button', 'row-btn', 'Join');
      join.type = 'button';
      join.addEventListener('click', function () { joinInvite(invite); });
      row.appendChild(join);

      var dismiss = el('button', 'row-btn quiet', '\u00d7');
      dismiss.type = 'button';
      dismiss.title = 'Dismiss';
      dismiss.addEventListener('click', function () { dismissInvite(invite); });
      row.appendChild(dismiss);

      node.appendChild(row);
    });
  }

  function joinInvite(invite) {
    setStatus('friends-status', 'Joining ' + (invite.fromName || 'the room') + '\u2026');
    request('POST', '/Matchmaking/JoinLobby', { lobbyId: invite.lobbyId }).then(function (reply) {
      if (!reply.ok || !reply.data) {
        setStatus('friends-status', reply.status === 404 ? 'That room has closed.' : ('Could not join (' + reply.status + ').'), true);
        loadInvites();
        return;
      }
      state.lobbyId = reply.data.id;
      state.ids.lobbyId = reply.data.id;
      lastInvites = lastInvites.filter(function (entry) { return entry.lobbyId !== invite.lobbyId; });
      renderInvites();
      request('POST', '/Matchmaking/Invites/Dismiss', { lobbyId: invite.lobbyId });
      setStatus('friends-status', 'Joined ' + (invite.fromName || 'the') + '\'s room.');
      enterHarness('game');
    });
  }

  function dismissInvite(invite) {
    lastInvites = lastInvites.filter(function (entry) { return entry.lobbyId !== invite.lobbyId; });
    renderInvites();
    request('POST', '/Matchmaking/Invites/Dismiss', { lobbyId: invite.lobbyId });
  }


  // ---------------------------------------------------------------- harness shell

  function enterHarness(startTab) {
    leaveFriendsScreen();
    show('harness');
    selectTab(startTab || 'console');
    var who = state.session ? state.session.name + ' · ' + state.session.steamId : '';
    $('whoami').textContent = who;
    renderGroups();
    selectGroup('Server');
    renderSessionFacts();
    loadHealth();
    if (window.WebworksArena) window.WebworksArena.attach({ request: request, mount: MOUNT, state: state });
  }

  function signOut() {
    var token = state.session && state.session.token;
    function clear() {
      stopPresence(true);
      state.session = null;
      state.account = null;
      state.lobbyId = null;
      saveCached();
      if (window.WebworksArena) window.WebworksArena.stop();
      startEntry();
    }
    if (!token) { clear(); return; }
    var path = state.account ? '/accounts/Logout' : null;
    if (path) request('POST', path, {}, { absolute: true }).finally(clear);
    else clear();
  }

  // ---------------------------------------------------------------- catalogue

  function lobby() { return state.lobbyId || ''; }
  function steamId() { return state.session ? state.session.steamId : ''; }

  // Values captured from earlier calls, so a card that needs an id (a lobby, a party, a
  // broadcast, a workshop item) works when it is run after the card that creates it.
  function idOf(key, fallback) {
    var value = state.ids[key];
    if (value === undefined || value === null || value === '') return fallback === undefined ? '' : fallback;
    return typeof value === 'string' ? value : String(value);
  }

  function serverToken() { return state.server && state.server.token ? state.server.token : null; }

  // Dedicated server cards sign one in on demand, so they work on their own and after the
  // LogOff card has dropped the previous token.
  function ensureServerToken() {
    if (serverToken()) return Promise.resolve(serverToken());
    return request('POST', '/GameServer/LogOn', { appId: APP_ID, gamePort: 27015, queryPort: 27016, dedicated: true }).then(function (reply) {
      if (!reply.ok || !reply.data || !reply.data.sessionToken) return null;
      state.server = { token: reply.data.sessionToken, serverId: reply.data.serverId || null };
      state.ids.serverId = reply.data.serverId || state.ids.serverId;
      renderSessionFacts();
      return state.server.token;
    });
  }

  // A real 1x1 transparent PNG, so the screenshot and avatar cards upload something the
  // server actually accepts instead of a string that is not an image.
  var TINY_PNG = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=';

  var CATALOG = [
    {
      group: 'Server',
      items: [
        { method: 'GET', path: '/health', absolute: true, desc: 'Everything the server reports: accounts, networking, engines, live counters' },
        { method: 'GET', path: '/status', absolute: true, desc: 'Same payload as /health' },
      ],
    },
    {
      group: 'Session',
      items: [
        { method: 'POST', path: '/User/GuestSession', desc: 'Create a session (guest, or linked to an account token)', body: function () { return { appId: APP_ID, name: 'Harness guest ' + Math.floor(Math.random() * 900 + 100) }; } },
        { method: 'GET', path: '/User/Profile', desc: 'This session: steam id, name, account, avatar' },
        { method: 'GET', path: '/User/Status', desc: 'Session and persona status' },
        { method: 'POST', path: '/User/PersonaState', desc: 'Set the persona state the server publishes to friends', body: function () { return { personaState: 'Away' }; } },
        { method: 'POST', path: '/User/AuthTicket', desc: 'Auth ticket for a dedicated server to validate', capture: ['ticket'] },
        { method: 'GET', path: '/User/DurationControl', desc: 'Playtime / duration control report' },
        { method: 'GET', path: '/User/PersonaState', desc: 'Persona state and name this server publishes for you' },
        { method: 'POST', path: '/User/LinkAccount', desc: 'Attach an account to this guest session', body: function () { return { accountToken: state.account ? state.account.token : '' }; } },
        { method: 'POST', path: '/User/BeginAuthSession', desc: 'Validate the ticket from the AuthTicket card', body: function () { return { ticket: idOf('ticket') }; } },
        { method: 'POST', path: '/User/EndAuthSession', desc: 'Close a validated auth session', body: function () { return { ticket: idOf('ticket') }; } },
        { method: 'POST', path: '/User/EncryptedAppTicket', desc: 'HMAC ticket that folds in caller data', body: function () { return { data: 'harness payload' }; } },
      ],
    },
    {
      group: 'Accounts',
      items: [
        { method: 'GET', path: '/Accounts/Mode', desc: 'local or gwas, and where the service lives' },
        { method: 'POST', path: '/Accounts/Register', desc: 'Create an account; the account cards below then use its token', body: function () { return { username: 'tester' + Math.floor(Math.random() * 9000 + 1000), password: 'hunter22', displayName: 'Tester' }; }, captureAccount: true },
        { method: 'POST', path: '/Accounts/Login', desc: 'Sign in and receive an account token', body: function () { return { username: state.account ? state.account.username : 'tester', password: 'hunter22' }; } },
        { method: 'GET', path: '/Accounts/Me', desc: 'The signed-in account', account: true },
        { method: 'PATCH', path: '/Accounts/Me', desc: 'Update display name / persona', account: true, body: function () { return { displayName: 'Renamed by the tester' }; } },
        { method: 'GET', path: '/Accounts/Friends', desc: 'Account-level friend graph', account: true },
        { method: 'POST', path: '/Accounts/Friends/Add', desc: 'Request an account-level friendship', account: true, body: function () { return { username: 'someone' }; } },
        { method: 'POST', path: '/Accounts/Friends/Accept', desc: 'Accept an account-level friend request', account: true, body: function () { return { username: 'someone' }; } },
        { method: 'POST', path: '/Accounts/Friends/Remove', desc: 'Remove an account-level friend', account: true, body: function () { return { username: 'someone' }; } },
        { method: 'POST', path: '/Accounts/Friends/Block', desc: 'Block an account', account: true, body: function () { return { username: 'someone' }; } },
        { method: 'POST', path: '/Accounts/Friends/Unblock', desc: 'Unblock an account', account: true, body: function () { return { username: 'someone' }; } },
        { method: 'GET', path: '/Accounts/Search?q=test', desc: 'Search accounts by username', account: true },
        { method: 'GET', path: '/Accounts/Avatar', desc: 'Avatar bytes or JSON for a username', account: true, dynamicPath: function () { return '/Accounts/Avatar?username=' + encodeURIComponent(state.account ? state.account.username : 'test'); } },
        { method: 'DELETE', path: '/Accounts/Me', desc: 'Delete this account. Type the password in the body to actually delete it', account: true, body: function () { return { password: '' }; } },
        { method: 'POST', path: '/Accounts/Password', desc: 'Change password. This revokes every account token, so run it before the Logout card', account: true, body: function () { return { currentPassword: 'hunter22', newPassword: 'hunter22' }; } },
        { method: 'POST', path: '/Accounts/Logout', desc: 'Drop every token this account issued (answers 401 once the password card revoked them)', account: true, body: function () { return {}; } },
      ],
    },
    {
      group: 'Friends',
      items: [
        { method: 'GET', path: '/Friends/List', desc: 'Friends with presence, game and lobby' },
        { method: 'GET', path: '/Friends/Graph', desc: 'Accepted / incoming / outgoing / blocked' },
        { method: 'GET', path: '/Friends/Search?q=test', desc: 'Search players you can add' },
        { method: 'POST', path: '/Friends/Add', desc: 'Send a friend request', body: function () { return { username: 'someone' }; } },
        { method: 'POST', path: '/Friends/Accept', desc: 'Accept a friend request', body: function () { return { username: 'someone' }; } },
        { method: 'GET', path: '/Friends/Online', desc: 'Every player connected right now' },
        { method: 'POST', path: '/Friends/Message', desc: 'Direct message another player', body: function () { return { to: '', message: 'hello from the tester' }; } },
        { method: 'POST', path: '/Friends/GameInvite', desc: 'Invite a friend into your game', body: function () { return { to: '', connect: '+connect 127.0.0.1:27015' }; } },
        { method: 'POST', path: '/Friends/RichPresence', desc: 'Publish one rich presence key', body: function () { return { key: 'status', value: 'testing Webworks' }; } },
        { method: 'GET', path: '/Friends/RichPresence', desc: 'Read rich presence' },
        { method: 'POST', path: '/Friends/Follow', desc: 'Follow a player', body: function () { return { steamId: '' }; } },
        { method: 'GET', path: '/Friends/Followers', desc: 'Followers of a player', dynamicPath: function () { return '/Friends/Followers?steamId=' + steamId(); } },
        { method: 'GET', path: '/Friends/Following', desc: 'Players this session follows' },
        { method: 'POST', path: '/Friends/Unfollow', desc: 'Stop following a player', body: function () { return { username: 'someone' }; } },
        { method: 'GET', path: '/Friends/IsFollowing?username=test', desc: 'Whether this session follows that account' },
        { method: 'POST', path: '/Friends/Remove', desc: 'Remove a friend', body: function () { return { username: 'someone' }; } },
        { method: 'POST', path: '/Friends/Block', desc: 'Block a player', body: function () { return { username: 'someone' }; } },
        { method: 'POST', path: '/Friends/RequestUserInfo', desc: 'Refresh one player\'s info (a no-op: this server is always live)' , body: function () { return { steamId: steamId() }; } },
        { method: 'GET', path: '/Friends/PlayedWith', desc: 'Players recently played with' },
        { method: 'GET', path: '/Friends/Avatar?username=test', desc: 'Friend avatar bytes as RGBA' },
        { method: 'GET', path: '/Friends/RichPresence/Other', desc: 'A friend\'s rich presence key', dynamicPath: function () { return '/Friends/RichPresence/Other?steamid=' + steamId() + '&key=status'; } },
        { method: 'DELETE', path: '/Friends/RichPresence', desc: 'Clear every rich presence key' },
      ],
    },
    {
      group: 'Lobbies',
      items: [
        { method: 'POST', path: '/Matchmaking/CreateLobby', desc: 'Create a lobby (also used by the arena)', body: function () { return { maxMembers: 8 }; }, saveLobby: true },
        { method: 'GET', path: '/Matchmaking/Lobby', desc: 'Lobby snapshot', dynamicPath: function () { return '/Matchmaking/Lobby?lobbyId=' + lobby(); } },
        { method: 'GET', path: '/Matchmaking/LobbyMembers', desc: 'Members of a lobby', dynamicPath: function () { return '/Matchmaking/LobbyMembers?lobbyId=' + lobby(); } },
        { method: 'GET', path: '/Matchmaking/LobbyData', desc: 'Lobby key/value data', dynamicPath: function () { return '/Matchmaking/LobbyData?lobbyId=' + lobby(); } },
        { method: 'POST', path: '/Matchmaking/LobbyData', desc: 'Set one lobby data key', body: function () { return { lobbyId: lobby(), key: 'map', value: 'arena' }; } },
        { method: 'GET', path: '/Matchmaking/MemberData', desc: 'Per-member data in a lobby', dynamicPath: function () { return '/Matchmaking/MemberData?lobbyId=' + lobby(); } },
        { method: 'POST', path: '/Matchmaking/ChatSend', desc: 'Send a lobby chat message over HTTP', body: function () { return { lobbyId: lobby(), message: 'hello lobby' }; } },
        { method: 'POST', path: '/Matchmaking/MemberData', desc: 'Set one of your member data keys', body: function () { return { lobbyId: lobby(), key: 'ready', value: true }; } },
        { method: 'DELETE', path: '/Matchmaking/LobbyData', desc: 'Remove one lobby data key', body: function () { return { lobbyId: lobby(), key: 'map' }; } },
        { method: 'POST', path: '/Matchmaking/LobbyVisibility', desc: 'Set visibility (public, friends, private, invisible)', body: function () { return { lobbyId: lobby(), mode: 'public' }; } },
        { method: 'POST', path: '/Matchmaking/LobbyJoinable', desc: 'Open or close the lobby to new members', body: function () { return { lobbyId: lobby(), joinable: true }; } },
        { method: 'POST', path: '/Matchmaking/LobbyMaxMembers', desc: 'Change the member cap', body: function () { return { lobbyId: lobby(), maxMembers: 8 }; } },
        { method: 'POST', path: '/Matchmaking/LobbyOwner', desc: 'Hand ownership to another member', body: function () { return { lobbyId: lobby(), ownerId: steamId() }; } },
        { method: 'POST', path: '/Matchmaking/LobbyLinked', desc: 'Link this lobby to another lobby id', body: function () { return { lobbyId: lobby(), linkedLobbyId: '0' }; } },
        { method: 'POST', path: '/Matchmaking/LobbyGameServer', desc: 'Advertise the dedicated server behind the lobby', body: function () { return { lobbyId: lobby(), ip: 0, port: 27015, serverId: '0' }; } },
        { method: 'POST', path: '/Matchmaking/CreateInvite', desc: 'Mint a shareable lobby invite', body: function () { return { lobbyId: lobby() }; }, capture: [['token', 'inviteToken']] },
        { method: 'GET', path: '/Matchmaking/ResolveInvite', desc: 'Turn an invite token back into a lobby id', dynamicPath: function () { return '/Matchmaking/ResolveInvite?token=' + idOf('inviteToken'); } },
        { method: 'POST', path: '/Matchmaking/InviteFriend', desc: 'Push a lobby invite to a friend', body: function () { return { lobbyId: lobby(), friendSteamId: steamId() }; } },
        { method: 'GET', path: '/Matchmaking/FriendsActive', desc: 'Friends who are online, with the lobby they are in' },
        { method: 'POST', path: '/Matchmaking/LobbyList', desc: 'Search public and friends\' lobbies', body: function () { return { max: 20, filters: {} }; } },
        { method: 'POST', path: '/Matchmaking/JoinLobby', desc: 'Join a lobby by id', body: function () { return { lobbyId: lobby() }; } },
        { method: 'POST', path: '/Networking/RelayTicket', desc: 'Ticket for this lobby\'s peer relay (run while you are a member)', body: function () { return { lobbyId: lobby() }; } },
        { method: 'POST', path: '/Matchmaking/LeaveLobby', desc: 'Leave a lobby', body: function () { return { lobbyId: lobby() }; } },
      ],
    },
    {
      group: 'Stats',
      items: [
        { method: 'GET', path: '/UserStats/Stats', desc: 'All stats for this app and player' },
        { method: 'POST', path: '/UserStats/Stats', desc: 'Write several stats at once', body: function () { return { stats: { tester_runs: 1, tester_note: 'hello' } }; } },
        { method: 'GET', path: '/UserStats/Stat?name=tester_runs', desc: 'Read one stat' },
        { method: 'POST', path: '/UserStats/Stat', desc: 'Write or increment one stat', body: function () { return { name: 'tester_runs', increment: 1 }; } },
        { method: 'GET', path: '/UserStats/Achievements', desc: 'Achievement catalog with this player\'s progress' },
        { method: 'POST', path: '/UserStats/Achievements/Define', desc: 'Register an achievement for this app', body: function () { return { achievements: [{ key: 'tester_first', name: 'Harness tested', description: 'Ran a call from the browser tester' }] }; } },
        { method: 'POST', path: '/UserStats/Achievements/Unlock', desc: 'Unlock an achievement', body: function () { return { key: 'tester_first' }; } },
        { method: 'GET', path: '/UserStats/Achievements/Earned', desc: 'Achievements this player has earned' },
        { method: 'GET', path: '/UserStats/GlobalStats', desc: 'Aggregated global stats' },
        { method: 'GET', path: '/UserStats/PlayerCount', desc: 'Players seen for this app' },
        { method: 'POST', path: '/UserStats/AchievementProgress', desc: 'Report progress toward a multi-step achievement', body: function () { return { name: 'tester_first', current: 1, max: 1 }; } },
        { method: 'POST', path: '/UserStats/Achievements/Clear', desc: 'Revoke one achievement from this player', body: function () { return { key: 'tester_first' }; } },
        { method: 'GET', path: '/UserStats/Achievements/Catalog', desc: 'Every definition with its stable number' },
        { method: 'GET', path: '/UserStats/Achievements/Profile', desc: 'Achievements this player earned in every game' },
        { method: 'GET', path: '/UserStats/Achievements/Percentages', desc: 'How many players have each achievement' },
        { method: 'GET', path: '/UserStats/UserStats', desc: 'Stats and achievements for one player', dynamicPath: function () { return '/UserStats/UserStats?steamId=' + steamId(); } },
        { method: 'POST', path: '/UserStats/Reset', desc: 'Wipe this player\'s stats and achievements (403 unless WEBWORKS_ALLOW_STATS_RESET=true)' },
      ],
    },
    {
      group: 'Leaderboards',
      items: [
        { method: 'POST', path: '/UserStats/Leaderboard/FindOrCreate', desc: 'Create the board if missing', body: function () { return { name: 'webworks_test', sort: 'descending', display: 'numeric' }; } },
        { method: 'POST', path: '/UserStats/Leaderboard/Submit', desc: 'Submit a score', body: function () { return { name: 'webworks_test', score: Math.floor(Math.random() * 1000), forceUpdate: true }; } },
        { method: 'GET', path: '/UserStats/Leaderboard/Entries?name=webworks_test', desc: 'Read the board' },
        { method: 'GET', path: '/UserStats/Leaderboard?name=webworks_test', desc: 'Board metadata and your entry' },
        { method: 'GET', path: '/UserStats/Leaderboard/Find?name=webworks_test', desc: 'Look up one board without creating it' },
        { method: 'GET', path: '/UserStats/Leaderboard/EntriesForUsers', desc: 'Only the entries for the steam ids you list', dynamicPath: function () { return '/UserStats/Leaderboard/EntriesForUsers?name=webworks_test&users=' + steamId(); } },
        { method: 'POST', path: '/UserStats/Leaderboard', desc: 'Find or create a board and submit a score in one call', body: function () { return { name: 'webworks_combined', sort: 'descending', display: 'numeric', score: Math.floor(Math.random() * 1000) }; } },
        { method: 'POST', path: '/UserStats/Leaderboard/AttachUGC', desc: 'Attach a workshop item to a board', body: function () { return { name: 'webworks_test', ugcHandle: idOf('fileId', '1') }; } },
      ],
    },
    {
      group: 'Cloud saves',
      items: [
        { method: 'GET', path: '/RemoteStorage/Files', desc: 'Files stored for this app and player' },
        { method: 'POST', path: '/RemoteStorage/Write', desc: 'Write a file (base64 payload)', body: function () { return { name: 'tester.json', data: btoa(JSON.stringify({ hello: 'webworks', at: Date.now() })) }; } },
        { method: 'POST', path: '/RemoteStorage/Delete', desc: 'Delete a file', body: function () { return { name: 'tester.json' }; } },
      ],
    },
    {
      group: 'Game server',
      items: [
        { method: 'POST', path: '/GameServer/LogOn', desc: 'Register a dedicated server; the cards below use its token', body: function () { return { appId: APP_ID, gamePort: 27015, queryPort: 27016, versionString: '1.0.0.0', dedicated: true }; }, captureServer: true },
        { method: 'GET', path: '/GameServer/List', desc: 'Registered game servers' },
        { method: 'POST', path: '/GameServer/Heartbeat', desc: 'Keep the server registration alive', server: true, body: function () { return {}; } },
        { method: 'POST', path: '/GameServer/Keys', desc: 'Set the server\'s rules', server: true, body: function () { return { set: { map: 'arena', mode: 'harness' } }; } },
        { method: 'POST', path: '/GameServer/Player', desc: 'Report a player joining the server', server: true, body: function () { return { steamId: steamId(), name: 'Harness player', score: 1 }; } },
        { method: 'GET', path: '/GameServer/Stats', desc: 'Read a player\'s stats as the server', server: true, dynamicPath: function () { return '/GameServer/Stats?steamId=' + steamId(); } },
        { method: 'POST', path: '/GameServer/Stats', desc: 'Write stats as the server', server: true, body: function () { return { steamId: steamId(), stats: { tester_runs: 2 } }; } },
        { method: 'POST', path: '/GameServer/LogOff', desc: 'Drop the dedicated server and its token (run it last)', server: true, body: function () { return {}; }, dropServer: true },
      ],
    },
    {
      group: 'Workshop',
      items: [
        { method: 'POST', path: '/UGC/Publish', desc: 'Publish an item; the cards below reuse its file id', body: function () { return { title: 'Tester item', description: 'from the browser harness', content: btoa('hello workshop') }; }, capture: ['fileId'] },
        { method: 'POST', path: '/UGC/Query', desc: 'Search published items', body: function () { return { searchText: 'Tester', perPage: 20 }; } },
        { method: 'GET', path: '/UGC/Item', desc: 'Read one item', dynamicPath: function () { return '/UGC/Item?id=' + idOf('fileId', '1'); } },
        { method: 'GET', path: '/UGC/Download', desc: 'Download an item\'s content', dynamicPath: function () { return '/UGC/Download?id=' + idOf('fileId', '1'); } },
        { method: 'POST', path: '/UGC/Update', desc: 'Edit a published item', body: function () { return { fileId: idOf('fileId', '1'), title: 'Tester item (edited)', description: 'updated from the harness' }; } },
        { method: 'POST', path: '/UGC/Subscribe', desc: 'Subscribe to an item', body: function () { return { fileId: idOf('fileId', '1') }; } },
        { method: 'POST', path: '/UGC/Unsubscribe', desc: 'Unsubscribe from an item', body: function () { return { fileId: idOf('fileId', '1') }; } },
        { method: 'POST', path: '/UGC/Favorite', desc: 'Favourite an item', body: function () { return { fileId: idOf('fileId', '1') }; } },
        { method: 'POST', path: '/UGC/Unfavorite', desc: 'Remove the favourite flag', body: function () { return { fileId: idOf('fileId', '1') }; } },
        { method: 'POST', path: '/UGC/Vote', desc: 'Vote an item up or down', body: function () { return { fileId: idOf('fileId', '1'), up: true }; } },
        { method: 'GET', path: '/UGC/Vote', desc: 'Your vote state for an item', dynamicPath: function () { return '/UGC/Vote?fileId=' + idOf('fileId', '1'); } },
        { method: 'POST', path: '/UGC/Delete', desc: 'Delete an item you own (run it last)', body: function () { return { fileId: idOf('fileId', '1') }; } },
      ],
    },
    {
      group: 'Inventory',
      items: [
        { method: 'GET', path: '/Inventory/Definitions', desc: 'Item definitions for this app' },
        { method: 'POST', path: '/Inventory/Define', desc: 'Define an item (a dedicated server call)', server: true, body: function () { return { itemDefId: 'tester_coin', name: 'Tester coin', type: 'item', tradable: true, clientGrantable: true }; } },
        { method: 'GET', path: '/Inventory/Items', desc: 'This player\'s inventory' },
        { method: 'POST', path: '/Inventory/GrantSelf', desc: 'Grant yourself an item', body: function () { return { itemDefId: 'tester_coin', quantity: 2 }; } },
        { method: 'POST', path: '/Inventory/Consume', desc: 'Consume an item', body: function () { return { itemDefId: 'tester_coin', quantity: 1 }; } },
        { method: 'POST', path: '/Inventory/Exchange', desc: 'Destroy items and generate others in one step', body: function () { return { destroy: [{ itemDefId: 'tester_coin', quantity: 1 }], generate: [{ itemDefId: 'tester_coin', quantity: 1 }] }; } },
        { method: 'POST', path: '/Inventory/Trade', desc: 'Offer items to another player', body: function () { return { partner: steamId(), give: [{ itemDefId: 'tester_coin', quantity: 1 }], get: [] }; } },
        { method: 'POST', path: '/Inventory/Properties', desc: 'Set properties on every item of a definition', body: function () { return { itemDefId: 'tester_coin', properties: { colour: 'gold' } }; } },
        { method: 'POST', path: '/Inventory/Inspect', desc: 'Inspect an item from its inspect token', body: function () { return { token: '0' }; } },
        { method: 'POST', path: '/Inventory/Grant', desc: 'Server-side grant: give an item to a player', server: true, body: function () { return { steamId: steamId(), itemDefId: 'tester_coin', quantity: 1 }; } },
        { method: 'POST', path: '/Inventory/Transfer', desc: 'Server-side transfer of an item to a player', server: true, body: function () { return { steamId: steamId(), itemDefId: 'tester_coin', quantity: 1 }; } },
      ],
    },
    {
      group: 'Social extras',
      items: [
        { method: 'GET', path: '/Apps/Info', desc: 'App metadata and feature flags' },
        { method: 'GET', path: '/Screenshots/List', desc: 'Uploaded screenshots' },
        { method: 'POST', path: '/Screenshots/Add', desc: 'Upload a screenshot (a real 1x1 PNG)', body: function () { return { data: TINY_PNG, width: 1, height: 1, caption: 'from the harness' }; }, captureFrom: [{ from: 'screenshot', key: 'id', as: 'screenshotId' }] },
        { method: 'GET', path: '/Screenshots/Image', desc: 'Download one screenshot as base64', dynamicPath: function () { return '/Screenshots/Image?id=' + idOf('screenshotId', ''); } },
        { method: 'DELETE', path: '/Screenshots/Delete', desc: 'Delete one screenshot', dynamicPath: function () { return '/Screenshots/Delete?id=' + idOf('screenshotId', ''); } },
        { method: 'GET', path: '/Timeline/Events', desc: 'Timeline events for this game' },
        { method: 'POST', path: '/Timeline/Event', desc: 'Add a timeline event', body: function () { return { title: 'Tested the harness' }; } },
        { method: 'DELETE', path: '/Timeline/Events', desc: 'Clear the timeline for this player' },
        { method: 'POST', path: '/Log', desc: 'Write a line to the server log', body: function () { return { message: 'hello from the browser harness' }; } },
        { method: 'GET', path: '/HTML/Fetch?url=https://example.com', desc: 'Fetch a public page through the server (HTML surface)' },
        { method: 'POST', path: '/HTTP/Request', desc: 'Proxy one HTTP request (SteamHTTP)', body: function () { return { method: 'GET', url: 'https://example.com' }; } },
        { method: 'GET', path: '/Ping', desc: 'Round trip check' },
      ],
    },
    {
      group: 'Media',
      items: [
        { method: 'GET', path: '/Music/State', desc: 'Music player state' },
        { method: 'POST', path: '/Music/State', desc: 'Set music state', body: function () { return { playing: true, title: 'Test track' }; } },
        { method: 'POST', path: '/Party/Create', desc: 'Create a party beacon', body: function () { return { maxMembers: 4 }; }, capture: ['beaconId'] },
        { method: 'GET', path: '/Party/Beacons', desc: 'Live party beacons' },
        { method: 'GET', path: '/Party/Beacon', desc: 'Read one party beacon', dynamicPath: function () { return '/Party/Beacon?id=' + idOf('beaconId'); } },
        { method: 'POST', path: '/Party/Join', desc: 'Join a party by beacon id', body: function () { return { beaconId: idOf('beaconId') }; } },
        { method: 'POST', path: '/Party/Invite', desc: 'Push a party invite to a player', body: function () { return { steamId: steamId(), beaconId: idOf('beaconId') }; } },
        { method: 'POST', path: '/Party/Leave', desc: 'Leave a party', body: function () { return { beaconId: idOf('beaconId') }; } },
        { method: 'DELETE', path: '/Party/Beacon', desc: 'Destroy your party beacon (owner only)', dynamicPath: function () { return '/Party/Beacon?id=' + idOf('beaconId'); } },
        { method: 'POST', path: '/Video/Broadcast/Start', desc: 'Start a video broadcast', body: function () { return { title: 'Tester stream' }; }, captureFrom: [{ from: 'broadcast', key: 'broadcastId', as: 'broadcastId' }] },
        { method: 'GET', path: '/Video/Status', desc: 'Broadcast status' },
        { method: 'GET', path: '/Video/URL', desc: 'The watch URL for the live broadcast' },
        { method: 'POST', path: '/Video/Watch', desc: 'Watch a player\'s broadcast', body: function () { return { steamId: steamId() }; } },
        { method: 'POST', path: '/Video/Unwatch', desc: 'Stop watching a broadcast', body: function () { return { steamId: steamId() }; } },
        { method: 'POST', path: '/Video/Broadcast/Stop', desc: 'Stop your broadcast' },
        { method: 'POST', path: '/RemotePlay/Start', desc: 'Start a remote play session', body: function () { return { title: 'Harness session', maxGuests: 2 }; }, captureFrom: [{ from: 'session', key: 'id', as: 'remoteSessionId' }] },
        { method: 'GET', path: '/RemotePlay/Sessions', desc: 'Live remote play sessions for this game' },
        { method: 'GET', path: '/RemotePlay/Session', desc: 'Read one remote play session', dynamicPath: function () { return '/RemotePlay/Session?id=' + idOf('remoteSessionId'); } },
        { method: 'POST', path: '/RemotePlay/Join', desc: 'Join a remote play session as a guest (needs the session of another player)', body: function () { return { sessionId: idOf('remoteSessionId') }; } },
        { method: 'POST', path: '/RemotePlay/Invite', desc: 'Invite a player to remote play', body: function () { return { steamId: steamId(), sessionId: idOf('remoteSessionId') }; } },
        { method: 'POST', path: '/RemotePlay/Input', desc: 'Send input events to the host (as a guest of that session)', body: function () { return { sessionId: idOf('remoteSessionId'), events: [{ type: 'move', x: 0.5, y: 0.5 }] }; } },
        { method: 'GET', path: '/RemotePlay/Input', desc: 'Drain queued input as the host', dynamicPath: function () { return '/RemotePlay/Input?sessionId=' + idOf('remoteSessionId') + '&guestSteamId=' + steamId(); } },
        { method: 'POST', path: '/RemotePlay/Leave', desc: 'Leave a remote play session', body: function () { return { sessionId: idOf('remoteSessionId') }; } },
        { method: 'POST', path: '/RemotePlay/Stop', desc: 'End the session as its host (run it last)', body: function () { return { sessionId: idOf('remoteSessionId') }; } },
      ],
    },
    {
      group: 'Networking',
      items: [
        { method: 'POST', path: '/Networking/AcceptSession', desc: 'Tell a peer you accepted its P2P session', body: function () { return { peerSteamId: steamId() }; } },
        { method: 'POST', path: '/Networking/CloseSession', desc: 'Tell a peer the P2P session closed', body: function () { return { peerSteamId: steamId() }; } },
      ],
    },
    {
      group: 'Custom',
      custom: true,
    },
  ];

  function groupNames() {
    return CATALOG.map(function (group) { return group.group; });
  }

  function renderGroups() {
    var rail = $('groups');
    rail.innerHTML = '';
    groupNames().forEach(function (name) {
      var button = el('button', 'group', name);
      button.type = 'button';
      button.addEventListener('click', function () { selectGroup(name); });
      rail.appendChild(button);
    });
  }

  function selectGroup(name) {
    Array.prototype.forEach.call(document.querySelectorAll('.group'), function (node) {
      node.classList.toggle('active', node.textContent === name);
    });

    var pane = $('endpoints');
    pane.innerHTML = '';
    var group = CATALOG.filter(function (item) { return item.group === name; })[0];
    if (!group) return;

    if (group.custom) {
      pane.appendChild(renderCustom());
      return;
    }

    group.items.forEach(function (item) {
      pane.appendChild(renderCall(item));
    });
  }

  function pathFor(item) {
    return item.dynamicPath ? item.dynamicPath() : item.path;
  }

  function renderCall(item) {
    var card = el('div', 'call');
    var head = el('div', 'call-head');
    head.appendChild(el('span', 'call-method', item.method));
    head.appendChild(el('span', 'call-path', pathFor(item)));
    if (item.desc) head.appendChild(el('span', 'call-desc', item.desc));
    card.appendChild(head);

    var bodyText = null;
    var edited = false;
    if (item.body) {
      bodyText = JSON.stringify(item.body(), null, 2);
      var area = el('textarea', 'call-body');
      area.value = bodyText;
      area.spellcheck = false;
      area.addEventListener('input', function () { edited = true; });
      card.appendChild(area);
    }

    var foot = el('div', 'call-foot');
    var run = el('button', 'run', 'Run');
    run.type = 'button';
    foot.appendChild(run);

    if (item.body) {
      var toggle = el('button', 'toggle', 'Body');
      toggle.type = 'button';
      toggle.addEventListener('click', function () {
        card.classList.toggle('open');
        toggle.textContent = card.classList.contains('open') ? 'Hide body' : 'Body';
      });
      foot.appendChild(toggle);
    }

    var noteText = '';
    if (item.account && !state.account) noteText = 'needs an account sign-in';
    var note = el('span', 'note', noteText);
    foot.appendChild(note);
    card.appendChild(foot);

    run.addEventListener('click', function () {
      var payload;
      if (item.body) {
        var area = card.querySelector('.call-body');
        try {
          // An untouched body is rebuilt right before the call, so it picks up ids captured
          // by the cards run above it. A body the tester edited is sent exactly as typed.
          payload = edited
            ? (area.value.trim() ? JSON.parse(area.value) : {})
            : item.body();
        } catch (err) {
          showResponse({ ok: false, status: 0, ms: 0, data: { error: 'body is not valid JSON', detail: err.message } }, item.method + ' ' + pathFor(item));
          return;
        }
      }
      var path = pathFor(item);
      var call;
      if (item.absolute) call = request(item.method, path, payload, { absolute: true });
      else if (item.server) call = ensureServerToken().then(function (token) { return request(item.method, path, payload, { token: token }); });
      else if (item.account) call = accountCall(item.method, path, payload);
      else call = request(item.method, path, payload);

      call.then(function (reply) {
        showResponse(reply, item.method + ' ' + path);
        capture(item, reply);
      });
    });

    return card;
  }

  // Remember the ids and tokens a successful call handed out. Everything that follows keeps
  // working whether the cards are run in order or one at a time.
  function capture(item, reply) {
    if (!reply.ok || !reply.data || typeof reply.data !== 'object') return;
    var data = reply.data;

    if (item.saveLobby && data.id) {
      state.lobbyId = data.id;
      state.ids.lobbyId = data.id;
    }
    (item.capture || []).forEach(function (key) {
      if (typeof key === 'string') {
        if (data[key] !== undefined && data[key] !== null && data[key] !== '') state.ids[key] = data[key];
        return;
      }
      if (Array.isArray(key)) {
        if (data[key[0]] !== undefined && data[key[0]] !== null && data[key[0]] !== '') state.ids[key[1]] = data[key[0]];
        return;
      }
      if (data[key.from] !== undefined && data[key.from] !== null && data[key.from] !== '') state.ids[key.as] = data[key.from];
    });
    (item.captureFrom || []).forEach(function (pair) {
      var nested = data[pair.from];
      if (nested && nested[pair.key] !== undefined && nested[pair.key] !== null) state.ids[pair.as] = nested[pair.key];
    });
    if (item.captureServer && data.sessionToken) {
      state.server = { token: data.sessionToken, serverId: data.serverId || null };
      state.ids.serverId = data.serverId || state.ids.serverId;
    }
    if (item.dropServer) state.server = null;
    if (item.captureAccount && data.token) {
      state.account = {
        token: data.token,
        username: (data.account && data.account.username) || state.account && state.account.username || '',
        displayName: (data.account && (data.account.displayName || data.account.username)) || '',
      };
      saveCached();
    }
    renderSessionFacts();
  }

  function renderCustom() {
    var card = el('div', 'call open');
    card.appendChild(el('div', 'call-head')).appendChild(el('span', 'call-desc', 'Any method, any path on this server.'));
    var head = card.querySelector('.call-head');
    head.innerHTML = '';

    var method = document.createElement('select');
    ['GET', 'POST', 'PATCH', 'DELETE'].forEach(function (name) {
      var option = el('option', null, name);
      option.value = name;
      method.appendChild(option);
    });
    method.className = 'call-method';
    head.appendChild(method);

    var path = document.createElement('input');
    path.className = 'call-path';
    path.style.background = 'transparent';
    path.style.border = '0';
    path.style.color = '#e8e8e8';
    path.value = '/api/Unity/SteamworksNET/User/Profile';
    head.appendChild(path);

    var area = el('textarea', 'call-body');
    area.value = '{}';
    area.spellcheck = false;
    card.appendChild(area);

    var foot = el('div', 'call-foot');
    var run = el('button', 'run', 'Send');
    run.type = 'button';
    foot.appendChild(run);
    card.appendChild(foot);

    run.addEventListener('click', function () {
      var payload = null;
      if (method.value !== 'GET') {
        try { payload = area.value.trim() ? JSON.parse(area.value) : {}; }
        catch (err) { showResponse({ ok: false, status: 0, ms: 0, data: { error: 'body is not valid JSON' } }, method.value + ' ' + path.value); return; }
      }
      request(method.value, path.value, payload, { absolute: true }).then(function (reply) {
        showResponse(reply, method.value + ' ' + path.value);
      });
    });

    return card;
  }

  function showResponse(reply, label) {
    $('response-meta').textContent = label + ' → ' + (reply.status || 'network error') + ' · ' + reply.ms + ' ms';
    var body = typeof reply.data === 'string' ? reply.data : JSON.stringify(reply.data, null, 2);
    $('response-body').textContent = body || '(empty body)';
  }

  // ---------------------------------------------------------------- status

  function loadHealth() {
    request('GET', '/health', null, { absolute: true }).then(function (reply) {
      $('health-json').textContent = JSON.stringify(reply.data, null, 2);
      if (reply.data) state.health = reply.data;
      renderSessionFacts();
    });
  }

  function renderSessionFacts() {
    var facts = $('session-facts');
    if (!facts) return;
    facts.innerHTML = '';
    function pair(key, value) {
      facts.appendChild(el('dt', null, key));
      facts.appendChild(el('dd', null, value === undefined || value === null || value === '' ? '—' : String(value)));
    }
    pair('Server', location.origin);
    pair('Mount', MOUNT);
    pair('App id', APP_ID);
    pair('Steam id', state.session && state.session.steamId);
    pair('Name', state.session && state.session.name);
    pair('Account', state.account ? state.account.username : 'guest session');
    pair('Account system', state.health && state.health.accounts);
    pair('Presence', state.session ? 'heartbeat every ' + Math.round(HEARTBEAT_MS / 1000) + ' s' : 'signed out');
    pair('Current lobby', state.lobbyId);
    pair('Dedicated server', state.server ? (state.server.serverId || 'session held') : null);
    pair('Version', state.health && state.health.version);
  }

  // ---------------------------------------------------------------- tabs

  function selectTab(name) {
    Array.prototype.forEach.call(document.querySelectorAll('.tab'), function (tab) {
      tab.classList.toggle('active', tab.getAttribute('data-tab') === name);
    });
    ['console', 'game', 'status'].forEach(function (pane) {
      $('pane-' + pane).classList.toggle('hidden', pane !== name);
    });
    if (name === 'status') loadHealth();
    if (name === 'game' && window.WebworksArena) window.WebworksArena.focus();
  }

  // ---------------------------------------------------------------- wiring

  function wire() {
    $('signin-btn').addEventListener('click', signIn);
    $('guest-btn').addEventListener('click', continueAsGuest);
    $('mode-toggle').addEventListener('click', function () {
      state.entryMode = state.entryMode === 'signin' ? 'register' : 'signin';
      primeEntry();
      if (state.account && state.account.token) {
        state.account = null;
        saveCached();
      }
    });
    $('pick-avatar').addEventListener('click', function () { $('avatar-file').click(); });
    $('avatar-file').addEventListener('change', function (event) {
      var file = event.target.files && event.target.files[0];
      if (!file) return;
      var reader = new FileReader();
      reader.onload = function () {
        var image = new Image();
        image.onload = function () {
          var side = Math.min(image.width, image.height);
          var canvas = document.createElement('canvas');
          canvas.width = 184;
          canvas.height = 184;
          var context = canvas.getContext('2d');
          context.drawImage(image, (image.width - side) / 2, (image.height - side) / 2, side, side, 0, 0, 184, 184);
          var dataUrl = canvas.toDataURL('image/png');
          state.avatarBase64 = dataUrl;
          $('avatar-preview').src = dataUrl;
          $('avatar-note').textContent = 'Icon ready.';
        };
        image.onerror = function () { $('avatar-note').textContent = 'That file is not an image.'; };
        image.src = reader.result;
      };
      reader.readAsDataURL(file);
    });

    $('add-friend-btn').addEventListener('click', addFriend);
    $('add-friend').addEventListener('keydown', function (event) { if (event.key === 'Enter') addFriend(); });
    $('continue-btn').addEventListener('click', function () {
      enterHarness(state.lobbyId ? 'game' : 'console');
    });
    $('signout-btn').addEventListener('click', function () {
      leaveFriendsScreen();
      stopPresence(true);
      state.account = null;
      state.session = null;
      saveCached();
      startEntry();
    });

    // A closed tab says goodbye; a backgrounded tab keeps its heartbeat.
    window.addEventListener('pagehide', function () { stopPresence(true); });
    window.addEventListener('pageshow', function () {
      if (state.session && state.session.token) startPresence();
    });
    document.addEventListener('visibilitychange', function () {
      if (document.visibilityState !== 'visible') return;
      if (!state.session || !state.session.token) return;
      heartbeat();
      connectPresence();
      if (onFriendsScreen) loadFriends({ soft: true });
    });
    $('harness-signout').addEventListener('click', signOut);
    $('refresh-health').addEventListener('click', loadHealth);
    $('copy-response').addEventListener('click', function () {
      var text = $('response-body').textContent || '';
      if (navigator.clipboard) navigator.clipboard.writeText(text);
    });

    Array.prototype.forEach.call(document.querySelectorAll('.tab'), function (tab) {
      tab.addEventListener('click', function () { selectTab(tab.getAttribute('data-tab')); });
    });

    $('username').addEventListener('keydown', function (event) { if (event.key === 'Enter') { $('password').focus(); } });
    $('password').addEventListener('keydown', function (event) { if (event.key === 'Enter') signIn(); });
    $('guest-name').addEventListener('keydown', function (event) { if (event.key === 'Enter') continueAsGuest(); });
  }

  function boot() {
    wire();
    loadCached();

    // A cached session can be replayed without asking for a password: verify it still works,
    // then land on the friends step exactly like a fresh sign-in does.
    if (state.session && state.session.token) {
      request('GET', '/User/Profile').then(function (reply) {
        if (!reply.ok) { state.session = null; saveCached(); startEntry(); return; }
        startPresence();
        // The friends step belongs to accounts; a guest goes straight to the console.
        if (state.account && state.account.token) showFriends();
        else enterHarness();
      });
      return;
    }

    startEntry();
  }

  // A handle for scripted runs and for the page's own smoke test: the same catalogue and
  // request helper the cards use, so a full pass can be driven from the console.
  window.WebworksHarness = {
    catalog: CATALOG,
    state: state,
    request: request,
    accountCall: accountCall,
    capture: capture,
    ensureServerToken: ensureServerToken,
    idOf: idOf,
    heartbeat: heartbeat,
    startPresence: startPresence,
    stopPresence: stopPresence,
    loadFriends: loadFriends,
    loadInvites: loadInvites,
    inviteFriend: inviteFriend,
    joinInvite: joinInvite,
    probes: {
      onFriendsScreen: function () { return onFriendsScreen; },
      invites: function () { return lastInvites; },
      socket: function () { return presenceSocket ? presenceSocket.readyState : null; },
    },
  };

  document.addEventListener('DOMContentLoaded', boot);
})();
