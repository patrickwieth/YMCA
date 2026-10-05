using System;
using System.Collections.Generic;
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
				var plan = new MapPlan(options.Width, options.Height)
				{
					Seed = options.Seed,
					GenerationAttempt = attempt,
					SymmetrySlotCount = options.Players,
				};
				var random = new MersenneTwister(unchecked(options.Seed + attempt * 104729));
				if (TryGenerate(plan, options, random) && Validate(plan, options))
					return plan;
			}

			throw new InvalidOperationException($"Failed to generate a valid Hill Fortress map after {MaximumAttempts} attempts.");
		}

		static bool TryGenerate(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			var minimumDimension = Math.Min(plan.Width, plan.Height);
			var angle = random.NextFloat() * Math.PI;
			var cos = Math.Cos(angle);
			var sin = Math.Sin(angle);
			var centerX = (plan.Width - 1) / 2.0;
			var centerY = (plan.Height - 1) / 2.0;
			var homeCenterV = minimumDimension * 0.32;
			var homeRadiusU = minimumDimension * 0.34;
			var separatorV = minimumDimension * (0.16 + random.NextFloat() * 0.025);
			var centerRadiusU = minimumDimension * (0.17 + random.NextFloat() * 0.04);
			var centerRadiusV = separatorV * 0.72;
			var gateOffset = (random.NextFloat() - 0.5) * centerRadiusU * 0.8;
			var gateHalfWidth = Math.Clamp(minimumDimension / 30, 4, 7);
			var ridgeHalfWidth = Math.Clamp(minimumDimension / 55, 2, 4);
			var ridgeWaveLength = minimumDimension * (0.24 + random.NextFloat() * 0.12);
			var ridgeWaveAmplitude = minimumDimension * (0.025 + random.NextFloat() * 0.025);

			PlanPoint ToMap(double u, double v)
			{
				return new PlanPoint(
					(int)Math.Round(centerX + u * cos - v * sin),
					(int)Math.Round(centerY + u * sin + v * cos));
			}

			(double U, double V) ToLocal(int x, int y)
			{
				var dx = x - centerX;
				var dy = y - centerY;
				return (dx * cos + dy * sin, -dx * sin + dy * cos);
			}

			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					plan.SetTerrain(x, y, PlannedTerrain.Land);
					var (u, v) = ToLocal(x, y);
					var ridgeOffset = ridgeWaveAmplitude * Math.Sin(u / ridgeWaveLength * Math.PI * 2);
					var upperRidgeV = -separatorV + ridgeOffset;
					var lowerRidgeV = separatorV - ridgeOffset;
					var upperGate = Math.Abs(v - upperRidgeV) <= ridgeHalfWidth + 1 &&
						Math.Abs(u - gateOffset) <= gateHalfWidth;
					var lowerGate = Math.Abs(v - lowerRidgeV) <= ridgeHalfWidth + 1 &&
						Math.Abs(u + gateOffset) <= gateHalfWidth;
					var upperMountain = Math.Abs(v - upperRidgeV) <= ridgeHalfWidth && !upperGate;
					var lowerMountain = Math.Abs(v - lowerRidgeV) <= ridgeHalfWidth && !lowerGate;

					if (upperMountain || lowerMountain)
					{
						plan.SetTerrain(x, y, PlannedTerrain.Mountain);
						continue;
					}

					if (upperGate || lowerGate)
						plan.AddFeature(x, y, PlannedFeature.Bottleneck);
					if (EllipseContains(u, v, centerRadiusU, centerRadiusV))
						plan.AddFeature(x, y, PlannedFeature.ContestedCenter);
					if (v < upperRidgeV - ridgeHalfWidth || v > lowerRidgeV + ridgeHalfWidth)
						plan.AddFeature(x, y, PlannedFeature.Home);
				}

			var firstTeamPlayers = (options.Players + 1) / 2;
			PlaceTeam(plan, 0, firstTeamPlayers, 0, -homeCenterV, homeRadiusU, ToMap);
			PlaceTeam(plan, firstTeamPlayers, options.Players - firstTeamPlayers, 1, homeCenterV, homeRadiusU, ToMap);
			PlaceResources(plan, options, random);
			return true;
		}

		static void PlaceTeam(
			MapPlan plan,
			int firstPlayer,
			int count,
			int team,
			double homeV,
			double homeRadiusU,
			Func<double, double, PlanPoint> toMap)
		{
			for (var index = 0; index < count; index++)
			{
				var fraction = (index + 1.0) / (count + 1.0);
				var u = (fraction - 0.5) * homeRadiusU * 1.25;
				var spawn = toMap(u, homeV);
				if (!plan.IsTraversable(spawn.X, spawn.Y))
					continue;

				plan.Spawns.Add(spawn);
				PaintDisk(plan, spawn, 9, (x, y) =>
				{
					if (plan.TerrainAt(x, y) == PlannedTerrain.Land)
						plan.SetHomeOwner(x, y, team);
				});
				PaintDisk(plan, spawn, 9, (x, y) => plan.AddFeature(x, y, PlannedFeature.BuildClearance));
				plan.AddFeature(spawn.X, spawn.Y, PlannedFeature.Spawn);
			}
		}

		static void PlaceResources(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			if (options.Resources == ResourceFieldLayout.Glitter)
			{
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (plan.TerrainAt(x, y) == PlannedTerrain.Land && plan.HasFeature(x, y, PlannedFeature.Home) &&
							!plan.HasFeature(x, y, PlannedFeature.BuildClearance) && random.Next(100) < 78)
							plan.AddFeature(x, y, PlannedFeature.Resource);
			}
			else if (options.Resources == ResourceFieldLayout.Patches)
			{
				var patchRadius = random.Next(7, 10);
				foreach (var spawn in plan.Spawns)
					for (var attempt = 0; attempt < 200; attempt++)
					{
						var angle = random.NextFloat() * Math.PI * 2;
						var distance = patchRadius + 4 + random.NextFloat() * 7;
						var center = new PlanPoint(
							(int)Math.Round(spawn.X + Math.Cos(angle) * distance),
							(int)Math.Round(spawn.Y + Math.Sin(angle) * distance));
						if (!CanPlacePatch(plan, center, patchRadius))
							continue;
						PaintDisk(plan, center, patchRadius, (x, y) => plan.AddFeature(x, y, PlannedFeature.Resource));
						break;
					}
			}

			if (!options.FiniteResources)
				PlaceSymmetric(plan, PlannedFeature.ResourceGenerator, Math.Max(1, options.Players / 4), random);

			PlaceCenterTechBuildings(plan, options.TechBuildings, random);
		}

		static void PlaceCenterTechBuildings(MapPlan plan, TechBuildingDensity density, MersenneTwister random)
		{
			if (density == TechBuildingDensity.None)
				return;

			var target = density == TechBuildingDensity.Sparse ? 1 : random.Next(4, 7);
			var center = new PlanPoint((plan.Width - 1) / 2, (plan.Height - 1) / 2);
			for (var placed = 0; placed < target; placed++)
			{
				for (var attempt = 0; attempt < 200; attempt++)
				{
					var x = center.X + (density == TechBuildingDensity.Sparse ? 0 : random.Next(-5, 6));
					var y = center.Y + (density == TechBuildingDensity.Sparse ? 0 : random.Next(-5, 6));
					if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Land ||
						!plan.HasFeature(x, y, PlannedFeature.ContestedCenter) ||
						plan.HasFeature(x, y, PlannedFeature.TechBuilding | PlannedFeature.Bottleneck))
						continue;

					plan.AddFeature(x, y, PlannedFeature.TechBuilding);
					break;
				}
			}
		}

		static bool CanPlacePatch(MapPlan plan, PlanPoint center, int radius)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++)
				for (var x = center.X - radius; x <= center.X + radius; x++)
				{
					if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) > radius * radius)
						continue;
					if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Land ||
						plan.HasFeature(x, y, PlannedFeature.Resource | PlannedFeature.Bottleneck | PlannedFeature.ContestedCenter))
						return false;
				}
			return true;
		}

		static void PlaceSymmetric(MapPlan plan, PlannedFeature feature, int pairs, MersenneTwister random)
		{
			for (var pair = 0; pair < pairs; pair++)
				for (var attempt = 0; attempt < 300; attempt++)
				{
					var x = random.Next(4, plan.Width / 2);
					var y = random.Next(4, plan.Height - 4);
					var mirrorX = plan.Width - 1 - x;
					var mirrorY = plan.Height - 1 - y;
					if (plan.TerrainAt(x, y) != PlannedTerrain.Land || plan.TerrainAt(mirrorX, mirrorY) != PlannedTerrain.Land ||
						plan.HasFeature(x, y, PlannedFeature.Bottleneck | PlannedFeature.ContestedCenter | PlannedFeature.BuildClearance) ||
						plan.HasFeature(mirrorX, mirrorY, PlannedFeature.Bottleneck | PlannedFeature.ContestedCenter | PlannedFeature.BuildClearance))
						continue;
					plan.AddFeature(x, y, feature);
					plan.AddFeature(mirrorX, mirrorY, feature);
					break;
				}
		}

		static bool Validate(MapPlan plan, MapGenerationOptions options)
		{
			if (plan.Spawns.Count != options.Players)
				return false;

			var reachable = FindReachable(plan, plan.Spawns[0], false);
			foreach (var spawn in plan.Spawns)
				if (!reachable[spawn.X + spawn.Y * plan.Width])
					return false;

			var firstTeamPlayers = (options.Players + 1) / 2;
			var homeOnly = FindReachable(plan, plan.Spawns[0], true);
			if (homeOnly[plan.Spawns[firstTeamPlayers].X + plan.Spawns[firstTeamPlayers].Y * plan.Width])
				return false;

			if (options.FiniteResources)
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (plan.HasFeature(x, y, PlannedFeature.ResourceGenerator))
							return false;

			plan.ValidationMessages.Add("Each team Stronghold has exactly one route into the compact battle center.");
			plan.ValidationMessages.Add("No resources were placed in the contested center.");
			return true;
		}

		static bool[] FindReachable(MapPlan plan, PlanPoint start, bool blockBottleneck)
		{
			var reachable = new bool[plan.Width * plan.Height];
			var queue = new Queue<PlanPoint>();
			queue.Enqueue(start);
			reachable[start.X + start.Y * plan.Width] = true;
			var offsets = new[] { new PlanPoint(1, 0), new PlanPoint(-1, 0), new PlanPoint(0, 1), new PlanPoint(0, -1) };
			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				foreach (var offset in offsets)
				{
					var x = current.X + offset.X;
					var y = current.Y + offset.Y;
					if (!plan.IsTraversable(x, y) || blockBottleneck && plan.HasFeature(x, y, PlannedFeature.Bottleneck))
						continue;
					var index = x + y * plan.Width;
					if (reachable[index])
						continue;
					reachable[index] = true;
					queue.Enqueue(new PlanPoint(x, y));
				}
			}
			return reachable;
		}

		static bool EllipseContains(double x, double y, double radiusX, double radiusY)
		{
			return x * x / (radiusX * radiusX) + y * y / (radiusY * radiusY) <= 1;
		}

		static double DistanceToSegmentSquared(double x, double y, double ax, double ay, double bx, double by)
		{
			var dx = bx - ax;
			var dy = by - ay;
			var lengthSquared = dx * dx + dy * dy;
			var t = lengthSquared == 0 ? 0 : Math.Clamp(((x - ax) * dx + (y - ay) * dy) / lengthSquared, 0, 1);
			var offsetX = x - (ax + t * dx);
			var offsetY = y - (ay + t * dy);
			return offsetX * offsetX + offsetY * offsetY;
		}

		static void PaintDisk(MapPlan plan, PlanPoint center, int radius, Action<int, int> paint)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++)
				for (var x = center.X - radius; x <= center.X + radius; x++)
					if (plan.Contains(x, y) &&
						(x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) <= radius * radius)
						paint(x, y);
		}
	}
}
