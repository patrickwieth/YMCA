// Run with tsx/pngjs from an external tools directory; see docs/seven-green-crystals.md.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve, join } from 'node:path';
const { PNG } = require('pngjs');
type Image = { width: number; height: number; data: Buffer };
const source = resolve(process.argv[2] ?? '../7 green crystals.png');
const output = resolve(process.argv[3] ?? 'mods/ca/bits/resources/green-crystals-seven');
const original = readFileSync(source);
const hash = createHash('sha256').update(original).digest('hex');
if (hash !== 'b87fc7d54394a3c4be0b2437749d180a6054e1d0c44dce905c48de5a0cd994c3')
  throw new Error('Unexpected source artwork: review segmentation before extracting a revised sheet.');
const image: Image = PNG.sync.read(original);
if (image.width !== 256 || image.height !== 256) throw new Error('Expected a 256x256 sheet.');
const blank = (width: number, height: number): Image => ({ width, height, data: Buffer.alloc(width * height * 4) });
const labels = new Int32Array(256 * 256).fill(-1);
const components: number[][] = [];
for (let i = 0; i < labels.length; i++) {
  if (!image.data[i * 4 + 3] || labels[i] >= 0) continue;
  const pixels = [i]; labels[i] = components.length;
  for (let j = 0; j < pixels.length; j++) {
    const x = pixels[j] % 256, y = Math.floor(pixels[j] / 256);
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const nx = x + dx, ny = y + dy, next = ny * 256 + nx;
      if (nx < 0 || ny < 0 || nx >= 256 || ny >= 256 || labels[next] >= 0 || !image.data[next * 4 + 3]) continue;
      labels[next] = components.length; pixels.push(next);
    }
  }
  components.push(pixels);
}
// Explicit foot anchors: upper left/right, middle left, large center, middle right,
// lower left, lower right. Keep native artwork sizes, including the two large motifs.
const anchors = [[39, 84], [224, 85], [62, 145], [155, 145], [232, 164], [72, 214], [235, 242]];
const main = anchors.map(([x, y]) => labels[y * 256 + x]);
if (new Set(main).size !== 7 || main.some(i => i < 0 || components[i].length < 400))
  throw new Error('Expected seven separate crystal bodies.');
const groups = main.map(i => [...components[i]]);
// Assign each detached shard/crack component to the nearest authored body, not
// its anchor. Preserve whole components and all nontransparent source pixels.
for (let i = 0; i < components.length; i++) {
  if (main.includes(i)) continue;
  let nearest = -1, distance = Infinity;
  for (const pixel of components[i]) {
    const x = pixel % 256, y = Math.floor(pixel / 256);
    for (let variant = 0; variant < main.length; variant++) for (const other of components[main[variant]]) {
      const dx = x - other % 256, dy = y - Math.floor(other / 256), d = dx * dx + dy * dy;
      if (d < distance) { nearest = variant; distance = d; }
    }
  }
  groups[nearest].push(...components[i]);
}
function chunk(name: string, text: string): Buffer {
  const payload = Buffer.from(name + '\0' + text), data = Buffer.concat([Buffer.from('tEXt'), payload]);
  let crc = 0xffffffff;
  for (const byte of data) { crc ^= byte; for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0); }
  const size = Buffer.alloc(4), checksum = Buffer.alloc(4);
  size.writeUInt32BE(payload.length); checksum.writeUInt32BE((crc ^ 0xffffffff) >>> 0);
  return Buffer.concat([size, data, checksum]);
}
function save(name: string, art: Image, metadata: Record<string, string> = {}) {
  const encoded: Buffer = PNG.sync.write(art, { colorType: 6 });
  writeFileSync(join(output, name), Buffer.concat([encoded.subarray(0, -12),
    ...Object.entries(metadata).map(([key, value]) => chunk(key, value)), encoded.subarray(-12)]));
}
mkdirSync(output, { recursive: true });
const preview = blank(160 * 7, 128 * 3);
const backgrounds = [[101, 126, 75], [126, 91, 62], [51, 57, 66]];
const assigned = new Set<number>();
for (let variant = 0; variant < anchors.length; variant++) {
  const body = blank(160, 128), [ax, ay] = anchors[variant];
  for (const pixel of groups[variant]) {
    if (assigned.has(pixel)) throw new Error('Source pixel assigned twice.');
    assigned.add(pixel);
    const x = pixel % 256 - ax + 80, y = Math.floor(pixel / 256) - ay + 80;
    if (x < 0 || y < 0 || x >= 160 || y >= 128) throw new Error(`Clipped source pixel ${pixel} in motif ${variant + 1}.`);
    image.data.copy(body.data, (y * 160 + x) * 4, pixel * 4, pixel * 4 + 4);
  }
  const growth = blank(160 * 12, 128);
  for (let frame = 0; frame < 12; frame++) {
    const scale = 0.45 + 0.55 * frame / 11;
    for (let y = 0; y < 128; y++) for (let x = 0; x < 160; x++) {
      const sx = Math.round((x - 80) / scale + 80), sy = Math.round((y - 80) / scale + 80);
      if (sx >= 0 && sy >= 0 && sx < 160 && sy < 128)
        body.data.copy(growth.data, (y * growth.width + frame * 160 + x) * 4, (sy * 160 + sx) * 4, (sy * 160 + sx) * 4 + 4);
    }
  }
  const prefix = `green-crystal-${variant + 1}`;
  save(prefix + '-body.png', body);
  save(prefix + '-growth.png', growth, { FrameSize: '160,128', FrameAmount: '12', Offset: '0,-16' });
  for (let row = 0; row < 3; row++) for (let y = 0; y < 128; y++) for (let x = 0; x < 160; x++) {
    const s = (y * 160 + x) * 4, d = ((row * 128 + y) * preview.width + variant * 160 + x) * 4;
    const alpha = body.data[s + 3] / 255;
    for (let c = 0; c < 3; c++) preview.data[d + c] = Math.round(body.data[s + c] * alpha + backgrounds[row][c] * (1 - alpha));
    preview.data[d + 3] = 255;
  }
}
const opaqueCount = Array.from(labels).filter(i => i >= 0).length;
if (assigned.size !== opaqueCount) throw new Error('Opaque source pixels were lost.');
save('preview.png', preview);
writeFileSync(join(output, 'provenance.txt'), `Source: 7 green crystals.png\nSHA256: ${hash}\nGenerator: tools/extract-seven-green-crystals.ts\nSeven connected main bodies; detached components assigned by nearest body pixel.\nAll ${assigned.size} nontransparent source RGBA pixels retained exactly once; no recoloring, glow or ground replacement.\nSource anchors: ${JSON.stringify(anchors)}\nPer-motif retained pixels: ${groups.map(g => g.length).join(', ')}\n12 nearest-neighbor density frames, 45–100%; final frame equals the original-size body.\nFrameSize 160,128; FrameAmount 12; base anchor 80,80; Offset 0,-16.\n`);
if (createHash('sha256').update(readFileSync(source)).digest('hex') !== hash) throw new Error('Source modified.');
console.log(`PASS: seven motifs; ${assigned.size} source RGBA pixels preserved; source ${hash}`);
