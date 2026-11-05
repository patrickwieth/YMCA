// Run with tsx and pngjs installed in an external tools directory; see docs/green-crystals.md.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve, join } from 'node:path';
const { PNG } = require('pngjs');
type Image = { width: number; height: number; data: Buffer };
const source = resolve(process.argv[2] ?? '../4 green crystals.png');
const output = resolve(process.argv[3] ?? 'mods/ca/bits/resources/green-crystals');
const original = readFileSync(source);
const image: Image = PNG.sync.read(original);
if (image.width !== 256 || image.height !== 256) throw new Error('Expected the supplied 256x256 sheet.');
const hash = createHash('sha256').update(original).digest('hex');
const blank = (width: number, height: number): Image => ({ width, height, data: Buffer.alloc(width * height * 4) });
const labels = new Int32Array(256 * 256).fill(-1);
const components: number[][] = [];
for (let i = 0; i < labels.length; i++) {
  if (!image.data[i * 4 + 3] || labels[i] >= 0) continue;
  const group = [i]; labels[i] = components.length;
  for (let j = 0; j < group.length; j++) {
    const p = group[j], x = p % 256, y = Math.floor(p / 256);
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const nx = x + dx, ny = y + dy, q = ny * 256 + nx;
      if (nx < 0 || ny < 0 || nx >= 256 || ny >= 256 || labels[q] >= 0 || !image.data[q * 4 + 3]) continue;
      labels[q] = components.length; group.push(q);
    }
  }
  components.push(group);
}
// Explicit art order: small upper cluster, large center, lower left, lower right.
const anchors = [[82, 105], [131, 168], [54, 215], [211, 214]];
const main = anchors.map(([x, y]) => labels[y * 256 + x]);
if (new Set(main).size !== 4 || main.some(i => i < 0 || components[i].length < 2000)) throw new Error('Crystal segmentation changed.');
const groups = main.map(i => new Set(components[i]));
// Keep detached authored crack pixels, but never steal pixels from another crystal.
for (let i = 0; i < components.length; i++) {
  if (main.includes(i)) continue;
  let nearest = -1, distance = 26;
  for (const p of components[i]) {
    const x = p % 256, y = Math.floor(p / 256);
    for (let dy = -5; dy <= 5; dy++) for (let dx = -5; dx <= 5; dx++) {
      if (x + dx < 0 || y + dy < 0 || x + dx >= 256 || y + dy >= 256) continue;
      const candidate = main.indexOf(labels[(y + dy) * 256 + x + dx]);
      if (candidate >= 0 && dx * dx + dy * dy < distance) { nearest = candidate; distance = dx * dx + dy * dy; }
    }
  }
  if (nearest >= 0) for (const p of components[i]) groups[nearest].add(p);
}
function over(dst: Image, src: Image, ox = 0, oy = 0) {
  for (let y = 0; y < src.height; y++) for (let x = 0; x < src.width; x++) {
    if (x + ox < 0 || y + oy < 0 || x + ox >= dst.width || y + oy >= dst.height) continue;
    const s = (y * src.width + x) * 4, d = ((y + oy) * dst.width + x + ox) * 4;
    const a = src.data[s + 3] / 255, b = dst.data[d + 3] / 255, total = a + b * (1 - a);
    if (!total) continue;
    for (let c = 0; c < 3; c++) dst.data[d + c] = Math.round((src.data[s + c] * a + dst.data[d + c] * b * (1 - a)) / total);
    dst.data[d + 3] = Math.round(total * 255);
  }
}
function chunk(name: string, text: string) {
  const payload = Buffer.from(name + '\0' + text), type = Buffer.from('tEXt');
  const data = Buffer.concat([type, payload]); let crc = 0xffffffff;
  for (const byte of data) { crc ^= byte; for (let b = 0; b < 8; b++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0); }
  const size = Buffer.alloc(4), checksum = Buffer.alloc(4);
  size.writeUInt32BE(payload.length); checksum.writeUInt32BE((crc ^ 0xffffffff) >>> 0);
  return Buffer.concat([size, data, checksum]);
}
function save(name: string, art: Image, metadata: Record<string, string> = {}) {
  const encoded: Buffer = PNG.sync.write(art, { colorType: 6 });
  writeFileSync(join(output, name), Buffer.concat([encoded.subarray(0, -12), ...Object.entries(metadata).map(([k, v]) => chunk(k, v)), encoded.subarray(-12)]));
}
mkdirSync(output, { recursive: true });
const preview = blank(640, 384);
const backgrounds = [[101, 126, 75], [126, 91, 62], [51, 57, 66]];
let retained = 0, ground = 0;
for (let variant = 0; variant < 4; variant++) {
  const body = blank(160, 128), glow = blank(160, 128);
  let mask = new Float64Array(160 * 128);
  const [ax, ay] = anchors[variant];
  for (const p of groups[variant]) {
    const x = p % 256 - ax + 80, y = Math.floor(p / 256) - ay + 80;
    if (x < 0 || y < 0 || x >= 160 || y >= 128) throw new Error('Clipped authored pixel.');
    const s = p * 4, d = (y * 160 + x) * 4;
    // Only the flat olive ground color and its one-byte variants are replaced.
    const isGround = Math.abs(image.data[s] - 130) <= 1 && Math.abs(image.data[s + 1] - 156) <= 1 && Math.abs(image.data[s + 2] - 118) <= 1;
    if (isGround) { mask[y * 160 + x] = 1; ground++; }
    else { image.data.copy(body.data, d, s, s + 4); retained++; }
  }
  // Three small box filters soften the old hard-edged ground into an alpha glow.
  for (let pass = 0; pass < 3; pass++) {
    const next = new Float64Array(mask.length);
    for (let y = 2; y < 126; y++) for (let x = 2; x < 158; x++) {
      let sum = 0;
      for (let dy = -2; dy <= 2; dy++) for (let dx = -2; dx <= 2; dx++) sum += mask[(y + dy) * 160 + x + dx];
      next[y * 160 + x] = sum / 25;
    }
    mask = next;
  }
  for (let p = 0; p < mask.length; p++) {
    const alpha = Math.round(mask[p] * 100);
    if (alpha) glow.data.set([76, 226, 63, alpha], p * 4);
  }
  // The game supplies terrain lighting; never bake the diagnostic glow into sprites.
  const combined = blank(160, 128); over(combined, body);
  const prefix = `green-crystal-${variant + 1}`;
  save(prefix + '-body.png', body); save(prefix + '-glow.png', glow); save(prefix + '.png', combined);
  const growth = blank(160 * 12, 128);
  for (let frame = 0; frame < 12; frame++) {
    const scale = 0.45 + 0.55 * frame / 11;
    for (let y = 0; y < 128; y++) for (let x = 0; x < 160; x++) {
      const sx = Math.round((x - 80) / scale + 80), sy = Math.round((y - 80) / scale + 80);
      if (sx >= 0 && sy >= 0 && sx < 160 && sy < 128) combined.data.copy(growth.data, (y * growth.width + frame * 160 + x) * 4, (sy * 160 + sx) * 4, (sy * 160 + sx) * 4 + 4);
    }
  }
  save(prefix + '-growth.png', growth, { FrameSize: '160,128', FrameAmount: '12', Offset: '0,-16' });
  for (let row = 0; row < 3; row++) {
    const panel = blank(160, 128);
    for (let p = 0; p < 160 * 128; p++) panel.data.set([...backgrounds[row], 255], p * 4);
    over(panel, combined); over(preview, panel, variant * 160, row * 128);
  }
}
save('preview.png', preview);
writeFileSync(join(output, 'provenance.txt'), `Source: 4 green crystals.png\nSHA256: ${hash}\nGenerator: tools/extract-green-crystals.ts\nFour connected components; original crystal/stone/crack RGBA preserved (${retained} pixels).\nFlat ground recolored into blurred green RGBA glow (${ground} source pixels); alpha <=100/255.\nRuntime combined/growth PNGs contain body only; separate glow PNGs are diagnostic references, not used in-game.\n12 nearest-neighbor density frames per variant; native base anchor (80,80), offset (0,-16).\n`);
if (createHash('sha256').update(readFileSync(source)).digest('hex') !== hash) throw new Error('Source modified.');
console.log(`PASS: four variants; ${retained} preserved body pixels, ${ground} ground pixels converted; source ${hash}`);
