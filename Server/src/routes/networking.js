const express = require('express');
const crypto = require('crypto');
const { requireSession } = require('../authMiddleware');
const { pushToPeer } = require('../realtime');
const { state } = require('../state');
const relayManager = require('../relayManager');

const router = express.Router();

router.post('/AcceptSession', requireSession, (req, res) => {
  pushToPeer(req.session, req.body.peerSteamId, {
    type: 'P2PSessionAccepted',
    from: req.session.steamId,
  });
  res.json({ ok: true });
});

router.post('/CloseSession', requireSession, (req, res) => {
  pushToPeer(req.session, req.body.peerSteamId, {
    type: 'P2PSessionClosed',
    from: req.session.steamId,
  });
  res.json({ ok: true });
});

router.post('/RelayTicket', requireSession, async (req, res) => {
  const { lobbyId } = req.body || {};
  const lobby = lobbyId && state.lobbies.get(lobbyId);
  if (!lobby || !lobby.members.has(req.session.steamId)) {
    return res.status(403).json({ error: 'not_a_lobby_member' });
  }

  try {
    const udpHost = (req.hostname || 'localhost');
    const { wsPort, udpPort } = await relayManager.ensureRelay(lobbyId, req.session.appId, udpHost);

    const expiresAt = Date.now() + 5 * 60 * 1000;
    const payload = `${req.session.steamId}.${lobbyId}.${expiresAt}`;
    const signature = crypto.createHmac('sha256', process.env.WEBWORKS_TICKET_SECRET || 'dev-secret-change-me').update(payload).digest('hex');
    const ticket = Buffer.from(`${payload}.${signature}`).toString('base64');

    const wsProtocol = req.protocol === 'https' ? 'wss' : 'ws';
    res.json({
      wsUrl: `${wsProtocol}://${udpHost}:${wsPort}`,
      udpHost,
      udpPort,
      ticket,
      expiresAt,
    });
  } catch (err) {
    res.status(503).json({ error: 'relay_unavailable', message: err.message });
  }
});

module.exports = router;
