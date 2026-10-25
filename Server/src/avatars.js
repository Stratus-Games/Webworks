const crypto = require('crypto');
const zlib = require('zlib');

function crc32(buffer) {
  let c = 0xffffffff;
  for (let i = 0; i < buffer.length; i++) {
    c ^= buffer[i];
    for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xedb88320 & -(c & 1));
  }
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length, 0);
  const body = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body), 0);
  return Buffer.concat([len, body, crc]);
}

function encodePng(width, height, rgba) {
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;
  ihdr[10] = 0;
  ihdr[11] = 0;
  ihdr[12] = 0;

  const stride = width * 4;
  const raw = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y++) {
    raw[y * (stride + 1)] = 0;
    rgba.copy(raw, y * (stride + 1) + 1, y * stride, (y + 1) * stride);
  }

  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', zlib.deflateSync(raw)),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

function identicon(seed, pixelSize = 64) {
  const gridSize = 5;
  const cell = Math.max(1, Math.floor(pixelSize / gridSize));
  const size = cell * gridSize;

  const hash = crypto.createHash('sha256').update(String(seed)).digest();
  const fg = [hash[0], hash[1], hash[2]];
  const bg = [18, 16, 18];

  const rgba = Buffer.alloc(size * size * 4, 255);
  for (let i = 0; i < size * size; i++) {
    const idx = i * 4;
    rgba[idx] = bg[0];
    rgba[idx + 1] = bg[1];
    rgba[idx + 2] = bg[2];
    rgba[idx + 3] = 255;
  }
  const cols = Math.ceil(gridSize / 2);

  for (let y = 0; y < gridSize; y++) {
    for (let x = 0; x < cols; x++) {
      const bit = hash[(y * cols + x) % hash.length] % 2;
      const targets = bit ? [x, gridSize - 1 - x] : [];
      for (const cx of new Set(targets)) {
        for (let dy = 0; dy < cell; dy++) {
          for (let dx = 0; dx < cell; dx++) {
            const px = cx * cell + dx;
            const py = y * cell + dy;
            const idx = (py * size + px) * 4;
            rgba[idx] = fg[0];
            rgba[idx + 1] = fg[1];
            rgba[idx + 2] = fg[2];
            rgba[idx + 3] = 255;
          }
        }
      }
    }
  }

  return {
    width: size,
    height: size,
    rgba,
    png: encodePng(size, size, rgba).toString('base64'),
  };
}

function paeth(a, b, c) {
  const p = a + b - c;
  const pa = Math.abs(p - a);
  const pb = Math.abs(p - b);
  const pc = Math.abs(p - c);
  if (pa <= pb && pa <= pc) return a;
  if (pb <= pc) return b;
  return c;
}

function toRgba(pngBytes) {
  if (!pngBytes || pngBytes.length < 33) return null;
  if (pngBytes[0] !== 0x89 || pngBytes[1] !== 0x50) return null;

  let offset = 8;
  let width = 0;
  let height = 0;
  let bitDepth = 0;
  let colorType = 0;
  const idat = [];

  while (offset + 8 <= pngBytes.length) {
    const length = pngBytes.readUInt32BE(offset);
    const type = pngBytes.toString('ascii', offset + 4, offset + 8);
    const data = pngBytes.subarray(offset + 8, offset + 8 + length);
    if (type === 'IHDR') {
      width = data.readUInt32BE(0);
      height = data.readUInt32BE(4);
      bitDepth = data[8];
      colorType = data[9];
      if (data[12] !== 0) return null;
    } else if (type === 'IDAT') {
      idat.push(data);
    } else if (type === 'IEND') {
      break;
    }
    offset += 12 + length;
  }

  if (bitDepth !== 8 || (colorType !== 2 && colorType !== 6)) return null;
  if (!width || !height || idat.length === 0) return null;

  let raw;
  try {
    raw = zlib.inflateSync(Buffer.concat(idat));
  } catch {
    return null;
  }

  const channels = colorType === 6 ? 4 : 3;
  const stride = width * channels;
  if (raw.length < (stride + 1) * height) return null;

  const out = Buffer.alloc(width * height * 4);
  const prior = Buffer.alloc(stride);
  let pos = 0;

  for (let y = 0; y < height; y++) {
    const filter = raw[pos++];
    const line = raw.subarray(pos, pos + stride);
    pos += stride;
    const recon = Buffer.alloc(stride);

    for (let x = 0; x < stride; x++) {
      const left = x >= channels ? recon[x - channels] : 0;
      const up = prior[x];
      const upLeft = x >= channels ? prior[x - channels] : 0;
      let value = line[x];
      if (filter === 1) value += left;
      else if (filter === 2) value += up;
      else if (filter === 3) value += (left + up) >> 1;
      else if (filter === 4) value += paeth(left, up, upLeft);
      recon[x] = value & 0xff;
    }

    for (let x = 0; x < width; x++) {
      const src = x * channels;
      const dst = (y * width + x) * 4;
      out[dst] = recon[src];
      out[dst + 1] = recon[src + 1];
      out[dst + 2] = recon[src + 2];
      out[dst + 3] = channels === 4 ? recon[src + 3] : 255;
    }
    recon.copy(prior);
  }

  return { width, height, rgba: out };
}

module.exports = { identicon, encodePng, toRgba };
