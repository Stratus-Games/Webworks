// The federation identity of this node server: a short id, a bridge secret, a claim
// secret and an Ed25519 key pair.
//
// - The id is what other servers and the GWAS directory see.
// - The bridge secret never leaves this machine except as a registration field sent to
//   GWAS over HTTPS, and GWAS only uses it to mint tickets this server can accept.
// - The claim secret is generated here on first start and sent once, over HTTPS, to
//   /servers/claim. GWAS keeps only a salted hash, so an id stays this machine's id without
//   anyone typing a password and without the worker being able to impersonate the server.
// - The key pair is how the *dialing* server proves it is really talking to this server
//   and not to whoever holds the bridge URL. GWAS publishes the public half; the private
//   half signs the link handshake.
//
// Losing this file re-registers the server under a new id; it loses no game data.

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const log = require('../log');

const FILE = 'federation.json';

let identity = null;

function generateKeys() {
  const { publicKey, privateKey } = crypto.generateKeyPairSync('ed25519');
  return {
    publicKey: publicKey.export({ type: 'spki', format: 'der' }).toString('base64url'),
    privateKey: privateKey.export({ type: 'pkcs8', format: 'der' }).toString('base64url'),
  };
}

function generate() {
  return {
    serverId: 'srv_' + crypto.randomBytes(9).toString('hex'),
    secret: crypto.randomBytes(32).toString('base64url'),
    claimSecret: crypto.randomBytes(32).toString('base64url'),
    ...generateKeys(),
    createdAt: Date.now(),
  };
}

function read(file) {
  try {
    if (!fs.existsSync(file)) return null;
    const parsed = JSON.parse(fs.readFileSync(file, 'utf8'));
    if (!parsed || typeof parsed.serverId !== 'string' || typeof parsed.secret !== 'string') return null;
    if (!/^[A-Za-z0-9_-]{4,64}$/.test(parsed.serverId) || parsed.secret.length < 32) return null;
    if (!parsed.publicKey || !parsed.privateKey) {
      // Identities created before link signing existed get keys in place; the id and
      // secret stay, so tickets minted by GWAS keep working.
      Object.assign(parsed, generateKeys());
    }
    if (typeof parsed.claimSecret !== 'string' || parsed.claimSecret.length < 32) {
      // Identities from before server claims existed get one; the next start claims the id
      // with it and nothing is asked of the operator.
      parsed.claimSecret = crypto.randomBytes(32).toString('base64url');
    }
    return parsed;
  } catch (err) {
    log.warn(`federation identity unreadable (${err.message}); a new one will be created`);
    return null;
  }
}

function persist(file, value) {
  try {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, JSON.stringify(value, null, 2) + '\n', { mode: 0o600 });
  } catch (err) {
    log.warn(`could not persist federation identity (${err.message}); it will change on restart`);
  }
}

function init({ dataDir, serverId, secret } = {}) {
  const fromEnv = {
    serverId: serverId || process.env.WEBWORKS_FEDERATION_SERVER_ID || '',
    secret: secret || process.env.WEBWORKS_FEDERATION_SECRET || '',
  };

  let loaded = null;
  if (dataDir) {
    const file = path.join(dataDir, FILE);
    loaded = read(file);
    if (!loaded) {
      loaded = generate();
      persist(file, loaded);
      log.info(`federation identity created: ${loaded.serverId}`);
    } else {
      persist(file, loaded);
    }
  } else {
    loaded = generate();
  }

  if (fromEnv.serverId) loaded.serverId = fromEnv.serverId;
  if (fromEnv.secret) loaded.secret = fromEnv.secret;

  identity = loaded;
  return identity;
}

function loaded() {
  return Boolean(identity);
}

function current() {
  if (!identity) identity = generate();
  return identity;
}

function publicKeyObject(base64url) {
  return crypto.createPublicKey({ key: Buffer.from(base64url, 'base64url'), format: 'der', type: 'spki' });
}

function privateKeyObject(base64url) {
  return crypto.createPrivateKey({ key: Buffer.from(base64url, 'base64url'), format: 'der', type: 'pkcs8' });
}

// Sign the handshake so the dialer can tell the real host from anyone else who
// happens to accept its ticket.
function signHandshake(linkId, peerId, self = current()) {
  return crypto.sign(null, Buffer.from(`${linkId}.${peerId}`), privateKeyObject(self.privateKey)).toString('base64url');
}

function verifyHandshake(linkId, peerId, signature, publicKeyB64) {
  try {
    return crypto.verify(
      null,
      Buffer.from(`${linkId}.${peerId}`),
      publicKeyObject(publicKeyB64),
      Buffer.from(String(signature), 'base64url'),
    );
  } catch {
    return false;
  }
}

function reset() {
  identity = null;
}

module.exports = {
  init,
  current,
  loaded,
  reset,
  generate,
  signHandshake,
  verifyHandshake,
  FILE,
};
