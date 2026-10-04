using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Flat coast semantics: the cliff is artwork, never a heightmap operation.
	// The caller must install BlockedWater as water artwork with Cliff terrain.
	static class RubberduckRockCoastPlan
	{
		static readonly HashSet<string> Walkable = new(StringComparer.Ordinal) { "Clear", "Sand", "Road", "Rough", "Ore", "Gems", "Tiberium", "BlueTiberium" };
		public const ushort BlockedWater = 14010;
		public const ushort BlockedSand = 14011;
		public const ushort BlockedGrass = 14012;
		public static IReadOnlyDictionary<CPos, TerrainTile> Create(Map map, IEnumerable<CPos> bankCells,
			IReadOnlyCollection<CPos> protectedCells, Func<CPos, ushort> terrainType = null)
		{
			var patch = Footprint(map, bankCells, protectedCells, terrainType ?? (c => map.Tiles[c].Type));
			PreserveConnections(map, patch, true);
			PreserveConnections(map, patch, false);
			return patch;
		}

		// Also used against a nonmutating restored view when trimming an existing coast.
		internal static Dictionary<CPos, TerrainTile> Footprint(Map map, IEnumerable<CPos> bankCells,
			IReadOnlyCollection<CPos> protectedCells, Func<CPos, ushort> terrainType)
		{
			var banks = bankCells.ToHashSet();
			if (banks.Count == 0 || banks.Count > 512) throw new InvalidOperationException("Select 1–512 coast cells.");
			var patch = new Dictionary<CPos, TerrainTile>();
			var protection = protectedCells.ToHashSet();
			foreach (var c in banks)
			{
				if (!map.Contains(c) || map.Height[c] != 0 || map.Ramp[c] != 0 ||
					(terrainType(c) != 1000 && terrainType(c) != 1020))
					throw new InvalidOperationException("Rock coasts require flat grass or sand, not raised plateaus.");
				var water = PlateauTopology.Directions.Where(d => map.Contains(c + d) && terrainType(c + d) == 1050).ToArray();
				if (water.Length == 0 || water.Length > 2 || (water.Length == 2 && water[0] + water[1] == CVec.Zero))
					throw new InvalidOperationException("Coast needs a straight bank or a convex corner, not a narrow spit.");
				patch[c] = new TerrainTile(terrainType(c) == 1020 ? BlockedSand : BlockedGrass, 0);
				// Reserve the leaning native rock's water-side footprint as well, so
				// ships cannot sail through it or unload across a decorative wall.
				void Reserve(CPos foot)
				{
					if (!map.Contains(foot) || terrainType(foot) != 1050 || map.Height[foot] != 0 || map.Ramp[foot] != 0)
						throw new InvalidOperationException("Rock coast needs two clear water rows; narrow channels are protected.");
					patch[foot] = new TerrainTile(BlockedWater, 0);
				}
				foreach (var outward in water)
					for (var depth = 1; depth <= 2; depth++) Reserve(c + outward * depth);
				if (water.Length == 2)
					for (var x = 1; x <= 2; x++)
						for (var y = 1; y <= 2; y++) Reserve(c + water[0] * x + water[1] * y);
			}
			// Native end piles lean into water beyond the last face. Reserve their
			// actual end sockets too, not just the straight face's two water rows.
			foreach (var cap in RubberduckRockCoastRenderer.EndCaps(map, banks, terrainType))
			{
				var outward = PlateauTopology.Directions[cap.Direction];
				var tangent = cap.Direction % 2 == 0 ? new CVec(0, 1) : new CVec(1, 0);
				for (var lane = 0; lane <= (cap.Direction < 2 ? 1 : 0); lane++)
					for (var depth = 1; depth <= 2; depth++)
					{
						var foot = cap.Cell + tangent * lane + outward * depth;
						if (!map.Contains(foot) || terrainType(foot) != 1050 || map.Height[foot] != 0 || map.Ramp[foot] != 0)
							throw new InvalidOperationException("Coast end piece needs clear water; nearby land and narrow landings are protected.");
						patch[foot] = new TerrainTile(BlockedWater, 0);
					}
			}
			if (patch.Keys.Any(c => protection.Contains(c) || map.Resources[c].Type != 0))
				throw new InvalidOperationException("Rock coast overlaps protected actors, resources or landing access.");
			return patch;
		}

		static void PreserveConnections(Map map, Dictionary<CPos, TerrainTile> patch, bool water)
		{
			var remaining = map.AllCells.Where(map.Contains).Where(c => water ? map.GetTerrainInfo(c).Type == "Water" :
				Walkable.Contains(map.GetTerrainInfo(c).Type)).ToHashSet();
			bool Connected(CPos a, CPos b, int direction) => water || PlateauTopology.Continuous(
				new PlateauSurface(map.Height[a], map.Ramp[a]), new PlateauSurface(map.Height[b], map.Ramp[b]), direction);
			HashSet<CPos> Flood(CPos first, HashSet<CPos> allowed)
			{
				var seen = new HashSet<CPos>(); var queue = new Queue<CPos>(); queue.Enqueue(first);
				while (queue.Count != 0)
				{
					var c = queue.Dequeue(); if (!seen.Add(c)) continue;
					for (var d = 0; d < 4; d++)
					{
						var n = c + PlateauTopology.Directions[d];
						if (allowed.Contains(n) && !seen.Contains(n) && Connected(c, n, d)) queue.Enqueue(n);
					}
				}
				return seen;
			}
			while (remaining.Count != 0)
			{
				var component = Flood(remaining.First(), remaining); remaining.ExceptWith(component);
				var survivors = component.Where(c => !patch.ContainsKey(c)).ToHashSet();
				if (survivors.Count == 0 || Flood(survivors.First(), survivors).Count != survivors.Count)
					throw new InvalidOperationException("Rock coast would disconnect or erase a " + (water ? "waterway." : "land route."));
				if (!water)
				{
					bool Landing(CPos c) => PlateauTopology.Directions.Any(d => map.Contains(c + d) && map.Height[c] == 0 &&
						map.GetTerrainInfo(c + d).Type == "Water" && !patch.ContainsKey(c + d));
					if (component.Any(c => PlateauTopology.Directions.Any(d => map.Contains(c + d) && map.GetTerrainInfo(c + d).Type == "Water")) && !survivors.Any(Landing))
						throw new InvalidOperationException("Rock coast would remove the last landing access.");
				}
			}
		}
	}
}
