using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class StartBalanceMetrics
	{
		public static int[] Distances(MapPlan plan, PlanPoint start)
		{
			var distance = Enumerable.Repeat(-1, plan.Width * plan.Height).ToArray();
			var queue = new Queue<PlanPoint>(); queue.Enqueue(start); distance[start.X + start.Y * plan.Width] = 0;
			while (queue.Count > 0)
			{
				var p = queue.Dequeue(); var c = MountainPlateauPlanner.Cell(p.X, p.Y);
				var h = plan.PlateauSurfaces.TryGetValue(new MPos(p.X, p.Y), out var s) ? s.Height : 0;
				foreach (var d in PlateauTopology.Directions)
				{
					var n = MountainPlateauPlanner.Point(c + d);
					if (!plan.IsTraversable(n.U, n.V)) continue;
					var nh = plan.PlateauSurfaces.TryGetValue(n, out var ns) ? ns.Height : 0;
					var i = n.U + n.V * plan.Width;
					if (distance[i] >= 0 || Math.Abs(h - nh) > 1) continue;
					distance[i] = distance[p.X + p.Y * plan.Width] + 1;
					queue.Enqueue(new PlanPoint(n.U, n.V));
				}
			}
			return distance;
		}

		public static object[] Report(MapPlan plan)
		{
			return plan.Spawns.Select((start, index) =>
			{
				var distances = Distances(plan, start);
				var resources = new List<int>(); var oils = new List<int>(); var build = 0;
				var field = plan.StartingFields.FirstOrDefault(f => f.Start.Equals(start) && !f.Expansion);
				var h = plan.PlateauSurfaces.TryGetValue(new MPos(start.X, start.Y), out var ss) ? ss.Height : 0;
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				{
					var distance = distances[x + y * plan.Width]; if (distance < 0) continue;
					if (plan.HasFeature(x, y, PlannedFeature.Resource | PlannedFeature.RichResource)) resources.Add(distance);
					if (plan.HasFeature(x, y, PlannedFeature.TechBuilding)) oils.Add(distance);
					var hasSurface = plan.PlateauSurfaces.TryGetValue(new MPos(x, y), out var s);
					if (distance <= 12 && (hasSurface ? s.Height == h && s.Ramp == 0 && !s.Blocked : h == 0 && plan.TerrainAt(x, y) == PlannedTerrain.Land) &&
						!plan.HasFeature(x, y, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding) &&
						!plan.StrategicReservations.Contains(new MPos(x, y))) build++;
				}
				return (object)new { Player = index + 1, Start = start, NearestResourceSteps = resources.DefaultIfEmpty(-1).Min(),
					ResourceCellsWithin24 = resources.Count(d => d <= 24), BuildableCellsWithin12 = build, NearestOilSteps = oils.DefaultIfEmpty(-1).Min(),
					OwnedStartingCells = field?.Cells.Length ?? 0,
					OwnedStartingEdgeSteps = field?.Cells.Min(p => distances[p.X + p.Y * plan.Width]) ?? -1 };
			}).ToArray();
		}
	}
}
