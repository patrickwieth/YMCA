using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed record PlainsOilCluster(PlanPoint Target, PlanPoint[] Towers);

	static class PlainsOilClusters
	{
		static readonly CVec[] Offsets = { new(0, 0), new(3, 0), new(0, 3), new(3, 3) };

		public static bool Place(MapPlan plan, TechBuildingDensity density, bool glitter, MersenneTwister random)
		{
			if (density == TechBuildingDensity.None) return true;
			var count = density == TechBuildingDensity.Dense ? 4 : 3;
			var distances = plan.Spawns.Select(s => StartBalanceMetrics.Distances(plan, s)).ToArray();
			var band = Math.Max(12, Math.Min(plan.Width, plan.Height) / 8);
			foreach (var target in Targets(plan))
			{
				var goal = MountainPlateauPlanner.Cell(target.X, target.Y);
				var candidates = new List<(PlanPoint Point, int Score)>();
				for (var y = 2; y < plan.Height - 2; y++) for (var x = 2; x < plan.Width - 2; x++)
				{
					if (Math.Min(Math.Min(x, plan.Width - 1 - x), Math.Min(y, plan.Height - 1 - y)) > band) continue;
					var cell = MountainPlateauPlanner.Cell(x, y);
					var score = (cell - goal).LengthSquared;
					if (score > Math.Max(24, Math.Min(plan.Width, plan.Height) / 5) * Math.Max(24, Math.Min(plan.Width, plan.Height) / 5)) continue;
					if (distances.Any(d => d[x + y * plan.Width] >= 0 && d[x + y * plan.Width] < 18)) continue;
					if (distances.Count(d => d[x + y * plan.Width] >= 0) < 2) continue;
					if (plan.OilClusters.Any(c => (MountainPlateauPlanner.Cell(c.Towers[0].X, c.Towers[0].Y) - cell).LengthSquared < 14 * 14)) continue;
					candidates.Add((new PlanPoint(x, y), score));
				}
				PlanPoint[] chosen = null;
				var firstRotation = random.Next(4);
				foreach (var candidate in candidates.OrderBy(c => c.Score).ThenBy(c => c.Point.X).ThenBy(c => c.Point.Y))
				{
					var origin = MountainPlateauPlanner.Cell(candidate.Point.X, candidate.Point.Y);
					for (var rotation = 0; rotation < 4; rotation++)
					{
						CVec Rotate(CVec p)
						{
							for (var i = 0; i < (rotation + firstRotation) % 4; i++) p = new CVec(-p.Y, p.X);
							return p;
						}
						var towers = Offsets.Take(count).Select(d => MountainPlateauPlanner.Point(origin + Rotate(d)))
							.Select(p => new PlanPoint(p.U, p.V)).ToArray();
						if (towers.All(p => SymmetricOilPlacement.CanPlace(plan, p, glitter) &&
							distances.Count(d => d[p.X + p.Y * plan.Width] >= 18) >= 2 &&
							!distances.Any(d => d[p.X + p.Y * plan.Width] >= 0 && d[p.X + p.Y * plan.Width] < 18)))
						{ chosen = towers; break; }
					}
					if (chosen != null) break;
				}
				if (chosen == null) return false;
				foreach (var tower in chosen)
				{
					var origin = MountainPlateauPlanner.Cell(tower.X, tower.Y);
					for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++)
					{
						var p = MountainPlateauPlanner.Point(origin + new CVec(x, y));
						plan.RemoveFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource);
					}
					plan.AddFeature(tower.X, tower.Y, PlannedFeature.TechBuilding);
				}
				plan.OilClusters.Add(new PlainsOilCluster(target, chosen));
			}
			return true;
		}

		public static void Validate(MapPlan plan, TechBuildingDensity density)
		{
			var count = density == TechBuildingDensity.None ? 0 : density == TechBuildingDensity.Dense ? 4 : 3;
			var goals = count == 0 ? Array.Empty<PlanPoint>() : Targets(plan);
			if (plan.OilClusters.Count != goals.Length || !plan.OilClusters.Select(c => c.Target).SequenceEqual(goals))
				throw new InvalidOperationException("Plains oil cluster targets or count changed.");
			var distances = plan.Spawns.Select(s => StartBalanceMetrics.Distances(plan, s)).ToArray();
			var occupied = new HashSet<CPos>(); var anchors = new HashSet<PlanPoint>();
			foreach (var cluster in plan.OilClusters)
			{
				if (cluster.Towers.Length != count) throw new InvalidOperationException("Incorrect Plains oil cluster size.");
				foreach (var tower in cluster.Towers)
				{
					if (!anchors.Add(tower) || !plan.HasFeature(tower.X, tower.Y, PlannedFeature.TechBuilding) ||
						distances.Any(d => d[tower.X + tower.Y * plan.Width] < 18))
						throw new InvalidOperationException("Plains oil is missing, unreachable or too close to a start.");
					if (Math.Min(Math.Min(tower.X, plan.Width - 1 - tower.X), Math.Min(tower.Y, plan.Height - 1 - tower.Y)) > Math.Max(12, Math.Min(plan.Width, plan.Height) / 8) + 6)
						throw new InvalidOperationException("Plains oil cluster left the outer band.");
					var cell = MountainPlateauPlanner.Cell(tower.X, tower.Y);
					for (var y = -1; y <= 2; y++) for (var x = -1; x <= 2; x++)
					{
						var c = cell + new CVec(x, y); var p = MountainPlateauPlanner.Point(c);
						if (!plan.ContainsPlayable(p.U, p.V) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land || plan.PlateauSurfaces.ContainsKey(p) || plan.StrategicReservations.Contains(p))
							throw new InvalidOperationException("Plains oil lost its native footprint/access margin.");
						if (x >= 0 && x <= 1 && y >= 0 && y <= 1 && (!occupied.Add(c) || plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.BuildClearance)))
							throw new InvalidOperationException("Plains oil overlaps terrain reservations, resources or another tower.");
					}
				}
			}
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				if (plan.HasFeature(x, y, PlannedFeature.TechBuilding) && !anchors.Contains(new PlanPoint(x, y)))
					throw new InvalidOperationException("Plains retained an unclustered oil derrick.");
		}

		internal static PlanPoint[] Targets(MapPlan plan)
		{
			const int inset = 7;
			if (plan.Spawns.Count == 2)
			{
				var corners = new[] { new PlanPoint(inset, inset), new PlanPoint(plan.Width - 1 - inset, inset),
					new PlanPoint(plan.Width - 1 - inset, plan.Height - 1 - inset), new PlanPoint(inset, plan.Height - 1 - inset) };
				int Clearance(PlanPoint p) => plan.Spawns.Min(s => (MountainPlateauPlanner.Cell(s.X, s.Y) - MountainPlateauPlanner.Cell(p.X, p.Y)).LengthSquared);
				var pair = Math.Min(Clearance(corners[0]), Clearance(corners[2])) >= Math.Min(Clearance(corners[1]), Clearance(corners[3])) ? 0 : 1;
				return new[] { corners[pair], corners[pair + 2] };
			}
			double Perimeter(PlanPoint p)
			{
				var x = 2.0 * p.X / (plan.Width - 1) - 1; var y = 2.0 * p.Y / (plan.Height - 1) - 1;
				var scale = Math.Max(Math.Abs(x), Math.Abs(y)); x /= scale; y /= scale;
				if (y <= -0.999999) return (x + 1) / 2;
				if (x >= 0.999999) return 1 + (y + 1) / 2;
				if (y >= 0.999999) return 2 + (1 - x) / 2;
				return 3 + (1 - y) / 2;
			}
			var order = plan.Spawns.Select(Perimeter).OrderBy(p => p).ToArray();
			return order.Select((p, i) =>
			{
				var t = ((p + (i + 1 < order.Length ? order[i + 1] : order[0] + 4)) / 2) % 4;
				var segment = (int)t; var fraction = t - segment;
				var x = segment == 0 ? fraction : segment == 1 ? 1 : segment == 2 ? 1 - fraction : 0;
				var y = segment == 0 ? 0 : segment == 1 ? fraction : segment == 2 ? 1 : 1 - fraction;
				return new PlanPoint(inset + (int)Math.Round(x * (plan.Width - 1 - 2 * inset)), inset + (int)Math.Round(y * (plan.Height - 1 - 2 * inset)));
			}).ToArray();
		}
	}
}
