using System;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Shared, stateless art selection: no world RNG, actor IDs or generator preset.
	static class RubberduckCliffVisuals
	{
		public static int Variant(CPos cell, int count, int direction = 0)
		{
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
			var hash = unchecked((uint)cell.X * 0x9e3779b9u ^ (uint)cell.Y * 0x85ebca6bu ^ (uint)direction * 0xc2b2ae35u);
			hash ^= hash >> 16; hash = unchecked(hash * 0x7feb352du);
			hash ^= hash >> 15; hash = unchecked(hash * 0x846ca68bu); hash ^= hash >> 16;
			return (int)(hash % (uint)count);
		}

		public static int StraightSlot(int slot, CPos cell)
		{
			var first = slot >= 0 && slot <= 2 ? 0 : slot >= 5 && slot <= 7 ? 5 :
				slot >= 8 && slot <= 10 ? 8 : slot >= 12 && slot <= 14 ? 12 : -1;
			return first < 0 ? slot : first + Variant(cell, 3, first);
		}

		// -1: a straight join, 0: convex corner, 1: concave corner.
		public static int Corner(Map map, CPos cell, int direction, int end)
		{
			var side = PlateauTopology.Directions[(direction + (end == 0 ? 3 : 1)) % 4];
			var height = map.Height[cell];
			bool High(CPos c) => map.Contains(c) && map.Ramp[c] == 0 && map.Height[c] >= height;
			if (!High(cell + side)) return 0;
			return High(cell + side + PlateauTopology.Directions[direction]) ? 1 : -1;
		}
	}
}
