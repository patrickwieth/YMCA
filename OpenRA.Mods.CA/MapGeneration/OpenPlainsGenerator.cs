using System;
using System.Collections.Generic;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class OpenPlainsGenerator
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
				};
				var random = new MersenneTwister(unchecked(options.Seed + attempt * 104729));
				if (TryGenerate(plan, options, random) && Validate(plan, options))
					return plan;
			}

			throw new InvalidOperationException($"Failed to generate a valid {options.Preset} map after {MaximumAttempts} attempts.");
		}

		static bool TryGenerate(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			var slots = PlayerSpawnLayout.Create(plan, options.Players, 10, random);
			plan.SymmetrySlotCount = slots.Count;
			foreach (var slot in slots)
				PaintDisk(plan, slot, 10, (x, y) => plan.AddFeature(x, y, PlannedFeature.BuildClearance));

			for (var player = 0; player < options.Players; player++)
			{
				var spawn = slots[player];
				plan.Spawns.Add(spawn);
				PaintDisk(plan, spawn, 10, (x, y) => plan.SetHomeOwner(x, y, player));
				plan.AddFeature(spawn.X, spawn.Y, PlannedFeature.Spawn);
			}

			if (options.MountainLines)
			{
				if (!PlaceResources(plan, options, slots, random))
					return false;
				PlaceMountainLines(plan, options.Players, random);
			}
			else
			{
				if (options.WaterPatches && !PlaceWaterBasins(plan, options.Players, random))
					return false;
				// Reserve coast geometry before scattering Glitter, instead of later
				// deleting resources or rejecting every shoreline as occupied.
				RubberduckCoastTopology.Mark(plan, options);
				if (!PlaceResources(plan, options, slots, random))
					return false;
			}
			PlaceTechBuildings(plan, options.TechBuildings, options.Players, random);
			return true;
		}

		static void PlaceMountainLines(MapPlan plan, int playerCount, MersenneTwister random)
		{
			var symmetryOrder = PlayerSpawnLayout.SymmetryOrder(playerCount);
			var centerX = (plan.Width - 1) / 2.0;
			var centerY = (plan.Height - 1) / 2.0;
			for (var group = 0; group < 2; group++)
				for (var attempt = 0; attempt < 500; attempt++)
				{
					var orbitRadius = Math.Min(plan.Width, plan.Height) * (0.14 + random.NextFloat() * 0.34);
					var orbitAngle = random.NextFloat() * Math.PI * 2;
					var lineAngle = random.NextFloat() * Math.PI * 2;
					var lineLength = random.Next(10, 19);
					var bend = (random.NextFloat() - 0.5) * 7;
					var lines = new List<List<PlanPoint>>();
					var totalPoints = 0;
					var insidePoints = 0;
					var blockedPoints = 0;
					for (var rotation = 0; rotation < symmetryOrder; rotation++)
					{
						var rotationAngle = rotation * Math.PI * 2 / symmetryOrder;
						var cx = centerX + Math.Cos(orbitAngle + rotationAngle) * orbitRadius;
						var cy = centerY + Math.Sin(orbitAngle + rotationAngle) * orbitRadius;
						var direction = lineAngle + rotationAngle;
						var line = new List<PlanPoint>();
						for (var step = -lineLength; step <= lineLength; step++)
						{
							var t = step / (double)lineLength;
							var offset = bend * (1 - t * t);
							var point = new PlanPoint(
								(int)Math.Round(cx + Math.Cos(direction) * step - Math.Sin(direction) * offset),
								(int)Math.Round(cy + Math.Sin(direction) * step + Math.Cos(direction) * offset));
							line.Add(point);
							totalPoints++;
							if (!plan.Contains(point.X, point.Y))
								continue;
							insidePoints++;
							if (DiskIntersectsFeature(plan, point, 2, PlannedFeature.BuildClearance | PlannedFeature.Resource))
								blockedPoints++;
						}
						lines.Add(line);
					}

					if (insidePoints < totalPoints * 3 / 4 || blockedPoints != 0)
						continue;
					foreach (var line in lines)
						foreach (var point in line)
							PaintObstacleDisk(plan, point, 2, PlannedTerrain.Mountain);
					break;
				}
		}

		static bool DiskIntersectsFeature(MapPlan plan, PlanPoint center, int radius, PlannedFeature feature)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++)
				for (var x = center.X - radius; x <= center.X + radius; x++)
					if (plan.Contains(x, y) &&
						(x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) <= radius * radius &&
						plan.HasFeature(x, y, feature))
						return true;
			return false;
		}

		static bool PlaceWaterBasins(MapPlan plan, int playerCount, MersenneTwister random)
		{
			var symmetryOrder = PlayerSpawnLayout.SymmetryOrder(playerCount);
			var minimumGroups = (playerCount + symmetryOrder - 1) / symmetryOrder;
			var targetWaterCells = (int)Math.Ceiling(plan.Width * plan.Height * 0.1);
			var centerX = (plan.Width - 1) / 2.0;
			var centerY = (plan.Height - 1) / 2.0;
			var group = 0;
			while (group < minimumGroups || CountTerrainCells(plan, PlannedTerrain.Water) < targetWaterCells)
			{
				var placed = false;
				for (var attempt = 0; attempt < 1600 && !placed; attempt++)
				{
					var radius = random.Next(5, 9);
					var orbitRadius = Math.Min(plan.Width, plan.Height) * (0.08 + random.NextFloat() * 0.35);
					var phase = random.NextFloat() * Math.PI * 2;
					var centers = new List<PlanPoint>();
					for (var rotation = 0; rotation < symmetryOrder; rotation++)
					{
						var angle = phase + rotation * Math.PI * 2 / symmetryOrder;
						centers.Add(new PlanPoint(
							(int)Math.Round(centerX + Math.Cos(angle) * orbitRadius),
							(int)Math.Round(centerY + Math.Sin(angle) * orbitRadius)));
					}

					if (!CanPlaceWaterOrbit(plan, centers, radius))
						continue;
					foreach (var center in centers)
						PaintObstacleDisk(plan, center, radius, PlannedTerrain.Water);
					placed = true;
				}

				if (!placed)
					return false;
				group++;
			}
			return true;
		}

		static int CountTerrainCells(MapPlan plan, PlannedTerrain terrain)
		{
			var count = 0;
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (plan.TerrainAt(x, y) == terrain)
						count++;
			return count;
		}

		static bool CanPlaceWaterOrbit(MapPlan plan, List<PlanPoint> centers, int radius)
		{
			for (var i = 0; i < centers.Count; i++)
			{
				if (!CanPlaceWaterBasin(plan, centers[i], radius))
					return false;
				for (var j = 0; j < i; j++)
				{
					var dx = centers[i].X - centers[j].X;
					var dy = centers[i].Y - centers[j].Y;
					var minimumDistance = radius * 2 + 3;
					if (dx * dx + dy * dy < minimumDistance * minimumDistance)
						return false;
				}
			}
			return true;
		}

		static bool CanPlaceWaterBasin(MapPlan plan, PlanPoint center, int radius)
		{
			var clearance = radius + 1;
			for (var y = center.Y - clearance; y <= center.Y + clearance; y++)
				for (var x = center.X - clearance; x <= center.X + clearance; x++)
				{
					if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) > clearance * clearance)
						continue;
					if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Land ||
						plan.HasFeature(x, y, PlannedFeature.BuildClearance))
						return false;
				}
			return true;
		}

		static void PaintObstacleDisk(MapPlan plan, PlanPoint center, int radius, PlannedTerrain terrain)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++)
				for (var x = center.X - radius; x <= center.X + radius; x++)
				{
					if (!plan.Contains(x, y) ||
						(x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) > radius * radius)
						continue;
					if (plan.HasFeature(x, y, PlannedFeature.BuildClearance | PlannedFeature.Resource |
						PlannedFeature.RichResource | PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding))
						continue;
					plan.SetTerrain(x, y, terrain);
				}
		}

		static bool PlaceResources(
			MapPlan plan, MapGenerationOptions options, List<PlanPoint> symmetrySlots, MersenneTwister random)
		{
			if (options.Resources == ResourceFieldLayout.Glitter)
			{
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (plan.TerrainAt(x, y) == PlannedTerrain.Land &&
							!plan.HasFeature(x, y, PlannedFeature.BuildClearance | PlannedFeature.CliffShore) && random.Next(100) < 78)
							plan.AddFeature(x, y, PlannedFeature.Resource);
			}
			else if (options.Resources == ResourceFieldLayout.Patches)
			{
				if (!PlaceRotatedPatchGroup(plan, symmetrySlots, random.Next(5, 8), 18, 24,
					Math.PI, !options.FiniteResources, random) ||
					!PlaceRotatedPatchGroup(plan, symmetrySlots, random.Next(4, 7), 28, 37,
						Math.PI, !options.FiniteResources, random))
					return false;
			}
			return true;
		}

		static bool PlaceRotatedPatchGroup(
			MapPlan plan, List<PlanPoint> slots, int radius, int minimumDistance, int maximumDistance,
			double angleVariation, bool addGenerators, MersenneTwister random)
		{
			var mapCenterX = (plan.Width - 1) / 2.0;
			var mapCenterY = (plan.Height - 1) / 2.0;
			for (var attempt = 0; attempt < 1000; attempt++)
			{
				var distance = minimumDistance + random.NextFloat() * (maximumDistance - minimumDistance);
				var angleOffset = Math.PI + (random.NextFloat() - 0.5) * angleVariation * 2;
				var centers = new List<PlanPoint>();
				var valid = true;
				foreach (var slot in slots)
				{
					var radialAngle = Math.Atan2(slot.Y - mapCenterY, slot.X - mapCenterX);
					var angle = radialAngle + angleOffset;
					var center = new PlanPoint(
						(int)Math.Round(slot.X + Math.Cos(angle) * distance),
						(int)Math.Round(slot.Y + Math.Sin(angle) * distance));
					if (!CanPlacePatch(plan, center, radius) || !SeparatedFrom(centers, center, radius * 2 + 3))
					{
						valid = false;
						break;
					}
					centers.Add(center);
				}

				if (!valid)
					continue;
				foreach (var center in centers)
				{
					PaintDisk(plan, center, radius, (x, y) => plan.AddFeature(x, y, PlannedFeature.Resource));
					if (addGenerators)
						plan.AddFeature(center.X, center.Y, PlannedFeature.ResourceGenerator);
				}
				return true;
			}

			return false;
		}

		static bool SeparatedFrom(List<PlanPoint> points, PlanPoint candidate, int minimumDistance)
		{
			foreach (var point in points)
			{
				var dx = point.X - candidate.X;
				var dy = point.Y - candidate.Y;
				if (dx * dx + dy * dy < minimumDistance * minimumDistance)
					return false;
			}
			return true;
		}

		static bool CanPlacePatch(MapPlan plan, PlanPoint center, int radius)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++)
				for (var x = center.X - radius; x <= center.X + radius; x++)
				{
					if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) > radius * radius)
						continue;
					if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Land ||
						plan.HasFeature(x, y, PlannedFeature.Resource | PlannedFeature.BuildClearance))
						return false;
				}
			return true;
		}

		static void PlaceTechBuildings(MapPlan plan, TechBuildingDensity density, int players, MersenneTwister random)
		{
			if (density == TechBuildingDensity.None)
				return;
			var count = density == TechBuildingDensity.Sparse ? Math.Max(1, players / 4) : Math.Max(4, players);
			PlaceSymmetricPoints(plan, PlannedFeature.TechBuilding, count, random);
		}

		static void PlaceSymmetricPoints(MapPlan plan, PlannedFeature feature, int count, MersenneTwister random)
		{
			for (var i = 0; i < count; i++)
				for (var attempt = 0; attempt < 300; attempt++)
				{
					var x = random.Next(8, plan.Width - 8);
					var y = random.Next(8, plan.Height - 8);
					if (plan.TerrainAt(x, y) != PlannedTerrain.Land ||
						plan.HasFeature(x, y, PlannedFeature.BuildClearance | PlannedFeature.TechBuilding |
							PlannedFeature.ResourceGenerator))
						continue;
					plan.AddFeature(x, y, feature);
					break;
				}
		}

		static bool Validate(MapPlan plan, MapGenerationOptions options)
		{
			if (plan.Spawns.Count != options.Players)
				return false;
			var reachable = MountainValleysGenerator.FindReachable(plan, plan.Spawns[0]);
			foreach (var spawn in plan.Spawns)
				if (!reachable[spawn.X + spawn.Y * plan.Width])
					return false;

			var traversable = 0;
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (plan.IsTraversable(x, y))
						traversable++;
					if (options.FiniteResources && plan.HasFeature(x, y, PlannedFeature.ResourceGenerator))
						return false;
				}

			if (traversable < plan.Width * plan.Height * 2 / 3)
				return false;
			if (options.WaterPatches &&
				(CountTerrainComponents(plan, PlannedTerrain.Water) < options.Players ||
				CountTerrainCells(plan, PlannedTerrain.Water) < (int)Math.Ceiling(plan.Width * plan.Height * 0.1)))
				return false;
			plan.ValidationMessages.Add("All player starts share one broad open land region.");
			plan.ValidationMessages.Add("Local obstacles leave at least two thirds of the map traversable.");
			if (options.MountainLines)
				plan.ValidationMessages.Add("Mountain lines are placed in rotationally symmetric groups.");
			if (options.WaterPatches)
				plan.ValidationMessages.Add($"At least {options.Players} separate water basins cover at least 10% of the map.");
			return true;
		}

		static int CountTerrainComponents(MapPlan plan, PlannedTerrain terrain)
		{
			var visited = new bool[plan.Width * plan.Height];
			var components = 0;
			var queue = new Queue<PlanPoint>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var index = x + y * plan.Width;
					if (visited[index] || plan.TerrainAt(x, y) != terrain)
						continue;
					components++;
					visited[index] = true;
					queue.Enqueue(new PlanPoint(x, y));
					while (queue.Count > 0)
					{
						var cell = queue.Dequeue();
						foreach (var offset in CardinalOffsets)
						{
							var nx = cell.X + offset.X;
							var ny = cell.Y + offset.Y;
							if (!plan.Contains(nx, ny))
								continue;
							var neighborIndex = nx + ny * plan.Width;
							if (visited[neighborIndex] || plan.TerrainAt(nx, ny) != terrain)
								continue;
							visited[neighborIndex] = true;
							queue.Enqueue(new PlanPoint(nx, ny));
						}
					}
				}
			return components;
		}

		static readonly PlanPoint[] CardinalOffsets =
		{
			new PlanPoint(1, 0),
			new PlanPoint(-1, 0),
			new PlanPoint(0, 1),
			new PlanPoint(0, -1),
		};

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
