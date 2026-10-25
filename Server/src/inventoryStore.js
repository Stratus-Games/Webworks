const fs = require('fs');
const path = require('path');
const log = require('./log');

class InventoryStore {
  constructor() {
    this.file = null;
    this.db = { version: 1, definitions: {}, items: {} };
  }

  init(dataDir) {
    this.file = path.join(dataDir, 'inventory.json');
    try {
      if (fs.existsSync(this.file)) {
        const parsed = JSON.parse(fs.readFileSync(this.file, 'utf8'));
        this.db = {
          version: 1,
          definitions: parsed.definitions || {},
          items: parsed.items || {},
        };
      }
    } catch (err) {
      log.warn(`inventory.json was not readable (${err.message}); starting empty`);
    }
    return this;
  }

  _save() {
    if (!this.file) return;
    const tmp = `${this.file}.tmp`;
    fs.writeFileSync(tmp, JSON.stringify(this.db, null, 2));
    fs.renameSync(tmp, this.file);
  }

  _defs(appId) {
    if (!this.db.definitions[appId]) this.db.definitions[appId] = {};
    return this.db.definitions[appId];
  }

  _items(appId, steamId) {
    if (!this.db.items[appId]) this.db.items[appId] = {};
    if (!this.db.items[appId][steamId]) this.db.items[appId][steamId] = {};
    return this.db.items[appId][steamId];
  }

  definitions(appId) {
    return Object.entries(this._defs(appId)).map(([id, def]) => Object.assign({ itemDefId: id }, def));
  }

  define(appId, itemDefId, def) {
    const id = String(itemDefId || '').trim();
    if (!id || id.length > 64) return { error: 'invalid_item_def_id' };
    this._defs(appId)[id] = {
      name: String((def && def.name) || id).slice(0, 128),
      description: String((def && def.description) || '').slice(0, 1024),
      type: String((def && def.type) || 'item').slice(0, 32),
      iconUrl: String((def && def.iconUrl) || '').slice(0, 512),
      tradable: Boolean(def && def.tradable),
      marketable: Boolean(def && def.marketable),
      clientGrantable: Boolean(def && def.clientGrantable),
      promo: Boolean(def && def.promo),
      dropList: String((def && def.dropList) || '').slice(0, 128),
    };
    this._save();
    return { ok: true, itemDefId: id };
  }

  items(appId, steamId) {
    const owned = this._items(appId, steamId);
    const defs = this._defs(appId);
    return Object.entries(owned).map(([itemDefId, entry]) => ({
      itemDefId,
      quantity: entry.quantity,
      acquiredAt: entry.acquiredAt,
      properties: entry.properties || {},
      definition: defs[itemDefId] || null,
    }));
  }

  grant(appId, steamId, itemDefId, quantity = 1) {
    const id = String(itemDefId || '');
    if (!this._defs(appId)[id]) return { error: 'unknown_item_def' };
    const qty = Math.max(1, Math.min(9999, Math.floor(Number(quantity)) || 1));
    const owned = this._items(appId, steamId);
    const entry = owned[id] || { quantity: 0, acquiredAt: Date.now(), properties: {} };
    entry.quantity += qty;
    owned[id] = entry;
    this._save();
    return { ok: true, itemDefId: id, quantity: entry.quantity };
  }

  isClientGrantable(appId, itemDefId) {
    const def = this._defs(appId)[String(itemDefId)];
    return !!(def && def.clientGrantable);
  }

  consume(appId, steamId, itemDefId, quantity = 1) {
    const owned = this._items(appId, steamId);
    const entry = owned[String(itemDefId)];
    if (!entry) return { error: 'not_owned' };
    const qty = Math.max(1, Math.floor(Number(quantity)) || 1);
    if (entry.quantity < qty) return { error: 'not_enough_items' };
    entry.quantity -= qty;
    if (entry.quantity <= 0) delete owned[String(itemDefId)];
    this._save();
    return { ok: true, itemDefId: String(itemDefId), remaining: entry.quantity || 0 };
  }

  item(appId, steamId, itemDefId) {
    return this._items(appId, steamId)[String(itemDefId)] || null;
  }

  setProperties(appId, steamId, itemDefId, properties) {
    const entry = this._items(appId, steamId)[String(itemDefId)];
    if (!entry) return { error: 'not_owned' };
    const next = { ...(entry.properties || {}) };
    for (const [key, value] of Object.entries(properties || {})) {
      if (!key || key.length > 64) continue;
      if (value === null) delete next[key];
      else next[key] = String(value).slice(0, 512);
    }
    entry.properties = next;
    this._save();
    return { ok: true, itemDefId: String(itemDefId), properties: next };
  }

  // Destroy some items and create others in one step (exchange recipes).
  exchange(appId, steamId, destroy = [], generate = []) {
    for (const entry of destroy) {
      const owned = this._items(appId, steamId)[String(entry.itemDefId)];
      if (!owned || owned.quantity < Math.max(1, Math.floor(Number(entry.quantity)) || 1)) {
        return { error: 'not_enough_items', itemDefId: String(entry.itemDefId) };
      }
    }
    for (const entry of generate) {
      if (!this._defs(appId)[String(entry.itemDefId)]) return { error: 'unknown_item_def' };
    }

    for (const entry of destroy) this.consume(appId, steamId, entry.itemDefId, entry.quantity);
    const produced = [];
    for (const entry of generate) {
      this.grant(appId, steamId, entry.itemDefId, entry.quantity);
      produced.push({ itemDefId: String(entry.itemDefId), quantity: this._items(appId, steamId)[String(entry.itemDefId)].quantity });
    }
    return { ok: true, generated: produced };
  }

  transfer(appId, fromSteamId, toSteamId, itemDefId, quantity = 1) {
    const def = this._defs(appId)[String(itemDefId)];
    if (!def) return { error: 'unknown_item_def' };
    if (!def.tradable) return { error: 'item_not_tradable' };
    const owned = this._items(appId, fromSteamId)[String(itemDefId)];
    const qty = Math.max(1, Math.floor(Number(quantity)) || 1);
    if (!owned || owned.quantity < qty) return { error: 'not_enough_items' };

    this.consume(appId, fromSteamId, itemDefId, qty);
    const target = this._items(appId, toSteamId)[String(itemDefId)] || { quantity: 0, acquiredAt: Date.now(), properties: {} };
    target.quantity += qty;
    this._items(appId, toSteamId)[String(itemDefId)] = target;
    this._save();
    return { ok: true, itemDefId: String(itemDefId), quantity: target.quantity };
  }

  trade(appId, steamId, partnerSteamId, give = [], get = []) {
    for (const entry of give) {
      const def = this._defs(appId)[String(entry.itemDefId)];
      if (!def) return { error: 'unknown_item_def' };
      if (!def.tradable) return { error: 'item_not_tradable' };
      const owned = this._items(appId, steamId)[String(entry.itemDefId)];
      if (!owned || owned.quantity < Math.max(1, Math.floor(Number(entry.quantity)) || 1)) return { error: 'not_enough_items' };
    }
    for (const entry of get) {
      const def = this._defs(appId)[String(entry.itemDefId)];
      if (!def) return { error: 'unknown_item_def' };
      if (!def.tradable) return { error: 'item_not_tradable' };
      const owned = this._items(appId, partnerSteamId)[String(entry.itemDefId)];
      if (!owned || owned.quantity < Math.max(1, Math.floor(Number(entry.quantity)) || 1)) return { error: 'partner_not_enough_items' };
    }

    for (const entry of give) this.transfer(appId, steamId, partnerSteamId, entry.itemDefId, entry.quantity);
    for (const entry of get) this.transfer(appId, partnerSteamId, steamId, entry.itemDefId, entry.quantity);
    return { ok: true };
  }
}

module.exports = new InventoryStore();
