import assert from 'node:assert/strict';
import { withEarthBase } from './crystal-earth';
import { withBlackOutline } from './crystal-outlines';

for (const size of [3, 9, 17, 25]) {
  const source = { width: 160, height: 128, data: Buffer.alloc(160 * 128 * 4) };
  for (let y = 64; y <= 80; y++) for (let x = 80 - Math.floor(size / 2); x <= 80 + Math.floor(size / 2); x++)
    source.data.set([40, 170, 220, x === 80 ? 128 : 255], (y * 160 + x) * 4);
  const before = Buffer.from(source.data);
  const grounded = withEarthBase(source);
  assert.deepEqual(source.data, before, 'Do not mutate source art.');
  assert.deepEqual(withEarthBase(source).data, grounded.data, 'Generation must be deterministic.');
  let added = 0;
  for (let p = 0; p < before.length; p += 4) {
    if (before[p + 3]) assert.deepEqual(grounded.data.subarray(p, p + 4), before.subarray(p, p + 4));
    else if (grounded.data[p + 3]) {
      added++;
      const y = Math.floor(p / 4 / 160);
      assert.ok(y >= 76 && y <= 84, 'Keep earth near the ground, not the crystal tips.');
      assert.equal(grounded.data[p + 3], 255);
    }
  }
  assert.ok(added > 0);
  const outlined = withBlackOutline(grounded);
  assert.deepEqual(withBlackOutline(outlined).data, outlined.data, 'Soil must not introduce double contours.');
}
assert.throws(() => withEarthBase({ width: 160, height: 128, data: Buffer.alloc(160 * 128 * 4) }), /empty/);
console.log('PASS: four native-size earth stamps; foreground/alpha preserved, deterministic, grounded and thin-contour compatible.');
