type CrystalImage = { width: number; height: number; data: Buffer };

/** Complete missing one-pixel contours; never expand existing dark outlines. */
export function withBlackOutline(source: CrystalImage): CrystalImage {
  const { width, height } = source;
  const result = { width, height, data: Buffer.from(source.data) };
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
    const sourcePixel = (y * width + x) * 4;
    if (!source.data[sourcePixel + 3]) continue;
    const r = source.data[sourcePixel], g = source.data[sourcePixel + 1], b = source.data[sourcePixel + 2];
    // Authored outlines are often near-black green/blue or dark gray, not RGB 0.
    if (Math.max(r, g, b) <= 96 && 299 * r + 587 * g + 114 * b <= 64000) continue;
    if (x === 0 || y === 0 || x === width - 1 || y === height - 1)
      throw new Error('Insufficient padding for crystal outline.');
    // Cardinal neighbors only: no square diagonal dilation at tips/corners.
    // Read the source, not the result. Reapplying this pass is also idempotent.
    for (const [dx, dy] of [[-1, 0], [1, 0], [0, -1], [0, 1]]) {
      const p = ((y + dy) * width + x + dx) * 4;
      if (source.data[p + 3]) continue;
      result.data[p] = result.data[p + 1] = result.data[p + 2] = 0;
      result.data[p + 3] = 255;
    }
  }
  return result;
}
