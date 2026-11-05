using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Non-central river layouts: reserve whole land patches and real bridge banks
	// before placing starts/economy. Preset generators never enter this path.
	sealed class CustomRiverGenerator
	{
		public MapPlan Generate(MapGenerationOptions options)
		{
			for (var attempt = 0; attempt < 48; attempt++)
			{
				var plan = new MapPlan(options.Width, options.Height) { Seed = options.Seed, GenerationAttempt = attempt, SymmetrySlotCount = options.Players };
				var random = new MersenneTwister(unchecked(options.Seed + attempt * 104729));
				var connected = options.Custom.ConnectedStartAreas;
				var columns = connected ? 2 : (int)Math.Ceiling(Math.Sqrt(options.Players + 2));
				var rows = connected ? 1 : (int)Math.Ceiling((options.Players + 2.0) / columns);
				var halfWidth = 2 + attempt % 2;
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				{
					var water = Enumerable.Range(1, columns - 1).Any(i => Math.Abs(x - i * plan.Width / columns) <= halfWidth) ||
						Enumerable.Range(1, rows - 1).Any(i => Math.Abs(y - i * plan.Height / rows) <= halfWidth);
					if (water) plan.SetTerrain(x, y, PlannedTerrain.Water);
				}
				var components = CustomIslandConnections.Components(plan);
				if (components.Count != columns * rows) continue;
				if (!connected)
				{
					var available = components.ToList(); var homes = new List<HashSet<CPos>>();
					var middle = PlannedRubberduckBridge.Cell(plan.Width / 2, plan.Height / 2);
					while (homes.Count < options.Players)
					{
						var next = available.OrderByDescending(c => homes.Count == 0 ?
							Math.Pow(c.Average(p => p.X) - middle.X, 2) + Math.Pow(c.Average(p => p.Y) - middle.Y, 2) :
							homes.Min(h => Math.Pow(c.Average(p => p.X) - h.Average(p => p.X), 2) + Math.Pow(c.Average(p => p.Y) - h.Average(p => p.Y), 2))).First();
						homes.Add(next); available.Remove(next);
					}
					components = homes.OrderBy(c => Math.Atan2(c.Average(p => p.Y) - middle.Y, c.Average(p => p.X) - middle.X)).ToList();
				}
				var failed = false;
				for (var player = 0; player < options.Players; player++)
				{
					var owner = connected ? Math.Min(1, player * 2 / options.Players) : player;
					var component = components[owner];
					var cx = component.Average(c => c.X); var cy = component.Average(c => c.Y);
					var candidates = component.Where(c => Enumerable.Range(-5, 11).All(dx => Enumerable.Range(-5, 11)
						.All(dy => component.Contains(c + new CVec(dx, dy)))))
						.Where(c => { var p = PlannedRubberduckBridge.Point(c); return Enumerable.Range(-9, 19).All(x => Enumerable.Range(-9, 19)
							.All(y => x * x + y * y > 81 || component.Contains(PlannedRubberduckBridge.Cell(p.U + x, p.V + y)))); })
						.Where(c => plan.Spawns.All(s => { var p = PlannedRubberduckBridge.Cell(s.X, s.Y); return Math.Abs(c.X - p.X) + Math.Abs(c.Y - p.Y) >= 14; }))
						.OrderBy(c => (c.X - cx) * (c.X - cx) + (c.Y - cy) * (c.Y - cy)).ThenBy(c => c.X).ThenBy(c => c.Y).ToArray();
					if (candidates.Length == 0) { failed = true; break; }
					var start = candidates[(int)(random.Next() % Math.Min(4, candidates.Length))];
					var point = PlannedRubberduckBridge.Point(start);
					plan.Spawns.Add(new PlanPoint(point.U, point.V)); plan.AddFeature(point.U, point.V, PlannedFeature.Spawn);
					foreach (var c in component) { var p = PlannedRubberduckBridge.Point(c); plan.SetHomeOwner(p.U, p.V, owner); }
					for (var dx = -9; dx <= 9; dx++) for (var dy = -9; dy <= 9; dy++)
						if (dx * dx + dy * dy <= 81) plan.AddFeature(point.U + dx, point.V + dy, PlannedFeature.BuildClearance);
				}
				if (failed || !CustomIslandConnections.Connect(plan, connected ? 3 : 0) || CustomIslandConnections.Components(plan, true).Count != 1) continue;
				if (options.Mode == GeneratedMapMode.Strategic)
				{
					plan.PreserveObjectiveTerrain = true;
					if (!connected) StrategicCheckpointPlanner.OrderSpawns(plan.Spawns, options.Players);
					if (!StrategicCheckpointPlanner.ReserveFront(plan, options)) continue;
				}
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				{
					if (plan.StrategicReservations.Contains(new MPos(x, y)) || plan.TerrainAt(x, y) != PlannedTerrain.Land || plan.HasFeature(x, y, PlannedFeature.BuildClearance | PlannedFeature.Bottleneck | PlannedFeature.LaunchShore)) continue;
					var nearHome = plan.Spawns.Any(p => Math.Abs(p.X - x) + Math.Abs(p.Y - y) < 19);
					if (options.Resources != ResourceFieldLayout.None && (options.Resources == ResourceFieldLayout.Glitter || nearHome) && random.Next(100) < 65)
						plan.AddFeature(x, y, PlannedFeature.Resource);
				}
				foreach (var spawn in plan.Spawns)
				{
					var sites = Enumerable.Range(0, plan.Width * plan.Height).Select(i => new PlanPoint(i % plan.Width, i / plan.Width))
						.Where(p => !plan.StrategicReservations.Contains(new MPos(p.X, p.Y)) && plan.TerrainAt(p.X, p.Y) == PlannedTerrain.Land && !plan.HasFeature(p.X, p.Y, PlannedFeature.BuildClearance | PlannedFeature.Bottleneck | PlannedFeature.LaunchShore | PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding))
						.OrderBy(p => Math.Abs(p.X - spawn.X) + Math.Abs(p.Y - spawn.Y)).Take(2).ToArray();
					if (sites.Length != 2) { failed = true; break; }
					if (!options.FiniteResources && options.Resources != ResourceFieldLayout.None) plan.AddFeature(sites[0].X, sites[0].Y, PlannedFeature.ResourceGenerator);
					if (options.TechBuildings != TechBuildingDensity.None) plan.AddFeature(sites[1].X, sites[1].Y, PlannedFeature.TechBuilding);
				}
				if (failed) continue;
				return plan;
			}
			throw new InvalidOperationException("No river-patch layout fits the starts, protected banks and bridge network at this size.");
		}
	}
}
