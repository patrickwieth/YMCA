using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class StrategicCheckpointPlanner
	{
		static readonly PlanPoint[] CardinalOffsets =
		{
			new PlanPoint(1, 0),
			new PlanPoint(-1, 0),
			new PlanPoint(0, 1),
			new PlanPoint(0, -1),
		};

		public static void Place(MapPlan plan, MapGenerationOptions options)
		{
			if (options.Mode != GeneratedMapMode.Strategic)
				return;

			ArrangeSpawnsIntoTeamAreas(plan, options.Players);
			var firstTeamPlayers = (options.Players + 1) / 2;
			var firstAnchor = FindTeamAnchor(plan, plan.Spawns.Take(firstTeamPlayers));
			var secondAnchor = FindTeamAnchor(plan, plan.Spawns.Skip(firstTeamPlayers));
			var path = FindPath(plan, firstAnchor, secondAnchor);
			if (path.Count == 0)
				throw new InvalidOperationException("Could not find a traversable route between the two Strategic team areas.");

			var checkpointCount = Math.Min(plan.Width, plan.Height) switch
			{
				<= 96 => 3,
				<= 144 => 4,
				_ => 5,
			};

			var usedPathIndices = new List<int>();
			for (var hierarchy = 0; hierarchy < checkpointCount; hierarchy++)
			{
				var fraction = (hierarchy + 1.0) / (checkpointCount + 1.0);
				var targetIndex = (int)Math.Round(fraction * (path.Count - 1));
				var selectedIndex = FindCheckpointIndex(plan, path, targetIndex, usedPathIndices);
				if (selectedIndex < 0)
					throw new InvalidOperationException($"Could not place Strategic checkpoint {hierarchy + 1} of {checkpointCount}.");

				usedPathIndices.Add(selectedIndex);
				PrepareCheckpointArea(plan, path[selectedIndex]);
				plan.StrategicCheckpoints.Add(new PlanCheckpoint(path[selectedIndex], hierarchy));
			}

			plan.StrategicCheckpoints.Sort((a, b) => a.Hierarchy.CompareTo(b.Hierarchy));
			plan.ValidationMessages.Add(
				$"Strategic route validated with {checkpointCount} ordered checkpoints and {path.Count} traversable cells between team areas.");
		}

		static void ArrangeSpawnsIntoTeamAreas(MapPlan plan, int playerCount)
		{
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
				var ordered = plan.Spawns.OrderBy(projection).ThenBy(point => point.X).ThenBy(point => point.Y).ToArray();
				var firstX = ordered.Take(firstTeamPlayers).Average(point => point.X);
				var firstY = ordered.Take(firstTeamPlayers).Average(point => point.Y);
				var secondX = ordered.Skip(firstTeamPlayers).Average(point => point.X);
				var secondY = ordered.Skip(firstTeamPlayers).Average(point => point.Y);
				var dx = firstX - secondX;
				var dy = firstY - secondY;
				var separation = dx * dx + dy * dy;
				if (separation > bestSeparation)
				{
					bestSeparation = separation;
					bestOrder = ordered;
				}
			}

			plan.Spawns.Clear();
			plan.Spawns.AddRange(bestOrder);
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
					var distance = dx * dx + dy * dy;
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
				foreach (var offset in CardinalOffsets)
				{
					var x = current.X + offset.X;
					var y = current.Y + offset.Y;
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
				foreach (var offset in CardinalOffsets)
				{
					var next = new PlanPoint(current.X + offset.X, current.Y + offset.Y);
					if (!IsRouteCell(plan, next.X, next.Y) || !SameHeightStep(plan, current, next)) continue;
					var nextIndex = next.X + next.Y * plan.Width;
					// Prefer valley space that can actually hold checkpoint pads. The
					// shortest geometric route often only follows a narrow ramp throat.
					var cost = distance[index] + (IsValidCheckpointCell(plan, next) ? 1 : 8);
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
			if (plan.Plateaus.Count > 0)
				for (var dy = -3; dy <= 3; dy++)
					for (var dx = -3; dx <= 3; dx++)
						if (dx * dx + dy * dy <= 9 && plan.Contains(point.X + dx, point.Y + dy) &&
							(plan.HasFeature(point.X + dx, point.Y + dy, PlannedFeature.Enclosed | PlannedFeature.Home) ||
							plan.PlateauSurfaces.ContainsKey(new MPos(point.X + dx, point.Y + dy)))) return false;
			if (!plan.Contains(point.X, point.Y) ||
				plan.HasFeature(point.X, point.Y, PlannedFeature.Spawn | PlannedFeature.BuildClearance))
				return false;
			return point.X >= 4 && point.Y >= 4 && point.X < plan.Width - 4 && point.Y < plan.Height - 4;
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
