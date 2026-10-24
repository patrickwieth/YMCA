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
		public static int Apply(Map map, MapPlan plan, List<MiniYamlNode> actors, Func<int, int, CPos> cell, bool continuousBanks = false)
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
				if (!continuousBanks && Enumerable.Range(-4, 9).Any(x => Enumerable.Range(-4, 9).Any(y => !map.Contains(c + new CVec(x, y))))) return false;
				var directions = Enumerable.Range(0, 4).Where(d => map.Contains(c + PlateauTopology.Directions[d]) && map.Tiles[c + PlateauTopology.Directions[d]].Type == 1050).ToArray();
				if (!(directions.Length == 1 || directions.Length == 2 && (directions[0] + 2) % 4 != directions[1])) return false;
				if (!continuousBanks) return true;
				// Validate the face footprint here, not hypothetical end caps at every
				// cell. End footprints belong only at the actual contour endpoints;
				// checking tangent lanes here cut holes at every gentle bank bend.
				foreach (var d in directions)
				{
					var outward = PlateauTopology.Directions[d];
					for (var depth = 1; depth <= 2; depth++)
					{
						var foot = c + outward * depth;
						if (!map.Contains(foot)) continue;
						if (protection.Contains(foot) || map.Resources[foot].Type != 0 || map.Tiles[foot].Type != 1050) return false;
					}
				}
				if (directions.Length == 2)
					for (var x = 1; x <= 2; x++)
						for (var y = 1; y <= 2; y++)
							{
								var foot = c + PlateauTopology.Directions[directions[0]] * x + PlateauTopology.Directions[directions[1]] * y;
								if (!map.Contains(foot)) continue;
								if (protection.Contains(foot) || map.Tiles[foot].Type != 1050) return false;
							}
				return true;
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
				if (banks.Count < (continuousBanks ? 6 : 3) || banks.Count > 512) continue;
				if (continuousBanks && !banks.Any(c => RubberduckRockCoastRenderer.AtMapEdge(map, c))) continue;
				try
				{
					var patch = continuousBanks ? FitWholeBank(map, banks, protection) : RubberduckRockCoastPlan.Create(map, banks, protection);
					var pieces = RubberduckRockCoastRenderer.Actors(map, banks, $"RockCoast{segments}_", naturalJoints: continuousBanks, clipMapEdge: continuousBanks, lowRearEnds: continuousBanks);
					if (continuousBanks && (!banks.Any(c => RubberduckRockCoastRenderer.AtMapEdge(map, c)) ||
						pieces.Count(n => n.Value.Value.Contains("coast-abutment-", StringComparison.Ordinal)) != 1))
						throw new InvalidOperationException("Defensive bank must reach the map boundary without internal gaps.");
					foreach (var pair in patch) map.Tiles[pair.Key] = pair.Value;
					actors.AddRange(pieces); segments++;
					foreach (var bank in banks) Protect(bank, 2);
				}
				catch (InvalidOperationException e)
				{
					// Keep the whole rejected contour as shore, not isolated rock posts.
					plan.ValidationMessages.Add($"Rock coast candidate skipped ({banks.Count} banks): {e.Message}");
				}
			}
			if (continuousBanks && segments != 2)
				throw new InvalidOperationException("Two Sides requires two complete, connected defensive banks reaching the map edge.");
			plan.ValidationMessages.Add($"Flat rock coasts: {segments} safe segments on planned CliffShore; launch shores and protected features retained.");
			return segments;
		}

		static IReadOnlyDictionary<CPos, TerrainTile> FitWholeBank(Map map, List<CPos> banks, HashSet<CPos> protection)
		{
			// Two Sides banks are monotone along a native river axis. Only retreat
			// endpoints when a cap does not fit; never punch holes in the interior.
			var alongX = banks.Max(c => c.X) - banks.Min(c => c.X) >= banks.Max(c => c.Y) - banks.Min(c => c.Y);
			var ordered = banks.OrderBy(c => alongX ? c.X : c.Y).ThenBy(c => alongX ? c.Y : c.X).ToArray();
			InvalidOperationException failure = null;
			for (var trim = 0; trim <= Math.Min(8, ordered.Length - 6); trim++)
				for (var start = 0; start <= trim; start++)
				{
					var candidate = ordered.Skip(start).Take(ordered.Length - trim).ToArray();
					try
					{
						// Do not retreat an edge-reaching endpoint back into the map.
					if (RubberduckRockCoastRenderer.AtMapEdge(map, ordered[0]) && start != 0 ||
						RubberduckRockCoastRenderer.AtMapEdge(map, ordered[ordered.Length - 1]) && trim != start) continue;
					var patch = RubberduckRockCoastPlan.Create(map, candidate, protection, clipMapEdge: true);
						banks.Clear(); banks.AddRange(candidate);
						return patch;
					}
					catch (InvalidOperationException e)
					{
						// Trimming decorative sockets cannot repair a disconnected bank
						// at the map edge. Reject this layout instead of repeating floods.
						if (e is RubberduckRockCoastPlan.ConnectivityException) throw;
						failure = e;
					}
				}
			throw failure ?? new InvalidOperationException("No complete safe bank contour fits.");
		}
	}
}
