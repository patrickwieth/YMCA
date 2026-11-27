using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Derive a continuous visual beach from the FINAL water substrate, including
	// isolated offshore boulders. Original gameplay terrain remains in map overrides.
	static class HandcraftedNavalShore
	{
		internal static void Apply(Map map, IReadOnlyDictionary<CPos, string> terrain)
		{
			var water = terrain.Keys.Where(c => map.Tiles[c].Type == 1050).ToHashSet();
			bool Eligible(CPos c) => terrain.TryGetValue(c, out var t) &&
				(t == "Clear" || t == "Beach" || t == "Rough" || t == "Rock" && map.Tiles[c].Type == 1000) && map.Resources[c].Type == 0;
			var beach = new HashSet<CPos>();
			var frontier = water;
			// Grow through land, not across a rock barrier. A square distance stencil
			// would leave detached sand flecks on the grass behind the rock bank.
			for (var depth = 0; depth < 3; depth++)
			{
				var next = new HashSet<CPos>();
				foreach (var c in frontier)
					foreach (var d in PlateauTopology.Directions)
						if (Eligible(c + d) && beach.Add(c + d)) next.Add(c + d);
				frontier = next;
			}
			foreach (var pair in terrain)
			{
				if (!Eligible(pair.Key)) continue;
				var c = pair.Key;
				if (map.Resources[c].Type != 0) continue;
				map.Tiles[c] = new TerrainTile((ushort)(beach.Contains(c) ? 1020 : pair.Value == "Rough" ? 1040 : 1000), 0);
			}
		}
	}
}
