// Guard rails for the one outbound connection a federated server ever makes.
//
// The directory tells us "the lobby you asked for lives on wss://host/federation/bridge".
// That is a URL chosen by a remote service, so it gets treated like user input: TLS only,
// no credentials, no loopback/private/link-local/metadata addresses, and optionally a
// hostname allow list. Without this a hostile (or compromised) directory entry could
// turn this server into an SSRF pivot into the operator's LAN or cloud metadata service.

const dns = require('dns');
const net = require('net');

const METADATA_HOSTS = new Set([
  'metadata',
  'metadata.google.internal',
  'instance-data',
  'instance-data.ec2.internal',
]);

function isPrivateIPv4(address) {
  const parts = address.split('.').map(Number);
  if (parts.length !== 4 || parts.some((n) => !Number.isInteger(n) || n < 0 || n > 255)) return true;
  const [a, b] = parts;
  if (a === 0) return true; // "this network"
  if (a === 10) return true;
  if (a === 127) return true;
  if (a === 169 && b === 254) return true; // link local + cloud metadata
  if (a === 172 && b >= 16 && b <= 31) return true;
  if (a === 192 && b === 168) return true;
  if (a === 192 && b === 0) return true; // IETF protocol assignments
  if (a === 100 && b >= 64 && b <= 127) return true; // CGNAT
  if (a === 198 && (b === 18 || b === 19)) return true; // benchmarking
  if (a === 192 && b === 0 && parts[2] === 2) return true; // TEST-NET-1
  if (a === 198 && b === 51 && parts[2] === 100) return true; // TEST-NET-2
  if (a === 203 && b === 0 && parts[2] === 113) return true; // TEST-NET-3
  if (a >= 224) return true; // multicast + reserved + broadcast
  return false;
}

function isPrivateIPv6(address) {
  const value = address.toLowerCase().split('%')[0];
  if (value === '::' || value === '::1') return true;
  if (value.startsWith('fc') || value.startsWith('fd')) return true; // unique local
  if (value.startsWith('fe8') || value.startsWith('fe9') || value.startsWith('fea') || value.startsWith('feb')) return true; // link local
  if (value.startsWith('ff')) return true; // multicast
  if (value.startsWith('2001:db8')) return true; // documentation
  // IPv4-mapped (::ffff:10.0.0.1) and NAT64 (64:ff9b::/96) forms.
  const mapped = value.match(/::ffff:(\d+\.\d+\.\d+\.\d+)$/);
  if (mapped) return isPrivateIPv4(mapped[1]);
  if (value.startsWith('64:ff9b::')) return true;
  return false;
}

function isPublicAddress(address) {
  const family = net.isIP(address);
  if (family === 4) return !isPrivateIPv4(address);
  if (family === 6) return !isPrivateIPv6(address);
  return false;
}

function isIpLiteral(hostname) {
  return net.isIP(hostname.replace(/^\[|\]$/g, '')) !== 0;
}

function hostnameAllowed(hostname, allowedHosts) {
  if (!allowedHosts || allowedHosts.length === 0) return true;
  const host = hostname.toLowerCase();
  return allowedHosts.some((pattern) => {
    const value = String(pattern).toLowerCase().trim();
    if (!value) return false;
    if (value.startsWith('*.')) return host === value.slice(2) || host.endsWith(value.slice(1));
    return host === value;
  });
}

// Synchronous part of the check: shape, scheme, literals and allow list. Returns
// { ok: true, url } or { ok: false, error }.
function parseBridgeUrl(raw, { allowInsecure = false, allowedHosts = [] } = {}) {
  if (typeof raw !== 'string' || raw.length === 0 || raw.length > 2048) return { ok: false, error: 'invalid_url' };

  let url;
  try {
    url = new URL(raw);
  } catch {
    return { ok: false, error: 'invalid_url' };
  }

  const scheme = url.protocol === 'wss:' || (allowInsecure && url.protocol === 'ws:');
  if (!scheme) return { ok: false, error: 'insecure_bridge_scheme' };
  if (url.username || url.password) return { ok: false, error: 'bridge_url_has_credentials' };
  if (!url.hostname) return { ok: false, error: 'invalid_url' };
  if (url.port && (Number(url.port) < 1 || Number(url.port) > 65535)) return { ok: false, error: 'invalid_port' };

  const hostname = url.hostname.toLowerCase().replace(/^\[|\]$/g, '');
  // `allowInsecure` is the development escape hatch: it exists so two servers on
  // localhost can bridge without TLS. Production keeps wss + public addresses only.
  if (!allowInsecure) {
    if (hostname === 'localhost' || hostname.endsWith('.localhost') || hostname.endsWith('.local') || hostname.endsWith('.internal')) {
      return { ok: false, error: 'bridge_host_not_public' };
    }
    if (METADATA_HOSTS.has(hostname)) return { ok: false, error: 'bridge_host_not_public' };
    if (isIpLiteral(hostname) && !isPublicAddress(hostname)) return { ok: false, error: 'bridge_host_not_public' };
  }
  if (!hostnameAllowed(hostname, allowedHosts)) return { ok: false, error: 'bridge_host_not_allowed' };

  return { ok: true, url };
}

// Async part: resolve the hostname and refuse anything that answers with a private
// address. This is not a full DNS-rebinding defence (the TLS handshake still resolves
// again), but combined with wss-only it stops the obvious pivot into a LAN/metadata
// service and makes an operator's allow list meaningful.
async function assertSafeBridgeUrl(raw, options = {}) {
  const parsed = parseBridgeUrl(raw, options);
  if (!parsed.ok) return parsed;

  if (options.allowInsecure) return parsed;

  const hostname = parsed.url.hostname.replace(/^\[|\]$/g, '');
  if (isIpLiteral(hostname)) return parsed;

  let addresses;
  try {
    addresses = await dns.promises.lookup(hostname, { all: true, verbatim: true });
  } catch {
    return { ok: false, error: 'bridge_host_unresolvable' };
  }
  if (!addresses.length) return { ok: false, error: 'bridge_host_unresolvable' };
  for (const entry of addresses) {
    if (!isPublicAddress(entry.address)) return { ok: false, error: 'bridge_host_not_public' };
  }
  return parsed;
}

module.exports = {
  parseBridgeUrl,
  assertSafeBridgeUrl,
  isPublicAddress,
  isPrivateIPv4,
  isPrivateIPv6,
  hostnameAllowed,
};
