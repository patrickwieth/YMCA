using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class StrategicCheckpointPlanner
	{
		public static void Place(MapPlan plan, MapGenerationOptions options)
		{
			if (options.Mode != GeneratedMapMode.Strategic)
				return;

			if (options.Preset == MapGenerationPreset.WaterBasins)
			{
				WaterBasinsStrategicPlanner.Validate(plan, options);
				return;
			}

			if (plan.StrategicRoute.Count != 0)
			{
				ValidateReservedFront(plan, options);
				return;
			}
			// Two Sides already groups starts by river bank. A geometric projection
			// can split along the river instead, putting both teams on each bank.
			if (options.Preset != MapGenerationPreset.TwoSides)
				OrderSpawns(plan.Spawns, options.Players);
			var firstTeamPlayers = (options.Players + 1) / 2;
			var firstAnchor = FindTeamAnchor(plan, plan.Spawns.Take(firstTeamPlayers));
			var secondAnchor = FindTeamAnchor(plan, plan.Spawns.Skip(firstTeamPlayers));
			var path = FindPath(plan, firstAnchor, secondAnchor);
			if (path.Count == 0)
				throw new InvalidOperationException("Could not find a traversable route between the two Strategic team areas.");

			var checkpointCount = (Math.Min(plan.Width, plan.Height) - 2 * plan.PlayableInset) switch
			{
				<= 96 => 3,
				<= 144 => 4,
				_ => 5,
			};

			var centerIndex = -1;
			if (options.Preset == MapGenerationPreset.TwoSides && checkpointCount % 2 == 1)
			{
				var center = TwoSidesObjectiveCenter(plan);
				var firstHalf = FindPath(plan, firstAnchor, center);
				var secondHalf = FindPath(plan, center, secondAnchor);
				if (firstHalf.Count == 0 || secondHalf.Count == 0)
					throw new InvalidOperationException("Two Sides causeway center is not reachable from both banks.");
				path = firstHalf.Concat(secondHalf.Skip(1)).ToList();
				if (path.Distinct().Count() != path.Count)
					throw new InvalidOperationException("Two Sides center route doubles back on itself.");
				centerIndex = firstHalf.Count - 1;
			}

			var preservedIndices = options.Preset == MapGenerationPreset.TwoSides
				? SelectUnmodifiedObjectives(plan, path, checkpointCount, centerIndex) : null;
			var usedPathIndices = new List<int>();
			for (var hierarchy = 0; hierarchy < checkpointCount; hierarchy++)
			{
				var fraction = (hierarchy + 1.0) / (checkpointCount + 1.0);
				var targetIndex = (int)Math.Round(fraction * (path.Count - 1));
				var selectedIndex = preservedIndices != null ? preservedIndices[hierarchy] :
					FindCheckpointIndex(plan, path, targetIndex, usedPathIndices);
				if (selectedIndex < 0)
					throw new InvalidOperationException($"Could not place Strategic checkpoint {hierarchy + 1} of {checkpointCount}.");

				usedPathIndices.Add(selectedIndex);
				if (options.Preset != MapGenerationPreset.TwoSides)
					PrepareCheckpointArea(plan, path[selectedIndex]);
				plan.StrategicCheckpoints.Add(new PlanCheckpoint(path[selectedIndex], hierarchy));
			}

			plan.StrategicCheckpoints.Sort((a, b) => a.Hierarchy.CompareTo(b.Hierarchy));
			plan.ValidationMessages.Add(
				$"Strategic route validated with {checkpointCount} ordered checkpoints and {path.Count} traversable cells between team areas.");
		}

		internal static void OrderSpawns(List<PlanPoint> spawns, int playerCount)
		{
			var vacant = spawns.Skip(playerCount).ToArray();
			var firstTeamPlayers = (playerCount + 1) / 2;
			var projections = new Func<PlanPoint, int>[]
			{
				point => point.X,
				point => point.Y,
				point => point.X + point.Y,
				point => point.X - point.Y,
			};
			PlanPoint[] bestOrder = null;
			var bestSeparation = double.MinValue;
			foreach (var projection in projections)
			{
				var ordered = spawns.Take(playerCount).OrderBy(projection).ThenBy(point => point.X).ThenBy(point => point.Y).ToArray();
				var firstX = ordered.Take(firstTeamPlayers).Average(point => point.X);
				var firstY = ordered.Take(firstTeamPlayers).Average(point => point.Y);
				var secondX = ordered.Skip(firstTeamPlayers).Average(point => point.X);
				var secondY = ordered.Skip(firstTeamPlayers).Average(point => point.Y);
				var dx = firstX - secondX;
				var dy = firstY - secondY;
				var separation = 4 * dx * dx + dy * dy;
				if (separation > bestSeparation)
				{
					bestSeparation = separation;
					bestOrder = ordered;
				}
			}

			spawns.Clear();
			spawns.AddRange(bestOrder);
			spawns.AddRange(vacant);
		}

		// Called after home/ramp placement, BEFORE expansion mountains and economy.
		// Reserving the front first prevents later terrain from displacing objectives
		// into unrelated valleys. Nothing is flattened to rescue a failed layout.
		internal static bool ReserveFront(MapPlan plan, MapGenerationOptions options, bool reserveCorridor = true)
		{
			var split = (options.Players + 1) / 2;
			var start = FindTeamAnchor(plan, plan.Spawns.Take(split));
			var end = FindTeamAnchor(plan, plan.Spawns.Skip(split));
			// Plains have many equal-length native-grid paths. Keep the front near
			// the team axis instead of reserving an arbitrary map-edge detour.
			var path = options.Preset == MapGenerationPreset.Arabia
				? FindWideValleyPath(plan, start, end) : FindPath(plan, start, end);
			var size = Math.Min(plan.Width, plan.Height) - 2 * plan.PlayableInset;
			var count = size <= 96 ? 3 : size <= 144 ? 4 : 5;
			var selected = SelectFrontObjectives(plan, path, count);
			if (selected == null) return false;
			plan.StrategicRoute.AddRange(path);
			foreach (var p in reserveCorridor ? path : Enumerable.Empty<PlanPoint>())
				for (var y = -3; y <= 3; y++)
					for (var x = -3; x <= 3; x++)
					{
						var c = new MPos(p.X + x, p.Y + y);
						if (x * x + y * y <= 9 && plan.Contains(c.U, c.V) && plan.TerrainAt(c.U, c.V) == PlannedTerrain.Land &&
							!plan.PlateauSurfaces.ContainsKey(c) && (plan.PreserveObjectiveTerrain || !plan.HasFeature(c.U, c.V, PlannedFeature.Home))) plan.StrategicReservations.Add(c);
					}
			for (var i = 0; i < selected.Count; i++)
			{
				plan.StrategicCheckpoints.Add(new PlanCheckpoint(path[selected[i]], i));
				if (!plan.PreserveObjectiveTerrain) continue;
				var origin = MountainPlateauPlanner.Cell(path[selected[i]].X, path[selected[i]].Y);
				for (var y = -1; y <= 3; y++) for (var x = -1; x <= 2; x++)
				{
					var p = MountainPlateauPlanner.Point(origin + new CVec(x, y));
					if (plan.Contains(p.U, p.V) && plan.TerrainAt(p.U, p.V) == PlannedTerrain.Land) plan.StrategicReservations.Add(p);
				}
			}
			return true;
		}

		static List<int> SelectFrontObjectives(MapPlan plan, List<PlanPoint> path, int count)
		{
			var candidates = Enumerable.Range(0, path.Count).Where(i => IsValidCheckpointCell(plan, path[i])).ToArray();
			if (candidates.Length < count) return null;
			List<int> best = null;
			var bestCost = double.PositiveInfinity;
			foreach (var first in Enumerable.Range(0, candidates.Length))
			{
				var costs = new double[count, candidates.Length];
				var previous = new int[count, candidates.Length];
				// Optimize the whole ordered chain: greedy choices can consume the last
				// usable pad before the final objective, especially with uneven teams.
				for (var i = 0; i < count; i++)
				{
					var target = (i + 1.0) * (path.Count - 1) / (count + 1);
					for (var j = 0; j < candidates.Length; j++)
					{
						var penalty = Math.Pow(candidates[j] - target, 2);
						costs[i, j] = i == 0 && j == first ? penalty : double.PositiveInfinity;
						previous[i, j] = -1;
						if (i == 0) continue;
						for (var k = 0; k < j; k++)
							if (candidates[j] - candidates[k] >= 8 && DistanceSquared(path[candidates[j]], path[candidates[k]]) >= 64 &&
								costs[i - 1, k] + penalty < costs[i, j])
							{ costs[i, j] = costs[i - 1, k] + penalty; previous[i, j] = k; }
					}
				}
				foreach (var end in Enumerable.Range(0, candidates.Length).OrderBy(j => costs[count - 1, j]))
				{
					if (costs[count - 1, end] >= bestCost) break;
					if (!HomesFaceEnds(plan, path[candidates[first]], path[candidates[end]])) continue;
					var last = end;
					var selected = new List<int>();
					for (var i = count - 1; i >= 0; i--) { selected.Add(candidates[last]); last = previous[i, last]; }
					selected.Reverse();
					if (selected.Any(i => selected.Any(j => i != j && DistanceSquared(path[i], path[j]) < 64))) continue;
					best = selected; bestCost = costs[count - 1, end];
					break;
				}
			}
			return best;
		}

		static bool HomesFaceEnds(MapPlan plan, PlanPoint first, PlanPoint last)
		{
			var split = (plan.Spawns.Count + 1) / 2;
			return plan.Spawns.Take(split).All(p => DistanceSquared(p, first) < DistanceSquared(p, last)) &&
				plan.Spawns.Skip(split).All(p => DistanceSquared(p, last) < DistanceSquared(p, first));
		}

		static int DistanceSquared(PlanPoint a, PlanPoint b) => 4 * (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);

		internal static void ValidateReservedFront(MapPlan plan, MapGenerationOptions options)
		{
			foreach (var p in plan.StrategicReservations)
				if (plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land || plan.PlateauSurfaces.ContainsKey(p) ||
					plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding))
					throw new InvalidOperationException("Strategic front reservation was occupied by terrain or economy.");
			var previous = -1;
			foreach (var cp in plan.StrategicCheckpoints)
			{
				var index = plan.StrategicRoute.FindIndex(p => p.Equals(cp.Point));
				if (index <= previous || !IsValidCheckpointCell(plan, cp.Point)) throw new InvalidOperationException("Strategic objectives lost their front order/clearance.");
				previous = index;
			}
			var split = (options.Players + 1) / 2;
			for (var i = 0; i < plan.Spawns.Count; i++)
			{
				var reachable = MountainPlateauPlanner.Reachable(plan, plan.Spawns[i]);
				foreach (var cp in plan.StrategicCheckpoints)
					if (!reachable[cp.Point.X + cp.Point.Y * plan.Width]) throw new InvalidOperationException("A Strategic home cannot reach the complete front.");
				var entry = plan.StrategicCheckpoints[i < split ? 0 : plan.StrategicCheckpoints.Count - 1].Point;
				var opposite = plan.StrategicCheckpoints[i < split ? plan.StrategicCheckpoints.Count - 1 : 0].Point;
				if (DistanceSquared(plan.Spawns[i], entry) >= DistanceSquared(plan.Spawns[i], opposite))
					throw new InvalidOperationException("Strategic home lies at the wrong end of the objective chain.");
			}
			plan.ValidationMessages.Add("Strategic front reserved before mountains/economy; ordered objectives and every home approach validated.");
		}

		static PlanPoint FindTeamAnchor(MapPlan plan, IEnumerable<PlanPoint> spawns)
		{
			var teamSpawns = spawns.ToArray();
			if (teamSpawns.Length == 0)
				throw new InvalidOperationException("Strategic mode requires at least one player on each team.");

			var centerX = teamSpawns.Average(spawn => spawn.X);
			var centerY = teamSpawns.Average(spawn => spawn.Y);
			var best = teamSpawns[0];
			var bestDistance = double.MaxValue;
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (!IsRouteCell(plan, x, y))
						continue;
					var dx = x - centerX;
					var dy = y - centerY;
					// MPos columns are twice as far apart as rows in physical ground space.
					var distance = 4 * dx * dx + dy * dy;
					if (distance < bestDistance)
					{
						bestDistance = distance;
						best = new PlanPoint(x, y);
					}
				}
			return best;
		}

		static List<PlanPoint> FindPath(MapPlan plan, PlanPoint start, PlanPoint destination)
		{
			if (plan.Plateaus.Count > 0) return FindWideValleyPath(plan, start, destination);
			var cellCount = plan.Width * plan.Height;
			var previous = new int[cellCount];
			Array.Fill(previous, -1);
			var startIndex = start.X + start.Y * plan.Width;
			var destinationIndex = destination.X + destination.Y * plan.Width;
			var queue = new Queue<PlanPoint>();
			queue.Enqueue(start);
			previous[startIndex] = startIndex;
			while (queue.Count > 0 && previous[destinationIndex] < 0)
			{
				var current = queue.Dequeue();
				var currentIndex = current.X + current.Y * plan.Width;
				foreach (var neighbor in NativeNeighbors(current))
				{
					var x = neighbor.X;
					var y = neighbor.Y;
					if (!IsRouteCell(plan, x, y) || !SameHeightStep(plan, current, new PlanPoint(x, y)))
						continue;
					var index = x + y * plan.Width;
					if (previous[index] >= 0)
						continue;
					previous[index] = currentIndex;
					queue.Enqueue(new PlanPoint(x, y));
				}
			}

			if (previous[destinationIndex] < 0)
				return new List<PlanPoint>();
			var path = new List<PlanPoint>();
			for (var index = destinationIndex; ; index = previous[index])
			{
				path.Add(new PlanPoint(index % plan.Width, index / plan.Width));
				if (index == startIndex)
					break;
			}
			path.Reverse();
			return path;
		}

		static IEnumerable<PlanPoint> NativeNeighbors(PlanPoint point)
		{
			var cell = MountainPlateauPlanner.Cell(point.X, point.Y);
			foreach (var direction in PlateauTopology.Directions)
			{
				var p = MountainPlateauPlanner.Point(cell + direction);
				yield return new PlanPoint(p.U, p.V);
			}
		}

		static List<PlanPoint> FindWideValleyPath(MapPlan plan, PlanPoint start, PlanPoint destination)
		{
			var count = plan.Width * plan.Height;
			var distance = Enumerable.Repeat(int.MaxValue, count).ToArray();
			var previous = Enumerable.Repeat(-1, count).ToArray();
			var startIndex = start.X + start.Y * plan.Width;
			var endIndex = destination.X + destination.Y * plan.Width;
			var queue = new PriorityQueue<PlanPoint, (int Cost, int Index)>();
			distance[startIndex] = 0;
			queue.Enqueue(start, (0, startIndex));
			while (queue.TryDequeue(out var current, out var priority))
			{
				var index = current.X + current.Y * plan.Width;
				if (priority.Cost != distance[index]) continue;
				if (index == endIndex) break;
				foreach (var next in NativeNeighbors(current))
				{
					if (!IsRouteCell(plan, next.X, next.Y) || !SameHeightStep(plan, current, next)) continue;
					var nextIndex = next.X + next.Y * plan.Width;
					// Prefer valley space that can actually hold checkpoint pads. The
					// shortest geometric route often only follows a narrow ramp throat.
					var axisX = 2.0 * (destination.X - start.X); var axisY = destination.Y - start.Y;
					var cross = 2.0 * (next.X - start.X) * axisY - (next.Y - start.Y) * axisX;
					var deviation = cross * cross / Math.Max(1, axisX * axisX + axisY * axisY);
					// Native grid shortest paths have many equal-cost staircases. Prefer
					// the team axis, not the top map edge as an accidental index tie-break.
					var cost = distance[index] + (IsValidCheckpointCell(plan, next) ? 1000 : 1400) + (int)Math.Min(400, deviation * 4);
					if (cost >= distance[nextIndex]) continue;
					distance[nextIndex] = cost;
					previous[nextIndex] = index;
					queue.Enqueue(next, (cost, nextIndex));
				}
			}
			if (distance[endIndex] == int.MaxValue) return new List<PlanPoint>();
			var path = new List<PlanPoint>();
			for (var index = endIndex; ; index = previous[index])
			{
				path.Add(new PlanPoint(index % plan.Width, index / plan.Width));
				if (index == startIndex) break;
			}
			path.Reverse();
			return path;
		}

		static int FindCheckpointIndex(
			MapPlan plan, IReadOnlyList<PlanPoint> path, int targetIndex, IReadOnlyCollection<int> usedIndices)
		{
			return Enumerable.Range(0, path.Count)
				.OrderBy(index => Math.Abs(index - targetIndex))
				.FirstOrDefault(index => IsValidCheckpointCell(plan, path[index]) &&
					usedIndices.All(used => Math.Abs(used - index) >= 8), -1);
		}

		internal static PlanPoint TwoSidesObjectiveCenter(MapPlan plan)
		{
			var sand = new List<PlanPoint>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (plan.HasFeature(x, y, PlannedFeature.Bottleneck)) sand.Add(new PlanPoint(x, y));
			if (sand.Count == 0) throw new InvalidOperationException("Two Sides has no causeway.");
			var centerX = sand.Average(p => MountainPlateauPlanner.Cell(p.X, p.Y).X);
			var centerY = sand.Average(p => MountainPlateauPlanner.Cell(p.X, p.Y).Y);
			// Compare FCOM's visible center, not its footprint origin, with the
			// crossing center. Its 2x3 footprint has LocalCenterOffset 0,-512,0;
			// 724 world units per native axis project to 64x32 screen pixels.
			double CenterDistance(PlanPoint p)
			{
				var cell = MountainPlateauPlanner.Cell(p.X, p.Y);
				var dx = cell.X + 0.5 - 512 / 1448.0 - centerX;
				var dy = cell.Y + 1 - 512 / 1448.0 - centerY;
				return 4 * (dx - dy) * (dx - dy) + (dx + dy) * (dx + dy);
			}

			foreach (var point in sand.OrderBy(CenterDistance).ThenBy(p => p.Y).ThenBy(p => p.X))
			{
				var origin = MountainPlateauPlanner.Cell(point.X, point.Y);
				var onSand = true;
				for (var y = 0; y < 3; y++)
					for (var x = 0; x < 2; x++)
					{
						var p = MountainPlateauPlanner.Point(origin + new CVec(x, y));
						onSand &= plan.HasFeature(p.U, p.V, PlannedFeature.Bottleneck);
					}
				if (onSand && IsValidCheckpointCell(plan, point) && FitsUnmodifiedCheckpointArea(plan, point)) return point;
			}
			throw new InvalidOperationException("Two Sides cannot fit its central objective entirely on the sand crossing.");
		}

		static int[] SelectUnmodifiedObjectives(MapPlan plan, IReadOnlyList<PlanPoint> path, int count, int centerIndex)
		{
			var candidates = Enumerable.Range(0, path.Count).Where(i => IsValidCheckpointCell(plan, path[i]) &&
				FitsUnmodifiedCheckpointArea(plan, path[i])).ToArray();
			var cost = new double[count, candidates.Length];
			var previous = new int[count, candidates.Length];
			for (var step = 0; step < count; step++)
				for (var i = 0; i < candidates.Length; i++)
				{
					var delta = candidates[i] - (step + 1.0) * (path.Count - 1) / (count + 1);
					cost[step, i] = step == 0 ? delta * delta : double.PositiveInfinity;
					previous[step, i] = -1;
					if (centerIndex >= 0 && step == count / 2 && candidates[i] != centerIndex)
					{
						cost[step, i] = double.PositiveInfinity;
						continue;
					}
					if (step == 0) continue;
					for (var j = 0; j < i; j++)
						if (candidates[i] - candidates[j] >= 8 && DistanceSquared(path[candidates[i]], path[candidates[j]]) >= 64 &&
							cost[step - 1, j] + delta * delta < cost[step, i])
						{
							cost[step, i] = cost[step - 1, j] + delta * delta;
							previous[step, i] = j;
						}
				}
			var end = Enumerable.Range(0, candidates.Length).OrderBy(i => cost[count - 1, i]).FirstOrDefault(-1);
			if (end < 0 || double.IsPositiveInfinity(cost[count - 1, end]))
				throw new InvalidOperationException($"Two Sides needs {count} unmodified objective pads; found {candidates.Length} candidates along {path.Count} route cells.");
			var result = new int[count];
			for (var step = count - 1; step >= 0; step--) { result[step] = candidates[end]; end = previous[step, end]; }
			return result;
		}

		internal static bool FitsUnmodifiedCheckpointArea(MapPlan plan, PlanPoint point)
		{
			// FCOM's native 2x3 footprint, including its passable approach row. Never turn river
			// water into square grass pads or delete ore to accommodate an objective.
			var origin = MountainPlateauPlanner.Cell(point.X, point.Y);
			for (var y = 0; y < 3; y++)
				for (var x = 0; x < 2; x++)
				{
					var p = MountainPlateauPlanner.Point(origin + new CVec(x, y));
					if (!plan.IsTraversable(p.U, p.V) || plan.HasFeature(p.U, p.V,
						PlannedFeature.Spawn | PlannedFeature.BuildClearance | PlannedFeature.Resource |
						PlannedFeature.RichResource | PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding))
						return false;
				}
			return true;
		}

		static bool SameHeightStep(MapPlan plan, PlanPoint from, PlanPoint to)
		{
			var a = plan.PlateauSurfaces.TryGetValue(new MPos(from.X, from.Y), out var first) ? first.Height : 0;
			var b = plan.PlateauSurfaces.TryGetValue(new MPos(to.X, to.Y), out var second) ? second.Height : 0;
			return Math.Abs(a - b) <= 1;
		}

		static bool IsRouteCell(MapPlan plan, int x, int y) => plan.IsTraversable(x, y) &&
			(plan.Plateaus.Count == 0 || !plan.HasFeature(x, y, PlannedFeature.Enclosed) ||
				plan.HasFeature(x, y, PlannedFeature.Entrance));

		static bool IsValidCheckpointCell(MapPlan plan, PlanPoint point)
		{
			if (plan.PreserveObjectiveTerrain)
			{
				if (!FitsUnmodifiedCheckpointArea(plan, point)) return false;
				var origin = MountainPlateauPlanner.Cell(point.X, point.Y);
				for (var y = 0; y < 3; y++) for (var x = 0; x < 2; x++)
				{
					var p = MountainPlateauPlanner.Point(origin + new CVec(x, y));
					if (!plan.Contains(p.U, p.V) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land ||
						plan.HasFeature(p.U, p.V, PlannedFeature.DestructibleBridge | PlannedFeature.Bottleneck)) return false;
				}
			}
			// A checkpoint must not flatten an already reserved closed mountain foot.
			if (plan.RequireClosedPlateaus)
				for (var y = point.Y - 3; y <= point.Y + 3; y++)
					for (var x = point.X - 3; x <= point.X + 3; x++)
						if ((x - point.X) * (x - point.X) + (y - point.Y) * (y - point.Y) <= 9 &&
							plan.PlateauSurfaces.ContainsKey(new MPos(x, y))) return false;
			if (plan.Plateaus.Count > 0)
				for (var dy = -3; dy <= 3; dy++)
					for (var dx = -3; dx <= 3; dx++)
						if (dx * dx + dy * dy <= 9 && plan.Contains(point.X + dx, point.Y + dy) &&
							(plan.HasFeature(point.X + dx, point.Y + dy, PlannedFeature.Enclosed | PlannedFeature.Home) ||
							plan.PlateauSurfaces.ContainsKey(new MPos(point.X + dx, point.Y + dy)))) return false;
			if (!plan.Contains(point.X, point.Y) ||
				plan.HasFeature(point.X, point.Y, PlannedFeature.Spawn | PlannedFeature.BuildClearance))
				return false;
			var margin = plan.PlayableInset + 4;
			return point.X >= margin && point.Y >= margin && point.X < plan.Width - margin && point.Y < plan.Height - margin;
		}

		static void PrepareCheckpointArea(MapPlan plan, PlanPoint point)
		{
			for (var y = point.Y - 3; y <= point.Y + 3; y++)
				for (var x = point.X - 3; x <= point.X + 3; x++)
				{
					var dx = x - point.X;
					var dy = y - point.Y;
					if (!plan.Contains(x, y) || dx * dx + dy * dy > 9)
						continue;
					plan.SetTerrain(x, y, PlannedTerrain.Land);
					plan.RemoveFeature(x, y, PlannedFeature.Resource | PlannedFeature.RichResource |
						PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding);
				}
		}
	}
}
