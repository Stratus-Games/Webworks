
const fs = require('fs');
const path = require('path');

const MAX_ENTRIES = Math.max(1, Math.floor(Number(process.env.WEBWORKS_LEADERBOARD_MAX)) || 30);
const CACHE_DIR = process.env.WEBWORKS_CACHE_DIR
  ? path.resolve(process.env.WEBWORKS_CACHE_DIR)
  : path.join(__dirname, '..', 'cache');
const FILE = path.join(CACHE_DIR, 'leaderboard.json');

const SORTS = new Set(['ascending', 'descending', 'none']);
const DISPLAYS = new Set(['numeric', 'timeseconds', 'timemilliseconds', 'none']);

const games = new Map();
let saveTimer = null;

function clampInt(n) {
  n = Math.trunc(Number(n));
  if (!Number.isFinite(n)) return null;
  return Math.max(-2147483648, Math.min(2147483647, n));
}

function cleanDetails(details) {
  if (!Array.isArray(details)) return [];
  return details.slice(0, 64).map(clampInt).filter((n) => n !== null);
}

function validName(name) {
  return typeof name === 'string' && name.length > 0 && name.length <= 128;
}

function isBetter(board, a, b) {
  return board.sort === 'ascending' ? a < b : a > b;
}

function sortBoard(board) {
  const asc = board.sort === 'ascending';
  board.entries.sort((x, y) => (x.score === y.score ? x.updatedAt - y.updatedAt : asc ? x.score - y.score : y.score - x.score));
}

function gameBoards(appId) {
  if (!games.has(appId)) games.set(appId, new Map());
  return games.get(appId);
}

function toJSON() {
  const out = {};
  for (const [appId, boards] of games) {
    const g = {};
    for (const [name, board] of boards) {
      g[name] = {
        sort: board.sort,
        display: board.display,
        entries: board.entries.map((e, i) => ({
          rank: i + 1,
          steamId: e.steamId,
          name: e.name,
          score: e.score,
          details: e.details,
          updatedAt: new Date(e.updatedAt).toISOString(),
        })),
      };
    }
    out[appId] = g;
  }
  return { version: 1, maxEntriesPerBoard: MAX_ENTRIES, updatedAt: new Date().toISOString(), games: out };
}

function saveNow() {
  if (saveTimer) { clearTimeout(saveTimer); saveTimer = null; }
  try {
    fs.mkdirSync(CACHE_DIR, { recursive: true });
    const tmp = FILE + '.tmp';
    fs.writeFileSync(tmp, JSON.stringify(toJSON(), null, 2));
    fs.renameSync(tmp, FILE);
  } catch (err) {
    console.error('[webworks] could not write ' + FILE + ': ' + err.message);
  }
}

function scheduleSave() {
  if (saveTimer) return;
  saveTimer = setTimeout(saveNow, 200);
  if (saveTimer.unref) saveTimer.unref();
}

function load() {
  let raw;
  try {
    raw = fs.readFileSync(FILE, 'utf8');
  } catch {
    return;
  }

  let data;
  try {
    data = JSON.parse(raw);
  } catch {
    const bak = FILE + '.corrupt-' + Date.now();
    try { fs.renameSync(FILE, bak); } catch {  }
    console.warn('[webworks] leaderboard.json was not valid JSON; moved to ' + path.basename(bak) + ' and starting fresh');
    return;
  }

  for (const [appId, boards] of Object.entries((data && data.games) || {})) {
    for (const [name, b] of Object.entries(boards || {})) {
      if (!validName(name) || !b) continue;
      const board = {
        sort: SORTS.has(b.sort) ? b.sort : 'descending',
        display: DISPLAYS.has(b.display) ? b.display : 'numeric',
        entries: [],
      };
      for (const e of Array.isArray(b.entries) ? b.entries : []) {
        const score = clampInt(e && e.score);
        if (score === null || !e.steamId) continue;
        board.entries.push({
          steamId: String(e.steamId),
          name: String(e.name || ''),
          score,
          details: cleanDetails(e.details),
          updatedAt: Date.parse(e.updatedAt) || Date.now(),
        });
      }
      sortBoard(board);
      board.entries.length = Math.min(board.entries.length, MAX_ENTRIES);
      gameBoards(appId).set(name, board);
    }
  }
}

function init() {
  fs.mkdirSync(CACHE_DIR, { recursive: true });
  load();
  saveNow();
  return { file: FILE, max: MAX_ENTRIES };
}

function find(appId, name) {
  const boards = games.get(appId);
  return boards ? boards.get(name) || null : null;
}

function findOrCreate(appId, name, sort, display) {
  const boards = gameBoards(appId);
  let board = boards.get(name);
  if (!board) {
    board = {
      sort: SORTS.has(sort) ? sort : 'descending',
      display: DISPLAYS.has(display) ? display : 'numeric',
      entries: [],
    };
    boards.set(name, board);
    scheduleSave();
  }
  return board;
}

function submit(appId, name, session, rawScore, rawDetails, forceUpdate) {
  const board = findOrCreate(appId, name);
  const score = clampInt(rawScore);
  if (score === null) return { error: 'invalid_score' };

  const details = cleanDetails(rawDetails);
  const now = Date.now();
  const idx = board.entries.findIndex((e) => e.steamId === session.steamId);
  const oldRank = idx >= 0 ? idx + 1 : 0;
  let changed = false;

  if (idx >= 0) {
    const cur = board.entries[idx];
    cur.name = session.name;
    if (forceUpdate || isBetter(board, score, cur.score)) {
      cur.score = score;
      cur.details = details;
      cur.updatedAt = now;
      changed = true;
    }
  } else {
    board.entries.push({ steamId: session.steamId, name: session.name, score, details, updatedAt: now });
    changed = true;
  }

  sortBoard(board);
  if (board.entries.length > MAX_ENTRIES) board.entries.length = MAX_ENTRIES;

  const newIdx = board.entries.findIndex((e) => e.steamId === session.steamId);
  const newRank = newIdx >= 0 ? newIdx + 1 : 0;
  scheduleSave();

  return {
    score: newIdx >= 0 ? board.entries[newIdx].score : score,
    changed: changed && newRank > 0,
    newGlobalRank: newRank,
    oldGlobalRank: oldRank,
    entryCount: board.entries.length,
  };
}

function entryView(e, i) {
  return { steamId: e.steamId, name: e.name, globalRank: i + 1, score: e.score, details: e.details };
}

function entries(appId, name, steamId, type, opts) {
  const board = find(appId, name);
  if (!board) return null;
  let slice;

  if (type === 'around') {
    const at = board.entries.findIndex((e) => e.steamId === steamId);
    if (at < 0) return { entryCount: board.entries.length, entries: [] };
    const from = Math.max(0, at + (opts.start ?? -10));
    const to = Math.min(board.entries.length - 1, at + (opts.end ?? 10));
    slice = board.entries.map(entryView).slice(from, to + 1);
  } else if (type === 'friends') {
    slice = board.entries.map(entryView).filter((e) => e.steamId === steamId);
  } else {
    const start = Math.max(1, opts.offset || 1);
    const count = Math.max(0, Math.min(MAX_ENTRIES, opts.count || MAX_ENTRIES));
    slice = board.entries.map(entryView).slice(start - 1, start - 1 + count);
  }
  return { entryCount: board.entries.length, entries: slice };
}

function boardView(board, name) {
  return { name, sort: board.sort, display: board.display, entryCount: board.entries.length };
}

module.exports = { init, saveNow, find, findOrCreate, submit, entries, boardView, validName, MAX_ENTRIES, FILE, CACHE_DIR };
