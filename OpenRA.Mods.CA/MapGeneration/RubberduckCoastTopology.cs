using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Coast policy belongs to topology, before export/art. Keep explicit landing
	// areas for each traversable component and each player, not just one global beach.
	static class RubberduckCoastTopology
	{
		public static void Mark(MapPlan plan, MapGenerationOptions options)
		{
			if (options.Preset != MapGenerationPreset.Archipelago && options.Preset != MapGenerationPreset.Continents &&
				options.Preset != MapGenerationPreset.Migration && options.Preset != MapGenerationPreset.WaterBasins) return;
			if (options.Preset == MapGenerationPreset.WaterBasins) RubberduckCoastSmoother.Apply(plan, false, PlayerSpawnLayout.SymmetryOrder(options.Players));
			var directions = new (int X, int Y)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
			var shores = new HashSet<(int X, int Y)>(); var land = new HashSet<(int X, int Y)>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (plan.IsTraversable(x, y)) land.Add((x, y));
					if (plan.TerrainAt(x, y) == PlannedTerrain.Land && directions.Any(d => plan.Contains(x + d.X, y + d.Y) && plan.TerrainAt(x + d.X, y + d.Y) == PlannedTerrain.Water)) shores.Add((x, y));
				}
			if (shores.Count == 0) return;
			void Landing((int X, int Y) c)
			{
				for (var x = -1; x <= 1; x++)
					for (var y = -1; y <= 1; y++)
						if (plan.Contains(c.X + x, c.Y + y) && plan.IsTraversable(c.X + x, c.Y + y))
							plan.AddFeature(c.X + x, c.Y + y, PlannedFeature.LaunchShore);
			}
			foreach (var spawn in plan.Spawns)
				Landing(shores.OrderBy(c => (long)(c.X - spawn.X) * (c.X - spawn.X) + (long)(c.Y - spawn.Y) * (c.Y - spawn.Y)).ThenBy(c => c.X).ThenBy(c => c.Y).First());
			while (land.Count != 0)
			{
				var pending = new Queue<(int X, int Y)>(); pending.Enqueue(land.First()); var boundary = new List<(int X, int Y)>();
				while (pending.Count != 0)
				{
					var c = pending.Dequeue(); if (!land.Remove(c)) continue;
					if (shores.Contains(c)) boundary.Add(c);
					foreach (var d in directions) if (land.Contains((c.X + d.X, c.Y + d.Y))) pending.Enqueue((c.X + d.X, c.Y + d.Y));
				}
				if (boundary.Count == 0) continue;
				Landing(boundary.OrderBy(c => c.X).ThenBy(c => c.Y).First());
				Landing(boundary.OrderByDescending(c => c.X).ThenBy(c => c.Y).First());
			}
			var water = new HashSet<(int X, int Y)>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++) if (plan.TerrainAt(x, y) == PlannedTerrain.Water) water.Add((x, y));
			while (water.Count != 0)
			{
				var pending = new Queue<(int X, int Y)>(); pending.Enqueue(water.First()); var boundary = new HashSet<(int X, int Y)>();
				while (pending.Count != 0)
				{
					var c = pending.Dequeue(); if (!water.Remove(c)) continue;
					foreach (var d in directions)
					{
						var n = (c.X + d.X, c.Y + d.Y);
						if (water.Contains(n)) pending.Enqueue(n);
						if (shores.Contains(n)) boundary.Add(n);
					}
				}
				if (boundary.Count != 0)
				{
					Landing(boundary.OrderBy(c => c.X).ThenBy(c => c.Y).First());
					Landing(boundary.OrderByDescending(c => c.X).ThenByDescending(c => c.Y).First());
				}
			}
			foreach (var c in shores)
				if (!plan.HasFeature(c.X, c.Y, PlannedFeature.LaunchShore | PlannedFeature.DestructibleBridge | PlannedFeature.Bottleneck)) plan.AddFeature(c.X, c.Y, PlannedFeature.CliffShore);
			plan.ValidationMessages.Add("Coast topology: player and component landing areas reserved before rock-coast selection.");
		}
	}
}
