using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Immutable cell values are shared by terrain planning and future editor transactions.
	// Elevation is independent of artwork and collision; a high flat cell is walkable.
	readonly struct PlateauSurface
	{
		public readonly byte Height;
		public readonly byte Ramp;
		public readonly bool Blocked;

		public PlateauSurface(byte height, byte ramp = 0, bool blocked = false)
		{
			if (ramp > 4) throw new ArgumentOutOfRangeException(nameof(ramp));
			Height = height;
			Ramp = ramp;
			Blocked = blocked;
		}

		// N/E/S/W corners, matching MapGrid.Ramps 1..4, not screen-facing guesses.
		public int CornerHeight(int corner)
		{
			if (corner < 0 || corner > 3) throw new ArgumentOutOfRangeException(nameof(corner));
			var mask = Ramp == 1 ? 6 : Ramp == 2 ? 12 : Ramp == 3 ? 9 : Ramp == 4 ? 3 : 0;
			return Height + ((mask >> corner) & 1);
		}
	}

	static class PlateauTopology
	{
		public static readonly CVec[] Directions = { new CVec(1, 0), new CVec(0, 1), new CVec(-1, 0), new CVec(0, -1) };

		public static PlateauSurface Get(IReadOnlyDictionary<CPos, PlateauSurface> cells, CPos cell) =>
			cells.TryGetValue(cell, out var surface) ? surface : default;

		// The two common vertices are ordered along the owning cell's edge.
		public static (int A, int B, int NeighborA, int NeighborB) Edge(int direction)
		{
			switch (direction)
			{
				case 0: return (1, 2, 0, 3);
				case 1: return (2, 3, 1, 0);
				case 2: return (3, 0, 2, 1);
				case 3: return (0, 1, 3, 2);
				default: throw new ArgumentOutOfRangeException(nameof(direction));
			}
		}

		public static bool Continuous(PlateauSurface a, PlateauSurface b, int direction)
		{
			var edge = Edge(direction);
			return a.CornerHeight(edge.A) == b.CornerHeight(edge.NeighborA) &&
				a.CornerHeight(edge.B) == b.CornerHeight(edge.NeighborB);
		}

		public static Dictionary<CPos, PlateauSurface> WithCliffFeet(IReadOnlyDictionary<CPos, PlateauSurface> surfaces)
		{
			var result = new Dictionary<CPos, PlateauSurface>(surfaces);
			foreach (var pair in surfaces)
			{
				if (pair.Value.Height != 4 || pair.Value.Ramp != 0) continue;
				for (var x = -1; x <= 1; x++)
					for (var y = -1; y <= 1; y++)
					{
						var foot = pair.Key + new CVec(x, y);
						if (!surfaces.ContainsKey(foot)) result[foot] = new PlateauSurface(0, 0, true);
					}
			}
			// Keep every downhill approach lane open; adjacent high guards must
			// not reserve the diagonal cells through which units enter the ramp.
			foreach (var pair in surfaces)
				if (pair.Value.Ramp != 0 && pair.Value.Height == 0)
				{
					var outward = Directions[(pair.Value.Ramp + 1) % 4];
					if (!surfaces.ContainsKey(pair.Key + outward)) result.Remove(pair.Key + outward);
				}
			return result;
		}

		public static Dictionary<CPos, PlateauSurface> CalibrationPlateau(CPos center, int radius = 7)
		{
			if (radius < 7) throw new ArgumentOutOfRangeException(nameof(radius));
			var cells = new Dictionary<CPos, PlateauSurface>();
			for (var x = -radius; x <= radius; x++)
				for (var y = -radius; y <= radius; y++) cells.Add(center + new CVec(x, y), new PlateauSurface(4));
			for (var direction = 0; direction < 4; direction++)
			{
				var outward = Directions[direction];
				var lateral = Directions[(direction + 1) % 4];
				var ramp = (byte)((direction + 2) % 4 + 1);
				for (var step = 0; step < 4; step++)
				{
					for (var lane = -1; lane <= 1; lane++)
						cells[center + outward * (radius - step) + lateral * lane] = new PlateauSurface((byte)step, ramp);
					// Explicit retaining-wall guards prevent the engine's permitted one-level
					// side steps from bypassing the ascent. They are not the plateau interior.
					foreach (var lane in new[] { -2, 2 })
						cells[center + outward * (radius - step) + lateral * lane] = new PlateauSurface(4, 0, true);
				}
				for (var step = -1; step < 4; step++)
				{
					var cell = center + outward * (radius - step);
					if (!Continuous(Get(cells, cell), Get(cells, cell - outward), (direction + 2) % 4))
						throw new InvalidOperationException("Discontinuous plateau ascent.");
				}
			}
			return cells;
		}
	}
}
