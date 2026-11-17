import assert from 'node:assert/strict';
import { repairGreenCrystalContrast } from './repair-green-crystal-contrast';

const palette = [[6, 50, 11], [31, 94, 34], [64, 114, 62], [95, 103, 85],
  [104, 150, 84], [147, 173, 117], [175, 222, 139]];
const art = { width: 7, height: 7, data: Buffer.alloc(7 * 7 * 4) };
const at = (x: number, y: number) => (y * 7 + x) * 4;
for (let y = 1; y <= 5; y++) for (let x = 1; x <= 5; x++)
  art.data.set([100, 180, 70, 255], at(x, y));
art.data.set([20, 110, 20, 128], at(1, 3)); // Tinted exposed edge.
art.data.set([30, 110, 20, 255], at(3, 3)); // Dark internal facet.
art.data.set([95, 103, 85, 255], at(2, 2)); // Olive-gray soil, not a crystal.
const before = Buffer.from(art.data);
repairGreenCrystalContrast(art, palette);
assert.deepEqual([...art.data.subarray(at(1, 3), at(1, 3) + 4)], [0, 0, 0, 128]);
assert.deepEqual(art.data.subarray(at(2, 2), at(2, 2) + 4), before.subarray(at(2, 2), at(2, 2) + 4));
assert.ok(art.data[at(3, 3) + 1] <= 110, 'Do not lift the dark facet into a pale green.');
for (let p = 0; p < before.length; p += 4) {
  assert.equal(art.data[p + 3], before[p + 3], 'No silhouette expansion or alpha change.');
  if (!before[p + 3]) assert.deepEqual(art.data.subarray(p, p + 4), before.subarray(p, p + 4));
}
console.log('PASS: dark fringe neutralized in place; dark facets, olive soil and alpha preserved.');
