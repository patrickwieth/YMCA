type Image = { width: number; height: number; data: Buffer };

function crystalGreen(r: number, g: number, b: number): boolean {
  const chroma = g - Math.min(r, b);
  // Do not treat muted gray/olive earth as a green crystal face.
  return g >= 40 && g - r >= 15 && g - b >= 20 &&
    chroma >= 0.25 * (255 - Math.abs(g + Math.min(r, b) - 255));
}

/** Preserve authored facet contrast instead of lifting dark edges via histogram matching. */
export function repairGreenCrystalContrast(art: Image, referencePalette: number[][]): void {
  const shades = referencePalette.filter(([r, g, b]) => crystalGreen(r, g, b));
  if (!shades.length) throw new Error('Missing saturated reference green shades.');
  for (let y = 0; y < art.height; y++) for (let x = 0; x < art.width; x++) {
    const p = (y * art.width + x) * 4;
    const r = art.data[p], g = art.data[p + 1], b = art.data[p + 2];
    if (!art.data[p + 3] || !crystalGreen(r, g, b)) continue;
    const lightness = (g + Math.min(r, b)) / 2;
    const exposed = [[-1, 0], [1, 0], [0, -1], [0, 1]].some(([dx, dy]) => {
      const nx = x + dx, ny = y + dy;
      return nx < 0 || ny < 0 || nx >= art.width || ny >= art.height ||
        !art.data[(ny * art.width + nx) * 4 + 3];
    });
    // Replace the existing dark green boundary pixel, never dilate the outline.
    if (exposed && lightness < 100) {
      art.data[p] = art.data[p + 1] = art.data[p + 2] = 0;
      continue;
    }
    // Compress the bright source range gently rather than clipping broad facets
    // to one pale highlight color. Dark face values must not be histogram-lifted.
    const target = lightness * 0.85;
    const distance = (rgb: number[]) => Math.abs((Math.max(...rgb) + Math.min(...rgb)) / 2 - target);
    const match = shades.reduce((best, next) => distance(next) < distance(best) ? next : best);
    for (let c = 0; c < 3; c++) art.data[p + c] = match[c];
  }
}
