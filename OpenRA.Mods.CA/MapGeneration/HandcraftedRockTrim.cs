using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// User-approved Glitters adjustment: reclaim one original boundary layer.
	// Keep a connected rock spine and never join the isolated oil pockets to land.
	static class HandcraftedRockTrim
	{
		public static HashSet<CPos> Apply(Dictionary<CPos, string> terrain, IEnumerable<CPos> starts, HashSet<CPos> protectedCells)
		{
			var land = terrain.Where(p => p.Value == "Clear" || p.Value == "Rough").Select(p => p.Key).ToHashSet();
			var accessible = new HashSet<CPos>();
			var pending = new Queue<CPos>(starts);
			while (pending.Count != 0)
			{
				var c = pending.Dequeue();
				if (!land.Contains(c) || !accessible.Add(c)) continue;
				foreach (var d in PlateauTopology.Directions) pending.Enqueue(c + d);
			}
			var rocks = terrain.Where(p => p.Value == "Rock").Select(p => p.Key).ToHashSet();
			var candidates = rocks.Where(c => !protectedCells.Contains(c) && PlateauTopology.Directions.Any(d => accessible.Contains(c + d)))
				.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
			var reclaimed = new HashSet<CPos>();
			foreach (var c in candidates)
			{
				var ring = new HashSet<CPos>();
				var protectedPocket = false;
				for (var x = -1; x <= 1; x++)
					for (var y = -1; y <= 1; y++)
					{
						if (x == 0 && y == 0) continue;
						var n = c + new CVec(x, y);
						if (rocks.Contains(n)) ring.Add(n);
						// Also preserve diagonal separation, not merely cardinal access.
						if (land.Contains(n) && !accessible.Contains(n)) protectedPocket = true;
					}
				if (protectedPocket || ring.Count < 2) continue;
				// Removing a simple boundary point must not split the remaining rock.
				pending.Enqueue(ring.First());
				while (pending.Count != 0)
				{
					var n = pending.Dequeue();
					if (!ring.Remove(n)) continue;
					foreach (var direction in PlateauTopology.Directions)
						if (ring.Contains(n + direction)) pending.Enqueue(n + direction);
				}
				if (ring.Count != 0) continue;
				rocks.Remove(c); land.Add(c); accessible.Add(c); reclaimed.Add(c);
				terrain[c] = "Clear";
			}
			return reclaimed;
		}
	}
}
