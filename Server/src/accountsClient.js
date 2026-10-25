const WORKER_URL = (process.env.WEBWORKS_ACCOUNTS_WORKER_URL || 'https://gwas.webeam.workers.dev').replace(/\/+$/, '');

function accountsEnabled() {
  return Boolean(WORKER_URL);
}

async function verifyAccountToken(token) {
  if (!token) return null;
  if (!accountsEnabled()) {
    console.warn('[webworks] got an accountToken but WEBWORKS_ACCOUNTS_WORKER_URL is not set on this server -- falling back to guest identity. Set that env var to the same Accounts Worker URL the shim uses.');
    return null;
  }
  try {
    const resp = await fetch(`${WORKER_URL}/accounts/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!resp.ok) return null;
    const data = await resp.json();
    return data.account || null;
  } catch (err) {
    console.warn('[webworks] accounts worker verify failed:', err.message);
    return null;
  }
}

async function fetchAccountFriends(token) {
  if (!accountsEnabled() || !token) return null;
  try {
    const resp = await fetch(`${WORKER_URL}/accounts/friends`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!resp.ok) return null;
    return await resp.json();
  } catch (err) {
    console.warn('[webworks] accounts worker friends fetch failed:', err.message);
    return null;
  }
}

module.exports = { accountsEnabled, verifyAccountToken, fetchAccountFriends };