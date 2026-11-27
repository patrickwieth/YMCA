using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Visual roof closure only. Never edits collision, resources or heights.
	static class HandcraftedRockRoof
	{
		internal static CPos[] EnclosedCells(Map map, IReadOnlyDictionary<CPos, string> terrain,
			HashSet<CPos> rocks, IEnumerable<CPos> starts, HashSet<CPos> reclaimed)
		{
			var exterior = new HashSet<CPos>();
			var pending = new Queue<CPos>();
			foreach (var c in terrain.Keys)
				if (!rocks.Contains(c) && (reclaimed.Contains(c) || map.Resources[c].Type != 0 ||
					PlateauTopology.Directions.Any(d => !terrain.ContainsKey(c + d)))) pending.Enqueue(c);
			foreach (var c in starts) pending.Enqueue(c);
			while (pending.Count != 0)
			{
				var c = pending.Dequeue();
				if (!terrain.ContainsKey(c) || rocks.Contains(c) || !exterior.Add(c)) continue;
				// Retain even diagonal openings; do not visually bridge a ground route.
				for (var x = -1; x <= 1; x++)
					for (var y = -1; y <= 1; y++)
						if (x != 0 || y != 0) pending.Enqueue(c + new CVec(x, y));
			}
			return terrain.Where(p => (p.Value == "Clear" || p.Value == "Rough") && !rocks.Contains(p.Key) &&
				!exterior.Contains(p.Key) && !reclaimed.Contains(p.Key) && map.Resources[p.Key].Type == 0)
				.Select(p => p.Key).OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
		}
	}
}
