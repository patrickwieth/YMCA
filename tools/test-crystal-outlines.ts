import assert from 'node:assert/strict';
import { withBlackOutline } from './crystal-outlines';

const blank = () => ({ width: 7, height: 7, data: Buffer.alloc(7 * 7 * 4) });
const pixel = (x: number, y: number) => (y * 7 + x) * 4;
const seed = blank();
seed.data.set([100, 200, 80, 255], pixel(3, 3));
const original = Buffer.from(seed.data);
const outlined = withBlackOutline(seed);
assert.deepEqual(seed.data, original);
assert.equal([...outlined.data].filter((_, i) => i % 4 === 3 && outlined.data[i] > 0).length, 5);
assert.equal(outlined.data[pixel(2, 2) + 3], 0, 'No diagonal corner thickening.');
assert.deepEqual(withBlackOutline(outlined).data, outlined.data, 'Never add a second contour layer.');
for (const color of [[0, 0, 0], [1, 9, 0], [1, 2, 29], [54, 54, 49]]) {
  const bordered = blank();
  for (let y = 2; y <= 4; y++) for (let x = 2; x <= 4; x++)
    bordered.data.set([...color, 255], pixel(x, y));
  bordered.data.set([100, 200, 80, 255], pixel(3, 3));
  assert.deepEqual(withBlackOutline(bordered).data, bordered.data, 'Keep authored black/tinted outlines unchanged.');
}
const translucent = blank();
translucent.data.set([40, 150, 240, 128], pixel(3, 3));
assert.deepEqual(withBlackOutline(translucent).data.subarray(pixel(3, 3), pixel(3, 3) + 4),
  translucent.data.subarray(pixel(3, 3), pixel(3, 3) + 4));
assert.deepEqual(withBlackOutline(blank()).data, blank().data);
const clipped = blank();
clipped.data.set([100, 200, 80, 255], pixel(0, 3));
assert.throws(() => withBlackOutline(clipped), /padding/);
console.log('PASS: thin cardinal contour, existing dark outlines retained, idempotent, source/alpha unchanged, clipping guarded.');
