using System;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class HillFortressGenerator
	{
		const int MaximumAttempts = 32;

		public MapPlan Generate(MapGenerationOptions options)
		{
			options.Validate();
			for (var attempt = 0; attempt < MaximumAttempts; attempt++)
			{
				// Keep the closed outer contour in non-playable terrain backing.
				// The visible map retains the requested dimensions.
				const int inset = 16;
				var plan = new MapPlan(options.Width + 2 * inset, options.Height + 2 * inset)
				{
					PlayableInset = inset,
					Seed = options.Seed, GenerationAttempt = attempt, SymmetrySlotCount = options.Players,
				};
				var random = new MersenneTwister(unchecked(options.Seed + attempt * 104729));
				if (HillFortressPlateauPlanner.Prepare(plan, options, random) && PlaceResources(plan, options, random) && Validate(plan, options))
					return plan;
			}
			throw new InvalidOperationException($"Failed to generate a valid elevated Hill Fortress map after {MaximumAttempts} attempts.");
		}

		static bool PlaceResources(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			if (options.Resources == ResourceFieldLayout.Glitter)
			{
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
					if (HillFortressPlateauPlanner.FlatHome(plan, x, y) && !plan.HasFeature(x, y, PlannedFeature.BuildClearance) && random.Next(100) < 78)
						plan.AddFeature(x, y, PlannedFeature.Resource);
			}
			else if (options.Resources == ResourceFieldLayout.Patches)
			{
				var patchRadius = random.Next(7, 10);
				foreach (var spawn in plan.Spawns)
				{
					var placed = false;
					for (var attempt = 0; attempt < 200; attempt++)
					{
						var angle = random.NextFloat() * Math.PI * 2;
						var distance = patchRadius + 4 + random.NextFloat() * 7;
						var center = new PlanPoint((int)Math.Round(spawn.X + Math.Cos(angle) * distance), (int)Math.Round(spawn.Y + Math.Sin(angle) * distance));
						if (!CanPlacePatch(plan, center, patchRadius, plan.HomeOwnerAt(spawn.X, spawn.Y))) continue;
						PaintDisk(plan, center, patchRadius, (x, y) => plan.AddFeature(x, y, PlannedFeature.Resource));
						placed = true; break;
					}
					if (!placed) return false;
				}
			}

			if (!options.FiniteResources && !PlaceSymmetric(plan, Math.Max(1, options.Players / 4), random)) return false;
			PlaceCenterTechBuildings(plan, options.TechBuildings, random);
			return true;
		}

		static void PlaceCenterTechBuildings(MapPlan plan, TechBuildingDensity density, MersenneTwister random)
		{
			if (density == TechBuildingDensity.None) return;
			var target = density == TechBuildingDensity.Sparse ? 1 : random.Next(4, 7);
			var center = new PlanPoint((plan.Width - 1) / 2, (plan.Height - 1) / 2);
			for (var placed = 0; placed < target; placed++)
				for (var attempt = 0; attempt < 200; attempt++)
				{
					var x = center.X + (density == TechBuildingDensity.Sparse ? 0 : random.Next(-5, 6));
					var y = center.Y + (density == TechBuildingDensity.Sparse ? 0 : random.Next(-5, 6));
					if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Land || plan.PlateauSurfaces.ContainsKey(new MPos(x, y)) ||
						!plan.HasFeature(x, y, PlannedFeature.ContestedCenter) || plan.HasFeature(x, y, PlannedFeature.TechBuilding | PlannedFeature.Bottleneck)) continue;
					plan.AddFeature(x, y, PlannedFeature.TechBuilding); break;
				}
		}

		static bool CanPlacePatch(MapPlan plan, PlanPoint center, int radius, int owner)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++) for (var x = center.X - radius; x <= center.X + radius; x++)
			{
				if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) > radius * radius) continue;
				if (!HillFortressPlateauPlanner.FlatHome(plan, x, y, owner) ||
					plan.HasFeature(x, y, PlannedFeature.Resource | PlannedFeature.BuildClearance | PlannedFeature.Bottleneck | PlannedFeature.ContestedCenter)) return false;
			}
			return true;
		}

		static bool PlaceSymmetric(MapPlan plan, int pairs, MersenneTwister random)
		{
			bool Fits(int x, int y)
			{
				var cell = MountainPlateauPlanner.Cell(x, y);
				for (var dx = 0; dx <= 1; dx++) for (var dy = 0; dy <= 1; dy++)
				{
					var p = MountainPlateauPlanner.Point(cell + new CVec(dx, dy));
					if (!HillFortressPlateauPlanner.FlatHome(plan, p.U, p.V) ||
						plan.HasFeature(p.U, p.V, PlannedFeature.Bottleneck | PlannedFeature.BuildClearance | PlannedFeature.ResourceGenerator)) return false;
				}
				return true;
			}
			for (var pair = 0; pair < pairs; pair++)
			{
				var placed = false;
				for (var attempt = 0; attempt < 300; attempt++)
				{
					var x = random.Next(4, plan.Width / 2); var y = random.Next(4, plan.Height - 4);
					var mx = plan.Width - 1 - x; var my = plan.Height - 1 - y;
					if (!Fits(x, y) || !Fits(mx, my) || plan.HomeOwnerAt(x, y) == plan.HomeOwnerAt(mx, my)) continue;
					plan.AddFeature(x, y, PlannedFeature.ResourceGenerator); plan.AddFeature(mx, my, PlannedFeature.ResourceGenerator);
					placed = true; break;
				}
				if (!placed) return false;
			}
			return true;
		}

		static bool Validate(MapPlan plan, MapGenerationOptions options)
		{
			if (plan.Spawns.Count != options.Players || plan.Plateaus.Count != (options.Custom?.BattleInTheCenter == false ? 4 : 2) ||
				plan.Spawns.Any(p => !HillFortressPlateauPlanner.FlatHome(plan, p.X, p.Y))) return false;
			var reachable = MountainPlateauPlanner.Reachable(plan, plan.Spawns[0]);
			foreach (var spawn in plan.Spawns) if (!reachable[spawn.X + spawn.Y * plan.Width]) return false;
			foreach (var pair in plan.PlateauSurfaces)
				if (!pair.Value.Blocked && plan.ContainsPlayable(pair.Key.U, pair.Key.V) && !reachable[pair.Key.U + pair.Key.V * plan.Width]) return false;
			var sealedPlan = plan.Clone();
			foreach (var pair in sealedPlan.PlateauSurfaces.Where(p => p.Value.Ramp != 0)) sealedPlan.SetTerrain(pair.Key.U, pair.Key.V, PlannedTerrain.Mountain);
			var sealedReachable = MountainPlateauPlanner.Reachable(sealedPlan, plan.Spawns[0]);
			var otherTeam = plan.Spawns[(options.Players + 1) / 2];
			if (sealedReachable[otherTeam.X + otherTeam.Y * plan.Width]) return false;
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
			{
				if (plan.HasFeature(x, y, PlannedFeature.BuildClearance | PlannedFeature.Resource | PlannedFeature.ResourceGenerator) && !HillFortressPlateauPlanner.FlatHome(plan, x, y)) return false;
				if (options.FiniteResources && plan.HasFeature(x, y, PlannedFeature.ResourceGenerator)) return false;
				if (plan.HasFeature(x, y, PlannedFeature.ContestedCenter) && (plan.PlateauSurfaces.ContainsKey(new MPos(x, y)) || !reachable[x + y * plan.Width])) return false;
			}
			plan.ValidationMessages.Add("Two complete walkable height-four team plateaus; each has a five-wide recessed ramp into the height-zero battle valley.");
			plan.ValidationMessages.Add("All starting clearances and resources are on the plateaus; the contested valley has no resource fields.");
			return true;
		}

		static void PaintDisk(MapPlan plan, PlanPoint center, int radius, Action<int, int> paint)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++) for (var x = center.X - radius; x <= center.X + radius; x++)
				if (plan.Contains(x, y) && (x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) <= radius * radius) paint(x, y);
		}
	}
}
