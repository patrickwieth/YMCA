using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class IslandsGenerator
	{
		const int MaximumAttempts = 48;
		static readonly PlanPoint[] CardinalOffsets =
		{
			new PlanPoint(1, 0),
			new PlanPoint(-1, 0),
			new PlanPoint(0, 1),
			new PlanPoint(0, -1),
		};

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
				Fill(plan, PlannedTerrain.Water);
				var random = new MersenneTwister(unchecked(options.Seed + attempt * 130363));
				var generated = options.Preset switch
				{
					MapGenerationPreset.Continents => GenerateContinents(plan, options, random),
					MapGenerationPreset.Archipelago => GenerateArchipelago(plan, options, random),
					MapGenerationPreset.Migration => GenerateMigration(plan, options, random),
					_ => false,
				};
				if (generated && options.Custom != null)
				{
					ApplyCustomResources(plan, options);
					generated = ApplyCustomEconomy(plan, options);
				}
				if (generated && Validate(plan, options))
					return plan;
			}

			throw new InvalidOperationException($"Failed to generate a valid {options.Preset} map after {MaximumAttempts} attempts.");
		}

		static bool ApplyCustomEconomy(MapPlan plan, MapGenerationOptions options)
		{
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
			{
				if (options.TechBuildings == TechBuildingDensity.None) plan.RemoveFeature(x, y, PlannedFeature.TechBuilding);
				if (options.FiniteResources || options.Resources == ResourceFieldLayout.None) plan.RemoveFeature(x, y, PlannedFeature.ResourceGenerator);
			}
			if (options.FiniteResources || options.Resources == ResourceFieldLayout.None || options.Preset != MapGenerationPreset.Migration) return true;

			// Migration normally has no generators. Infinite custom resources need
			// real trees on the central resource island, never on economy-free homes.
			var candidates = new List<PlanPoint>();
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
			{
				if (plan.TerrainAt(x, y) != PlannedTerrain.Land || !plan.HasFeature(x, y, PlannedFeature.Resource) ||
					!plan.HasFeature(x, y, PlannedFeature.ContestedCenter) || plan.HasFeature(x, y,
						PlannedFeature.BuildClearance | PlannedFeature.Bottleneck | PlannedFeature.DestructibleBridge | PlannedFeature.LaunchShore)) continue;
				var blocked = false;
				for (var dy = -3; dy <= 3; dy++) for (var dx = -3; dx <= 3; dx++)
					if (plan.Contains(x + dx, y + dy) && plan.HasFeature(x + dx, y + dy, PlannedFeature.TechBuilding)) blocked = true;
				if (!blocked) candidates.Add(new PlanPoint(x, y));
			}
			if (candidates.Count == 0) return false;
			var center = Center(plan);
			var radius = Math.Sqrt(candidates.Max(p => DistanceSquared(p.X, p.Y, center))) * 0.6;
			var trees = new List<PlanPoint>();
			for (var i = 0; i < options.Players; i++)
			{
				var target = new PlanPoint(center.X + (int)Math.Round(Math.Cos(i * 2 * Math.PI / options.Players) * radius),
					center.Y + (int)Math.Round(Math.Sin(i * 2 * Math.PI / options.Players) * radius));
				var available = candidates.Where(p => trees.All(t => DistanceSquared(p.X, p.Y, t) >= 25))
					.OrderBy(p => DistanceSquared(p.X, p.Y, target)).ToArray();
				if (available.Length == 0) return false;
				trees.Add(available[0]); plan.AddFeature(available[0].X, available[0].Y, PlannedFeature.ResourceGenerator);
			}
			return true;
		}

		static void ApplyCustomResources(MapPlan plan, MapGenerationOptions options)
		{
			var migration = options.Preset == MapGenerationPreset.Migration;
			var baseline = migration ? ResourceFieldLayout.Glitter : ResourceFieldLayout.Patches;
			if (options.Resources == baseline) return;
			if (options.Resources == ResourceFieldLayout.None)
			{
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
					plan.RemoveFeature(x, y, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.ResourceGenerator);
				return;
			}

			if (!migration)
			{
				// Glitter spreads over home land, not neutral oil islands. Keep starting
				// build disks, launch shores, bridge approaches and tech footprints clear.
				var homes = plan.Spawns.Select(p => PlannedRubberduckBridge.Cell(p.X, p.Y)).ToHashSet();
				var land = CustomIslandConnections.Components(plan).Where(c => c.Overlaps(homes)).SelectMany(c => c)
					.OrderBy(c => c.X).ThenBy(c => c.Y);
				var random = new MersenneTwister(unchecked(options.Seed + plan.GenerationAttempt * 130363 + 8719));
				foreach (var c in land)
				{
					var p = PlannedRubberduckBridge.Point(c);
					if (plan.HasFeature(p.U, p.V, PlannedFeature.BuildClearance | PlannedFeature.DestructibleBridge |
						PlannedFeature.Bottleneck | PlannedFeature.LaunchShore)) continue;
					var shore = false;
					for (var dy = -2; dy <= 2; dy++) for (var dx = -2; dx <= 2; dx++)
					{
						var bank = PlannedRubberduckBridge.Point(c + new CVec(dx, dy));
						if (!plan.Contains(bank.U, bank.V) || plan.TerrainAt(bank.U, bank.V) != PlannedTerrain.Land) shore = true;
					}
					if (shore) continue;
					var nearTech = false;
					for (var dy = -3; dy <= 3; dy++) for (var dx = -3; dx <= 3; dx++)
						if (plan.Contains(p.U + dx, p.V + dy) && plan.HasFeature(p.U + dx, p.V + dy, PlannedFeature.TechBuilding)) nearTech = true;
					if (!nearTech && random.Next(100) < 65) plan.AddFeature(p.U, p.V, PlannedFeature.Resource);
				}
				return;
			}

			// Migration keeps its economy on the central island. Selecting Patches
			// explicitly replaces the dense field with separated radial deposits.
			var cells = new List<PlanPoint>();
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				if (plan.HasFeature(x, y, PlannedFeature.Resource)) cells.Add(new PlanPoint(x, y));
			if (cells.Count == 0) return;
			var center = Center(plan);
			var radius = Math.Sqrt(cells.Max(p => DistanceSquared(p.X, p.Y, center))) * 0.58;
			var anchors = Enumerable.Range(0, options.Players).Select(i => new PlanPoint(
				center.X + (int)Math.Round(Math.Cos(i * 2 * Math.PI / options.Players) * radius),
				center.Y + (int)Math.Round(Math.Sin(i * 2 * Math.PI / options.Players) * radius))).ToArray();
			foreach (var p in cells)
				if (!anchors.Any(a => DistanceSquared(p.X, p.Y, a) <= 16)) plan.RemoveFeature(p.X, p.Y, PlannedFeature.Resource);
		}

		static bool GenerateContinents(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			var teamCount = EffectiveTeamCount(options);
			var mapCenter = Center(plan);
			var orbitRadius = Math.Min(plan.Width, plan.Height) * (teamCount == 2 ? 0.245 : 0.265);
			var continentRadius = Math.Min(plan.Width, plan.Height) * (teamCount == 2 ? 0.225 : 0.19 / Math.Sqrt(teamCount / 2.0));
			var phase = random.NextFloat() * Math.PI * 2;
			var teamCenters = RadialCenters(plan, teamCount, orbitRadius, phase);
			if (teamCount == 2)
			{
				var shapeA = random.NextFloat() * Math.PI * 2;
				var shapeB = random.NextFloat() * Math.PI * 2;
				var radiusX = plan.Width * 0.245;
				var radiusY = plan.Height * 0.43;
				for (var team = 0; team < teamCenters.Count; team++)
					PaintContinentEllipse(plan, teamCenters[team], radiusX, radiusY,
						phase, team * Math.PI, shapeA, shapeB, team == 0);
			}
			else
				foreach (var center in teamCenters)
				{
					PaintDisk(plan, center, (int)Math.Round(continentRadius), (x, y) => plan.SetTerrain(x, y, PlannedTerrain.Land));
					var angle = Math.Atan2(center.Y - mapCenter.Y, center.X - mapCenter.X);
					var perpendicularX = (int)Math.Round(-Math.Sin(angle) * continentRadius * 0.42);
					var perpendicularY = (int)Math.Round(Math.Cos(angle) * continentRadius * 0.42);
					PaintDisk(plan, new PlanPoint(center.X + perpendicularX, center.Y + perpendicularY),
						(int)Math.Round(continentRadius * 0.82), (x, y) => plan.SetTerrain(x, y, PlannedTerrain.Land));
					PaintDisk(plan, new PlanPoint(center.X - perpendicularX, center.Y - perpendicularY),
						(int)Math.Round(continentRadius * 0.82), (x, y) => plan.SetTerrain(x, y, PlannedTerrain.Land));
				}

			for (var player = 0; player < options.Players; player++)
			{
				var team = TeamForPlayer(player, options.Players, teamCount);
				var members = PlayersOnTeam(team, options.Players, teamCount);
				var memberIndex = TeamMemberIndex(player, options.Players, teamCount);
				var center = teamCenters[team];
				var outwardAngle = Math.Atan2(center.Y - mapCenter.Y, center.X - mapCenter.X);
				var playerAngle = outwardAngle + (members == 1 ? 0 : memberIndex * Math.PI * 2 / members);
				var spawnRadius = continentRadius * (members <= 2 ? 0.42 : 0.55);
				var spawn = new PlanPoint(
					(int)Math.Round(center.X + Math.Cos(playerAngle) * spawnRadius),
					(int)Math.Round(center.Y + Math.Sin(playerAngle) * spawnRadius));
				AddSpawn(plan, spawn, player, 7);
				PlaceHomePatch(plan, spawn, center, 6, !options.FiniteResources);
			}

			foreach (var center in teamCenters)
				plan.AddFeature(center.X, center.Y, PlannedFeature.TechBuilding);
			if (options.Custom?.Connections != false && !RubberduckBridgePlanner.Connect(plan, teamCenters)) return false;
			MarkLaunchShores(plan);
			RubberduckCoastSmoother.Apply(plan, true, teamCount);
			return true;
		}

		static bool GenerateArchipelago(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			var mapCenter = Center(plan);
			var homeRadius = options.Players <= 3 ? 10 : options.Players <= 8 ? 12 : 11;
			var homeOrbit = Math.Min(plan.Width, plan.Height) / 2.0 - homeRadius - 8;
			var phase = random.NextFloat() * Math.PI * 2;
			var homes = RadialCenters(plan, options.Players, homeOrbit, phase);
			var homeShapeA = random.NextFloat() * Math.PI * 2;
			var homeShapeB = random.NextFloat() * Math.PI * 2;
			for (var player = 0; player < homes.Count; player++)
			{
				var rotation = phase + player * Math.PI * 2 / homes.Count;
				PaintRotatedIslandBlob(plan, homes[player], homeRadius, rotation, homeShapeA, homeShapeB);
				AddSpawn(plan, homes[player], player, 4);
				var inward = PointToward(homes[player], mapCenter, 7);
				// The radial blob can be narrower than the nominal home radius. Keep
				// the whole equal-sized patch on its island; retry the complete seeded
				// layout instead of dropping ore cells or painting over the sea.
				var patchFits = true;
				PaintDisk(plan, inward, 2, (x, y) => patchFits &= plan.TerrainAt(x, y) == PlannedTerrain.Land);
				if (!patchFits) return false;
				PaintDisk(plan, inward, 2, (x, y) => plan.AddFeature(x, y, PlannedFeature.Resource));
				plan.AddFeature(inward.X, inward.Y, PlannedFeature.ResourceGenerator);
			}

			var neutralCenters = new List<PlanPoint>();
			var neutralTarget = Math.Max(options.Players, 6);
			var symmetryOrder = PlayerSpawnLayout.SymmetryOrder(options.Players);
			for (var attempt = 0; attempt < 5000 && neutralCenters.Count < neutralTarget; attempt++)
			{
				// Four to six full 2x2 derricks plus a dry shore margin need a real interior.
				var radius = random.Next(6, 8);
				var extent = (int)Math.Ceiling(radius * 1.22) + 3;
				var baseCenter = new PlanPoint(
					random.Next(extent, plan.Width - extent),
					random.Next(extent, plan.Height - extent));
				var orbit = RotatedOrbit(plan, baseCenter, symmetryOrder);
				if (!CanPaintLandOrbit(plan, orbit, radius))
					continue;
				var shapeA = random.NextFloat() * Math.PI * 2;
				var shapeB = random.NextFloat() * Math.PI * 2;
				for (var rotation = 0; rotation < orbit.Count; rotation++)
				{
					PaintRotatedIslandBlob(plan, orbit[rotation], radius,
						rotation * Math.PI * 2 / symmetryOrder, shapeA, shapeB);
					neutralCenters.Add(orbit[rotation]);
				}
			}
			if (neutralCenters.Count < neutralTarget)
				return false;

			if (options.Custom == null && !PlaceSameTeamBridges(plan, homes, options)) return false;
			MarkLaunchShores(plan);
			RubberduckCoastSmoother.Apply(plan, true, PlayerSpawnLayout.SymmetryOrder(options.Players));
			foreach (var center in neutralCenters)
				if (!PlaceOilCluster(plan, center, random.Next(4, 7))) return false;
			if (options.Custom?.Connections == true && !CustomIslandConnections.Connect(plan)) return false;
			MarkLaunchShores(plan);
			return true;
		}

		static bool GenerateMigration(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			var mapCenter = Center(plan);
			var minimumDimension = Math.Min(plan.Width, plan.Height);
			var homeRadius = Math.Clamp(13 - options.Players / 3, 8, 12);
			var maximumCenterExtent = minimumDimension / 2.0 - homeRadius * 2 - 5;
			var centerRadius = (int)Math.Round(Math.Min(minimumDimension * 0.27, maximumCenterExtent / 1.2));
			PaintMigrationIsland(plan, mapCenter, centerRadius, 0, random.NextFloat() * Math.PI * 2, random.NextFloat() * Math.PI * 2);
			plan.AddFeature(mapCenter.X, mapCenter.Y, PlannedFeature.ContestedCenter);

			var orbit = minimumDimension / 2.0 - homeRadius - 3;
			var homePhase = random.NextFloat() * Math.PI * 2;
			var homes = RadialCenters(plan, options.Players, orbit, homePhase);
			for (var player = 0; player < homes.Count; player++)
			{
				PaintMigrationIsland(plan, homes[player], homeRadius, 0, 0, 0, false);
				AddSpawn(plan, homes[player], player, Math.Max(4, homeRadius - 3));
			}

			if (!PlaceMigrationOilIslands(plan, options, orbit, homePhase, random)) return false;
			MarkLaunchShores(plan);
			RubberduckCoastSmoother.Apply(plan, true, PlayerSpawnLayout.SymmetryOrder(options.Players));
			if ((options.Custom == null || options.TechBuildings != TechBuildingDensity.None) && !PlaceMigrationHomeOil(plan)) return false;
			if (options.Custom != null)
			{
				CustomIslandConnections.MarkMigrationCenter(plan);
				if (options.Custom.Connections && !CustomIslandConnections.Connect(plan)) return false;
			}
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (DistanceSquared(x, y, mapCenter) <= centerRadius * centerRadius * 1.25 &&
						plan.TerrainAt(x, y) == PlannedTerrain.Land && random.Next(100) < 82 &&
						(options.Custom == null || !plan.HasFeature(x, y, PlannedFeature.DestructibleBridge | PlannedFeature.Bottleneck | PlannedFeature.LaunchShore)))
						plan.AddFeature(x, y, PlannedFeature.Resource);

			var clusterPhase = random.NextFloat() * Math.PI * 2;
			for (var cluster = 0; cluster < options.Players; cluster++)
			{
				var angle = clusterPhase + cluster * Math.PI * 2 / options.Players;
				var center = new PlanPoint(
					(int)Math.Round(mapCenter.X + Math.Cos(angle) * centerRadius * 0.52),
					(int)Math.Round(mapCenter.Y + Math.Sin(angle) * centerRadius * 0.52));
				if (!PlaceOilCluster(plan, center, 6, true)) return false;
			}
			MarkLaunchShores(plan);
			return true;
		}

		static bool PlaceMigrationHomeOil(MapPlan plan)
		{
			var components = CustomIslandConnections.Components(plan);
			foreach (var home in plan.Spawns)
			{
				var spawn = PlannedRubberduckBridge.Cell(home.X, home.Y);
				var island = components.Single(c => c.Contains(spawn));
				bool Fits(CPos origin)
				{
					if (!OilHasDryMargin(plan, origin)) return false;
					for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++)
					{
						var c = origin + new CVec(x, y);
						if (!island.Contains(c)) return false;
						var p = PlannedRubberduckBridge.Point(c);
						if (plan.HasFeature(p.U, p.V, PlannedFeature.BuildClearance | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator)) return false;
						for (var dy = -2; dy <= 2; dy++) for (var dx = -2; dx <= 2; dx++)
						{
							var shore = PlannedRubberduckBridge.Point(c + new CVec(dx, dy));
							if (plan.Contains(shore.U, shore.V) && plan.HasFeature(shore.U, shore.V, PlannedFeature.LaunchShore)) return false;
						}
					}
					return true;
				}
				var candidates = island.Where(Fits).OrderBy(c => (c - spawn).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y).ToArray();
				var pair = (from a in candidates from b in candidates where (a - b).LengthSquared >= 16
					orderby (a - spawn).LengthSquared + (b - spawn).LengthSquared select new[] { a, b }).FirstOrDefault();
				if (pair == null) return false;
				foreach (var c in pair)
				{
					var p = PlannedRubberduckBridge.Point(c); plan.AddFeature(p.U, p.V, PlannedFeature.TechBuilding);
				}
			}
			return true;
		}

		static bool PlaceMigrationOilIslands(MapPlan plan, MapGenerationOptions options, double orbit, double homePhase, MersenneTwister random)
		{
			var centers = RadialCenters(plan, options.Players, orbit, homePhase + Math.PI / options.Players);
			var groupCount = options.Players / PlayerSpawnLayout.SymmetryOrder(options.Players);
			var counts = Enumerable.Range(0, groupCount).Select(_ => random.Next(1, 3)).ToArray();
			var shapeA = random.NextFloat() * Math.PI * 2; var shapeB = random.NextFloat() * Math.PI * 2;
			for (var i = 0; i < centers.Count; i++)
			{
				// One small island per gap, with a real sea corridor on every side.
				// A failed placement rejects the whole layout, never a partial orbit.
				if (!CanPaintLandIsland(plan, centers[i], 5, 4)) return false;
				PaintMigrationIsland(plan, centers[i], 4, homePhase + (2 * i + 1) * Math.PI / options.Players, shapeA, shapeB);
				if (!PlaceOilCluster(plan, centers[i], counts[i % groupCount])) return false;
			}
			return true;
		}

		static bool PlaceSameTeamBridges(MapPlan plan, List<PlanPoint> homes, MapGenerationOptions options)
		{
			var teams = new List<int>();
			for (var player = 0; player < options.Players; player++)
				teams.Add(TeamForPlayer(player, options.Players, EffectiveTeamCount(options)));
			return RubberduckBridgePlanner.Connect(plan, homes, teams);
		}

		static void PlaceHomePatch(MapPlan plan, PlanPoint spawn, PlanPoint continentCenter, int radius, bool generator)
		{
			var dx = spawn.X - continentCenter.X;
			var dy = spawn.Y - continentCenter.Y;
			var length = Math.Max(0.001, Math.Sqrt(dx * dx + dy * dy));
			var patchCenter = new PlanPoint(
				(int)Math.Round(spawn.X - dy / length * 14),
				(int)Math.Round(spawn.Y + dx / length * 14));
			PaintDisk(plan, patchCenter, radius, (x, y) =>
			{
				if (plan.TerrainAt(x, y) == PlannedTerrain.Land && !plan.HasFeature(x, y, PlannedFeature.BuildClearance))
					plan.AddFeature(x, y, PlannedFeature.Resource);
			});
			if (generator && plan.TerrainAt(patchCenter.X, patchCenter.Y) == PlannedTerrain.Land)
				plan.AddFeature(patchCenter.X, patchCenter.Y, PlannedFeature.ResourceGenerator);
		}

		internal static bool OilHasDryMargin(MapPlan plan, CPos origin)
		{
			// Native building coordinates: the 2x2 foundation plus one complete land
			// cell on every side, including corners, not just its anchor in plan space.
			for (var y = -1; y <= 2; y++) for (var x = -1; x <= 2; x++)
			{
				var p = PlannedRubberduckBridge.Point(origin + new CVec(x, y));
				if (!plan.Contains(p.U, p.V) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land ||
					plan.HasFeature(p.U, p.V, PlannedFeature.DestructibleBridge | PlannedFeature.CliffShore)) return false;
			}
			return true;
		}

		static bool PlaceOilCluster(MapPlan plan, PlanPoint center, int count, bool clearResources = false)
		{
			var target = PlannedRubberduckBridge.Cell(center.X, center.Y);
			var island = CustomIslandConnections.Components(plan).Single(c => c.Contains(target));
			bool Fits(CPos c)
			{
				if (!OilHasDryMargin(plan, c)) return false;
				for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++)
				{
					var p = PlannedRubberduckBridge.Point(c + new CVec(x, y));
					if (plan.HasFeature(p.U, p.V, PlannedFeature.BuildClearance | PlannedFeature.LaunchShore | PlannedFeature.Bottleneck)) return false;
				}
				for (var y = -1; y <= 1; y++) for (var x = -1; x <= 1; x++)
				{
					var p = PlannedRubberduckBridge.Point(c + new CVec(x, y));
					if (plan.HasFeature(p.U, p.V, PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator)) return false;
				}
				return true;
			}
			var candidates = island.Where(c => (c - target).LengthSquared <= 64 && Fits(c))
				.OrderBy(c => (c - target).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y).ToArray();
			// A two-cell lattice packs non-overlapping foundations. Try each phase
			// before rejecting the complete layout; never silently drop a derrick.
			for (var phase = 0; phase < 4; phase++)
			{
				var selected = candidates.Where(c => (c.X & 1) == phase % 2 && (c.Y & 1) == phase / 2).Take(count).ToArray();
				if (selected.Length != count) continue;
				foreach (var c in selected)
				{
					var p = PlannedRubberduckBridge.Point(c); plan.AddFeature(p.U, p.V, PlannedFeature.TechBuilding);
					if (clearResources)
						for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++)
						{
							var foot = PlannedRubberduckBridge.Point(c + new CVec(x, y));
							plan.RemoveFeature(foot.U, foot.V, PlannedFeature.Resource);
						}
				}
				return true;
			}
			return false;
		}

		static void AddSpawn(MapPlan plan, PlanPoint spawn, int player, int clearanceRadius)
		{
			if (!plan.Contains(spawn.X, spawn.Y) || plan.TerrainAt(spawn.X, spawn.Y) != PlannedTerrain.Land)
				return;
			plan.Spawns.Add(spawn);
			PaintDisk(plan, spawn, clearanceRadius, (x, y) =>
			{
				if (plan.TerrainAt(x, y) != PlannedTerrain.Land)
					return;
				plan.SetHomeOwner(x, y, player);
				plan.AddFeature(x, y, PlannedFeature.BuildClearance);
			});
			plan.AddFeature(spawn.X, spawn.Y, PlannedFeature.Spawn);
		}

		static void MarkLaunchShores(MapPlan plan)
		{
			foreach (var spawn in plan.Spawns)
			{
				PlanPoint? best = null;
				var bestDistance = int.MaxValue;
				for (var y = 1; y < plan.Height - 1; y++)
					for (var x = 1; x < plan.Width - 1; x++)
					{
						if (plan.TerrainAt(x, y) != PlannedTerrain.Land || !TouchesTerrain(plan, x, y, PlannedTerrain.Water))
							continue;
						var distance = DistanceSquared(x, y, spawn);
						if (distance < bestDistance)
						{
							bestDistance = distance;
							best = new PlanPoint(x, y);
						}
					}
				if (best.HasValue)
					PaintDisk(plan, best.Value, 1, (x, y) =>
					{
						if (plan.TerrainAt(x, y) == PlannedTerrain.Land)
							plan.AddFeature(x, y, PlannedFeature.LaunchShore);
					});
			}
		}

		static bool Validate(MapPlan plan, MapGenerationOptions options)
		{
			if (plan.Spawns.Count != options.Players)
				return false;
			foreach (var spawn in plan.Spawns)
				if (plan.TerrainAt(spawn.X, spawn.Y) != PlannedTerrain.Land ||
					!plan.HasFeature(spawn.X, spawn.Y, PlannedFeature.BuildClearance))
					return false;

			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				if (plan.HasFeature(x, y, PlannedFeature.TechBuilding) && !OilHasDryMargin(plan, PlannedRubberduckBridge.Cell(x, y))) return false;

			// Reject invalid candidates inside the retry loop, before the shared
			// validator/exporter sees them. Never silently discard inaccessible economy.
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (plan.HasFeature(x, y, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.ResourceGenerator) && !plan.IsTraversable(x, y))
						return false;

			if (options.FiniteResources && CountFeatures(plan, PlannedFeature.ResourceGenerator) != 0)
				return false;
			if (!options.FiniteResources && CountFeatures(plan, PlannedFeature.ResourceGenerator) < options.Players)
				return false;

			if (options.Preset == MapGenerationPreset.Continents)
			{
				if (options.Custom?.Connections != false && CountFeatures(plan, PlannedFeature.DestructibleBridge) == 0)
					return false;
				var waterCells = CountTerrain(plan, PlannedTerrain.Water) + CountTerrain(plan, PlannedTerrain.ShallowWater);
				var waterPercent = waterCells * 100.0 / (plan.Width * plan.Height);
				if (EffectiveTeamCount(options) == 2 && (waterPercent < 30 || waterPercent > 40))
					return false;
				plan.ValidationMessages.Add($"Water covers {waterPercent:0.0}% of the map.");
				plan.ValidationMessages.Add(options.Custom?.Connections == false ? "Continents have no bridges; naval transport is required." :
					"Continents are linked only by destructible bridges across deep water.");
			}
			else if (options.Preset == MapGenerationPreset.Archipelago)
			{
				if (options.Custom?.Connections != true && !ValidateTeamSafeCrossings(plan, options))
					return false;
				plan.ValidationMessages.Add(options.Custom == null ? "Only same-team destructible bridges cross deep water; no permanent shallow-water causeways or enemy ground connections." :
					options.Custom.Connections ? "Optional bridge network includes a cross-team route; protected small islands may still require transport." : "No bridges, including team bridges; naval transport is required.");
				plan.ValidationMessages.Add("Resource patches and generators are confined to player home islands.");
			}
			else
			{
				if (!ResourcesConfinedToCenter(plan) || !ValidateMigrationIslands(plan, options.Players, options.Custom?.Connections == true, options.Custom != null && options.Resources == ResourceFieldLayout.None, options.Custom != null && options.TechBuildings == TechBuildingDensity.None)) return false;
				plan.ValidationMessages.Add(options.Custom == null ?
					$"Central island: {options.Players * 6} Oil Derricks and finite resources. {options.Players} separate small oil islands: one or two derricks each." :
					$"Custom central economy: {options.Resources}, finite={options.FiniteResources}; tech buildings={CountFeatures(plan, PlannedFeature.TechBuilding)}, generators={CountFeatures(plan, PlannedFeature.ResourceGenerator)}.");
				plan.ValidationMessages.Add(options.Custom?.Connections == true ? "Dry home/oil islands remain distinct, with optional bridge connections; home islands have oil derricks but no Tiberium fields or rock banks." :
					"Every home and oil island is separated by deep water; home islands have oil derricks but no Tiberium fields or rock banks.");
			}

			plan.ValidationMessages.Add("Every player has a deep-water launch shore.");
			return true;
		}

		static bool ValidateTeamSafeCrossings(MapPlan plan, MapGenerationOptions options)
		{
			var teamCount = EffectiveTeamCount(options);
			var visited = new bool[plan.Width * plan.Height];
			var queue = new Queue<PlanPoint>();
			for (var player = 0; player < plan.Spawns.Count; player++)
			{
				var spawn = plan.Spawns[player];
				var index = spawn.X + spawn.Y * plan.Width;
				if (visited[index])
					continue;
				var componentTeam = -1;
				visited[index] = true;
				queue.Enqueue(spawn);
				while (queue.Count > 0)
				{
					var cell = queue.Dequeue();
					for (var other = 0; other < plan.Spawns.Count; other++)
						if (plan.Spawns[other].X == cell.X && plan.Spawns[other].Y == cell.Y)
						{
							var team = TeamForPlayer(other, options.Players, teamCount);
							if (componentTeam >= 0 && componentTeam != team)
								return false;
							componentTeam = team;
						}
					foreach (var offset in PlateauTopology.Directions)
					{
						// Bridges follow native cell axes, not the staggered debug grid.
						var neighbor = PlannedRubberduckBridge.Point(PlannedRubberduckBridge.Cell(cell.X, cell.Y) + offset);
						var x = neighbor.U;
						var y = neighbor.V;
						if (!plan.Contains(x, y) || !plan.IsTraversable(x, y))
							continue;
						var neighborIndex = x + y * plan.Width;
						if (visited[neighborIndex])
							continue;
						visited[neighborIndex] = true;
						queue.Enqueue(new PlanPoint(x, y));
					}
				}
			}
			return true;
		}

		internal static bool ValidateMigrationIslands(MapPlan plan, int players, bool bridges = false, bool noResources = false, bool noTech = false)
		{
			var remaining = new HashSet<CPos>();
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
			{
				if (plan.TerrainAt(x, y) != PlannedTerrain.Land && plan.TerrainAt(x, y) != PlannedTerrain.Water &&
					!(bridges && plan.TerrainAt(x, y) == PlannedTerrain.ShallowWater && plan.HasFeature(x, y, PlannedFeature.DestructibleBridge))) return false;
				if (plan.TerrainAt(x, y) == PlannedTerrain.Land) remaining.Add(PlannedRubberduckBridge.Cell(x, y));
			}
			var center = PlannedRubberduckBridge.Cell(Center(plan).X, Center(plan).Y);
			var homes = plan.Spawns.Select(p => PlannedRubberduckBridge.Cell(p.X, p.Y)).ToHashSet();
			var homeCount = 0; var oilIslands = 0; var foundCenter = false;
			while (remaining.Count != 0)
			{
				var pending = new Queue<CPos>(); pending.Enqueue(remaining.First());
				var component = new HashSet<CPos>(); var oils = 0; var resources = 0;
				while (pending.Count != 0)
				{
					var c = pending.Dequeue(); if (!remaining.Remove(c)) continue;
					component.Add(c); var p = PlannedRubberduckBridge.Point(c);
					if (plan.HasFeature(p.U, p.V, PlannedFeature.TechBuilding)) oils++;
					if (plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.ResourceGenerator)) resources++;
					foreach (var d in PlateauTopology.Directions) if (remaining.Contains(c + d)) pending.Enqueue(c + d);
				}
				var starts = component.Count(homes.Contains);
				if (component.Contains(center))
				{
					if (starts != 0 || oils != (noTech ? 0 : players * 6) || (noResources ? resources != 0 : resources == 0)) return false;
					foundCenter = true;
				}
				else if (starts != 0)
				{
					if (starts != 1 || oils != (noTech ? 0 : 2) || resources != 0) return false;
					homeCount++;
				}
				else
				{
					if ((noTech ? oils != 0 : oils < 1 || oils > 2) || resources != 0) return false;
					oilIslands++;
				}
			}
			return foundCenter && homeCount == players && oilIslands == players;
		}

		static bool ResourcesConfinedToCenter(MapPlan plan)
		{
			var center = Center(plan);
			var maximumDistance = Math.Min(plan.Width, plan.Height) * 0.39;
			var maximumDistanceSquared = maximumDistance * maximumDistance;
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (plan.HasFeature(x, y, PlannedFeature.Resource) &&
						DistanceSquared(x, y, center) > maximumDistanceSquared)
						return false;
			return true;
		}

		static int CountTerrain(MapPlan plan, PlannedTerrain terrain)
		{
			var count = 0;
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (plan.TerrainAt(x, y) == terrain)
						count++;
			return count;
		}

		static int CountFeatures(MapPlan plan, PlannedFeature feature)
		{
			var count = 0;
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (plan.HasFeature(x, y, feature))
						count++;
			return count;
		}

		static List<PlanPoint> RadialCenters(MapPlan plan, int count, double radius, double phase)
		{
			var result = new List<PlanPoint>();
			var center = Center(plan);
			for (var i = 0; i < count; i++)
			{
				var angle = phase + i * Math.PI * 2 / count;
				result.Add(new PlanPoint(
					(int)Math.Round(center.X + Math.Cos(angle) * radius),
					(int)Math.Round(center.Y + Math.Sin(angle) * radius)));
			}
			return result;
		}

		static List<PlanPoint> RotatedOrbit(MapPlan plan, PlanPoint point, int order)
		{
			var result = new List<PlanPoint>();
			var center = Center(plan);
			var dx = point.X - center.X;
			var dy = point.Y - center.Y;
			for (var rotation = 0; rotation < order; rotation++)
			{
				var angle = rotation * Math.PI * 2 / order;
				result.Add(new PlanPoint(
					(int)Math.Round(center.X + dx * Math.Cos(angle) - dy * Math.Sin(angle)),
					(int)Math.Round(center.Y + dx * Math.Sin(angle) + dy * Math.Cos(angle))));
			}
			return result;
		}

		static bool CanPaintLandOrbit(MapPlan plan, List<PlanPoint> centers, int radius)
		{
			var blobExtent = (int)Math.Ceiling(radius * 1.22);
			for (var i = 0; i < centers.Count; i++)
			{
				if (!CanPaintLandIsland(plan, centers[i], blobExtent, 3))
					return false;
				for (var j = 0; j < i; j++)
				{
					var dx = centers[i].X - centers[j].X;
					var dy = centers[i].Y - centers[j].Y;
					var minimumDistance = blobExtent * 2 + 3;
					if (dx * dx + dy * dy < minimumDistance * minimumDistance)
						return false;
				}
			}
			return true;
		}

		static bool CanPaintLandIsland(MapPlan plan, PlanPoint center, int radius, int clearance)
		{
			var checkedRadius = radius + clearance;
			for (var y = center.Y - checkedRadius; y <= center.Y + checkedRadius; y++)
				for (var x = center.X - checkedRadius; x <= center.X + checkedRadius; x++)
				{
					if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) > checkedRadius * checkedRadius)
						continue;
					if (!plan.Contains(x, y) || plan.TerrainAt(x, y) != PlannedTerrain.Water)
						return false;
				}
			return true;
		}

		static bool TouchesTerrain(MapPlan plan, int x, int y, PlannedTerrain terrain)
		{
			foreach (var offset in CardinalOffsets)
			{
				var nx = x + offset.X;
				var ny = y + offset.Y;
				if (plan.Contains(nx, ny) && plan.TerrainAt(nx, ny) == terrain)
					return true;
			}
			return false;
		}

		static PlanPoint PointToward(PlanPoint from, PlanPoint to, double distance)
		{
			var dx = to.X - from.X;
			var dy = to.Y - from.Y;
			var length = Math.Max(0.001, Math.Sqrt(dx * dx + dy * dy));
			return new PlanPoint(
				(int)Math.Round(from.X + dx / length * distance),
				(int)Math.Round(from.Y + dy / length * distance));
		}

		static PlanPoint Center(MapPlan plan)
		{
			return new PlanPoint((plan.Width - 1) / 2, (plan.Height - 1) / 2);
		}

		static int EffectiveTeamCount(MapGenerationOptions options)
		{
			return Math.Clamp(options.Teams, 1, options.Players);
		}

		static int TeamForPlayer(int player, int players, int teams)
		{
			return Math.Min(teams - 1, player * teams / players);
		}

		static int PlayersOnTeam(int team, int players, int teams)
		{
			var count = 0;
			for (var player = 0; player < players; player++)
				if (TeamForPlayer(player, players, teams) == team)
					count++;
			return count;
		}

		static int TeamMemberIndex(int player, int players, int teams)
		{
			var team = TeamForPlayer(player, players, teams);
			var index = 0;
			for (var other = 0; other < player; other++)
				if (TeamForPlayer(other, players, teams) == team)
					index++;
			return index;
		}

		static int DistanceSquared(int x, int y, PlanPoint point)
		{
			var dx = x - point.X;
			var dy = y - point.Y;
			return dx * dx + dy * dy;
		}

		static void PaintContinentEllipse(
			MapPlan plan, PlanPoint center, double radiusX, double radiusY, double axisRotation,
			double shapeRotation, double phaseA, double phaseB, bool positiveSide)
		{
			var mapCenter = Center(plan);
			var axisCos = Math.Cos(axisRotation);
			var axisSin = Math.Sin(axisRotation);
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var centerProjection = (x - mapCenter.X) * axisCos + (y - mapCenter.Y) * axisSin;
					if (positiveSide ? centerProjection <= 3 : centerProjection >= -3)
						continue;
					var dx = x - center.X;
					var dy = y - center.Y;
					var localX = dx * axisCos + dy * axisSin;
					var localY = -dx * axisSin + dy * axisCos;
					var localAngle = Math.Atan2(localY / radiusY, localX / radiusX) - shapeRotation;
					var boundary = 1 + 0.08 * Math.Sin(localAngle * 3 + phaseA) +
						0.05 * Math.Sin(localAngle * 5 + phaseB);
					var normalizedDistance = Math.Sqrt(localX * localX / (radiusX * radiusX) +
						localY * localY / (radiusY * radiusY));
					if (normalizedDistance <= boundary)
						plan.SetTerrain(x, y, PlannedTerrain.Land);
				}
		}

		static void PaintMigrationIsland(MapPlan plan, PlanPoint center, int radius, double rotation, double phaseA, double phaseB, bool irregular = true)
		{
			var extent = (int)Math.Ceiling(radius * 1.22) + 1;
			for (var y = center.Y - extent; y <= center.Y + extent; y++)
				for (var x = center.X - extent; x <= center.X + extent; x++)
				{
					if (!plan.Contains(x, y)) continue;
					// Account for the staggered native rows. A circle in raw MPos
					// coordinates otherwise leaves a disconnected tip on each home.
					var dx = x - center.X + (((y + 1) & 1) - ((center.Y + 1) & 1)) * .5;
					var dy = y - center.Y;
					var angle = Math.Atan2(dy, dx) - rotation;
					var localRadius = radius * (irregular ? 1 + .12 * Math.Sin(angle * 3 + phaseA) + .07 * Math.Sin(angle * 5 + phaseB) : 1);
					if (dx * dx + dy * dy <= localRadius * localRadius) plan.SetTerrain(x, y, PlannedTerrain.Land);
				}
		}

		static void PaintRotatedIslandBlob(
			MapPlan plan, PlanPoint center, int radius, double rotation, double phaseA, double phaseB)
		{
			var extent = (int)Math.Ceiling(radius * 1.22);
			for (var y = center.Y - extent; y <= center.Y + extent; y++)
				for (var x = center.X - extent; x <= center.X + extent; x++)
				{
					if (!plan.Contains(x, y))
						continue;
					var dx = x - center.X;
					var dy = y - center.Y;
					var localAngle = Math.Atan2(dy, dx) - rotation;
					var localRadius = radius * (1 + 0.12 * Math.Sin(localAngle * 3 + phaseA) +
						0.07 * Math.Sin(localAngle * 5 + phaseB));
					if (dx * dx + dy * dy <= localRadius * localRadius)
						plan.SetTerrain(x, y, PlannedTerrain.Land);
				}
		}

		static void Fill(MapPlan plan, PlannedTerrain terrain)
		{
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					plan.SetTerrain(x, y, terrain);
		}

		static void PaintDisk(MapPlan plan, PlanPoint center, int radius, Action<int, int> paint)
		{
			for (var y = center.Y - radius; y <= center.Y + radius; y++)
				for (var x = center.X - radius; x <= center.X + radius; x++)
					if (plan.Contains(x, y) && DistanceSquared(x, y, center) <= radius * radius)
						paint(x, y);
		}

	}
}
