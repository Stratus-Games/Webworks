const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const MAX_AVATAR_BYTES = 512 * 1024;
const MAX_AVATAR_EDGE = 1024;
const MIN_AVATAR_EDGE = 16;

class AvatarError extends Error {
  constructor(code, message) { super(message || code); this.code = code; }
}

function stripDataUrl(raw) {
  if (typeof raw !== 'string' || raw.length === 0) return null;
  const comma = raw.indexOf(',');
  if (raw.startsWith('data:') && comma > 0) return raw.slice(comma + 1);
  return raw;
}

function inspect(rawBase64) {
  const b64 = stripDataUrl(rawBase64);
  if (!b64) throw new AvatarError('missing_avatar');

  let bytes;
  try {
    bytes = Buffer.from(b64, 'base64');
  } catch {
    throw new AvatarError('invalid_avatar');
  }
  if (!bytes || bytes.length < 16) throw new AvatarError('invalid_avatar');
  if (bytes.length > MAX_AVATAR_BYTES) throw new AvatarError('avatar_too_large');

  const result = readDimensions(bytes);
  if (!result) throw new AvatarError('unsupported_avatar_format');
  if (result.width < MIN_AVATAR_EDGE || result.height < MIN_AVATAR_EDGE) throw new AvatarError('avatar_too_small');
  if (result.width > MAX_AVATAR_EDGE || result.height > MAX_AVATAR_EDGE) throw new AvatarError('avatar_too_large');

  return Object.assign(result, { bytes });
}

function readDimensions(b) {
  if (b.length > 24 && b[0] === 0x89 && b[1] === 0x50 && b[2] === 0x4e && b[3] === 0x47) {
    return { mime: 'image/png', ext: 'png', width: b.readUInt32BE(16), height: b.readUInt32BE(20) };
  }

  if (b.length > 4 && b[0] === 0xff && b[1] === 0xd8) {
    let i = 2;
    while (i + 9 < b.length) {
      if (b[i] !== 0xff) { i++; continue; }
      const marker = b[i + 1];
      if (marker === 0xd8 || marker === 0x01 || (marker >= 0xd0 && marker <= 0xd7)) { i += 2; continue; }
      const len = b.readUInt16BE(i + 2);
      if (len < 2) break;
      const isSof = marker >= 0xc0 && marker <= 0xcf && marker !== 0xc4 && marker !== 0xc8 && marker !== 0xcc;
      if (isSof) {
        return { mime: 'image/jpeg', ext: 'jpg', height: b.readUInt16BE(i + 5), width: b.readUInt16BE(i + 7) };
      }
      i += 2 + len;
    }
    return null;
  }

  if (b.length > 30 && b.toString('ascii', 0, 4) === 'RIFF' && b.toString('ascii', 8, 12) === 'WEBP') {
    const chunk = b.toString('ascii', 12, 16);
    if (chunk === 'VP8X') {
      return {
        mime: 'image/webp', ext: 'webp',
        width: 1 + (b[24] | (b[25] << 8) | (b[26] << 16)),
        height: 1 + (b[27] | (b[28] << 8) | (b[29] << 16)),
      };
    }
    if (chunk === 'VP8 ') {
      return { mime: 'image/webp', ext: 'webp', width: b.readUInt16LE(26) & 0x3fff, height: b.readUInt16LE(28) & 0x3fff };
    }
    if (chunk === 'VP8L') {
      const bits = b.readUInt32LE(21);
      return {
        mime: 'image/webp', ext: 'webp',
        width: 1 + (bits & 0x3fff),
        height: 1 + ((bits >> 14) & 0x3fff),
      };
    }
  }

  return null;
}

function save(dataDir, key, inspected) {
  const dir = path.join(dataDir, 'avatars');
  fs.mkdirSync(dir, { recursive: true });

  const safeKey = crypto.createHash('sha256').update(String(key)).digest('hex').slice(0, 32);
  const file = `${safeKey}.${inspected.ext}`;
  for (const ext of ['png', 'jpg', 'webp']) {
    if (ext === inspected.ext) continue;
    try { fs.unlinkSync(path.join(dir, `${safeKey}.${ext}`)); } catch {  }
  }
  fs.writeFileSync(path.join(dir, file), inspected.bytes);
  return { file, mime: inspected.mime, width: inspected.width, height: inspected.height };
}

function read(dataDir, avatarMeta) {
  if (!avatarMeta || !avatarMeta.file) return null;
  const file = path.join(dataDir, 'avatars', path.basename(avatarMeta.file));
  try {
    const bytes = fs.readFileSync(file);
    return { mime: avatarMeta.mime || 'image/png', width: avatarMeta.width, height: avatarMeta.height, bytes };
  } catch {
    return null;
  }
}

function remove(dataDir, avatarMeta) {
  if (!avatarMeta || !avatarMeta.file) return;
  try { fs.unlinkSync(path.join(dataDir, 'avatars', path.basename(avatarMeta.file))); } catch {  }
}

module.exports = { inspect, save, read, remove, AvatarError, MAX_AVATAR_BYTES, MAX_AVATAR_EDGE };
