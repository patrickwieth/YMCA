using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class CustomIslandConnections
	{
		public static List<HashSet<CPos>> Components(MapPlan plan, bool includeBridges = false)
		{
			var remaining = new HashSet<CPos>();
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				if (plan.TerrainAt(x, y) == PlannedTerrain.Land || includeBridges && plan.TerrainAt(x, y) == PlannedTerrain.ShallowWater)
					remaining.Add(PlannedRubberduckBridge.Cell(x, y));
			var result = new List<HashSet<CPos>>();
			while (remaining.Count > 0)
			{
				var component = new HashSet<CPos>(); var pending = new Queue<CPos>();
				pending.Enqueue(remaining.OrderBy(c => c.X).ThenBy(c => c.Y).First());
				while (pending.Count > 0)
				{
					var c = pending.Dequeue(); if (!remaining.Remove(c)) continue;
					component.Add(c);
					foreach (var d in PlateauTopology.Directions) if (remaining.Contains(c + d)) pending.Enqueue(c + d);
				}
				result.Add(component);
			}
			return result;
		}

		public static bool Connect(MapPlan plan, int parallelLinks = 0)
		{
			var centers = Components(plan).Select(c => PlannedRubberduckBridge.Point(c.OrderBy(p => p.X).ThenBy(p => p.Y).First()))
				.Select(p => new PlanPoint(p.U, p.V)).ToArray();
			if (centers.Length <= 1 || !RubberduckBridgePlanner.Connect(plan, centers, network: true, parallelLinks: parallelLinks)) return false;
			// Connections must create an actual route beyond a team's home islands,
			// not merely recreate Archipelago's legacy same-team links.
			return Components(plan, true).Any(component => plan.Spawns.Select((p, i) => (Point: p, Team: i * 2 / plan.Spawns.Count))
				.Where(p => component.Contains(PlannedRubberduckBridge.Cell(p.Point.X, p.Point.Y))).Select(p => p.Team).Distinct().Count() > 1);
		}

		public static void MarkMigrationCenter(MapPlan plan)
		{
			var center = PlannedRubberduckBridge.Cell(plan.Width / 2, plan.Height / 2);
			var component = Components(plan).Single(c => c.Contains(center));
			foreach (var c in component)
			{
				var p = PlannedRubberduckBridge.Point(c);
				plan.AddFeature(p.U, p.V, PlannedFeature.ContestedCenter);
			}
		}
	}
}
