type CrystalImage = { width: number; height: number; data: Buffer };

// Native-pixel broken-earth stamps, not scaled plates or shadows. Their open
// centers and separated wedges leave the original crystal roots visible.
const stamps = [
  [' 4   4 ', '132 231', ' 12 21 ', '  131  ', '   1   '],
  ['   41   14   ', ' 14331 13341 ', ' 1321   1231 ', '  12     21  ', '   132 231   ', '    13131    ', '      1      '],
  ['    41     14    ', '  14331   13341  ', ' 13221     12231 ', '  121       121  ', '   1321   1231   ', '     1321231     ', '       111       '],
  ['      41         14      ', '   143331       133341   ', '  132221         122231  ', '  121               121 ', '   1321           1231   ', '     1321       1231     ', '       1321   1231       ', '         1321231        ', '           111          '],
];
// Colors from the existing earthy green motifs; their source artwork stays unchanged.
const palette = [[0, 0, 0], [1, 3, 0], [54, 54, 49], [75, 75, 69], [95, 103, 85]];

export function withEarthBase(source: CrystalImage): CrystalImage {
  const { width, height } = source;
  const rows = Array.from({ length: height }, () => [] as number[]);
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++)
    if (source.data[(y * width + x) * 4 + 3] >= 128) rows[y].push(x);
  const peakWidth = Math.max(...rows.map(row => row.length));
  if (!peakWidth) throw new Error('Cannot ground an empty crystal motif.');
  // Ignore isolated decorative specks below the main body when locating its root.
  const threshold = Math.max(1, Math.ceil(peakWidth / 5));
  let baseY = height - 1;
  while (rows[baseY].length < threshold) baseY--;
  const feet = rows.slice(Math.max(0, baseY - 3), baseY + 1).flat();
  const left = Math.min(...feet), right = Math.max(...feet);
  const footprint = right - left + 1;
  const stamp = stamps[footprint <= 5 ? 0 : footprint <= 11 ? 1 : footprint <= 19 ? 2 : 3];
  const stampWidth = Math.max(...stamp.map(row => row.length));
  const x0 = Math.round((left + right) / 2) - Math.floor(stampWidth / 2);
  const y0 = baseY - Math.floor(stamp.length / 2);
  const result = { width, height, data: Buffer.from(source.data) };
  for (let y = 0; y < stamp.length; y++) for (let x = 0; x < stamp[y].length; x++) {
    const shade = Number(stamp[y][x]);
    if (!shade) continue;
    const tx = x0 + x, ty = y0 + y;
    if (tx <= 0 || ty <= 0 || tx >= width - 1 || ty >= height - 1)
      throw new Error('Insufficient padding for crystal earth base.');
    const p = (ty * width + tx) * 4;
    if (source.data[p + 3]) continue; // Never paint over existing artwork, including antialiasing.
    result.data.set([...palette[shade], 255], p);
  }
  return result;
}
