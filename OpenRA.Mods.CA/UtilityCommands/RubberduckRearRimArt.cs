using System;
using OpenRA.FileFormats;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	// A narrow rock band, not a dirt overlay: sample authored face relief into
	// a raised rear crest above the roof edge. Original source files stay intact.
	static class RubberduckRearRimArt
	{
		public const int Width = 12;

		public static byte[] Apply(byte[] original, Png atlas, int slot, int padding)
		{
			var result = (byte[])original.Clone();
			for (var x = 0; x < 128; x++)
				for (var y = 0; y < result.Length / 512; y++)
				{
					var geometry = RubberduckClosedCliffGeometry.Column(slot, x + .5, y + .5 - padding);
					var depth = y + .5 - padding - geometry.Roof;
					if (geometry.Drop != 64 || depth < -Width + (int)(unchecked((uint)(x / 3 + slot * 17) * 2654435761u) >> 30) || depth >= 0 ||
						slot >= 8 && slot <= 10 && x > 65 || slot >= 12 && slot <= 14 && x < 62) continue;
					var p = (y * 128 + x) * 4;
					// Keep existing authored pixels. The added crest sits outside the roof
					// overlay, so both grass and dirt cannot paint over the stone band.
					if (original[p + 3] != 0 && Math.Max(original[p], Math.Max(original[p + 1], original[p + 2])) > 8) continue;
					var donor = (slot >= 12 && slot <= 14 ? 5 + slot - 12 : slot >= 8 && slot <= 10 ? slot - 8 : x < 64 ? 0 : 5);
					var u = .2 + x * .6 / 128; var v = .35 - depth * .015;
					var sx = (int)(donor >= 5 ? 64 * u + 64 * v : 64 + 64 * u - 64 * v);
					var sy = (int)(donor >= 5 ? 64 - 32 * u + 192 * v : 32 + 32 * u + 192 * v);
					var found = false;
					for (var distance = 0; distance <= 16 && !found; distance++)
						foreach (var row in new[] { sy - distance, sy + distance })
						{
							if (row < 0 || row >= 256) continue;
							var q = ((donor / 12 * 256 + row) * atlas.Width + donor % 12 * 128 + sx) * 4;
							if (atlas.Data[q + 3] < 128 || Math.Max(atlas.Data[q], Math.Max(atlas.Data[q + 1], atlas.Data[q + 2])) <= 16) continue;
							Array.Copy(atlas.Data, q, result, p, 3); result[p + 3] = 255; found = true; break;
						}
					if (!found) throw new InvalidOperationException($"No authored rear-rim relief sample for slot {slot}, column {x}.");
				}
			return result;
		}
	}
}
