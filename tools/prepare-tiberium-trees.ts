// Run with external tsx/pngjs; see docs/authored-tiberium-trees.md.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve, join } from 'node:path';
const { PNG } = require('pngjs');
type Image = { width: number; height: number; data: Buffer };
const sourceRoot = resolve(process.argv[2] ?? '..');
const output = resolve(process.argv[3] ?? 'mods/ca/bits/resources/tiberium-trees');
const trees = [
  { color: 'green', hash: 'c7aa5de3848169040c659cfeeb8db218bec94d27c48c3d49cb8233d8a138589d', anchor: [128, 184],
    variants: [{ name: 'green-tree', sequenceOffset: [11, -15] }, { name: 'green-tree-split3', sequenceOffset: [7, -13] }] },
  { color: 'blue', hash: '95eabcc221bb7740ddc2f210484c474bb5d51cb33e5cddf28d7a7bc5b00f2b53', anchor: [128, 223],
    variants: [{ name: 'blue-tree', sequenceOffset: [7, -13] }] },
];
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
mkdirSync(output, { recursive: true });
const preview: Image = { width: 512, height: 512, data: Buffer.alloc(512 * 512 * 4) };
const records = ['Generator: tools/prepare-tiberium-trees.ts',
  'Original RGBA preserved; no resize, crop, recoloring, generated glow or shadow.',
  '55 logical PNG frames reference the same complete 256x256 image.',
  'Existing make (30 frames), active (24 frames), idle (frame 54) timing retained; artwork is static.'];
for (let index = 0; index < trees.length; index++) {
  const tree = trees[index], source = join(sourceRoot, tree.color + ' tib tree.png');
  const bytes = readFileSync(source), hash = createHash('sha256').update(bytes).digest('hex');
  if (hash !== tree.hash) throw new Error('Unexpected source artwork: ' + source);
  const image: Image = PNG.sync.read(bytes);
  if (image.width !== 256 || image.height !== 256) throw new Error('Expected a 256x256 tree.');
  save(tree.color + '-tree-body.png', image);
  for (const variant of tree.variants) {
    const ox = 128 - tree.anchor[0] - variant.sequenceOffset[0];
    const oy = 128 - tree.anchor[1] - variant.sequenceOffset[1];
    const metadata: Record<string, string> = {};
    // Explicit overlapping regions preserve legacy logical frame indices without
    // storing a wasteful 55-image atlas. Offsets compensate the legacy sequence.
    for (let frame = 0; frame < 55; frame++) metadata[`Frame[${frame}]`] = `0,0,256,256;${ox},${oy}`;
    save(variant.name + '.png', image, metadata);
    records.push(`${variant.name}.png: source anchor ${tree.anchor}; sequence offset ${variant.sequenceOffset}; PNG offset ${ox},${oy}`);
  }
  let retained = 0;
  for (let y = 0; y < 256; y++) for (let x = 0; x < 256; x++) {
    const s = (y * 256 + x) * 4, alpha = image.data[s + 3] / 255;
    if (alpha) retained++;
    for (let row = 0; row < 2; row++) {
      const d = ((row * 256 + y) * 512 + index * 256 + x) * 4;
      const background = row === 0 ? [101, 126, 75] : [126, 91, 62];
      for (let channel = 0; channel < 3; channel++)
        preview.data[d + channel] = Math.round(image.data[s + channel] * alpha + background[channel] * (1 - alpha));
      preview.data[d + 3] = 255;
    }
  }
  records.push(`${tree.color} tib tree.png: SHA256 ${hash}; ${retained} nontransparent pixels retained.`);
  if (createHash('sha256').update(readFileSync(source)).digest('hex') !== hash) throw new Error('Source modified.');
}
save('preview.png', preview);
writeFileSync(join(output, 'provenance.txt'), records.join('\n') + '\n');
console.log('PASS: two original-size trees; three legacy-compatible static sprite variants; sources unchanged.');
