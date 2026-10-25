const crypto = require('crypto');

const FAKE_STEAMID_BASE = 90071992547409920n;

function generateGuestSteamId() {
  const rand = BigInt('0x' + crypto.randomBytes(6).toString('hex'));
  return (FAKE_STEAMID_BASE + rand).toString();
}

const FAKE_LOBBYID_BASE = 109775240000000000n;

function generateLobbyId() {
  const rand = BigInt('0x' + crypto.randomBytes(5).toString('hex'));
  return (FAKE_LOBBYID_BASE + rand).toString();
}

const FAKE_WORKSHOPID_BASE = 305680000000000000n;

function generateWorkshopId() {
  const rand = BigInt('0x' + crypto.randomBytes(5).toString('hex'));
  return (FAKE_WORKSHOPID_BASE + rand).toString();
}

const FAKE_SERVERID_BASE = 90200000000000000n;

function generateServerId() {
  const rand = BigInt('0x' + crypto.randomBytes(5).toString('hex'));
  return (FAKE_SERVERID_BASE + rand).toString();
}

const ADJECTIVES = ['Swift', 'Lucky', 'Shadow', 'Iron', 'Quiet', 'Rusty', 'Wild', 'Clever', 'Salty', 'Brave'];
const NOUNS = ['Wolf', 'Falcon', 'Badger', 'Otter', 'Raven', 'Fox', 'Bear', 'Hawk', 'Lynx', 'Viper'];

function generateGuestName() {
  const a = ADJECTIVES[Math.floor(Math.random() * ADJECTIVES.length)];
  const n = NOUNS[Math.floor(Math.random() * NOUNS.length)];
  const num = Math.floor(Math.random() * 9000) + 1000;
  return `${a}${n}${num}`;
}

function generateSessionToken() {
  return crypto.randomBytes(24).toString('hex');
}

function stableIdentity(appId, deviceKey) {
  const hash = crypto.createHash('sha256').update(`${appId}:${deviceKey}`).digest();
  const steamId = (FAKE_STEAMID_BASE + BigInt('0x' + hash.subarray(0, 6).toString('hex'))).toString();
  const name = `${ADJECTIVES[hash[6] % ADJECTIVES.length]}${NOUNS[hash[7] % NOUNS.length]}${1000 + ((hash[8] << 8) | hash[9]) % 9000}`;
  return { steamId, name };
}

function globalAccountIdentity(username) {
  const hash = crypto.createHash('sha256').update(`global:${username.toLowerCase()}`).digest();
  const steamId = (FAKE_STEAMID_BASE + BigInt('0x' + hash.subarray(0, 6).toString('hex'))).toString();
  return { steamId };
}

module.exports = {
  generateLobbyId,
  generateWorkshopId,
  generateServerId,
  generateGuestSteamId,
  generateGuestName,
  generateSessionToken,
  stableIdentity,
  globalAccountIdentity,
};
