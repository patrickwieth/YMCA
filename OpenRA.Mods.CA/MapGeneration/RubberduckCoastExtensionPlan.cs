using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class RubberduckCoastExtensionPlan
	{
		public readonly IReadOnlyDictionary<CPos, TerrainTile> Patch;
		public readonly List<MiniYamlNode> BeforeActors;
		public readonly List<MiniYamlNode> AfterActors;
		RubberduckCoastExtensionPlan(IReadOnlyDictionary<CPos, TerrainTile> patch, List<MiniYamlNode> before, List<MiniYamlNode> after)
		{ Patch = patch; BeforeActors = before; AfterActors = after; }

		public static RubberduckCoastExtensionPlan Create(Map map, EditorActorLayer layer, IEnumerable<CPos> stroke)
		{
			var banks = stroke.ToHashSet();
			if (banks.Count == 0 || banks.Count > 512 || banks.Any(c => !map.Contains(c)))
				throw new InvalidOperationException("Select 1–512 bank cells inside the map.");
			var restored = new Dictionary<CPos, TerrainTile>();
			var before = new Dictionary<string, MiniYamlNode>();
			// Every candidate footprint lies within two cells of its bank. Recover
			// neighboring components first, including their end caps, without mutation.
			foreach (var bank in banks.ToArray())
				for (var x = -2; x <= 2; x++)
					for (var y = -2; y <= 2; y++)
					{
						var c = bank + new CVec(x, y);
						if (!map.Contains(c) || restored.ContainsKey(c) || map.Tiles[c].Type < 14010 || map.Tiles[c].Type > 14012) continue;
						var component = RubberduckCoastRemovalPlan.Create(map, layer, c);
						foreach (var p in component.Patch) restored[p.Key] = p.Value;
						foreach (var n in component.Actors) before[n.Key] = n;
						if (restored.Count > 4096) throw new InvalidOperationException("Affected coast is too large; use smaller isolated edits.");
					}
			if (banks.All(c => restored.ContainsKey(c))) throw new InvalidOperationException("Stroke adds no new bank cells.");
			banks.UnionWith(restored.Where(p => p.Value.Type != 1050).Select(p => p.Key));
			ushort Terrain(CPos c) => restored.TryGetValue(c, out var t) ? t.Type : map.Tiles[c].Type;
			var protection = layer.Save().Where(n => !before.ContainsKey(n.Key)).SelectMany(n =>
			{
				var preview = layer[n.Key];
				return preview.Footprint.Count == 0 ? new[] { preview.Location }.AsEnumerable() : preview.Footprint.Keys;
			}).ToHashSet();
			// Validate against current passable land/water, not the virtually restored
			// coast: existing landings and connections must survive the extension.
			var patch = RubberduckRockCoastPlan.Create(map, banks, protection, Terrain);
			if (restored.Keys.Any(c => !patch.ContainsKey(c))) throw new InvalidOperationException("Extension would unexpectedly remove an existing footprint.");
			var serial = 0; var saved = layer.Save();
			while (saved.Any(n => n.Key.StartsWith($"EditorRockCoast{serial}_", StringComparison.Ordinal))) serial++;
			var actors = RubberduckRockCoastRenderer.Actors(map, banks, $"EditorRockCoast{serial}_", Terrain);
			return new RubberduckCoastExtensionPlan(patch, before.Values.ToList(), actors);
		}
	}
}
