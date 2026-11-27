using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class HandcraftedGrassTrees
	{
		internal static readonly string[] Images = { "t01", "t05", "t11", "t16" };
		internal static List<MiniYamlNode> Actors(Map map, IReadOnlyDictionary<CPos, string> terrain,
			IEnumerable<CPos> starts, IEnumerable<CPos> originalActors)
		{
			var spawns = starts.ToArray(); var originals = originalActors.ToArray();
			int Distance(CPos a, CPos b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
			bool Eligible(CPos c)
			{
				if (spawns.Any(s => Distance(s, c) < 12) || originals.Any(a => Distance(a, c) < 5)) return false;
				for (var x = -5; x <= 5; x++)
					for (var y = -5; y <= 5; y++)
					{
						var n = c + new CVec(x, y);
						if (!terrain.TryGetValue(n, out var t) || map.Resources[n].Type != 0 ||
							Math.Abs(x) <= 4 && Math.Abs(y) <= 4 && t != "Clear") return false;
					}
				return true;
			}
			uint Hash(CPos c) => unchecked((uint)(c.X * 73856093 ^ c.Y * 19349663) * 1274126177);
			var eligible = terrain.Keys.Where(Eligible).ToHashSet();
			var anchors = new List<CPos>(); var occupied = new HashSet<CPos>();
			var result = new List<MiniYamlNode>();
			foreach (var c in eligible.OrderBy(Hash).ThenBy(c => c.Y).ThenBy(c => c.X))
			{
				if (anchors.Any(a => Distance(a, c) < 17)) continue;
				anchors.Add(c);
				foreach (var offset in new[] { CVec.Zero, new CVec(2, 1), new CVec(-1, 2) })
				{
					var n = c + offset;
					if (!eligible.Contains(n) || !occupied.Add(n)) continue;
					var variant = Hash(n) % Images.Length;
					result.Add(new MiniYamlNode("HandcraftedGrassTree" + result.Count,
						new ActorReference("terrain.handcrafted.grasstree" + variant)
						{ new LocationInit(n), new OwnerInit("Neutral") }.Save()));
				}
				if (anchors.Count == 14) break;
			}
			return result;
		}
	}
}
