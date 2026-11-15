// Derived runtime palette only; never edits authored sources or extraction outputs.
// Run from Game with external tsx/pngjs. See docs/crystal-size-and-color.md.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, join } from 'node:path';
const { PNG } = require('pngjs');
type Image = { width: number; height: number; data: Buffer };
const root = resolve(process.argv[2] ?? 'mods/ca/bits/resources');
const output = resolve(process.argv[3] ?? join(root, 'green-crystals-harmonized'));
function chunk(key: string, value: string): Buffer {
  const payload = Buffer.from(key + '\0' + value), data = Buffer.concat([Buffer.from('tEXt'), payload]);
  let crc = 0xffffffff;
  for (const byte of data) { crc ^= byte; for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0); }
  const size = Buffer.alloc(4), checksum = Buffer.alloc(4);
  size.writeUInt32BE(payload.length); checksum.writeUInt32BE((crc ^ 0xffffffff) >>> 0);
  return Buffer.concat([size, data, checksum]);
}
function save(name: string, image: Image, metadata: Record<string, string> = {}) {
  const bytes: Buffer = PNG.sync.write(image, { colorType: 6 });
  writeFileSync(join(output, name), Buffer.concat([bytes.subarray(0, -12),
    ...Object.entries(metadata).map(([key, value]) => chunk(key, value)), bytes.subarray(-12)]));
}
function harmonize(image: Image) {
  for (let p = 0; p < image.data.length; p += 4) {
    const r = image.data[p], g = image.data[p + 1], b = image.data[p + 2];
    const high = Math.max(r, g, b), low = Math.min(r, g, b), chroma = high - low;
    if (!image.data[p + 3] || high < 40 || g <= r || g <= b) continue;
    const hue = 60 * ((b - r) / chroma + 2);
    if (hue < 100 || hue > 170 || chroma < 15) continue;
    // Shift cool greens toward the new sheet's yellow-green palette. Retain
    // HSL lightness and saturation (equivalently min/max and chroma), alpha,
    // geometry, neutral rocks, brown cracks and near-black outlines.
    const h = (hue - 28) / 60;
    const x = chroma * (1 - Math.abs(h % 2 - 1));
    const rgb = h < 2 ? [x, chroma, 0] : [0, chroma, x];
    for (let c = 0; c < 3; c++) image.data[p + c] = Math.round(rgb[c] + low);
  }
}
mkdirSync(output, { recursive: true });
const bodies: Image[] = [];
for (const variant of [1, 3, 4]) {
  for (const kind of ['body', 'growth']) {
    const name = `green-crystal-${variant}-${kind}.png`;
    const image: Image = PNG.sync.read(readFileSync(join(root, 'green-crystals', name)));
    harmonize(image);
    save(name, image, kind === 'growth' ? { FrameSize: '160,128', FrameAmount: '12', Offset: '0,-16' } : {});
    if (kind === 'body') bodies.push(image);
  }
}
for (const variant of [1, 2, 3, 5, 7])
  bodies.push(PNG.sync.read(readFileSync(join(root, 'green-crystals-seven', `green-crystal-${variant}-body.png`))));
const preview: Image = { width: 160 * bodies.length, height: 256, data: Buffer.alloc(160 * bodies.length * 256 * 4) };
for (let index = 0; index < bodies.length; index++) for (let y = 0; y < 128; y++) for (let x = 0; x < 160; x++) {
  const source = (y * 160 + x) * 4, alpha = bodies[index].data[source + 3] / 255;
  for (let row = 0; row < 2; row++) {
    const target = ((row * 128 + y) * preview.width + index * 160 + x) * 4;
    const background = row ? [126, 91, 62] : [101, 126, 75];
    for (let c = 0; c < 3; c++) preview.data[target + c] = Math.round(bodies[index].data[source + c] * alpha + background[c] * (1 - alpha));
    preview.data[target + 3] = 255;
  }
}
save('preview.png', preview);
writeFileSync(join(output, 'provenance.txt'), 'Generator: tools/harmonize-green-crystals.ts\nInputs: green-crystals variants 1,3,4 (body/growth); originals unchanged.\nSelective green hue shift -28 degrees; HSL lightness/saturation and alpha unchanged apart from integer rounding.\nExcluded runtime motifs: old green 2; new green 4,6; blue 3,5.\nPreview: harmonized old 1,3,4; unchanged new 1,2,3,5,7.\n');
console.log('PASS: three retained old green motifs harmonized; eight-motif preview generated.');
