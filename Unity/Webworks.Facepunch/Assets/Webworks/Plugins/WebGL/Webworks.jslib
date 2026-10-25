mergeInto(LibraryManager.library, {

  Webworks_HttpSync: function (methodPtr, urlPtr, bodyPtr, tokenPtr) {
    var s = function (p) { return p ? ((typeof UTF8ToString === 'function') ? UTF8ToString(p) : Pointer_stringify(p)) : ''; };
    var method = s(methodPtr), url = s(urlPtr), body = s(bodyPtr), token = s(tokenPtr);
    var out = '';
    try {
      var x = new XMLHttpRequest();
      x.open(method, url, false);
      if (body) x.setRequestHeader('Content-Type', 'application/json');
      // Current header plus the pre-rename Weeam/Webeam spelling, so old servers authenticate too.
      if (token) { x.setRequestHeader('x-webworks-session', token); x.setRequestHeader('x-webeam-session', token); }
      x.send(body ? body : null);
      if (x.status >= 200 && x.status < 300) out = x.responseText;
      else console.warn('[Webworks] ' + method + ' ' + url + ' -> HTTP ' + x.status);
    } catch (e) {
      console.warn('[Webworks] ' + method + ' ' + url + ' failed: ' + e);
    }
    var len = lengthBytesUTF8(out) + 1;
    var buf = _malloc(len);
    stringToUTF8(out, buf, len);
    return buf;
  },

  Webworks_PageIsSecure: function () {
    return (typeof location !== 'undefined' && location.protocol === 'https:') ? 1 : 0;
  },

  Webworks_QueryParam: function (namePtr) {
    var name = (typeof UTF8ToString === 'function') ? UTF8ToString(namePtr) : Pointer_stringify(namePtr);
    var out = '';
    try {
      var params = new URLSearchParams(location.search);
      out = params.get(name) || '';
    } catch (e) {}
    var len = lengthBytesUTF8(out) + 1;
    var buf = _malloc(len);
    stringToUTF8(out, buf, len);
    return buf;
  },

  Webworks_WS_Connect: function (handle, urlPtr) {
    var url = (typeof UTF8ToString === 'function') ? UTF8ToString(urlPtr) : Pointer_stringify(urlPtr);
    var rest = url.substring(5);
    if (location.protocol === 'https:' && url.indexOf('ws://') === 0 && rest.indexOf('localhost') !== 0 && rest.indexOf('127.0.0.1') !== 0 && rest.indexOf('[::1]') !== 0) url = 'wss://' + rest;
    if (!window.__webworksWSPool) window.__webworksWSPool = {};
    var w = window.__webworksWSPool[handle] = { ws: null, inbox: [], outbox: [], code: 0, reason: '' };
    try {
      var ws = new WebSocket(url);
      w.ws = ws;
      ws.onopen = function () { for (var i = 0; i < w.outbox.length; i++) ws.send(w.outbox[i]); w.outbox = []; };
      ws.onmessage = function (e) { if (typeof e.data === 'string') w.inbox.push(e.data); };
      ws.onclose = function (e) { w.code = e.code; w.reason = e.reason || ''; };
      ws.onerror = function () { console.warn('[Webworks] WebSocket #' + handle + ' error (' + url + ')'); };
    } catch (e) {
      console.warn('[Webworks] could not open WebSocket #' + handle + ': ' + e);
    }
  },

  Webworks_WS_Send: function (handle, msgPtr) {
    var w = window.__webworksWSPool && window.__webworksWSPool[handle];
    if (!w || !w.ws) return;
    var msg = (typeof UTF8ToString === 'function') ? UTF8ToString(msgPtr) : Pointer_stringify(msgPtr);
    if (w.ws.readyState === 1) w.ws.send(msg);
    else if (w.ws.readyState === 0) w.outbox.push(msg);
  },

  Webworks_WS_Poll: function (handle) {
    var w = window.__webworksWSPool && window.__webworksWSPool[handle];
    var out = (w && w.inbox.length) ? w.inbox.shift() : '';
    var len = lengthBytesUTF8(out) + 1;
    var buf = _malloc(len);
    stringToUTF8(out, buf, len);
    return buf;
  },

  Webworks_WS_State: function (handle) {
    var w = window.__webworksWSPool && window.__webworksWSPool[handle];
    return (w && w.ws) ? w.ws.readyState : -1;
  },

  Webworks_WS_CloseInfo: function (handle) {
    var w = window.__webworksWSPool && window.__webworksWSPool[handle];
    var out = w ? (w.code + ' ' + w.reason) : '';
    var len = lengthBytesUTF8(out) + 1;
    var buf = _malloc(len);
    stringToUTF8(out, buf, len);
    return buf;
  },

  Webworks_Clipboard_Init: function () {
    if (window.__webworksClip) return;
    var c = window.__webworksClip = { pending: null, paste: '', pasteSeq: 0 };

    var flush = function () {
      if (c.pending === null) return;
      var prev = document.activeElement;
      var t = document.createElement('textarea');
      t.value = c.pending;
      t.setAttribute('readonly', '');
      t.style.position = 'fixed'; t.style.top = '0'; t.style.left = '0'; t.style.opacity = '0';
      document.body.appendChild(t);
      t.select();
      try { if (document.execCommand('copy')) c.pending = null; } catch (e) {}
      document.body.removeChild(t);
      if (prev && prev.focus) { try { prev.focus(); } catch (e) {} }
    };
    var gestures = ['pointerup', 'mouseup', 'touchend', 'keyup', 'keydown'];
    for (var i = 0; i < gestures.length; i++) document.addEventListener(gestures[i], flush, true);

    document.addEventListener('paste', function (e) {
      var text = '';
      try { text = (e.clipboardData || window.clipboardData).getData('text'); } catch (err) {}
      if (text) { c.paste = text; c.pasteSeq++; }
    }, true);

    document.addEventListener('keydown', function (e) {
      if (!((e.ctrlKey || e.metaKey) && (e.key === 'v' || e.key === 'V'))) return;
      var seq = c.pasteSeq;
      setTimeout(function () {
        if (c.pasteSeq !== seq || !navigator.clipboard || !navigator.clipboard.readText) return;
        navigator.clipboard.readText().then(function (t) { if (t) { c.paste = t; c.pasteSeq++; } }, function () {});
      }, 100);
    }, true);
  },

  Webworks_Clipboard_Copy: function (textPtr) {
    var c = window.__webworksClip;
    if (!c) return;
    var text = (typeof UTF8ToString === 'function') ? UTF8ToString(textPtr) : Pointer_stringify(textPtr);
    c.pending = text;
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(function () { if (c.pending === text) c.pending = null; }, function () {  });
    }
  },

  Webworks_Clipboard_TakePaste: function () {
    var c = window.__webworksClip;
    var out = c ? c.paste : '';
    if (c) c.paste = '';
    var len = lengthBytesUTF8(out) + 1;
    var buf = _malloc(len);
    stringToUTF8(out, buf, len);
    return buf;
  },

  Webworks_WS_Close: function (handle) {
    var w = window.__webworksWSPool && window.__webworksWSPool[handle];
    if (w && w.ws) { try { w.ws.close(); } catch (e) {} }
    if (window.__webworksWSPool) delete window.__webworksWSPool[handle];
  },

  Webworks_PickImage: function () {
    try {
      var input = document.createElement('input');
      input.type = 'file';
      input.accept = 'image/png,image/jpeg,image/webp';
      input.style.display = 'none';
      input.onchange = function () {
        var file = input.files && input.files[0];
        document.body.removeChild(input);
        if (!file) return;
        var reader = new FileReader();
        reader.onload = function () { window.__webworksPickedImage = String(reader.result || ''); };
        reader.readAsDataURL(file);
      };
      document.body.appendChild(input);
      input.click();
    } catch (e) {
      window.__webworksPickedImage = '';
    }
  },

  Webworks_TakePickedImage: function () {
    var out = window.__webworksPickedImage || '';
    window.__webworksPickedImage = '';
    var len = lengthBytesUTF8(out) + 1;
    var buf = _malloc(len);
    stringToUTF8(out, buf, len);
    return buf;
  },

  Webworks_HTML_Open: function (id, urlPtr) {
    if (!window.__webworksHtml) window.__webworksHtml = {};
    var url = (typeof UTF8ToString === 'function') ? UTF8ToString(urlPtr) : Pointer_stringify(urlPtr);
    var holder = window.__webworksHtml[id];
    if (!holder) {
      holder = document.createElement('div');
      holder.style.position = 'fixed';
      holder.style.zIndex = '2147483000';
      holder.style.background = '#fff';
      holder.style.border = '1px solid #333';
      holder.style.boxShadow = '0 8px 32px rgba(0,0,0,.5)';
      holder.style.overflow = 'hidden';
      holder.style.display = 'none';
      var frame = document.createElement('iframe');
      frame.style.width = '100%';
      frame.style.height = '100%';
      frame.style.border = '0';
      frame.setAttribute('referrerpolicy', 'no-referrer');
      holder.appendChild(frame);
      document.body.appendChild(holder);
      holder.__frame = frame;
      window.__webworksHtml[id] = holder;
    }
    holder.style.display = 'block';
    holder.__frame.src = url;
  },

  Webworks_HTML_Navigate: function (id, urlPtr) {
    var holder = window.__webworksHtml && window.__webworksHtml[id];
    if (!holder) return;
    var url = (typeof UTF8ToString === 'function') ? UTF8ToString(urlPtr) : Pointer_stringify(urlPtr);
    holder.style.display = 'block';
    holder.__frame.src = url;
  },

  Webworks_HTML_Resize: function (id, width, height) {
    var holder = window.__webworksHtml && window.__webworksHtml[id];
    if (!holder) return;
    var canvas = document.querySelector('canvas');
    var rect = canvas ? canvas.getBoundingClientRect() : { left: 0, top: 0 };
    holder.style.left = Math.round(rect.left) + 'px';
    holder.style.top = Math.round(rect.top) + 'px';
    holder.style.width = Math.max(0, width) + 'px';
    holder.style.height = Math.max(0, height) + 'px';
  },

  Webworks_HTML_Execute: function (id, scriptPtr) {
    var holder = window.__webworksHtml && window.__webworksHtml[id];
    if (!holder) return;
    var script = (typeof UTF8ToString === 'function') ? UTF8ToString(scriptPtr) : Pointer_stringify(scriptPtr);
    try { holder.__frame.contentWindow.eval(script); } catch (e) { console.warn('[Webworks] HTML surface script blocked: ' + e); }
  },

  Webworks_HTML_Visible: function (id, visible) {
    var holder = window.__webworksHtml && window.__webworksHtml[id];
    if (holder) holder.style.display = visible ? 'block' : 'none';
  },

  Webworks_HTML_Close: function (id) {
    var holder = window.__webworksHtml && window.__webworksHtml[id];
    if (!holder) return;
    if (holder.parentNode) holder.parentNode.removeChild(holder);
    delete window.__webworksHtml[id];
  },

});
