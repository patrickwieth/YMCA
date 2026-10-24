using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class TwoSidesGenerator
	{
		internal const int MaximumAttempts = 32;

		public MapPlan Generate(MapGenerationOptions options, int startAttempt = 0)
		{
			options.Validate();
			for (var attempt = startAttempt; attempt < MaximumAttempts; attempt++)
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
			// Native isometric axes have slope +/-2 in staggered MPos, not +/-1.
			// Follow those axes so long cliff faces join directly instead of placing
			// an inside/outside corner at almost every cell along a shallow raster line.
			var channelSlope = random.Next(2) == 0 ? 2.0 : -2.0;
			var waveAmplitude = Math.Min(plan.Width, plan.Height) * (0.018 + random.NextFloat() * 0.018);
			var wavePhase = random.NextFloat() * Math.PI * 2;
			var halfWaterWidth = Math.Clamp(Math.Min(plan.Width, plan.Height) / 9, 10, 18);
			var bankNormal = channelSlope > 0 ? new CVec(0, 1) : new CVec(1, 0);
			var nativeCenter = MountainPlateauPlanner.Cell(plan.Width / 2, plan.Height / 2);
			double BankDistance(int x, int y)
			{
				var cell = MountainPlateauPlanner.Cell(x, y);
				var along = bankNormal.X == 0 ? cell.X - nativeCenter.X : cell.Y - nativeCenter.Y;
				var across = bankNormal.X == 0 ? cell.Y - nativeCenter.Y : cell.X - nativeCenter.X;
				return across - Math.Sin(along / (double)Math.Min(plan.Width, plan.Height) * Math.PI * 2 + wavePhase) * waveAmplitude / 2;
			}

			double ChannelY(double x)
			{
				return centerY + (x - centerX) * channelSlope +
					Math.Sin((x - centerX) / plan.Width * Math.PI * 2 + wavePhase) * waveAmplitude;
			}

			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (Math.Abs(BankDistance(x, y)) <= halfWaterWidth / 2.0)
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
			var causewayHalfWidth = Math.Clamp(Math.Min(plan.Width, plan.Height) / 32, 4, 6);
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
				PaintDisk(plan, new PlanPoint(x, y), causewayHalfWidth, (px, py) =>
				{
					if (plan.TerrainAt(px, py) != PlannedTerrain.Water)
						return;

					plan.SetTerrain(px, py, PlannedTerrain.ShallowWater);
					plan.AddFeature(px, py, PlannedFeature.Bottleneck);
				});
			}

			PlaceTeamSpawns(plan, options.Players, bankNormal, halfWaterWidth);
			// Reduce one-cell coastline teeth before classifying banks or placing
			// economy. Paired swaps preserve area/connectivity and protect crossings/homes.
			RubberduckCoastSmoother.Apply(plan);
			var (upperContactX, lowerContactX) = FindCausewayContacts(plan, (x, y) => BankDistance(x, y) > 0);
			MarkShores(plan, (x, y) => BankDistance(x, y) > 0, upperContactX, lowerContactX);
			return PlaceResources(plan, options, random, bankNormal);
		}

		static (double UpperContactX, double LowerContactX) FindCausewayContacts(
			MapPlan plan,
			Func<int, int, bool> isLowerBank)
		{
			var upperX = 0.0;
			var lowerX = 0.0;
			var upperCount = 0;
			var lowerCount = 0;
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (!plan.HasFeature(x, y, PlannedFeature.Bottleneck))
						continue;

					foreach (var neighbor in NativeNeighbors(x, y))
					{
						var landX = neighbor.U;
						var landY = neighbor.V;
						if (!plan.Contains(landX, landY) || plan.TerrainAt(landX, landY) != PlannedTerrain.Land)
							continue;

						if (!isLowerBank(landX, landY))
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
			Func<int, int, bool> isLowerBank,
			double upperContactX,
			double lowerContactX)
		{

			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (!plan.IsTraversable(x, y) || plan.HasFeature(x, y, PlannedFeature.Bottleneck))
						continue;

					var touchesWater = false;
					foreach (var neighbor in NativeNeighbors(x, y))
						if (plan.Contains(neighbor.U, neighbor.V) &&
							plan.TerrainAt(neighbor.U, neighbor.V) == PlannedTerrain.Water)
						{
							touchesWater = true;
							break;
						}

					if (!touchesWater)
						continue;

					var lowerBank = isLowerBank(x, y);
					var contactX = lowerBank ? lowerContactX : upperContactX;
					if (Math.Abs(x - contactX) <= 3)
						continue;

					var defensiveSide = lowerBank ? x > contactX : x < contactX;
					plan.AddFeature(x, y, defensiveSide ? PlannedFeature.CliffShore : PlannedFeature.LaunchShore);
				}
		}

		static IEnumerable<MPos> NativeNeighbors(int x, int y)
		{
			var cell = MountainPlateauPlanner.Cell(x, y);
			foreach (var direction in PlateauTopology.Directions)
				yield return MountainPlateauPlanner.Point(cell + direction);
		}

		static void PlaceTeamSpawns(MapPlan plan, int playerCount, CVec bankNormal, int halfWaterWidth)
		{
			var firstTeamPlayers = (playerCount + 1) / 2;
			var secondTeamPlayers = playerCount - firstTeamPlayers;
			PlaceTeam(plan, firstTeamPlayers, 0, false, bankNormal, halfWaterWidth);
			PlaceTeam(plan, secondTeamPlayers, 1, true, bankNormal, halfWaterWidth);
		}

		static void PlaceTeam(
			MapPlan plan,
			int count,
			int team,
			bool lowerBank,
			CVec bankNormal,
			int halfWaterWidth)
		{
			if (count == 0)
				return;

			var center = MountainPlateauPlanner.Cell(plan.Width / 2, plan.Height / 2);
			var tangent = bankNormal.X == 0 ? new CVec(1, 0) : new CVec(0, 1);
			var depth = halfWaterWidth / 2 + Math.Clamp(Math.Min(plan.Width, plan.Height) / 6, 14, 32);
			var bankCenter = center + bankNormal * (lowerBank ? depth : -depth);
			var reach = Math.Max(plan.Width, plan.Height);
			var available = Enumerable.Range(-reach, 2 * reach + 1).Where(along =>
			{
				var p = MountainPlateauPlanner.Point(bankCenter + tangent * along);
				return p.U >= 12 && p.V >= 12 && p.U < plan.Width - 12 && p.V < plan.Height - 12;
			}).ToArray();
			if (available.Length == 0) return;
			var positions = Enumerable.Range(0, count).Select(i => available[(int)Math.Round((i + 1.0) * (available.Length - 1) / (count + 1))]).ToArray();
			var shift = positions.OrderBy(Math.Abs).ThenBy(p => p).First();
			// Translate the evenly spaced formation so its nearest slot is exactly
			// on the native river normal. The usual opponents therefore face each
			// other diagonally, while teammates use the available length of each bank.
			foreach (var along in positions.Select(p => p - shift).OrderBy(Math.Abs).ThenBy(p => p))
			{
				var p = MountainPlateauPlanner.Point(bankCenter + tangent * along);
				var x = p.U; var y = p.V;
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

		static bool PlaceResources(MapPlan plan, MapGenerationOptions options, MersenneTwister random, CVec bankNormal)
		{
			if (options.Resources == ResourceFieldLayout.Glitter)
			{
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (GlitterCell(plan, x, y) && random.Next(100) < 78)
							plan.AddFeature(x, y, PlannedFeature.Resource);
			}
			else if (options.Resources == ResourceFieldLayout.Patches)
			{
				var firstTeamPlayers = (options.Players + 1) / 2;
				var patchRadius = 7;
				if (!PlaceAllResourcePatches(plan, options.Players, firstTeamPlayers, patchRadius, random, bankNormal))
					return false;
			}

			if (options.Resources != ResourceFieldLayout.None && !options.FiniteResources)
				PlaceSymmetricActors(plan, PlannedFeature.ResourceGenerator, Math.Max(1, options.Players / 4), random);

			if (options.TechBuildings == TechBuildingDensity.Sparse)
				PlaceSymmetricActors(plan, PlannedFeature.TechBuilding, Math.Max(1, options.Players / 4), random);
			else if (options.TechBuildings == TechBuildingDensity.Dense)
				PlaceOilClusters(plan, Math.Max(1, options.Players / 4), random);
			return true;
		}

		static bool GlitterCell(MapPlan plan, int x, int y)
		{
			// HomeOwner only labels the cleared starting disk on Two Sides. Requiring
			// it together with !BuildClearance excluded every possible resource cell.
			if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Land ||
				plan.HasFeature(x, y, PlannedFeature.BuildClearance)) return false;
			for (var dy = -4; dy <= 4; dy++) for (var dx = -3; dx <= 3; dx++)
				if (!plan.Contains(x + dx, y + dy) || plan.TerrainAt(x + dx, y + dy) != PlannedTerrain.Land ||
					plan.HasFeature(x + dx, y + dy, PlannedFeature.CliffShore | PlannedFeature.LaunchShore | PlannedFeature.Bottleneck)) return false;
			return true;
		}

		static bool PlaceAllResourcePatches(
			MapPlan plan, int playerCount, int firstTeamPlayers, int radius, MersenneTwister random, CVec bankNormal)
		{
			var centers = new List<PlanPoint>();
			for (var player = 0; player < plan.Spawns.Count; player++)
			{
				var spawn = plan.Spawns[player];
				var outward = MountainPlateauPlanner.Point(MountainPlateauPlanner.Cell(spawn.X, spawn.Y) + bankNormal * (player < firstTeamPlayers ? -2 : 2));
				var outwardAngle = Math.Atan2(outward.V - spawn.Y, outward.U - spawn.X);
				var placed = false;
				for (var attempt = 0; attempt < 2000 && !placed; attempt++)
				{
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

			if (options.Resources != ResourceFieldLayout.None)
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
