const express = require('express');
const { requireSession } = require('../authMiddleware');
const cloud = require('../cloudStore');
const gwas = require('../gwasService');
const log = require('../log');

const router = express.Router();

// Cloud saves are the one thing that must outlive a single server: browser builds are
// the main target and localStorage is not a save file. With a GWAS account the files
// live on the account service; without one they stay on this server's disk.
let warnedAt = 0;
function noteRemoteFailure(err) {
  if (Date.now() - warnedAt < 60000) return;
  warnedAt = Date.now();
  log.warn(`GWAS cloud saves unavailable (${err.code || err.message}); using local storage for now`);
}

const remoteToken = (req) => gwas.linkedToken(req);

router.get('/Files', requireSession, async (req, res) => {
  const { appId, steamId } = req.session;
  const token = remoteToken(req);
  if (token) {
    try {
      const data = await gwas.storage.list(appId, token);
      return res.json({ quota: data.quota, used: data.used, files: data.files || [] });
    } catch (err) {
      noteRemoteFailure(err);
    }
  }
  res.json({ ...cloud.usage(appId, steamId), files: cloud.list(appId, steamId) });
});

router.post('/Write', requireSession, async (req, res) => {
  const { name, data } = req.body || {};
  if (typeof data !== 'string') return res.status(400).json({ error: 'invalid_data' });

  const token = remoteToken(req);
  if (token) {
    try {
      const result = await gwas.storage.write(req.session.appId, token, name, data);
      return res.json(result);
    } catch (err) {
      if (err instanceof gwas.GwasError && err.status !== 503) {
        return res.status(err.status).json({ error: err.code });
      }
      noteRemoteFailure(err);
    }
  }

  const result = cloud.write(req.session.appId, req.session.steamId, name, Buffer.from(data, 'base64'));
  res.status(result.error ? (result.error === 'quota_exceeded' ? 413 : 400) : 200).json(result);
});

router.post('/Delete', requireSession, async (req, res) => {
  const { name } = req.body || {};
  const token = remoteToken(req);
  if (token) {
    try {
      const result = await gwas.storage.remove(req.session.appId, token, name);
      return res.json(result);
    } catch (err) {
      noteRemoteFailure(err);
    }
  }

  const result = cloud.remove(req.session.appId, req.session.steamId, name);
  res.status(result.error ? 400 : 200).json(result);
});

module.exports = router;
