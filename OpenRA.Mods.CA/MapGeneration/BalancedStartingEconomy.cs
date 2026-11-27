using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class StartingResourceField
	{
		public readonly PlanPoint Start;
		public readonly PlanPoint[] Cells;
		public readonly int InitialEdgeDistance;
		public readonly bool Expansion;
		public readonly bool Contested;
		public StartingResourceField(PlanPoint start, PlanPoint[] cells, int distance, bool expansion, bool contested = false)
		{ Start = start; Cells = cells; InitialEdgeDistance = distance; Expansion = expansion; Contested = contested; }
	}

	static class BalancedStartingEconomy
	{
		sealed record Candidate(PlanPoint Center, PlanPoint[] Cells, int EdgeDistance, int Tie);

		public static bool PlaceFields(MapPlan plan, int radius, bool generators, MersenneTwister random, bool expansion = false, int resourceGap = 0)
		{
			var minimum = expansion ? 26 : 11;
			var maximum = expansion ? 48 : 30;
			var distances = plan.Spawns.Select(s => StartBalanceMetrics.Distances(plan, s)).ToArray();
			var choices = new List<Candidate>[plan.Spawns.Count];
			for (var owner = 0; owner < choices.Length; owner++)
			{
				choices[owner] = new();
				var eligible = new bool[plan.Width * plan.Height];
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				{
					var i = x + y * plan.Width; var own = distances[owner][i];
					eligible[i] = own >= minimum && FreeResourceCell(plan, x, y) && ClearOfResources(plan, x, y, resourceGap) &&
						!distances.Where((_, n) => n != owner).Any(d => d[i] >= 0 && d[i] < own + 3);
				}
				var budget = Enumerable.Range(-radius, radius * 2 + 1).Sum(x => Enumerable.Range(-radius, radius * 2 + 1).Count(y => x * x + y * y <= radius * radius));
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				{
					var i = x + y * plan.Width;
					if (!eligible[i] || distances[owner][i] > maximum + 15) continue;
					var cells = GatherField(plan, eligible, new PlanPoint(x, y), budget, radius + 3);
					if (cells.Length != budget) continue;
					var edge = cells.Min(p => distances[owner][p.X + p.Y * plan.Width]);
					if (edge <= maximum) choices[owner].Add(new(new PlanPoint(x, y), cells, edge, random.Next()));
				}
			}
			if (!Select(choices, out var selected, maximum)) return Failed(expansion ? "expansion fields" : "resource fields", choices);
			foreach (var (item, owner) in selected.Select((c, i) => (c, i)))
			{
				foreach (var p in item.Cells) plan.AddFeature(p.X, p.Y, PlannedFeature.Resource);
				if (generators) plan.AddFeature(item.Center.X, item.Center.Y, PlannedFeature.ResourceGenerator);
				plan.StartingFields.Add(new StartingResourceField(plan.Spawns[owner], item.Cells, item.EdgeDistance, expansion));
				// Preserve the native shortest approach before Plains rocks are planned.
				var current = item.Cells.OrderBy(p => distances[owner][p.X + p.Y * plan.Width]).First();
				while (distances[owner][current.X + current.Y * plan.Width] > 0)
				{
					var cell = MountainPlateauPlanner.Cell(current.X, current.Y);
					for (var dy = -2; dy <= 2; dy++) for (var dx = -2; dx <= 2; dx++)
					{
						var p = MountainPlateauPlanner.Point(cell + new CVec(dx, dy));
						if (plan.Contains(p.U, p.V) && plan.TerrainAt(p.U, p.V) == PlannedTerrain.Land && !plan.PlateauSurfaces.ContainsKey(p))
							plan.AddFeature(p.U, p.V, PlannedFeature.Entrance);
					}
					var currentHeight = plan.PlateauSurfaces.TryGetValue(new MPos(current.X, current.Y), out var currentSurface) ? currentSurface.Height : 0;
					var next = PlateauTopology.Directions.Select(d => MountainPlateauPlanner.Point(cell + d))
						.Where(p => plan.Contains(p.U, p.V) && distances[owner][p.U + p.V * plan.Width] == distances[owner][current.X + current.Y * plan.Width] - 1 &&
							Math.Abs((plan.PlateauSurfaces.TryGetValue(p, out var ns) ? ns.Height : 0) - currentHeight) <= 1)
						.First();
					current = new PlanPoint(next.U, next.V);
				}
			}
			return true;
		}

		static bool ClearOfResources(MapPlan plan, int x, int y, int gap)
		{
			var center = MountainPlateauPlanner.Cell(x, y);
			for (var dy = -gap; dy <= gap; dy++) for (var dx = -gap; dx <= gap; dx++)
			{
				var p = MountainPlateauPlanner.Point(center + new CVec(dx, dy));
				if (plan.Contains(p.U, p.V) && plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource)) return false;
			}
			return true;
		}

		public static bool PlaceContestedFields(MapPlan plan, bool generators, MersenneTwister random)
		{
			var distances = plan.Spawns.Select(s => StartBalanceMetrics.Distances(plan, s)).ToArray();
			for (var group = 0; group < Math.Max(2, plan.Spawns.Count / 2); group++)
			{
				var eligible = new bool[plan.Width * plan.Height];
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				{
					var i = x + y * plan.Width;
					var approaches = distances.Select(d => d[i]).Where(d => d >= 0).OrderBy(d => d).Take(2).ToArray();
					eligible[i] = approaches.Length == 2 && approaches[0] >= 20 && approaches[1] - approaches[0] <= 8 &&
						FreeResourceCell(plan, x, y) && ClearOfResources(plan, x, y, 2);
				}
				var choices = new List<Candidate>();
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				{
					if (!eligible[x + y * plan.Width]) continue;
					var cells = GatherField(plan, eligible, new PlanPoint(x, y), 49, 7);
					if (cells.Length == 49) choices.Add(new(new PlanPoint(x, y), cells, 0, random.Next()));
				}
				if (choices.Count == 0) return false;
				var selected = choices.OrderBy(c => c.Tie).First();
				foreach (var p in selected.Cells) plan.AddFeature(p.X, p.Y, PlannedFeature.Resource);
				if (generators) plan.AddFeature(selected.Center.X, selected.Center.Y, PlannedFeature.ResourceGenerator);
				// Resource masks prevent later rock placement. Validate access from both contenders after generation.
				foreach (var owner in Enumerable.Range(0, plan.Spawns.Count).Where(o => distances[o][selected.Center.X + selected.Center.Y * plan.Width] >= 0)
					.OrderBy(o => distances[o][selected.Center.X + selected.Center.Y * plan.Width]).Take(2))
					plan.StartingFields.Add(new StartingResourceField(plan.Spawns[owner], selected.Cells,
						selected.Cells.Min(p => distances[owner][p.X + p.Y * plan.Width]), true, contested: true));
			}
			return true;
		}

		static PlanPoint[] GatherField(MapPlan plan, bool[] eligible, PlanPoint center, int budget, int radius)
		{
			var origin = MountainPlateauPlanner.Cell(center.X, center.Y);
			// The coordinate tie-break is total: duplicate queue entries refer to the
			// same cell and would be discarded by seen. SortedSet retains that exact
			// traversal order without requiring .NET 6 PriorityQueue on Mono.
			var pending = new SortedSet<(int Distance, int X, int Y)>();
			var seen = new HashSet<CPos>(); var cells = new List<PlanPoint>();
			pending.Add((0, origin.X, origin.Y));
			while (pending.Count > 0 && cells.Count < budget)
			{
				var first = pending.Min;
				pending.Remove(first);
				var c = new CPos(first.X, first.Y); var p = MountainPlateauPlanner.Point(c);
				if (!seen.Add(c) || !plan.Contains(p.U, p.V) || !eligible[p.U + p.V * plan.Width]) continue;
				cells.Add(new PlanPoint(p.U, p.V));
				foreach (var d in PlateauTopology.Directions)
				{
					var next = c + d; var distance = (next - origin).LengthSquared;
					if (distance <= radius * radius && !seen.Contains(next)) pending.Add((distance, next.X, next.Y));
				}
			}
			return cells.ToArray();
		}

		static bool FreeResourceCell(MapPlan plan, int x, int y) => plan.ContainsPlayable(x, y) && plan.TerrainAt(x, y) == PlannedTerrain.Land &&
			!plan.PlateauSurfaces.ContainsKey(new MPos(x, y)) && !plan.StrategicReservations.Contains(new MPos(x, y)) &&
			!plan.HasFeature(x, y, PlannedFeature.BuildClearance | PlannedFeature.Home | PlannedFeature.Enclosed | PlannedFeature.Entrance |
				PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator);

		static bool Failed(string kind, List<Candidate>[] choices)
		{
			if (Environment.GetEnvironmentVariable("YMCA_BALANCE_DIAGNOSTICS") == "1")
				Console.Error.WriteLine(kind + ": " + string.Join(", ", choices.Select(c => $"{c.Count} candidates, min={c.Select(p => p.EdgeDistance).DefaultIfEmpty(-1).Min()}")));
			return false;
		}

		static bool Select(List<Candidate>[] choices, out Candidate[] selected, int maximum = 40)
		{
			selected = new Candidate[choices.Length];
			if (choices.Any(c => c.Count == 0)) return false;
			var first = choices.Max(c => c.Min(p => p.EdgeDistance));
			for (var target = first; target <= maximum; target++)
			{
				var used = new HashSet<PlanPoint>(); var ok = true;
				foreach (var owner in Enumerable.Range(0, choices.Length).OrderBy(i => choices[i].Count(c => c.EdgeDistance >= target && c.EdgeDistance <= target + 2)))
				{
					var candidate = choices[owner].Where(c => c.EdgeDistance >= target && c.EdgeDistance <= target + 2 && !c.Cells.Any(used.Contains))
						.OrderBy(c => c.Tie).FirstOrDefault();
					if (candidate == null) { ok = false; break; }
					selected[owner] = candidate; used.UnionWith(candidate.Cells);
				}
				if (ok) return true;
			}
			return false;
		}

		public static bool PlaceOil(MapPlan plan, TechBuildingDensity density, bool glitter, MersenneTwister random)
		{
			if (density == TechBuildingDensity.None) return true;
			var distances = plan.Spawns.Select(s => StartBalanceMetrics.Distances(plan, s)).ToArray();
			for (var group = 0; group < (density == TechBuildingDensity.Dense ? 2 : 1); group++)
			{
				var choices = new List<Candidate>[plan.Spawns.Count];
				for (var owner = 0; owner < choices.Length; owner++)
				{
					choices[owner] = new();
					for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
					{
						var i = x + y * plan.Width; var own = distances[owner][i];
						if (own < 14 || own > 42 || distances.Where((_, n) => n != owner).Any(d => d[i] >= 0 && d[i] < own + 3) ||
							!SymmetricOilPlacement.CanPlace(plan, new PlanPoint(x, y), glitter)) continue;
						var cell = MountainPlateauPlanner.Cell(x, y); var footprint = new List<PlanPoint>();
						for (var dy = -1; dy <= 2; dy++) for (var dx = -1; dx <= 2; dx++)
						{
							var p = MountainPlateauPlanner.Point(cell + new CVec(dx, dy)); footprint.Add(new PlanPoint(p.U, p.V));
						}
						choices[owner].Add(new(new PlanPoint(x, y), footprint.ToArray(), own, random.Next()));
					}
				}
				if (!Select(choices, out var selected)) return Failed("oil", choices);
				foreach (var item in selected)
				{
					var cell = MountainPlateauPlanner.Cell(item.Center.X, item.Center.Y);
					for (var dy = 0; dy < 2; dy++) for (var dx = 0; dx < 2; dx++)
					{
						var p = MountainPlateauPlanner.Point(cell + new CVec(dx, dy));
						plan.RemoveFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource);
					}
					plan.AddFeature(item.Center.X, item.Center.Y, PlannedFeature.TechBuilding);
				}
			}
			return true;
		}

		public static void ValidateFields(MapPlan plan)
		{
			foreach (var field in plan.StartingFields)
			{
				var distance = StartBalanceMetrics.Distances(plan, field.Start);
				if (field.Cells.Min(p => distance[p.X + p.Y * plan.Width]) > field.InitialEdgeDistance + 2 ||
					field.Cells.Any(p => distance[p.X + p.Y * plan.Width] < 0 || !plan.HasFeature(p.X, p.Y, PlannedFeature.Resource)))
					throw new InvalidOperationException("A protected starting resource field was removed or cut off.");
			}
		}
	}
}
