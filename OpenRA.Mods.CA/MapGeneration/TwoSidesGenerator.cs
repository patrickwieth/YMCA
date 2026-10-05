using System;
using System.Collections.Generic;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class TwoSidesGenerator
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
				if (!TryGenerate(plan, options, random))
					continue;
				if (Validate(plan, options))
					return plan;
			}

			throw new InvalidOperationException($"Failed to generate a valid Two Sides map after {MaximumAttempts} attempts.");
		}

		static bool TryGenerate(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			var centerX = (plan.Width - 1) / 2.0;
			var centerY = (plan.Height - 1) / 2.0;
			var channelSlope = 0.34 + random.NextFloat() * 0.12;
			var waveAmplitude = Math.Min(plan.Width, plan.Height) * (0.018 + random.NextFloat() * 0.018);
			var wavePhase = random.NextFloat() * Math.PI * 2;
			var halfWaterWidth = Math.Clamp(Math.Min(plan.Width, plan.Height) / 11, 8, 14);

			double ChannelY(double x)
			{
				return centerY + (x - centerX) * channelSlope +
					Math.Sin(x / plan.Width * Math.PI * 2 + wavePhase) * waveAmplitude;
			}

			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (Math.Abs(y - ChannelY(x)) <= halfWaterWidth)
						plan.SetTerrain(x, y, PlannedTerrain.Water);

			// Cross the water at a shallow random angle. Shallow crossings stay long while
			// varying the direction and silhouette of the contested causeway between seeds.
			var channelAngle = Math.Atan(channelSlope);
			var crossingDelta = (18 + random.NextFloat() * 17) * Math.PI / 180;
			if (random.Next(2) == 0)
				crossingDelta = -crossingDelta;
			var causewayAngle = channelAngle + crossingDelta;
			var causewayCenterY = ChannelY(centerX);
			var causewayHalfLength = (int)Math.Ceiling((halfWaterWidth + 9) / Math.Sin(Math.Abs(crossingDelta)));
			causewayHalfLength = Math.Clamp(causewayHalfLength, 28, (int)(Math.Min(plan.Width, plan.Height) * 0.43));
			var causewayHalfWidth = Math.Clamp(Math.Min(plan.Width, plan.Height) / 48, 2, 4);
			var causewayCos = Math.Cos(causewayAngle);
			var causewaySin = Math.Sin(causewayAngle);
			var curveAmplitude = random.NextFloat() * Math.Min(10, halfWaterWidth * 0.7);
			var secondCurveAmplitude = (random.NextFloat() - 0.5) * curveAmplitude * 0.7;
			for (var step = -causewayHalfLength * 2; step <= causewayHalfLength * 2; step++)
			{
				var along = step / 2.0;
				var normalized = along / causewayHalfLength;
				var curveOffset =
					curveAmplitude * Math.Sin(normalized * Math.PI) +
					secondCurveAmplitude * Math.Sin(normalized * Math.PI * 2);
				var x = (int)Math.Round(centerX + along * causewayCos - curveOffset * causewaySin);
				var y = (int)Math.Round(causewayCenterY + along * causewaySin + curveOffset * causewayCos);
				var localWidth = causewayHalfWidth + (Math.Abs(step) % 17 == 0 ? 1 : 0);
				PaintDisk(plan, new PlanPoint(x, y), localWidth, (px, py) =>
				{
					if (plan.TerrainAt(px, py) != PlannedTerrain.Water)
						return;

					plan.SetTerrain(px, py, PlannedTerrain.ShallowWater);
					plan.AddFeature(px, py, PlannedFeature.Bottleneck);
				});
			}

			var (upperContactX, lowerContactX) = FindCausewayContacts(plan, ChannelY);
			MarkShores(plan, ChannelY, upperContactX, lowerContactX);
			PlaceTeamSpawns(plan, options.Players, ChannelY, halfWaterWidth);
			PlaceResources(plan, options, random);
			return true;
		}

		static (double UpperContactX, double LowerContactX) FindCausewayContacts(
			MapPlan plan,
			Func<double, double> channelY)
		{
			var upperX = 0.0;
			var lowerX = 0.0;
			var upperCount = 0;
			var lowerCount = 0;
			var neighbors = new[]
			{
				new PlanPoint(1, 0),
				new PlanPoint(-1, 0),
				new PlanPoint(0, 1),
				new PlanPoint(0, -1),
			};
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (!plan.HasFeature(x, y, PlannedFeature.Bottleneck))
						continue;

					foreach (var offset in neighbors)
					{
						var landX = x + offset.X;
						var landY = y + offset.Y;
						if (!plan.Contains(landX, landY) || plan.TerrainAt(landX, landY) != PlannedTerrain.Land)
							continue;

						if (landY < channelY(landX))
						{
							upperX += landX;
							upperCount++;
						}
						else
						{
							lowerX += landX;
							lowerCount++;
						}
					}
				}

			return (
				upperCount > 0 ? upperX / upperCount : plan.Width / 2.0,
				lowerCount > 0 ? lowerX / lowerCount : plan.Width / 2.0);
		}

		static void MarkShores(
			MapPlan plan,
			Func<double, double> channelY,
			double upperContactX,
			double lowerContactX)
		{
			var neighbors = new[]
			{
				new PlanPoint(1, 0),
				new PlanPoint(-1, 0),
				new PlanPoint(0, 1),
				new PlanPoint(0, -1),
			};

			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (!plan.IsTraversable(x, y) || plan.HasFeature(x, y, PlannedFeature.Bottleneck))
						continue;

					var touchesWater = false;
					foreach (var offset in neighbors)
						if (plan.Contains(x + offset.X, y + offset.Y) &&
							plan.TerrainAt(x + offset.X, y + offset.Y) == PlannedTerrain.Water)
						{
							touchesWater = true;
							break;
						}

					if (!touchesWater)
						continue;

					var lowerBank = y > channelY(x);
					var contactX = lowerBank ? lowerContactX : upperContactX;
					if (Math.Abs(x - contactX) <= 3)
						continue;

					var defensiveSide = lowerBank ? x > contactX : x < contactX;
					plan.AddFeature(x, y, defensiveSide ? PlannedFeature.CliffShore : PlannedFeature.LaunchShore);
				}
		}

		static void PlaceTeamSpawns(MapPlan plan, int playerCount, Func<double, double> channelY, int halfWaterWidth)
		{
			var firstTeamPlayers = (playerCount + 1) / 2;
			var secondTeamPlayers = playerCount - firstTeamPlayers;
			PlaceTeam(plan, 0, firstTeamPlayers, 0, false, channelY, halfWaterWidth);
			PlaceTeam(plan, firstTeamPlayers, secondTeamPlayers, 1, true, channelY, halfWaterWidth);
		}

		static void PlaceTeam(
			MapPlan plan,
			int firstPlayer,
			int count,
			int team,
			bool lowerBank,
			Func<double, double> channelY,
			int halfWaterWidth)
		{
			if (count == 0)
				return;

			for (var index = 0; index < count; index++)
			{
				var fraction = (index + 1.0) / (count + 1.0);
				var x = (int)Math.Round(plan.Width * (0.12 + fraction * 0.76));
				var bankY = channelY(x) + (lowerBank ? 1 : -1) * (halfWaterWidth + 14);
				var y = (int)Math.Round(Math.Clamp(bankY, 10, plan.Height - 11));
				var spawn = new PlanPoint(x, y);
				if (!plan.IsTraversable(x, y))
					continue;

				plan.Spawns.Add(spawn);
				PaintDisk(plan, spawn, 9, (px, py) =>
				{
					if (plan.IsTraversable(px, py))
						plan.SetHomeOwner(px, py, team);
				});
				PaintDisk(plan, spawn, 9, (px, py) => plan.AddFeature(px, py, PlannedFeature.BuildClearance));
				plan.AddFeature(x, y, PlannedFeature.Spawn);
			}
		}

		static void PlaceResources(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			if (options.Resources == ResourceFieldLayout.Glitter)
			{
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (plan.IsTraversable(x, y) && plan.HomeOwnerAt(x, y) >= 0 &&
							!plan.HasFeature(x, y, PlannedFeature.BuildClearance) && random.Next(100) < 78)
							plan.AddFeature(x, y, PlannedFeature.Resource);
			}
			else if (options.Resources == ResourceFieldLayout.Patches)
			{
				var firstTeamPlayers = (options.Players + 1) / 2;
				var patchRadius = 7;
				if (!PlaceAllResourcePatches(plan, options.Players, firstTeamPlayers, patchRadius, random))
					throw new InvalidOperationException("Could not place a complete mirrored resource patch layout.");
			}

			if (!options.FiniteResources)
				PlaceSymmetricActors(plan, PlannedFeature.ResourceGenerator, Math.Max(1, options.Players / 4), random);

			if (options.TechBuildings == TechBuildingDensity.Sparse)
				PlaceSymmetricActors(plan, PlannedFeature.TechBuilding, Math.Max(1, options.Players / 4), random);
			else if (options.TechBuildings == TechBuildingDensity.Dense)
				PlaceOilClusters(plan, Math.Max(1, options.Players / 4), random);
		}

		static bool PlaceAllResourcePatches(
			MapPlan plan, int playerCount, int firstTeamPlayers, int radius, MersenneTwister random)
		{
			var centers = new List<PlanPoint>();
			foreach (var spawn in plan.Spawns)
			{
				var placed = false;
				for (var attempt = 0; attempt < 2000 && !placed; attempt++)
				{
					var outwardAngle = spawn.Y < plan.Height / 2 ? -Math.PI / 2 : Math.PI / 2;
					var angle = attempt < 41
						? outwardAngle + (attempt % 2 == 0 ? -1 : 1) * ((attempt + 1) / 2) * 0.025
						: random.NextFloat() * Math.PI * 2;
					var distance = radius + 11;
					var center = new PlanPoint(
						(int)Math.Round(spawn.X + Math.Cos(angle) * distance),
						(int)Math.Round(spawn.Y + Math.Sin(angle) * distance));
					if (!CanPlaceResourcePatch(plan, center, radius) ||
						!SeparatedFromResourceCenters(centers, center, radius) ||
						!FarEnoughFromOtherSpawns(plan.Spawns, spawn, center, distance - 2))
						continue;
					centers.Add(center);
					placed = true;
				}
				if (!placed)
					return false;
			}

			foreach (var center in centers)
				PaintResourcePatch(plan, center, radius);
			return true;
		}

		static bool FarEnoughFromOtherSpawns(
			IEnumerable<PlanPoint> spawns, PlanPoint owner, PlanPoint center, int minimumDistance)
		{
			foreach (var spawn in spawns)
			{
				if (spawn.X == owner.X && spawn.Y == owner.Y)
					continue;
				var dx = spawn.X - center.X;
				var dy = spawn.Y - center.Y;
				if (dx * dx + dy * dy < minimumDistance * minimumDistance)
					return false;
			}
			return true;
		}

		static bool SeparatedFromResourceCenters(List<PlanPoint> centers, PlanPoint candidate, int radius)
		{
			var minimumDistance = radius * 2 + 3;
			foreach (var center in centers)
			{
				var dx = center.X - candidate.X;
				var dy = center.Y - candidate.Y;
				if (dx * dx + dy * dy < minimumDistance * minimumDistance)
					return false;
			}
			return true;
		}

		static bool PlaceRotatedPatchPair(
			MapPlan plan, PlanPoint firstSpawn, PlanPoint secondSpawn, int radius, MersenneTwister random)
		{
			for (var attempt = 0; attempt < 500; attempt++)
			{
				var angle = Math.PI + (random.NextFloat() - 0.5) * 0.5;
				var distance = radius + 11 + random.NextFloat() * 8;
				var offsetX = (int)Math.Round(Math.Cos(angle) * distance);
				var offsetY = (int)Math.Round(Math.Sin(angle) * distance);
				var firstCenter = new PlanPoint(firstSpawn.X + offsetX, firstSpawn.Y + offsetY);
				var secondCenter = new PlanPoint(secondSpawn.X - offsetX, secondSpawn.Y - offsetY);
				if (!CanPlaceResourcePatch(plan, firstCenter, radius) ||
					!CanPlaceResourcePatch(plan, secondCenter, radius))
					continue;
				PaintResourcePatch(plan, firstCenter, radius);
				PaintResourcePatch(plan, secondCenter, radius);
				return true;
			}
			return false;
		}

		static bool PlaceSinglePatch(MapPlan plan, PlanPoint spawn, int radius, MersenneTwister random)
		{
			for (var attempt = 0; attempt < 500; attempt++)
			{
				var angle = (random.Next(2) == 0 ? 0 : Math.PI) + (random.NextFloat() - 0.5) * 0.7;
				var distance = radius + 11 + random.NextFloat() * 8;
				var center = new PlanPoint(
					(int)Math.Round(spawn.X + Math.Cos(angle) * distance),
					(int)Math.Round(spawn.Y + Math.Sin(angle) * distance));
				if (!CanPlaceResourcePatch(plan, center, radius))
					continue;
				PaintResourcePatch(plan, center, radius);
				return true;
			}
			return false;
		}

		static void PaintResourcePatch(MapPlan plan, PlanPoint center, int radius)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++)
				for (var x = center.X - radius; x <= center.X + radius; x++)
				{
					if (!InsideResourcePatch(x - center.X, y - center.Y, radius))
						continue;
					plan.AddFeature(x, y, PlannedFeature.Resource);
					plan.RemoveFeature(x, y, PlannedFeature.LaunchShore | PlannedFeature.CliffShore);
				}
		}

		static bool CanPlaceResourcePatch(MapPlan plan, PlanPoint center, int radius)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++)
				for (var x = center.X - radius; x <= center.X + radius; x++)
				{
					if (!InsideResourcePatch(x - center.X, y - center.Y, radius))
						continue;
					if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Land ||
						plan.HasFeature(x, y, PlannedFeature.Resource | PlannedFeature.BuildClearance |
							PlannedFeature.Bottleneck))
						return false;
				}

			return true;
		}

		static bool InsideResourcePatch(int dx, int dy, int radius)
		{
			var verticalRadius = radius - 1;
			var ellipse = dx * dx * verticalRadius * verticalRadius + dy * dy * radius * radius <=
				radius * radius * verticalRadius * verticalRadius;
			return ellipse || Math.Abs(dx) == radius - 3 && Math.Abs(dy) == verticalRadius - 1;
		}

		static void PlaceSymmetricActors(MapPlan plan, PlannedFeature feature, int pairs, MersenneTwister random)
		{
			for (var pair = 0; pair < pairs; pair++)
			{
				for (var attempt = 0; attempt < 300; attempt++)
				{
					var x = random.Next(8, plan.Width / 2);
					var y = random.Next(8, plan.Height - 8);
					var mirrorX = plan.Width - 1 - x;
					var mirrorY = plan.Height - 1 - y;
					if (plan.TerrainAt(x, y) != PlannedTerrain.Land ||
						plan.TerrainAt(mirrorX, mirrorY) != PlannedTerrain.Land ||
						plan.HasFeature(x, y, PlannedFeature.Bottleneck | PlannedFeature.BuildClearance) ||
						plan.HasFeature(mirrorX, mirrorY, PlannedFeature.Bottleneck | PlannedFeature.BuildClearance))
						continue;

					plan.AddFeature(x, y, feature);
					plan.AddFeature(mirrorX, mirrorY, feature);
					break;
				}
			}
		}

		static void PlaceOilClusters(MapPlan plan, int pairs, MersenneTwister random)
		{
			for (var pair = 0; pair < pairs; pair++)
			{
				for (var attempt = 0; attempt < 300; attempt++)
				{
					var center = new PlanPoint(random.Next(8, plan.Width / 2), random.Next(8, plan.Height - 8));
					var mirror = new PlanPoint(plan.Width - 1 - center.X, plan.Height - 1 - center.Y);
					if (plan.TerrainAt(center.X, center.Y) != PlannedTerrain.Land ||
						plan.TerrainAt(mirror.X, mirror.Y) != PlannedTerrain.Land)
						continue;

					var count = random.Next(4, 7);
					for (var i = 0; i < count; i++)
					{
						var dx = random.Next(-3, 4);
						var dy = random.Next(-3, 4);
						if (plan.Contains(center.X + dx, center.Y + dy) &&
							plan.TerrainAt(center.X + dx, center.Y + dy) == PlannedTerrain.Land)
							plan.AddFeature(center.X + dx, center.Y + dy, PlannedFeature.TechBuilding);
						if (plan.Contains(mirror.X - dx, mirror.Y - dy) &&
							plan.TerrainAt(mirror.X - dx, mirror.Y - dy) == PlannedTerrain.Land)
							plan.AddFeature(mirror.X - dx, mirror.Y - dy, PlannedFeature.TechBuilding);
					}

					break;
				}
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
			var withoutBottleneck = FindReachable(plan, plan.Spawns[0], true);
			if (withoutBottleneck[plan.Spawns[firstTeamPlayers].X + plan.Spawns[firstTeamPlayers].Y * plan.Width])
				return false;

			var launchShore = 0;
			var cliffShore = 0;
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (plan.HasFeature(x, y, PlannedFeature.LaunchShore))
						launchShore++;
					if (plan.HasFeature(x, y, PlannedFeature.CliffShore))
						cliffShore++;
					if (options.FiniteResources && plan.HasFeature(x, y, PlannedFeature.ResourceGenerator))
						return false;
				}

			if (launchShore == 0 || cliffShore == 0)
				return false;

			if (options.Resources == ResourceFieldLayout.Patches)
				foreach (var spawn in plan.Spawns)
				{
					var nearbyResource = false;
					for (var y = spawn.Y - 30; y <= spawn.Y + 30 && !nearbyResource; y++)
						for (var x = spawn.X - 30; x <= spawn.X + 30; x++)
							if (plan.Contains(x, y) && plan.HasFeature(x, y, PlannedFeature.Resource))
							{
								nearbyResource = true;
								break;
							}

					if (!nearbyResource)
						return false;
				}

			plan.ValidationMessages.Add("Both team regions are connected only through the planned bottleneck.");
			plan.ValidationMessages.Add("Launch shores and defensive cliff shores are present for both teams.");
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
