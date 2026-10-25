
const fs = require('fs');
const path = require('path');
const { CACHE_DIR } = require('./leaderboardStore');
const { generateWorkshopId } = require('./identity');

const ITEM_QUOTA = Math.max(1024, Math.floor(Number(process.env.WEBWORKS_WORKSHOP_ITEM_QUOTA_BYTES)) || 64 * 1024 * 1024);
const MAX_ITEMS_PER_APP = Math.max(1, Math.floor(Number(process.env.WEBWORKS_WORKSHOP_MAX_ITEMS)) || 5000);

const games = new Map();

function dirFor(appId) {
  return path.join(CACHE_DIR, 'workshop', appId);
}

function fileFor(appId, fileId) {
  return path.join(dirFor(appId), fileId + '.json');
}

function appItems(appId) {
  if (!games.has(appId)) games.set(appId, new Map());
  return games.get(appId);
}

function contentSize(content) {
  if (!content) return 0;
  let total = 0;
  for (const b64 of Object.values(content)) total += Buffer.byteLength(String(b64), 'base64');
  return total;
}

function save(appId, item) {
  fs.mkdirSync(dirFor(appId), { recursive: true });
  const target = fileFor(appId, item.id);
  const tmp = target + '.tmp';
  fs.writeFileSync(tmp, JSON.stringify(item));
  fs.renameSync(tmp, target);
}

function load() {
  const root = path.join(CACHE_DIR, 'workshop');
  let appDirs;
  try { appDirs = fs.readdirSync(root, { withFileTypes: true }); } catch { return; }

  for (const d of appDirs) {
    if (!d.isDirectory()) continue;
    const appId = d.name;
    const map = appItems(appId);
    let files;
    try { files = fs.readdirSync(dirFor(appId)); } catch { continue; }
    for (const f of files) {
      if (!f.endsWith('.json')) continue;
      try {
        const item = JSON.parse(fs.readFileSync(path.join(dirFor(appId), f), 'utf8'));
        if (item && item.id) map.set(item.id, item);
      } catch (err) {
        console.warn(`[webworks] skipping unreadable workshop item ${f}: ${err.message}`);
      }
    }
  }
}

function init() {
  fs.mkdirSync(path.join(CACHE_DIR, 'workshop'), { recursive: true });
  load();
  let count = 0;
  for (const map of games.values()) count += map.size;
  return { dir: path.join(CACHE_DIR, 'workshop'), items: count, quota: ITEM_QUOTA };
}

function validTitle(t) {
  return typeof t === 'string' && t.trim().length > 0 && t.length <= 128;
}

function cleanTags(tags) {
  if (!Array.isArray(tags)) return [];
  return [...new Set(tags.filter((t) => typeof t === 'string' && t.length > 0 && t.length <= 64).slice(0, 32))];
}

function publish(appId, session, body) {
  const { title, description, tags, previewName, previewData, content, changelog } = body || {};
  if (!validTitle(title)) return { error: 'invalid_title' };

  const map = appItems(appId);
  if (map.size >= MAX_ITEMS_PER_APP) return { error: 'too_many_items' };

  const size = contentSize(content) + (previewData ? Buffer.byteLength(String(previewData), 'base64') : 0);
  if (size > ITEM_QUOTA) return { error: 'quota_exceeded', quota: ITEM_QUOTA };

  const now = Date.now();
  const item = {
    id: generateWorkshopId(),
    appId,
    ownerId: session.steamId,
    ownerName: session.name,
    title: String(title).trim(),
    description: typeof description === 'string' ? description.slice(0, 8000) : '',
    tags: cleanTags(tags),
    previewName: typeof previewName === 'string' ? previewName : null,
    previewData: typeof previewData === 'string' ? previewData : null,
    content: content && typeof content === 'object' ? content : {},
    created: now,
    updated: now,
    changelog: typeof changelog === 'string' ? changelog.slice(0, 4000) : '',
    subscribers: [session.steamId],
    favorites: [],
    votes: {},
  };

  map.set(item.id, item);
  save(appId, item);
  return { ok: true, fileId: item.id };
}

function find(appId, fileId) {
  const map = games.get(appId);
  return map ? map.get(String(fileId)) || null : null;
}

function update(appId, fileId, session, patch) {
  const item = find(appId, fileId);
  if (!item) return { error: 'not_found' };
  if (item.ownerId !== session.steamId) return { error: 'not_owner' };

  const { title, description, tags, previewName, previewData, content, replaceContent, changelog } = patch || {};
  if (title !== undefined) {
    if (!validTitle(title)) return { error: 'invalid_title' };
    item.title = String(title).trim();
  }
  if (description !== undefined) item.description = String(description).slice(0, 8000);
  if (tags !== undefined) item.tags = cleanTags(tags);
  if (previewName !== undefined) item.previewName = previewName;
  if (previewData !== undefined) item.previewData = previewData;
  if (content !== undefined && typeof content === 'object') {
    item.content = replaceContent ? content : { ...item.content, ...content };
  }
  if (changelog !== undefined) item.changelog = String(changelog).slice(0, 4000);

  const size = contentSize(item.content) + (item.previewData ? Buffer.byteLength(String(item.previewData), 'base64') : 0);
  if (size > ITEM_QUOTA) return { error: 'quota_exceeded', quota: ITEM_QUOTA };

  item.updated = Date.now();
  save(appId, item);
  return { ok: true, fileId: item.id };
}

function remove(appId, fileId, session) {
  const item = find(appId, fileId);
  if (!item) return { ok: true, existed: false };
  if (item.ownerId !== session.steamId) return { error: 'not_owner' };
  games.get(appId).delete(item.id);
  try { fs.unlinkSync(fileFor(appId, item.id)); } catch {  }
  return { ok: true, existed: true };
}

function toggle(set, steamId, on) {
  const idx = set.indexOf(steamId);
  if (on && idx < 0) set.push(steamId);
  if (!on && idx >= 0) set.splice(idx, 1);
}

function subscribe(appId, fileId, steamId, on) {
  const item = find(appId, fileId);
  if (!item) return { error: 'not_found' };
  toggle(item.subscribers, steamId, on);
  save(appId, item);
  return { ok: true, subscribed: on };
}

function favorite(appId, fileId, steamId, on) {
  const item = find(appId, fileId);
  if (!item) return { error: 'not_found' };
  toggle(item.favorites, steamId, on);
  save(appId, item);
  return { ok: true, favorited: on };
}

function vote(appId, fileId, steamId, up) {
  const item = find(appId, fileId);
  if (!item) return { error: 'not_found' };
  item.votes[steamId] = up ? 'up' : 'down';
  save(appId, item);
  return { ok: true, ...voteCounts(item) };
}

function voteCounts(item) {
  let up = 0, down = 0;
  for (const v of Object.values(item.votes)) { if (v === 'up') up++; else down++; }
  return { votesUp: up, votesDown: down };
}

function userVote(appId, fileId, steamId) {
  const item = find(appId, fileId);
  const v = item ? item.votes[steamId] : undefined;
  return { votedUp: v === 'up', votedDown: v === 'down' };
}

function view(item) {
  const { up, down } = (() => { const c = voteCounts(item); return { up: c.votesUp, down: c.votesDown }; })();
  const total = up + down;
  return {
    id: item.id,
    appId: item.appId,
    ownerId: item.ownerId,
    ownerName: item.ownerName,
    title: item.title,
    description: item.description,
    tags: item.tags,
    previewName: item.previewName,
    previewData: item.previewData,
    created: item.created,
    updated: item.updated,
    changelog: item.changelog,
    subscriberCount: item.subscribers.length,
    favoriteCount: item.favorites.length,
    votesUp: up,
    votesDown: down,
    score: total > 0 ? up / total : 0,
    sizeBytes: contentSize(item.content) + (item.previewData ? Buffer.byteLength(String(item.previewData), 'base64') : 0),
    visibility: 'public',
    price: 0,
  };
}

const SORTERS = {
  creationdate: (a, b) => b.created - a.created,
  creationdateasc: (a, b) => a.created - b.created,
  updatedate: (a, b) => b.updated - a.updated,
  title: (a, b) => a.title.localeCompare(b.title),
  vote: (a, b) => voteCounts(b).votesUp - voteCounts(b).votesDown - (voteCounts(a).votesUp - voteCounts(a).votesDown),
  subscriptions: (a, b) => b.subscribers.length - a.subscribers.length,
};

function query(appId, opts) {
  const map = games.get(appId);
  let items = map ? [...map.values()] : [];

  const { searchText, tags, matchAllTags, type, steamId } = opts;

  if (type === 'published' && steamId) items = items.filter((i) => i.ownerId === steamId);
  else if (type === 'subscribed' && steamId) items = items.filter((i) => i.subscribers.includes(steamId));
  else if (type === 'favorited' && steamId) items = items.filter((i) => i.favorites.includes(steamId));
  else if (type === 'votedup' && steamId) items = items.filter((i) => i.votes[steamId] === 'up');
  else if (type === 'voteddown' && steamId) items = items.filter((i) => i.votes[steamId] === 'down');
  else if (type === 'votedon' && steamId) items = items.filter((i) => i.votes[steamId] !== undefined);

  if (searchText) {
    const needle = String(searchText).toLowerCase();
    items = items.filter((i) => i.title.toLowerCase().includes(needle) || i.description.toLowerCase().includes(needle));
  }

  if (Array.isArray(tags) && tags.length) {
    items = items.filter((i) => (matchAllTags ? tags.every((t) => i.tags.includes(t)) : tags.some((t) => i.tags.includes(t))));
  }

  const sorter = SORTERS[String(opts.sort || 'creationdate').toLowerCase()] || SORTERS.creationdate;
  items.sort(sorter);

  const totalCount = items.length;
  const perPage = Math.max(1, Math.min(50, Number(opts.perPage) || 50));
  const page = Math.max(1, Number(opts.page) || 1);
  const slice = items.slice((page - 1) * perPage, (page - 1) * perPage + perPage).map(view);

  return { resultCount: slice.length, totalCount, entries: slice };
}

function content(appId, fileId) {
  const item = find(appId, fileId);
  return item ? item.content : null;
}

module.exports = {
  init, publish, update, remove, find, view, query,
  subscribe, favorite, vote, userVote, content,
  ITEM_QUOTA, MAX_ITEMS_PER_APP,
};
