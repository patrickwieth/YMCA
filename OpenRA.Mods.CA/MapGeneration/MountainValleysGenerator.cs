using System;
using System.Collections.Generic;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class MountainValleysGenerator
	{
		public const int MaximumAttempts = 32;

		sealed class MountainSite
		{
			public PlanPoint Center;
			public int RadiusX;
			public int RadiusY;
			public double Rotation;
			public bool Raised;
			public double[] GateAngles = Array.Empty<double>();
			public double[] BorderAngles = Array.Empty<double>();
			public double GateHalfAngle;
		}

		public MapPlan Generate(MapGenerationOptions options, int startingAttempt = 0)
		{
			options.Validate();
			for (var attempt = startingAttempt; attempt < MaximumAttempts; attempt++)
			{
				var plan = new MapPlan(options.Width, options.Height)
				{
					Seed = options.Seed,
					GenerationAttempt = attempt,
					RequireClosedPlateaus = true,
				};

				var random = new MersenneTwister(unchecked(options.Seed + attempt * 104729));
				if (!TryGenerateTerrain(plan, options, random, out plan))
					continue;

				// Every committed plateau already owns its complete footprint. Do not
				// run the legacy apron expansion or rebuild an unused partial actor graph.
				PlaceResources(plan, options, random);
				if (Validate(plan, options))
					return plan;
				Failed("height connectivity or construction area");
			}

			throw new InvalidOperationException($"Failed to generate a valid map after {MaximumAttempts} attempts.");
		}

		static bool Failed(string stage)
		{
			if (Environment.GetEnvironmentVariable("YMCA_MAPGEN_DIAGNOSTICS") == "1") Console.Error.WriteLine("Rejected layout: " + stage);
			return false;
		}

		static bool TryGenerateTerrain(MapPlan plan, MapGenerationOptions options, MersenneTwister random, out MapPlan result)
		{
			result = plan;
			var occupiedSites = new List<MountainSite>();
			var minimumDimension = Math.Min(plan.Width, plan.Height);
			var valleyRadius = options.Players >= 9
				? 7
				: Math.Clamp(minimumDimension / (options.Players + 6), 7, 11);
			var homeRadius = Math.Min(14, valleyRadius + 3);
			var spawnCenters = CreateSpawnCenters(plan, options.Players, homeRadius, random);
			plan.SymmetrySlotCount = spawnCenters.Count;
			var homeGateOffsets = CreateOpenGateOffsets(random);
			var homeShapeSeed = random.Next();

			// Homes are filled high ground. Expansion valleys remain low; their gates
			// are valley passages, not substitutes for the home plateau ascents.
			for (var slot = 0; slot < spawnCenters.Count; slot++)
			{
				var occupied = slot < options.Players;
				var center = spawnCenters[slot];
				var inwardAngle = Math.Atan2((plan.Height - 1) / 2.0 - center.Y, (plan.Width - 1) / 2.0 - center.X);
				var borderAngles = FindBorderAngles(plan, center, homeRadius);
				var site = new MountainSite
				{
					Center = center,
					Raised = true,
					RadiusX = homeRadius,
					RadiusY = homeRadius,
					Rotation = inwardAngle + Math.PI,
					GateAngles = CreateHomeGateAngles(homeGateOffsets, inwardAngle, borderAngles.Length),
					BorderAngles = borderAngles,
					GateHalfAngle = borderAngles.Length > 0 ? 0.2 : 0,
				};
				if (!SiteFits(plan, occupiedSites, site, 0))
					return Failed("home overlap " + slot);

				occupiedSites.Add(site);
				if (!PaintMountainRing(plan, site, PlannedFeature.Home, occupied ? slot : -1, homeShapeSeed))
					return Failed("home ramp " + slot);
				if (occupied)
				{
					plan.Spawns.Add(site.Center);
					plan.AddFeature(site.Center.X, site.Center.Y, PlannedFeature.Spawn);
				}
				else
				{
					PaintDisk(plan, site.Center, homeRadius - 3, (x, y) => plan.AddFeature(x, y, PlannedFeature.VacantHome));
				}

				PaintDisk(plan, site.Center, 9, (x, y) => plan.AddFeature(x, y, PlannedFeature.BuildClearance));
			}

			for (var expansion = 0; expansion < options.ExpansionsPerPlayer; expansion++)
			{
				var targetDistance = minimumDimension * (0.27 + random.NextFloat() * 0.05);
				var expansionGateOffsets = CreateOpenGateOffsets(random);
				var expansionDirectionOffset = (random.NextFloat() - 0.5) * Math.PI * 0.9;
				var expansionShapeSeed = random.Next();
				for (var slot = 0; slot < spawnCenters.Count; slot++)
				{
					var site = FindExpansionSite(
						plan,
						occupiedSites,
						spawnCenters[slot],
						valleyRadius,
						targetDistance,
						expansionDirectionOffset,
						expansionGateOffsets,
						random);
					if (site == null)
						return Failed("expansion " + slot);

					occupiedSites.Add(site);
					PaintMountainRing(plan, site, PlannedFeature.Expansion, -1, expansionShapeSeed);
					plan.ExpansionCenters.Add(site.Center);
				}
			}

			// Closed islands are filled walkable plateaus with reserved ascents. Keep
			// broad low tank valleys between them and commit complete symmetry orbits.
			var targetClosedMountains = Math.Max(options.Players, plan.Width * plan.Height / 900);
			var symmetryOrder = ClosedMountainSymmetryOrder(plan, options.Players);
			for (var proposal = 0; proposal < 32 && occupiedSites.Count < plan.SymmetrySlotCount * (options.ExpansionsPerPlayer + 1) + targetClosedMountains; proposal++)
			{
				var orbit = FindClosedMountainOrbit(plan, occupiedSites, symmetryOrder, random);
				if (orbit == null)
					break;

				var closedShapeSeed = random.Next();
				// Test and commit an entire symmetry orbit, including its feet. A
				// rejected small island must not leave half an orbit or force every
				// already valid home to be regenerated.
				var draft = plan.Clone();
				var fits = true;
				foreach (var site in orbit)
					if (!PaintMountainRing(draft, site, PlannedFeature.Enclosed, -1, closedShapeSeed))
					{ fits = false; break; }
				if (!fits) continue;
				var before = MountainPlateauPlanner.Reachable(plan, plan.Spawns[0]);
				var after = MountainPlateauPlanner.Reachable(draft, draft.Spawns[0]);
				for (var y = 0; y < plan.Height && fits; y++)
					for (var x = 0; x < plan.Width; x++)
						if (before[x + y * plan.Width] && draft.IsTraversable(x, y) && !after[x + y * plan.Width])
						{ fits = false; break; }
				if (!fits) continue;
				plan = draft;
				occupiedSites.AddRange(orbit);
			}

			// Do not silently fall back to a ring-only map when no usable island orbit fits.
			result = plan;
			return plan.Plateaus.Count > 0;
		}

		static List<PlanPoint> CreateSpawnCenters(MapPlan plan, int playerCount, int radius, MersenneTwister random)
		{
			var centers = new List<PlanPoint>();
			var symmetryOrder = PlayerSymmetryOrder(playerCount);
			var phase = symmetryOrder == 4 ? -Math.PI * 3 / 4 : random.NextFloat() * Math.PI * 2;

			// The base orbit always touches the map edge. For six or more players the
			// remaining slots are inserted into the sectors and moved towards the center.
			for (var sector = 0; sector < symmetryOrder; sector++)
			{
				var angle = phase + sector * Math.PI * 2 / symmetryOrder;
				centers.Add(ProjectToMapEdge(plan, angle, radius));
			}

			var intermediateCounts = IntermediatePlayerCounts(playerCount);
			for (var sector = 0; sector < intermediateCounts.Length; sector++)
			{
				var count = intermediateCounts[sector];
				for (var slot = 0; slot < count; slot++)
				{
					var fraction = (slot + 1.0) / (count + 1.0);
					var angle = phase + (sector + fraction) * Math.PI * 2 / symmetryOrder;
					var radialPosition = count == 1 ? 0.72 : 0.64 + slot * 0.16;
					centers.Add(ProjectInsideMap(plan, angle, radius, radialPosition));
				}
			}

			return centers;
		}

		static int PlayerSymmetryOrder(int playerCount)
		{
			switch (playerCount)
			{
				case 2: return 2;
				case 3: return 3;
				case 4: return 4;
				case 5: return 5;
				case 6: return 4;
				case 7: return 7;
				case 8: return 4;
				case 9: return 5;
				case 10: return 5;
				case 11:
				case 12:
				case 16:
					return 4;
				default:
					throw new ArgumentOutOfRangeException(nameof(playerCount));
			}
		}

		static int[] IntermediatePlayerCounts(int playerCount)
		{
			switch (playerCount)
			{
				case 6: return new[] { 1, 0, 1, 0 };
				case 8: return new[] { 1, 1, 1, 1 };
				case 9: return new[] { 1, 1, 1, 1, 1 };
				case 10: return new[] { 1, 1, 1, 1, 1 };
				case 11: return new[] { 2, 2, 2, 2 };
				case 12: return new[] { 2, 2, 2, 2 };
				case 16: return new[] { 3, 3, 3, 3 };
				default: return Array.Empty<int>();
			}
		}

		static PlanPoint ProjectToMapEdge(MapPlan plan, double angle, int radius)
		{
			return ProjectInsideMap(plan, angle, radius, 1);
		}

		static PlanPoint ProjectInsideMap(MapPlan plan, double angle, int radius, double radialPosition)
		{
			// Four height levels project the roof towards the top map edge. Leave
			// that extra margin so the entire starting plateau stays inside bounds.
			var margin = radius + 7;
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

		static MountainSite FindExpansionSite(
			MapPlan plan,
			List<MountainSite> occupiedSites,
			PlanPoint spawn,
			int radius,
			double targetDistance,
			double directionOffset,
			double[] gateOffsets,
			MersenneTwister random)
		{
			var towardCenter = Math.Atan2((plan.Height - 1) / 2.0 - spawn.Y, (plan.Width - 1) / 2.0 - spawn.X);
			for (var attempt = 0; attempt < 400; attempt++)
			{
				// Raised homes need projection margin and real approach corridors. Prefer
				// the old expansion orbit, then also search the outer valley gaps rather
				// than forcing every expansion into the now smaller central space.
				var fallback = attempt >= 100;
				var retryJitter = attempt == 0 ? 0 : (random.NextFloat() - 0.5) *
					(fallback ? Math.PI * 2 : Math.Min(Math.PI * 0.7, attempt * 0.025));
				var direction = towardCenter + directionOffset + retryJitter;
				var distance = targetDistance * (attempt == 0 ? 1 : fallback ? 0.65 + random.NextFloat() * 0.6 : 0.95 + random.NextFloat() * 0.1);
				var center = new PlanPoint(
					(int)Math.Round(spawn.X + Math.Cos(direction) * distance),
					(int)Math.Round(spawn.Y + Math.Sin(direction) * distance));
				var preferredGateAngle = Math.Atan2(spawn.Y - center.Y, spawn.X - center.X);
				var site = new MountainSite
				{
					Center = center,
					RadiusX = radius,
					RadiusY = radius,
					Rotation = direction,
					GateAngles = RotateGateOffsets(gateOffsets, preferredGateAngle),
				};
				if (SiteFits(plan, occupiedSites, site, 1))
					return site;
			}

			return null;
		}

		static int ClosedMountainSymmetryOrder(MapPlan plan, int playerCount)
		{
			return PlayerSymmetryOrder(playerCount);
		}

		static List<MountainSite> FindClosedMountainOrbit(
			MapPlan plan,
			List<MountainSite> occupiedSites,
			int symmetryOrder,
			MersenneTwister random)
		{
			for (var attempt = 0; attempt < 500; attempt++)
			{
				var radius = random.Next(5, 10);
				var mapCenter = new PlanPoint((plan.Width - 1) / 2, (plan.Height - 1) / 2);
				var maximumOrbitRadius = Math.Min(plan.Width, plan.Height) / 2 - radius - 4;
				var orbitRadius = random.Next(0, Math.Max(1, maximumOrbitRadius));
				var orbitAngle = random.NextFloat() * Math.PI * 2;
				var center = new PlanPoint(
					(int)Math.Round(mapCenter.X + Math.Cos(orbitAngle) * orbitRadius),
					(int)Math.Round(mapCenter.Y + Math.Sin(orbitAngle) * orbitRadius));
				var orbit = new List<MountainSite>();
				var valid = true;
				for (var rotation = 0; rotation < symmetryOrder; rotation++)
				{
					var rotatedCenter = RotatePoint(plan, center, rotation, symmetryOrder);
					var site = new MountainSite
					{
						Center = rotatedCenter,
						Raised = true,
						RadiusX = radius,
						RadiusY = radius,
						Rotation = rotation * Math.PI * 2 / symmetryOrder,
					};
					if (!SiteFits(plan, occupiedSites, site, 3) || !SiteFits(plan, orbit, site, 3))
					{
						valid = false;
						break;
					}

					orbit.Add(site);
				}

				if (valid)
					return orbit;
			}

			return null;
		}

		static PlanPoint RotatePoint(MapPlan plan, PlanPoint point, int rotation, int symmetryOrder)
		{
			if (rotation == 0)
				return point;

			var centerX = (plan.Width - 1) / 2.0;
			var centerY = (plan.Height - 1) / 2.0;
			var angle = rotation * Math.PI * 2 / symmetryOrder;
			var dx = point.X - centerX;
			var dy = point.Y - centerY;
			return new PlanPoint(
				(int)Math.Round(centerX + dx * Math.Cos(angle) - dy * Math.Sin(angle)),
				(int)Math.Round(centerY + dx * Math.Sin(angle) + dy * Math.Cos(angle)));
		}

		static double[] CreateOpenGateOffsets(MersenneTwister random)
		{
			var first = (random.NextFloat() - 0.5) * Math.PI * 0.7;
			switch (random.Next(1, 4))
			{
				case 1:
					return new[] { first };
				case 2:
					return new[] { first, first + Math.PI * (0.7 + random.NextFloat() * 0.6) };
				default:
					return new[] { first, first + Math.PI * 2 / 3, first - Math.PI * 2 / 3 };
			}
		}

		static double[] RotateGateOffsets(double[] offsets, double preferredAngle)
		{
			var angles = new double[offsets.Length];
			for (var i = 0; i < offsets.Length; i++)
				angles[i] = preferredAngle + offsets[i];

			return angles;
		}

		static double[] CreateHomeGateAngles(double[] offsets, double inwardAngle, int borderCount)
		{
			if (borderCount >= 2)
				return new[] { inwardAngle + offsets[0] * 0.2 };
			if (borderCount == 1 && offsets.Length > 1)
				return new[] { inwardAngle - 0.4, inwardAngle + 0.4 };
			return RotateGateOffsets(offsets, inwardAngle);
		}

		static double[] FindBorderAngles(MapPlan plan, PlanPoint center, int radius)
		{
			var angles = new List<double>();
			var threshold = radius + 4;
			if (center.X <= threshold)
				angles.Add(Math.PI);
			if (center.X >= plan.Width - 1 - threshold)
				angles.Add(0);
			if (center.Y <= threshold)
				angles.Add(-Math.PI / 2);
			if (center.Y >= plan.Height - 1 - threshold)
				angles.Add(Math.PI / 2);
			return angles.ToArray();
		}

		static bool SiteFits(MapPlan plan, List<MountainSite> occupiedSites, MountainSite candidate, int clearance)
		{
			if (candidate.Center.X - candidate.RadiusX < 3 || candidate.Center.Y - candidate.RadiusY < (candidate.Raised ? 7 : 3) ||
				candidate.Center.X + candidate.RadiusX >= plan.Width - 3 ||
				candidate.Center.Y + candidate.RadiusY >= plan.Height - 3)
				return false;

			for (var y = candidate.Center.Y - candidate.RadiusY - 2; y <= candidate.Center.Y + candidate.RadiusY + 2; y++)
				for (var x = candidate.Center.X - candidate.RadiusX - 2; x <= candidate.Center.X + candidate.RadiusX + 2; x++)
					if (plan.Contains(x, y) && (plan.HasFeature(x, y, PlannedFeature.Entrance | PlannedFeature.Enclosed) ||
						plan.PlateauSurfaces.ContainsKey(new MPos(x, y))) &&
						(x - candidate.Center.X) * (x - candidate.Center.X) / (double)(candidate.RadiusX * candidate.RadiusX) +
						(y - candidate.Center.Y) * (y - candidate.Center.Y) / (double)(candidate.RadiusY * candidate.RadiusY) <= 1.14 * 1.14)
						return false;

			foreach (var site in occupiedSites)
			{
				var minimumDistance = Math.Max(candidate.RadiusX, candidate.RadiusY) +
					Math.Max(site.RadiusX, site.RadiusY) + clearance;
				if (DistanceSquared(candidate.Center, site.Center) < minimumDistance * minimumDistance)
					return false;
			}

			return true;
		}

		static bool PaintMountainRing(
			MapPlan plan,
			MountainSite site,
			PlannedFeature interiorFeature,
			int homeOwner,
			int shapeSeed)
		{
			var shapeRandom = new MersenneTwister(shapeSeed);
			var phaseA = shapeRandom.NextFloat() * Math.PI * 2;
			var phaseB = shapeRandom.NextFloat() * Math.PI * 2;
			var open = interiorFeature != PlannedFeature.Enclosed;
			var gateHalfAngle = open
				? site.GateHalfAngle > 0 ? site.GateHalfAngle : 0.22 + 2.0 / Math.Max(site.RadiusX, site.RadiusY)
				: 0;
			var ringThickness = 3.2;
			var plateauFootprint = new List<PlanPoint>();

			for (var y = site.Center.Y - site.RadiusY - 2; y <= site.Center.Y + site.RadiusY + 2; y++)
			{
				for (var x = site.Center.X - site.RadiusX - 2; x <= site.Center.X + site.RadiusX + 2; x++)
				{
					if (!plan.Contains(x, y))
						continue;

					var dx = x - site.Center.X;
					var dy = y - site.Center.Y;
					var angle = Math.Atan2(dy, dx);
					var localAngle = angle - site.Rotation;
					var edgeNoise = 1 + 0.09 * Math.Sin(localAngle * 3 + phaseA) + 0.05 * Math.Sin(localAngle * 5 + phaseB);
					var normalized = Math.Sqrt(
						dx * dx / (double)(site.RadiusX * site.RadiusX) +
						dy * dy / (double)(site.RadiusY * site.RadiusY));
					if (!open || interiorFeature == PlannedFeature.Home)
					{
						if (normalized <= edgeNoise) plateauFootprint.Add(new PlanPoint(x, y));
						continue;
					}
					var averageRadius = (site.RadiusX + site.RadiusY) / 2.0;
					var innerEdge = edgeNoise - ringThickness / averageRadius;
					var inGate = false;
					if (open)
						foreach (var gateAngle in site.GateAngles)
							if (AngularDistance(angle, gateAngle) <= gateHalfAngle)
							{
								inGate = true;
								break;
							}

					var openToBorder = false;
					foreach (var borderAngle in site.BorderAngles)
						if (AngularDistance(angle, borderAngle) <= 1.25)
						{
							openToBorder = true;
							break;
						}

					if (normalized >= innerEdge && normalized <= edgeNoise && !inGate && !openToBorder)
						plan.SetTerrain(x, y, PlannedTerrain.Mountain);
					else if (normalized < innerEdge - 0.04)
					{
						plan.AddFeature(x, y, interiorFeature);
						if (homeOwner >= 0)
							plan.SetHomeOwner(x, y, homeOwner);
					}
					else if (inGate && !openToBorder && normalized <= edgeNoise)
						plan.AddFeature(x, y, PlannedFeature.Entrance);
				}
			}
			if (interiorFeature == PlannedFeature.Home)
				return MountainPlateauPlanner.AddHome(plan, plateauFootprint, site.Center, homeOwner);
			return open || MountainPlateauPlanner.AddIsland(plan, plateauFootprint, site.Rotation);
		}

		static void PlaceResources(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			switch (options.Resources)
			{
				case ResourceFieldLayout.Glitter:
					for (var y = 0; y < plan.Height; y++)
						for (var x = 0; x < plan.Width; x++)
							if (CanPlaceResource(plan, x, y) && random.Next(100) < 82)
								plan.AddFeature(x, y, PlannedFeature.Resource);
					break;
				case ResourceFieldLayout.Patches:
					PlaceResourcePatches(plan, options.Players * 2, random);
					break;
			}

			if (options.Resources != ResourceFieldLayout.None)
				foreach (var spawn in plan.Spawns)
					foreach (var side in new[] { -1, 1 })
						for (var dy = -2; dy <= 2; dy++)
							for (var dx = -2; dx <= 2; dx++)
							{
								var x = spawn.X + side * 5 + dx;
								var y = spawn.Y + dy;
								if (!plan.PlateauSurfaces.TryGetValue(new MPos(x, y), out var surface) || surface.Height != 4 || surface.Ramp != 0 || surface.Blocked)
									throw new InvalidOperationException("Starting ore field does not fit on its plateau.");
								plan.AddFeature(x, y, PlannedFeature.Resource);
							}

			if (options.Resources != ResourceFieldLayout.None && !options.FiniteResources)
				PlaceSingletons(plan, PlannedFeature.ResourceGenerator, Math.Max(1, options.Players / 2), random);

			switch (options.TechBuildings)
			{
				case TechBuildingDensity.Sparse:
					PlaceSingletons(plan, PlannedFeature.TechBuilding, Math.Max(1, options.Players / 3), random);
					break;
				case TechBuildingDensity.Dense:
					PlaceTechClusters(plan, Math.Max(1, options.Players / 3), random);
					break;
			}
		}

		static void PlaceResourcePatches(MapPlan plan, int count, MersenneTwister random)
		{
			for (var patch = 0; patch < count; patch++)
			{
				if (!TryFindLandCell(plan, random, out var center))
					return;

				var radiusX = random.Next(3, 9);
				var radiusY = random.Next(3, 9);
				for (var y = center.Y - radiusY; y <= center.Y + radiusY; y++)
					for (var x = center.X - radiusX; x <= center.X + radiusX; x++)
					{
						var normalized =
							(x - center.X) * (x - center.X) / (double)(radiusX * radiusX) +
							(y - center.Y) * (y - center.Y) / (double)(radiusY * radiusY);
						if (normalized <= 1 && CanPlaceResource(plan, x, y) && random.Next(100) < 88)
							plan.AddFeature(x, y, PlannedFeature.Resource);
					}
			}
		}

		static bool CanPlaceResource(MapPlan plan, int x, int y)
		{
			return plan.IsTraversable(x, y) &&
				!plan.HasFeature(x, y, PlannedFeature.BuildClearance | PlannedFeature.Enclosed | PlannedFeature.Entrance);
		}

		static void PlaceSingletons(MapPlan plan, PlannedFeature feature, int count, MersenneTwister random)
		{
			for (var i = 0; i < count; i++)
				for (var attempt = 0; attempt < 500; attempt++)
				{
					if (!TryFindLandCell(plan, random, out var point)) break;
					if (!CanPlaceEconomyBuilding(plan, point.X, point.Y)) continue;
					plan.AddFeature(point.X, point.Y, feature);
					break;
				}
		}

		static void PlaceTechClusters(MapPlan plan, int sites, MersenneTwister random)
		{
			for (var site = 0; site < sites; site++)
			{
				var center = default(PlanPoint);
				var found = false;
				for (var attempt = 0; attempt < 500 && !found; attempt++)
					found = TryFindLandCell(plan, random, out center) && CanPlaceEconomyBuilding(plan, center.X, center.Y);
				if (!found) return;

				var target = random.Next(4, 7);
				plan.AddFeature(center.X, center.Y, PlannedFeature.TechBuilding);
				var placed = 1;
				for (var attempt = 0; attempt < 50 && placed < target; attempt++)
				{
					var x = center.X + random.Next(-3, 4);
					var y = center.Y + random.Next(-3, 4);
					if (!CanPlaceEconomyBuilding(plan, x, y) || plan.HasFeature(x, y, PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator))
						continue;

					plan.AddFeature(x, y, PlannedFeature.TechBuilding);
					placed++;
				}
				if (placed < target) PlaceSingletons(plan, PlannedFeature.TechBuilding, target - placed, random);
			}
		}

		static bool CanPlaceEconomyBuilding(MapPlan plan, int x, int y)
		{
			// Oil derricks occupy 2x2 CPos cells. Protect the entire footprint and
			// its approach, not just the anchor, from slopes and plateau edges.
			var cell = MountainPlateauPlanner.Cell(x, y);
			for (var dy = -3; dy <= 3; dy++)
				for (var dx = -3; dx <= 3; dx++)
				{
					if (!plan.IsTraversable(x + dx, y + dy) || plan.PlateauSurfaces.ContainsKey(new MPos(x + dx, y + dy)) ||
						plan.HasFeature(x + dx, y + dy, PlannedFeature.Home | PlannedFeature.Enclosed | PlannedFeature.Entrance)) return false;
					var other = MountainPlateauPlanner.Cell(x + dx, y + dy);
					if (Math.Abs(cell.X - other.X) <= 1 && Math.Abs(cell.Y - other.Y) <= 1 &&
						plan.HasFeature(x + dx, y + dy, PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator)) return false;
				}
			return true;
		}

		static bool TryFindLandCell(MapPlan plan, MersenneTwister random, out PlanPoint point)
		{
			for (var attempt = 0; attempt < 500; attempt++)
			{
				var x = random.Next(3, plan.Width - 3);
				var y = random.Next(3, plan.Height - 3);
				if (!plan.IsTraversable(x, y) ||
					plan.HasFeature(x, y, PlannedFeature.Home | PlannedFeature.Enclosed | PlannedFeature.Entrance))
					continue;

				point = new PlanPoint(x, y);
				return true;
			}

			point = default;
			return false;
		}

		static bool Validate(MapPlan plan, MapGenerationOptions options)
		{
			var reachable = FindReachable(plan, plan.Spawns[0]);
			foreach (var spawn in plan.Spawns)
				if (!reachable[spawn.X + spawn.Y * plan.Width])
					return false;

			foreach (var spawn in plan.Spawns)
				for (var dx = -2; dx <= 2; dx++)
					for (var dy = -2; dy <= 2; dy++)
					{
						var p = MountainPlateauPlanner.Point(MountainPlateauPlanner.Cell(spawn.X, spawn.Y) + new CVec(dx, dy));
						if (!plan.PlateauSurfaces.TryGetValue(p, out var surface) || surface.Height != 4 || surface.Ramp != 0 || surface.Blocked ||
							plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator)) return false;
					}

			foreach (var expansion in plan.ExpansionCenters)
				if (!reachable[expansion.X + expansion.Y * plan.Width])
					return false;

			foreach (var pair in plan.PlateauSurfaces)
				if (!pair.Value.Blocked && !reachable[pair.Key.U + pair.Key.V * plan.Width]) return false;

			if (plan.ExpansionCenters.Count != plan.SymmetrySlotCount * options.ExpansionsPerPlayer)
				return false;

			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (options.FiniteResources && plan.HasFeature(x, y, PlannedFeature.ResourceGenerator))
						return false;
					if (options.Resources == ResourceFieldLayout.None && plan.HasFeature(x, y, PlannedFeature.Resource))
						return false;
				}

			plan.ValidationMessages.Add($"All {options.Players} player spawns are connected and have a clear, flat height-4 construction area.");
			if (options.Resources != ResourceFieldLayout.None)
				plan.ValidationMessages.Add("Every start has two complete 25-cell ore fields on its plateau.");
			plan.ValidationMessages.Add($"All {plan.ExpansionCenters.Count} expansion valleys are reachable.");
			plan.ValidationMessages.Add($"All walkable cells of {plan.Plateaus.Count} filled plateaus are height-connected; each has one three-wide, four-level ascent.");
			if (options.FiniteResources)
				plan.ValidationMessages.Add("Finite resources validated: no resource generators were placed.");
			return true;
		}

		public static bool[] FindReachable(MapPlan plan, PlanPoint start)
		{
			if (plan.Plateaus.Count > 0) return MountainPlateauPlanner.Reachable(plan, start);
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
					if (!plan.IsTraversable(x, y))
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
					if (plan.Contains(x, y) && DistanceSquared(center, new PlanPoint(x, y)) <= radius * radius)
						paint(x, y);
		}

		static int DistanceSquared(PlanPoint a, PlanPoint b)
		{
			var dx = a.X - b.X;
			var dy = a.Y - b.Y;
			return dx * dx + dy * dy;
		}

		static double AngularDistance(double a, double b)
		{
			var difference = Math.Abs(a - b) % (Math.PI * 2);
			return difference > Math.PI ? Math.PI * 2 - difference : difference;
		}
	}
}
