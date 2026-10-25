// Local URLs, plus an optional Cloudflare Quick Tunnel.
//
// The server always knows its own reachable addresses: localhost, the LAN address when it is
// bound to 0.0.0.0, and (when one is running) the temporary public URL from a Cloudflare
// tunnel. Nothing here touches game traffic; the tunnel is a plain HTTPS reverse proxy to
// the same port the shims already use.
//
// Quick tunnels are created by Wrangler (`wrangler tunnel quick-start <url>`), the official
// Cloudflare CLI, which also downloads the cloudflared binary it needs. They are anonymous,
// temporary, and public: anybody holding the URL can reach this server, so the console says
// so and the choice is remembered in the config file.

const os = require('os');
const { spawn, spawnSync } = require('child_process');

// Anchored on a host boundary: a lookalike like https://evil.trycloudflare.com.example
// must not be mistaken for the tunnel Cloudflare assigned.
const URL_PATTERN = /https:\/\/[a-z0-9][a-z0-9-]*\.trycloudflare\.com(?![\w.-])/i;
const START_TIMEOUT_MS = 240 * 1000;

function isWildcardHost(host) {
  return !host || host === '0.0.0.0' || host === '::' || host === '[::]';
}

// Every address a client on this machine, or on the LAN, can use to reach the server.
function localUrls(host, port, scheme = 'http') {
  const base = `${scheme}://`;
  const urls = [];
  const add = (url) => { if (url && !urls.includes(url)) urls.push(url); };

  if (isWildcardHost(host)) {
    add(`${base}localhost:${port}`);
    add(`${base}127.0.0.1:${port}`);
    for (const entries of Object.values(os.networkInterfaces())) {
      for (const entry of entries || []) {
        if (entry.family !== 'IPv4' || entry.internal) continue;
        add(`${base}${entry.address}:${port}`);
      }
    }
  } else {
    add(`${base}${host}:${port}`);
    if (host !== 'localhost' && host !== '127.0.0.1') add(`${base}localhost:${port}`);
  }
  return urls;
}

// The address the tunnel should proxy to: the loopback name, so the tunnel never depends on
// a wildcard bind being reachable from outside.
function tunnelTarget(port, scheme = 'http') {
  return `${scheme}://localhost:${port}`;
}

// Commands are built as a single line because npm-installed CLIs on Windows are .cmd shims
// that only run through a shell; quoting here keeps the fixed arguments inert.
function shellQuote(value) {
  return /^[A-Za-z0-9_@%+=:,./-]+$/.test(value) ? value : `"${String(value).replace(/"/g, '""')}"`;
}

function runSync(line, timeout) {
  try {
    return spawnSync(line, { encoding: 'utf8', timeout, shell: true });
  } catch (err) {
    return { status: 1, error: err, stdout: '', stderr: '' };
  }
}

function wranglerVersion() {
  const result = runSync('wrangler --version', 20000);
  return result.status === 0 ? String(result.stdout || '').trim() : null;
}

function installWrangler(log) {
  log.notice('installing wrangler with npm (one time, a few megabytes)');
  const result = runSync('npm install --global wrangler --no-audit --no-fund', 300000);
  if (result.error) log.warn(`could not run npm: ${result.error.message}`);
  const version = wranglerVersion();
  if (!version) {
    log.warn('wrangler is still not on PATH; falling back to "npx wrangler" for this run');
    return null;
  }
  log.notice(`wrangler ${version.split('\n')[0]} installed`);
  return version;
}

function hasWrangler(log) {
  const version = wranglerVersion();
  if (version) return { command: 'wrangler', version };
  const installed = installWrangler(log);
  if (installed) return { command: 'wrangler', version: installed };
  return { command: 'npx', version: null };
}

// Starts a Quick Tunnel and resolves once Cloudflare has assigned a public URL. Resolves
// null (never rejects) when the tunnel cannot be created, so a failed tunnel never stops the
// server from serving players on the local network.
function startQuickTunnel({ port, scheme = 'http', log = console } = {}) {
  return new Promise((resolve) => {
    const target = tunnelTarget(port, scheme);
    const { command, version } = hasWrangler(log);
    const commandLine = [
      command,
      ...(command === 'npx' ? ['--yes', 'wrangler@latest'] : []),
      'tunnel', 'quick-start', shellQuote(target),
    ].join(' ');

    log.notice(`starting a Cloudflare quick tunnel to ${target} (${command}${version ? ' ' + version.split('\n')[0] : ''})`);

    let child;
    try {
      child = spawn(commandLine, {
        shell: true,
        stdio: ['ignore', 'pipe', 'pipe'],
        env: Object.assign({}, process.env, { WRANGLER_SEND_METRICS: 'false', CI: '1' }),
      });
    } catch (err) {
      log.warn(`could not start wrangler: ${err.message}`);
      return resolve(null);
    }

    let output = '';
    let settled = false;
    let url = null;

    const finish = (result) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      if (!result) {
        try { child.kill(); } catch {  }
      }
      resolve(result);
    };

    const timer = setTimeout(() => {
      log.warn('the Cloudflare tunnel did not report a URL in time; continuing without it');
      finish(null);
    }, START_TIMEOUT_MS);
    if (timer.unref) timer.unref();

    const scan = (chunk) => {
      output += chunk.toString();
      if (url) return;
      const match = URL_PATTERN.exec(output);
      if (!match) return;
      url = match[0];
      finish({
        url,
        target,
        stop() {
          try { child.kill(); } catch {  }
        },
      });
    };

    child.stdout.on('data', scan);
    child.stderr.on('data', scan);

    child.on('error', (err) => {
      log.warn(`Cloudflare tunnel failed: ${err.message}`);
      finish(null);
    });

    child.on('exit', (code) => {
      if (!settled) {
        const detail = output.trim().split('\n').slice(-3).join('\n');
        log.warn(`Cloudflare tunnel exited (code ${code}) before reporting a URL${detail ? `:\n${detail}` : ''}`);
        finish(null);
      }
    });
  });
}

module.exports = { localUrls, tunnelTarget, startQuickTunnel, isWildcardHost, URL_PATTERN };
