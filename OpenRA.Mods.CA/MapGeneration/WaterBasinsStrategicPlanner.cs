using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Reserve objectives before economy placement. Compare native-grid walking
	// distances from every home, not a shortest path between averaged positions.
	static class WaterBasinsStrategicPlanner
	{
		internal static List<PlanPoint> CreateStarts(MapPlan plan, int players)
		{
			// Circular FFA starts put additional players much nearer the center.
			// Two equally spaced home columns give Strategic teams comparable depth.
			var result = new List<PlanPoint>();
			var split = (players + 1) / 2;
			for (var side = 0; side < 2; side++)
			{
				var count = side == 0 ? split : players - split;
				if (plan.Width < 54 || (count - 1) * 20 > plan.Height - 27)
					throw new ArgumentException("Water Basins needs a larger map for separated Strategic home rows.");
				for (var i = 0; i < count; i++)
					result.Add(new PlanPoint(side == 0 ? 13 : plan.Width - 14,
						(int)Math.Round((plan.Height - 1) / 2.0 + (i - (count - 1) / 2.0) * 20)));
			}
			return result;
		}

		internal static int[] Distances(MapPlan plan, PlanPoint start)
		{
			var distance = Enumerable.Repeat(int.MaxValue, plan.Width * plan.Height).ToArray();
			var queue = new PriorityQueue<CPos, (int Cost, int Index)>();
			var origin = MountainPlateauPlanner.Cell(start.X, start.Y);
			var index = start.X + start.Y * plan.Width;
			distance[index] = 0; queue.Enqueue(origin, (0, index));
			bool Walk(CPos c) { var p = MountainPlateauPlanner.Point(c); return plan.IsTraversable(p.U, p.V); }
			while (queue.TryDequeue(out var c, out var priority))
			{
				var p = MountainPlateauPlanner.Point(c); index = p.U + p.V * plan.Width;
				if (distance[index] != priority.Cost) continue;
				for (var x = -1; x <= 1; x++)
					for (var y = -1; y <= 1; y++)
					{
						if (x == 0 && y == 0) continue;
						var n = c + new CVec(x, y);
						if (!Walk(n) || x != 0 && y != 0 && (!Walk(c + new CVec(x, 0)) || !Walk(c + new CVec(0, y)))) continue;
						var uv = MountainPlateauPlanner.Point(n); var ni = uv.U + uv.V * plan.Width;
						var cost = priority.Cost + (x == 0 || y == 0 ? 1000 : 1414);
						if (cost >= distance[ni]) continue;
						distance[ni] = cost; queue.Enqueue(n, (cost, ni));
					}
			}
			return distance;
		}

		static IEnumerable<MPos> Pad(PlanPoint point)
		{
			var c = MountainPlateauPlanner.Cell(point.X, point.Y);
			// Covers the 2x3 FCOM footprint with a walkable approach margin.
			for (var x = -2; x <= 3; x++)
				for (var y = -2; y <= 4; y++)
					yield return MountainPlateauPlanner.Point(c + new CVec(x, y));
		}

		static bool Fits(MapPlan plan, PlanPoint p) => Pad(p).All(n => plan.Contains(n.U, n.V) &&
			plan.TerrainAt(n.U, n.V) == PlannedTerrain.Land &&
			!plan.HasFeature(n.U, n.V, PlannedFeature.BuildClearance | PlannedFeature.Spawn | PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding));

		static bool Balanced(IEnumerable<int> values)
		{
			var distances = values.ToArray();
			return distances.All(d => d != int.MaxValue) && distances.Min() >= 8000 &&
				distances.Max() <= distances.Min() * 1.20 + 1000;
		}

		public static bool Reserve(MapPlan plan, MapGenerationOptions options)
		{
			StrategicCheckpointPlanner.OrderSpawns(plan.Spawns, options.Players);
			var distances = plan.Spawns.Select(p => Distances(plan, p)).ToArray();
			var split = (options.Players + 1) / 2;
			var count = Math.Min(plan.Width, plan.Height) <= 96 ? 3 : Math.Min(plan.Width, plan.Height) <= 144 ? 4 : 5;
			var candidates = new List<(PlanPoint Point, double Progress, double Spread, double Center)>();
			for (var y = 4; y < plan.Height - 4; y++)
				for (var x = 4; x < plan.Width - 4; x++)
				{
					var p = new PlanPoint(x, y); if (!Fits(plan, p)) continue;
					var ds = distances.Select(d => d[x + y * plan.Width]).ToArray();
					if (ds.Any(d => d == int.MaxValue)) continue;
					var a = ds.Take(split).ToArray(); var b = ds.Skip(split).ToArray();
					var first = a.Average(); var second = b.Average();
					var spread = (a.Max() - a.Min() + b.Max() - b.Min()) / (first + second);
					var dx = 2 * x - (plan.Width - 1); var dy = y - (plan.Height - 1) / 2.0;
					candidates.Add((p, (first - second) / (first + second), spread, dx * dx + dy * dy));
				}
			var selected = new List<PlanPoint>();
			var ranked = Enumerable.Range(0, count).Select(i => candidates
				.OrderBy(c => Math.Abs(c.Progress - (-0.45 + 0.9 * i / (count - 1))) + c.Spread * 2 + c.Center / (plan.Width * plan.Height) * 0.03)
				.ThenBy(c => c.Point.Y).ThenBy(c => c.Point.X).ToArray()).ToArray();
			var searchBudget = 4096;
			bool Select(int i)
			{
				if (i == count) return true;
				if (--searchBudget < 0) return false;
				var branches = 0;
				foreach (var candidate in ranked[i])
				{
					var p = candidate.Point; var index = p.X + p.Y * plan.Width;
					var cell = MountainPlateauPlanner.Cell(p.X, p.Y);
					if (selected.Any(s => { var other = MountainPlateauPlanner.Cell(s.X, s.Y); return Math.Abs(cell.X - other.X) < 6 && Math.Abs(cell.Y - other.Y) < 7; })) continue;
					if (i == 0 && (candidate.Progress >= -0.10 || !Balanced(distances.Take(split).Select(d => d[index])))) continue;
					if (i == count - 1 && (candidate.Progress <= 0.10 || !Balanced(distances.Skip(split).Select(d => d[index])))) continue;
					if (count % 2 == 1 && i == count / 2 && !Balanced(distances.Select(d => d[index]))) continue;
					if (selected.Count > 0)
					{
						var previous = selected[^1].X + selected[^1].Y * plan.Width;
						if (distances.Take(split).Any(d => d[index] <= d[previous]) || distances.Skip(split).Any(d => d[index] >= d[previous])) continue;
					}
					if (i == count - 1)
					{
						var first = selected[0].X + selected[0].Y * plan.Width;
						if (!Balanced(distances.Take(split).Select(d => d[first]).Concat(distances.Skip(split).Select(d => d[index])))) continue;
					}
					selected.Add(p);
					if (Select(i + 1)) return true;
					selected.RemoveAt(selected.Count - 1);
					if (++branches == 16 || searchBudget < 0) break;
				}
				return false;
			}
			if (!Select(0)) return false; // Retry a complete layout; never fill a lake.
			for (var i = 0; i < count; i++)
			{
				plan.StrategicCheckpoints.Add(new PlanCheckpoint(selected[i], i));
				plan.StrategicReservations.UnionWith(Pad(selected[i]));
			}
			return true;
		}

		public static void Validate(MapPlan plan, MapGenerationOptions options)
		{
			var split = (options.Players + 1) / 2;
			var checkpoints = plan.StrategicCheckpoints;
			if (checkpoints.Count == 0 || checkpoints.Any(c => !Fits(plan, c.Point))) throw new InvalidOperationException("Basin objectives need existing dry pads.");
			var distances = plan.Spawns.Select(p => Distances(plan, p)).ToArray();
			int At(int player, int cp) => distances[player][checkpoints[cp].Point.X + checkpoints[cp].Point.Y * plan.Width];
			var entries = Enumerable.Range(0, options.Players).Select(i => At(i, i < split ? 0 : checkpoints.Count - 1)).ToArray();
			if (!Balanced(entries)) throw new InvalidOperationException("Basin home-to-front routes exceed the 20% plus one native-cell tolerance.");
			if (checkpoints.Count % 2 == 1 && !Balanced(Enumerable.Range(0, options.Players).Select(i => At(i, checkpoints.Count / 2))))
				throw new InvalidOperationException("Basin central objective has unequal home routes.");
			for (var player = 0; player < options.Players; player++)
				for (var cp = 1; cp < checkpoints.Count; cp++)
					if (At(player, cp) == int.MaxValue || (player < split ? At(player, cp) <= At(player, cp - 1) : At(player, cp) >= At(player, cp - 1)))
						throw new InvalidOperationException("Basin objective chain doubles back for a home.");
			plan.ValidationMessages.Add("Basin home-to-own-front native walking distances: " + string.Join(", ", entries.Select(d => (d / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))));
		}
	}
}
