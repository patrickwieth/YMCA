using System;
using System.Collections.Generic;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class BattleNexusGenerator
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

			throw new InvalidOperationException($"Failed to generate a valid Battle Nexus map after {MaximumAttempts} attempts.");
		}

		static bool TryGenerate(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			var minimumDimension = Math.Min(plan.Width, plan.Height);
			var centerX = (plan.Width - 1) / 2.0;
			var centerY = (plan.Height - 1) / 2.0;
			var phase = random.NextFloat() * Math.PI * 2;
			var centerRadius = minimumDimension * (0.16 + random.NextFloat() * 0.035);
			var ringHalfWidth = Math.Clamp(minimumDimension / 52, 2, 4);
			var wallHalfWidth = Math.Clamp(minimumDimension / 60, 2, 4);
			var gateHalfAngle = Math.Min(0.18, Math.PI / options.Players * 0.36);
			var contourPhase = random.NextFloat() * Math.PI * 2;

			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var dx = x - centerX;
					var dy = y - centerY;
					var radius = Math.Sqrt(dx * dx + dy * dy);
					var angle = Math.Atan2(dy, dx);
					var radiusAtAngle = centerRadius *
						(1 + 0.045 * Math.Sin((angle - phase) * options.Players + contourPhase));
					var nearestPlayerAngle = phase +
						Math.Round((angle - phase) * options.Players / (Math.PI * 2)) * Math.PI * 2 / options.Players;
					var inGate = AngularDistance(angle, nearestPlayerAngle) <= gateHalfAngle;
					var inRing = Math.Abs(radius - radiusAtAngle) <= ringHalfWidth;

					var nearestWallAngle = phase + Math.PI / options.Players +
						Math.Round((angle - phase - Math.PI / options.Players) * options.Players / (Math.PI * 2)) *
						Math.PI * 2 / options.Players;
					var wallDistance = radius * Math.Sin(AngularDistance(angle, nearestWallAngle));
					var inWall = radius >= radiusAtAngle - ringHalfWidth && Math.Abs(wallDistance) <= wallHalfWidth;

					if (inWall || inRing && !inGate)
					{
						plan.SetTerrain(x, y, PlannedTerrain.Mountain);
						continue;
					}

					if (inRing && inGate)
						plan.AddFeature(x, y, PlannedFeature.Bottleneck);
					if (radius < radiusAtAngle - ringHalfWidth)
						plan.AddFeature(x, y, PlannedFeature.ContestedCenter);
					else if (radius > radiusAtAngle + ringHalfWidth)
						plan.AddFeature(x, y, PlannedFeature.Home);
				}

			for (var player = 0; player < options.Players; player++)
			{
				var angle = phase + player * Math.PI * 2 / options.Players;
				var spawn = ProjectInsideMap(plan, angle, 12, 0.78);
				if (!plan.IsTraversable(spawn.X, spawn.Y))
					return false;

				plan.Spawns.Add(spawn);
				PaintDisk(plan, spawn, 9, (x, y) =>
				{
					if (plan.TerrainAt(x, y) == PlannedTerrain.Land)
						plan.SetHomeOwner(x, y, player);
				});
				PaintDisk(plan, spawn, 9, (x, y) => plan.AddFeature(x, y, PlannedFeature.BuildClearance));
				plan.AddFeature(spawn.X, spawn.Y, PlannedFeature.Spawn);
			}

			PlaceResources(plan, options, centerRadius, random);
			PlaceCenterTechBuildings(plan, options.TechBuildings, random);
			return true;
		}

		static void PlaceResources(MapPlan plan, MapGenerationOptions options, double centerRadius, MersenneTwister random)
		{
			if (options.Resources == ResourceFieldLayout.Glitter)
			{
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (plan.TerrainAt(x, y) == PlannedTerrain.Land &&
							!plan.HasFeature(x, y, PlannedFeature.BuildClearance | PlannedFeature.Bottleneck) &&
							plan.HasFeature(x, y, PlannedFeature.Home) && random.Next(100) < 76)
							plan.AddFeature(x, y, PlannedFeature.Resource);
			}
			else if (options.Resources == ResourceFieldLayout.Patches)
			{
				var homePatchRadius = random.Next(7, 10);
				foreach (var spawn in plan.Spawns)
					PlacePatchNear(plan, spawn, homePatchRadius, false, random);
			}

			// The Nexus is always a dense contested field of tier-two resources when
			// resources are enabled, independent of the home-field layout.
			if (options.Resources != ResourceFieldLayout.None)
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (plan.TerrainAt(x, y) == PlannedTerrain.Land &&
							plan.HasFeature(x, y, PlannedFeature.ContestedCenter) && random.Next(100) < 84)
							plan.AddFeature(x, y, PlannedFeature.RichResource);

			if (!options.FiniteResources)
			{
				var generators = Math.Max(1, options.Players / 2);
				for (var i = 0; i < generators; i++)
				{
					var angle = i * Math.PI * 2 / generators;
					var radius = centerRadius * 0.55;
					var x = (int)Math.Round((plan.Width - 1) / 2.0 + Math.Cos(angle) * radius);
					var y = (int)Math.Round((plan.Height - 1) / 2.0 + Math.Sin(angle) * radius);
					if (plan.TerrainAt(x, y) == PlannedTerrain.Land)
						plan.AddFeature(x, y, PlannedFeature.ResourceGenerator);
				}
			}
		}

		static void PlacePatchNear(MapPlan plan, PlanPoint spawn, int radius, bool allowCenter, MersenneTwister random)
		{
			for (var attempt = 0; attempt < 200; attempt++)
			{
				var angle = random.NextFloat() * Math.PI * 2;
				var distance = radius + 4 + random.NextFloat() * 7;
				var center = new PlanPoint(
					(int)Math.Round(spawn.X + Math.Cos(angle) * distance),
					(int)Math.Round(spawn.Y + Math.Sin(angle) * distance));
				if (!CanPlacePatch(plan, center, radius, allowCenter))
					continue;
				PaintDisk(plan, center, radius, (x, y) => plan.AddFeature(x, y, PlannedFeature.Resource));
				return;
			}
		}

		static bool CanPlacePatch(MapPlan plan, PlanPoint center, int radius, bool allowCenter)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++)
				for (var x = center.X - radius; x <= center.X + radius; x++)
				{
					if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) > radius * radius)
						continue;
					if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Land ||
						plan.HasFeature(x, y, PlannedFeature.Resource | PlannedFeature.Bottleneck) ||
						!allowCenter && plan.HasFeature(x, y, PlannedFeature.ContestedCenter))
						return false;
				}
			return true;
		}

		static void PlaceCenterTechBuildings(MapPlan plan, TechBuildingDensity density, MersenneTwister random)
		{
			if (density == TechBuildingDensity.None)
				return;

			var target = density == TechBuildingDensity.Sparse ? 1 : random.Next(4, 7);
			var center = new PlanPoint((plan.Width - 1) / 2, (plan.Height - 1) / 2);
			for (var placed = 0; placed < target; placed++)
				for (var attempt = 0; attempt < 200; attempt++)
				{
					var x = center.X + (density == TechBuildingDensity.Sparse ? 0 : random.Next(-5, 6));
					var y = center.Y + (density == TechBuildingDensity.Sparse ? 0 : random.Next(-5, 6));
					if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Land ||
						!plan.HasFeature(x, y, PlannedFeature.ContestedCenter) ||
						plan.HasFeature(x, y, PlannedFeature.TechBuilding))
						continue;
					plan.AddFeature(x, y, PlannedFeature.TechBuilding);
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

			var isolated = FindReachable(plan, plan.Spawns[0], true);
			for (var i = 1; i < plan.Spawns.Count; i++)
				if (isolated[plan.Spawns[i].X + plan.Spawns[i].Y * plan.Width])
					return false;

			if (options.FiniteResources)
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (plan.HasFeature(x, y, PlannedFeature.ResourceGenerator))
							return false;

			plan.ValidationMessages.Add($"All {options.Players} individual Strongholds have exactly one center entrance.");
			plan.ValidationMessages.Add("Resources and Tech Buildings are enabled in the contested center.");
			return true;
		}

		static bool[] FindReachable(MapPlan plan, PlanPoint start, bool blockBottlenecks)
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
					if (!plan.IsTraversable(x, y) || blockBottlenecks && plan.HasFeature(x, y, PlannedFeature.Bottleneck))
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

		static PlanPoint ProjectInsideMap(MapPlan plan, double angle, int margin, double radialPosition)
		{
			var centerX = (plan.Width - 1) / 2.0;
			var centerY = (plan.Height - 1) / 2.0;
			var dx = Math.Cos(angle);
			var dy = Math.Sin(angle);
			var scaleX = ((plan.Width - 1) / 2.0 - margin) / Math.Max(0.0001, Math.Abs(dx));
			var scaleY = ((plan.Height - 1) / 2.0 - margin) / Math.Max(0.0001, Math.Abs(dy));
			var scale = Math.Min(scaleX, scaleY) * radialPosition;
			return new PlanPoint(
				(int)Math.Round(centerX + dx * scale),
				(int)Math.Round(centerY + dy * scale));
		}

		static double AngularDistance(double a, double b)
		{
			var difference = Math.Abs(a - b) % (Math.PI * 2);
			return difference > Math.PI ? Math.PI * 2 - difference : difference;
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
