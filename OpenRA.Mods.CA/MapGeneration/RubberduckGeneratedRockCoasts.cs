using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Consume topology's explicit CliffShore decisions; do not invent cliff banks
	// on LaunchShore or silently reinterpret every water preset's coast.
	static class RubberduckGeneratedRockCoasts
	{
		public static int Apply(Map map, MapPlan plan, List<MiniYamlNode> actors, Func<int, int, CPos> cell)
		{
			if (!Enumerable.Range(0, plan.Width * plan.Height).Any(i => plan.HasFeature(i % plan.Width, i / plan.Width, PlannedFeature.CliffShore))) return 0;
			var candidates = new HashSet<CPos>(); var protection = new HashSet<CPos>();
			void Protect(CPos c, int radius)
			{
				for (var x = -radius; x <= radius; x++)
					for (var y = -radius; y <= radius; y++) protection.Add(c + new CVec(x, y));
			}
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var features = plan.FeaturesAt(x, y);
					if ((features & PlannedFeature.CliffShore) != 0) candidates.Add(cell(x, y));
					if ((features & (PlannedFeature.Resource | PlannedFeature.RichResource)) != PlannedFeature.None) Protect(cell(x, y), 0);
					if ((features & ~(PlannedFeature.CliffShore | PlannedFeature.Resource | PlannedFeature.RichResource)) != PlannedFeature.None) Protect(cell(x, y), 2);
				}
			foreach (var actor in actors)
			{
				var location = new ActorReference(actor.Value.Value, actor.Value.ToDictionary()).GetOrDefault<LocationInit>();
				if (location != null) Protect(location.Value, 5);
			}
			bool Eligible(CPos c)
			{
				if (!map.Contains(c) || map.Height[c] != 0 || map.Ramp[c] != 0 || protection.Contains(c) ||
					(map.Tiles[c].Type != 1000 && map.Tiles[c].Type != 1020)) return false;
				if (Enumerable.Range(-4, 9).Any(x => Enumerable.Range(-4, 9).Any(y => !map.Contains(c + new CVec(x, y))))) return false;
				var directions = Enumerable.Range(0, 4).Where(d => map.Contains(c + PlateauTopology.Directions[d]) && map.Tiles[c + PlateauTopology.Directions[d]].Type == 1050).ToArray();
				return directions.Length == 1 || directions.Length == 2 && (directions[0] + 2) % 4 != directions[1];
			}
			var segments = 0; var remaining = candidates.Where(Eligible).ToHashSet();
			while (remaining.Count != 0)
			{
				var banks = new List<CPos>(); var pending = new Queue<CPos>();
				pending.Enqueue(remaining.OrderBy(c => c.X).ThenBy(c => c.Y).First());
				while (pending.Count != 0)
				{
					var c = pending.Dequeue(); if (!remaining.Remove(c)) continue; banks.Add(c);
					for (var x = -1; x <= 1; x++)
						for (var y = -1; y <= 1; y++) if (remaining.Contains(c + new CVec(x, y))) pending.Enqueue(c + new CVec(x, y));
				}
				if (banks.Count < 3 || banks.Count > 512) continue;
				try
				{
					var patch = RubberduckRockCoastPlan.Create(map, banks, protection);
					var pieces = RubberduckRockCoastRenderer.Actors(map, banks, $"RockCoast{segments}_");
					foreach (var pair in patch) map.Tiles[pair.Key] = pair.Value;
					actors.AddRange(pieces); segments++;
					foreach (var bank in banks) Protect(bank, 2);
				}
				catch (InvalidOperationException) { /* Protected or branching contours remain shore; do not fragment them into rock posts. */ }
			}
			plan.ValidationMessages.Add($"Flat rock coasts: {segments} safe segments on planned CliffShore; launch shores and protected features retained.");
			return segments;
		}
	}
}
