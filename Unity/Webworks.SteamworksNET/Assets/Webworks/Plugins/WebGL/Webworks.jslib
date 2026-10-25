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

  Webworks_Mic_Start: function (consumer, rate) {
    var m = window.__webworksMic;
    if (!m) {
      m = window.__webworksMic = { state: 0, consumers: {}, gen: 0, ctx: null, stream: null, node: null, src: null, sink: null, resumeFn: null };

      m.feed = function (block) {
        var srcRate = m.ctx ? m.ctx.sampleRate : 48000;
        var n = block.length;
        if (!n) return;
        for (var id in m.consumers) {
          if (!m.consumers.hasOwnProperty(id)) continue;
          var c = m.consumers[id];
          var ratio = srcRate / c.rate;
          var t = c.t, out = [];
          while (t < n - 1) {
            var i0 = Math.floor(t), f = t - i0;
            var s0 = i0 < 0 ? c.last : block[i0];
            var v = s0 + (block[i0 + 1] - s0) * f;
            out.push(v < -1 ? -32768 : (v > 1 ? 32767 : (v * 32767) | 0));
            t += ratio;
          }
          c.t = t - n;
          c.last = block[n - 1];
          if (!out.length) continue;
          var arr = new Int16Array(out.length);
          for (var k = 0; k < out.length; k++) arr[k] = out[k];
          c.q.push(arr);
          c.queued += arr.length;
          var cap = c.rate * 2;
          while (c.queued > cap && c.q.length > 1) { c.queued -= c.q[0].length - c.qoff; c.q.shift(); c.qoff = 0; }
        }
      };

      m.begin = function () {
        if (m.state === 1 || m.state === 2) return;
        var gen = ++m.gen;
        m.state = 1;
        var AC = window.AudioContext || window.webkitAudioContext;
        var md = (typeof navigator !== 'undefined') ? navigator.mediaDevices : null;
        if (!AC || !md || !md.getUserMedia) {
          m.state = 3;
          console.warn('[Webworks] Microphone capture is not available here (it needs https or localhost, and a browser with getUserMedia).');
          return;
        }
        md.getUserMedia({ audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true, channelCount: 1 }, video: false }).then(function (stream) {
          var stopTracks = function (s) { try { var ts = s.getTracks(); for (var i = 0; i < ts.length; i++) ts[i].stop(); } catch (e) {} };
          if (gen !== m.gen) { stopTracks(stream); return; }
          var ctx = new AC();
          m.ctx = ctx; m.stream = stream;
          var src = ctx.createMediaStreamSource(stream);
          var sink = ctx.createGain(); sink.gain.value = 0; sink.connect(ctx.destination);
          m.src = src; m.sink = sink;

          m.resumeFn = function () { if (m.ctx && m.ctx.state === 'suspended') { try { m.ctx.resume(); } catch (e) {} } };
          m.resumeEvents = ['pointerdown', 'mousedown', 'keydown', 'touchend'];
          for (var i = 0; i < m.resumeEvents.length; i++) document.addEventListener(m.resumeEvents[i], m.resumeFn, true);
          m.resumeFn();

          var viaScriptProcessor = function () {
            if (gen !== m.gen) return;
            var sp = ctx.createScriptProcessor(2048, 1, 1);
            sp.onaudioprocess = function (e) { m.feed(new Float32Array(e.inputBuffer.getChannelData(0))); };
            src.connect(sp); sp.connect(sink);
            m.node = sp; m.state = 2;
          };

          if (ctx.audioWorklet && typeof AudioWorkletNode !== 'undefined' && typeof Blob !== 'undefined' && window.URL && URL.createObjectURL) {
            var code = 'class WebworksMic extends AudioWorkletProcessor{constructor(){super();this.b=new Float32Array(1024);this.n=0;}' +
              'process(inputs){var c=inputs[0]&&inputs[0][0];if(!c)return true;for(var k=0;k<c.length;k++){this.b[this.n++]=c[k];' +
              'if(this.n===1024){this.port.postMessage(this.b.slice(0));this.n=0;}}return true;}}registerProcessor("webworks-mic",WebworksMic);';
            var url = URL.createObjectURL(new Blob([code], { type: 'application/javascript' }));
            ctx.audioWorklet.addModule(url).then(function () {
              try { URL.revokeObjectURL(url); } catch (e) {}
              if (gen !== m.gen) return;
              var node = new AudioWorkletNode(ctx, 'webworks-mic');
              node.port.onmessage = function (e) { m.feed(e.data); };
              src.connect(node); node.connect(sink);
              m.node = node; m.state = 2;
            }, function () { viaScriptProcessor(); });
          } else {
            viaScriptProcessor();
          }
        }, function (err) {
          if (gen !== m.gen) return;
          m.state = 3;
          console.warn('[Webworks] Microphone permission was denied or no input device was found: ' + err);
        });
      };

      m.end = function () {
        m.gen++;
        try {
          if (m.resumeFn) for (var i = 0; i < m.resumeEvents.length; i++) document.removeEventListener(m.resumeEvents[i], m.resumeFn, true);
          if (m.node) { try { m.node.disconnect(); } catch (e) {} if (m.node.port) m.node.port.onmessage = null; m.node.onaudioprocess = null; }
          if (m.src) m.src.disconnect();
          if (m.stream) { var ts = m.stream.getTracks(); for (var j = 0; j < ts.length; j++) ts[j].stop(); }
          if (m.ctx) m.ctx.close();
        } catch (e) {}
        m.ctx = null; m.stream = null; m.node = null; m.src = null; m.sink = null; m.resumeFn = null;
        m.state = 0;
      };
    }
    m.consumers[consumer] = { rate: (rate | 0) > 0 ? (rate | 0) : 16000, q: [], qoff: 0, queued: 0, t: 0, last: 0 };
    m.begin();
  },

  Webworks_Mic_Stop: function (consumer) {
    var m = window.__webworksMic;
    if (!m) return;
    delete m.consumers[consumer];
    for (var id in m.consumers) if (m.consumers.hasOwnProperty(id)) return;
    m.end();
  },

  Webworks_Mic_State: function () {
    var m = window.__webworksMic;
    return m ? m.state : 0;
  },

  Webworks_Mic_Available: function (consumer) {
    var m = window.__webworksMic;
    var c = m && m.consumers[consumer];
    return c ? c.queued : 0;
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

  Webworks_Mic_Read: function (consumer, ptr, maxSamples) {
    var m = window.__webworksMic;
    var c = m && m.consumers[consumer];
    if (!c || !c.queued || maxSamples <= 0) return 0;
    var want = Math.min(maxSamples, c.queued);
    var dst = new Int16Array(want), got = 0;
    while (got < want) {
      var chunk = c.q[0];
      var take = Math.min(chunk.length - c.qoff, want - got);
      dst.set(chunk.subarray(c.qoff, c.qoff + take), got);
      got += take; c.qoff += take;
      if (c.qoff >= chunk.length) { c.q.shift(); c.qoff = 0; }
    }
    c.queued -= want;
    HEAPU8.set(new Uint8Array(dst.buffer), ptr);
    return want;
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

  // RemoteStorage files for WebGL builds. They live in IndexedDB database
  // "WEBWORKS-SAVES", which is deliberately separate from wherever the game keeps its
  // own saves (PlayerPrefs/localStorage/IDB) so the two never overwrite each other and
  // the game still gets a durable file store with no server round trip.
  Webworks_Saves_Init: function () {
    var s = window.__webworksSaves;
    if (s) return s.ready ? 1 : 0;

    s = window.__webworksSaves = { db: null, ready: false, snapshot: null, queue: [], failed: false };

    var finish = function (json) {
      s.snapshot = json;
      s.ready = true;
      var queued = s.queue;
      s.queue = [];
      for (var i = 0; i < queued.length; i++) s.apply(queued[i]);
    };

    s.apply = function (op) {
      if (!s.db) return;
      try {
        var tx = s.db.transaction('files', 'readwrite');
        var store = tx.objectStore('files');
        if (op.type === 'write') store.put({ name: op.name, data: op.data, time: op.time });
        else if (op.type === 'delete') store.delete(op.name);
        else if (op.type === 'clear') store.clear();
      } catch (e) {
        console.warn('[Webworks] WEBWORKS-SAVES write failed: ' + e);
      }
    };

    try {
      var req = indexedDB.open('WEBWORKS-SAVES', 1);
      req.onupgradeneeded = function () {
        var db = req.result;
        if (!db.objectStoreNames.contains('files')) db.createObjectStore('files', { keyPath: 'name' });
      };
      req.onsuccess = function () {
        s.db = req.result;
        s.db.onversionchange = function () { try { s.db.close(); } catch (e) {} s.db = null; };
        try {
          var all = s.db.transaction('files', 'readonly').objectStore('files').getAll();
          all.onsuccess = function () {
            var files = (all.result || []).map(function (f) {
              return { name: f.name, data: f.data || '', time: f.time || 0 };
            });
            finish(JSON.stringify({ files: files }));
          };
          all.onerror = function () { finish('{"files":[]}'); };
        } catch (e) {
          finish('{"files":[]}');
        }
      };
      req.onerror = function () {
        s.failed = true;
        console.warn('[Webworks] IndexedDB "WEBWORKS-SAVES" unavailable; remote storage stays in memory for this session');
        finish('{"files":[]}');
      };
      req.onblocked = function () { finish('{"files":[]}'); };
    } catch (e) {
      s.failed = true;
      console.warn('[Webworks] IndexedDB "WEBWORKS-SAVES" could not be opened: ' + e);
      finish('{"files":[]}');
    }

    return 0;
  },

  Webworks_Saves_Ready: function () {
    var s = window.__webworksSaves;
    return s && s.ready ? 1 : 0;
  },

  Webworks_Saves_Take: function () {
    var s = window.__webworksSaves;
    var out = (s && s.snapshot) ? s.snapshot : '';
    if (s) s.snapshot = null;
    var len = lengthBytesUTF8(out) + 1;
    var buf = _malloc(len);
    stringToUTF8(out, buf, len);
    return buf;
  },

  Webworks_Saves_Write: function (namePtr, dataPtr) {
    var s = window.__webworksSaves;
    if (!s) return 0;
    var str = function (p) { return p ? ((typeof UTF8ToString === 'function') ? UTF8ToString(p) : Pointer_stringify(p)) : ''; };
    var op = { type: 'write', name: str(namePtr), data: str(dataPtr), time: Date.now() };
    if (!op.name) return 0;
    if (s.ready && s.db) s.apply(op);
    else s.queue.push(op);
    return 1;
  },

  Webworks_Saves_Delete: function (namePtr) {
    var s = window.__webworksSaves;
    if (!s) return 0;
    var name = namePtr ? ((typeof UTF8ToString === 'function') ? UTF8ToString(namePtr) : Pointer_stringify(namePtr)) : '';
    if (!name) return 0;
    var op = { type: 'delete', name: name };
    if (s.ready && s.db) s.apply(op);
    else s.queue.push(op);
    return 1;
  },

  Webworks_Saves_Clear: function () {
    var s = window.__webworksSaves;
    if (!s) return 0;
    var op = { type: 'clear' };
    if (s.ready && s.db) s.apply(op);
    else s.queue.push(op);
    return 1;
  },

});
