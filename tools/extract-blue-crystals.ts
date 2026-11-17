// Run with external tsx/pngjs dependencies, as documented in docs/blue-crystals.md.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve, join } from 'node:path';
const { PNG } = require('pngjs');
type Image = { width: number; height: number; data: Buffer };
const source = resolve(process.argv[2] ?? '../8 blue crystals.png');
const output = resolve(process.argv[3] ?? 'mods/ca/bits/resources/blue-crystals');
const original = readFileSync(source), hash = createHash('sha256').update(original).digest('hex');
const image: Image = PNG.sync.read(original);
if (image.width !== 256 || image.height !== 256) throw new Error('Expected the supplied 256x256 sheet.');
const labels = new Int32Array(65536).fill(-1), components: number[][] = [];
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
// Reading order, with the lower-left large cluster before the small right cluster.
const anchors = [[64, 78], [115, 54], [177, 110], [31, 126], [95, 173], [218, 145], [191, 215], [106, 226]];
const main = anchors.map(([x, y]) => labels[y * 256 + x]);
if (new Set(main).size !== 7 || main[2] !== main[4] || main.some(i => i < 0 || components[i].length < 700)) throw new Error('Source segmentation changed.');
const groups = anchors.map(() => new Set<number>());
const owner = new Int32Array(65536).fill(-1);
for (let v = 0; v < 8; v++) for (const p of components[main[v]]) {
  // The two large motifs touch through dark ground/crack pixels. This cut applies
  // ONLY to their connected component, not the separate floating cyan shards.
  if ((v === 2 || v === 4) && ((2 * Math.floor(p / 256) < p % 256 + 110) !== (v === 2))) continue;
  groups[v].add(p); owner[p] = v;
}
const mainPixels = [...groups.flatMap(g => [...g])];
for (let i = 0; i < components.length; i++) {
  if (main.includes(i)) continue;
  let nearest = -1, distance = Infinity;
  // Floating cyan shards belong to the two large motifs, not the adjacent small clusters.
  const shard = components[i].some(p => image.data[p * 4 + 1] > 120 && image.data[p * 4 + 2] > 150);
  for (const p of components[i]) for (const q of mainPixels) {
    if (shard && owner[q] !== 2 && owner[q] !== 4) continue;
    const dx = p % 256 - q % 256, dy = Math.floor(p / 256) - Math.floor(q / 256), d = dx * dx + dy * dy;
    if (d < distance) { nearest = owner[q]; distance = d; }
  }
  for (const p of components[i]) groups[nearest].add(p);
}
const width = 192, height = 192, ax = 96, ay = 112;
const blank = (w: number, h: number): Image => ({ width: w, height: h, data: Buffer.alloc(w * h * 4) });
function over(dst: Image, src: Image, ox = 0, oy = 0) {
  for (let y = 0; y < src.height; y++) for (let x = 0; x < src.width; x++) {
    const s = (y * src.width + x) * 4, d = ((y + oy) * dst.width + x + ox) * 4;
    const a = src.data[s + 3] / 255, b = dst.data[d + 3] / 255, total = a + b * (1 - a);
    if (!total) continue;
    for (let c = 0; c < 3; c++) dst.data[d + c] = Math.round((src.data[s + c] * a + dst.data[d + c] * b * (1 - a)) / total);
    dst.data[d + 3] = Math.round(total * 255);
  }
}
function chunk(name: string, text: string) {
  const payload = Buffer.from(name + '\0' + text), type = Buffer.from('tEXt');
  let crc = 0xffffffff;
  for (const byte of Buffer.concat([type, payload])) { crc ^= byte; for (let b = 0; b < 8; b++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0); }
  const size = Buffer.alloc(4), checksum = Buffer.alloc(4);
  size.writeUInt32BE(payload.length); checksum.writeUInt32BE((crc ^ 0xffffffff) >>> 0);
  return Buffer.concat([size, type, payload, checksum]);
}
function save(name: string, art: Image, metadata: Record<string, string> = {}) {
  const encoded: Buffer = PNG.sync.write(art, { colorType: 6 });
  writeFileSync(join(output, name), Buffer.concat([encoded.subarray(0, -12), ...Object.entries(metadata).map(([k, v]) => chunk(k, v)), encoded.subarray(-12)]));
}
mkdirSync(output, { recursive: true });
const preview = blank(width * 4, height * 6), backgrounds = [[101, 126, 75], [126, 91, 62], [51, 57, 66]];
let retained = 0, ground = 0;
for (let v = 0; v < 8; v++) {
  const body = blank(width, height), glow = blank(width, height);
  let mask = new Float64Array(width * height);
  for (const p of groups[v]) {
    const x = p % 256 - anchors[v][0] + ax, y = Math.floor(p / 256) - anchors[v][1] + ay;
    if (x < 7 || y < 7 || x >= width - 7 || y >= height - 7) throw new Error(`Insufficient authored-pixel/glow margin: motif ${v + 1}, source ${p % 256},${Math.floor(p / 256)}.`);
    const s = p * 4, d = (y * width + x) * 4;
    const isGround = (image.data[s] === 8 && image.data[s + 1] === 30 && image.data[s + 2] === 90) ||
      (image.data[s] === 5 && image.data[s + 1] === 18 && image.data[s + 2] === 75);
    if (isGround) { mask[y * width + x] = 1; ground++; }
    else { image.data.copy(body.data, d, s, s + 4); retained++; }
  }
  for (let pass = 0; pass < 3; pass++) {
    const next = new Float64Array(mask.length);
    for (let y = 2; y < height - 2; y++) for (let x = 2; x < width - 2; x++) {
      let sum = 0;
      for (let dy = -2; dy <= 2; dy++) for (let dx = -2; dx <= 2; dx++) sum += mask[(y + dy) * width + x + dx];
      next[y * width + x] = sum / 25;
    }
    mask = next;
  }
  for (let p = 0; p < mask.length; p++) {
    const alpha = Math.round(mask[p] * 100);
    if (alpha) glow.data.set([35, 166, 255, alpha], p * 4);
  }
  // The game supplies terrain lighting; never bake the diagnostic glow into sprites.
  const combined = blank(width, height); over(combined, body);
  const prefix = `blue-crystal-${v + 1}`;
  save(prefix + '-body.png', body); save(prefix + '-glow.png', glow); save(prefix + '.png', combined);
  const growth = blank(width * 12, height);
  for (let frame = 0; frame < 12; frame++) {
    const scale = 0.45 + 0.55 * frame / 11;
    for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
      const sx = Math.round((x - ax) / scale + ax), sy = Math.round((y - ay) / scale + ay);
      if (sx >= 0 && sy >= 0 && sx < width && sy < height) combined.data.copy(growth.data, (y * growth.width + frame * width + x) * 4, (sy * width + sx) * 4, (sy * width + sx) * 4 + 4);
    }
  }
  save(prefix + '-growth.png', growth, { FrameSize: `${width},${height}`, FrameAmount: '12', Offset: '0,-16' });
  for (let row = 0; row < 3; row++) {
    const panel = blank(width, height);
    for (let p = 0; p < width * height; p++) panel.data.set([...backgrounds[row], 255], p * 4);
    over(panel, combined); over(preview, panel, v % 4 * width, (Math.floor(v / 4) * 3 + row) * height);
  }
}
const occupied = [...image.data].filter((_, i) => i % 4 === 3 && image.data[i] !== 0).length;
if (retained + ground !== occupied) throw new Error('Source pixels lost or duplicated.');
save('preview.png', preview);
writeFileSync(join(output, 'provenance.txt'), `Source: 8 blue crystals.png\nSHA256: ${hash}\nGenerator: tools/extract-blue-crystals.ts\nEight motifs; two touching large components separated at 2*y=x+110; detached cyan shards assigned to nearest large motif, remaining crack fragments to nearest main motif.\nEvery source pixel accounted for: ${retained} original body RGBA pixels, ${ground} flat navy ground pixels converted to softened blue glow (alpha <=100/255).\nRuntime combined/growth PNGs contain body only; separate glow PNGs are diagnostic references, not used in-game.\n12 nearest-neighbor density frames (45-100%) per variant; frame 192x192, anchor (96,112), offset (0,-16).\n`);
if (createHash('sha256').update(readFileSync(source)).digest('hex') !== hash) throw new Error('Source modified.');
console.log(`PASS: eight motifs, ${retained} preserved body pixels, ${ground} ground pixels converted; source ${hash}`);
