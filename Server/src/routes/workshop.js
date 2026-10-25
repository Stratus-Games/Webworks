const express = require('express');
const { requireSession } = require('../authMiddleware');
const workshop = require('../workshopStore');

const router = express.Router();

const bad = (res, error) => res.status(error === 'quota_exceeded' ? 413 : error === 'not_owner' ? 403 : 400).json({ error });

router.post('/Publish', requireSession, (req, res) => {
  const result = workshop.publish(req.session.appId, req.session, req.body || {});
  if (result.error) return bad(res, result.error);
  res.json(result);
});

router.post('/Update', requireSession, (req, res) => {
  const { fileId, ...patch } = req.body || {};
  const result = workshop.update(req.session.appId, fileId, req.session, patch);
  if (result.error) return bad(res, result.error);
  res.json(result);
});

router.post('/Delete', requireSession, (req, res) => {
  const result = workshop.remove(req.session.appId, (req.body || {}).fileId, req.session);
  if (result.error) return bad(res, result.error);
  res.json(result);
});

router.get('/Item', requireSession, (req, res) => {
  const item = workshop.find(req.session.appId, req.query.id);
  res.json(item ? { found: true, item: workshop.view(item) } : { found: false });
});

router.get('/Download', requireSession, (req, res) => {
  const c = workshop.content(req.session.appId, req.query.id);
  res.json(c ? { found: true, content: c } : { found: false });
});

router.post('/Query', requireSession, (req, res) => {
  const b = req.body || {};
  const result = workshop.query(req.session.appId, {
    searchText: b.searchText,
    tags: b.tags,
    matchAllTags: !!b.matchAllTags,
    type: b.type ? String(b.type).toLowerCase() : undefined,
    steamId: b.userId || req.session.steamId,
    sort: b.sort,
    page: b.page,
    perPage: b.perPage,
  });
  res.json(result);
});

router.post('/Subscribe', requireSession, (req, res) => {
  const result = workshop.subscribe(req.session.appId, (req.body || {}).fileId, req.session.steamId, true);
  if (result.error) return bad(res, result.error);
  res.json(result);
});

router.post('/Unsubscribe', requireSession, (req, res) => {
  const result = workshop.subscribe(req.session.appId, (req.body || {}).fileId, req.session.steamId, false);
  if (result.error) return bad(res, result.error);
  res.json(result);
});

router.post('/Favorite', requireSession, (req, res) => {
  const result = workshop.favorite(req.session.appId, (req.body || {}).fileId, req.session.steamId, true);
  if (result.error) return bad(res, result.error);
  res.json(result);
});

router.post('/Unfavorite', requireSession, (req, res) => {
  const result = workshop.favorite(req.session.appId, (req.body || {}).fileId, req.session.steamId, false);
  if (result.error) return bad(res, result.error);
  res.json(result);
});

router.post('/Vote', requireSession, (req, res) => {
  const { fileId, up } = req.body || {};
  const result = workshop.vote(req.session.appId, fileId, req.session.steamId, !!up);
  if (result.error) return bad(res, result.error);
  res.json(result);
});

router.get('/Vote', requireSession, (req, res) => {
  res.json(workshop.userVote(req.session.appId, req.query.fileId, req.session.steamId));
});

module.exports = router;
