using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// Forward multi-goal A*: ordinary movement costs/layers come from the engine's
	// public graph API. Budget exhaustion falls back, never means "unreachable".
	static class SafeRangePathSearch
	{
		internal static bool TrySearch(CPos source, HashSet<CPos> targets,
			Func<CPos, IEnumerable<GraphConnection>> edges, Func<CPos, int> estimate,
			int budget, out List<CPos> path)
		{
			path = new List<CPos>();
			var cost = new Dictionary<CPos, int> { [source] = 0 };
			var parent = new Dictionary<CPos, CPos> { [source] = source };
			var queue = new System.Collections.Generic.PriorityQueue<(CPos Cell, int Cost), int>();
			queue.Enqueue((source, 0), estimate(source));
			var expanded = 0;
			while (queue.TryDequeue(out var current, out _))
			{
				if (cost[current.Cell] != current.Cost) continue;
				if (targets.Contains(current.Cell))
				{
					var cell = current.Cell;
					while (true)
					{
						path.Add(cell);
						if (parent[cell] == cell) return true;
						cell = parent[cell];
					}
				}
				if (++expanded > budget) return false;
				foreach (var edge in edges(current.Cell))
				{
					if (edge.Cost < 0) continue;
					var next = current.Cost + edge.Cost;
					if (cost.TryGetValue(edge.Destination, out var old) && old <= next) continue;
					cost[edge.Destination] = next; parent[edge.Destination] = current.Cell;
					queue.Enqueue((edge.Destination, next), next + estimate(edge.Destination));
				}
			}
			return true;
		}

		public static bool TryFind(Actor self, CPos source, CPos[] targets, BlockedByActor check,
			Actor ignoreActor, bool laneBias, out List<CPos> path)
		{
			var world = self.World; var map = world.Map;
			var locomotor = self.Trait<Mobile>().Locomotor;
			var goals = targets.Where(c => PathSearch.CellAllowsMovement(world, locomotor, c, null) &&
				locomotor.MovementCostToEnterCell(self, c, check, ignoreActor, true) != PathGraph.MovementCostForUnreachableCell).ToHashSet();
			path = new List<CPos>();
			if (goals.Count == 0 || !PathSearch.CellAllowsMovement(world, locomotor, source, null)) return true;
			var minX = goals.Min(c => c.X); var maxX = goals.Max(c => c.X);
			var minY = goals.Min(c => c.Y); var maxY = goals.Max(c => c.Y);
			var distance = PathSearch.DefaultCostEstimator(locomotor);
			int Estimate(CPos cell) => distance(cell, new CPos(Math.Clamp(cell.X, minX, maxX), Math.Clamp(cell.Y, minY, maxY))) * 125 / 100;
			using var storage = PathSearch.ToTargetCell(world, locomotor, self, new[] { source }, goals.First(), check, 125,
				ignoreActor: ignoreActor, laneBias: laneBias);
			var graph = storage.Graph;
			IEnumerable<GraphConnection> Edges(CPos cell)
			{
				// The engine's directed-neighbor pruning assumes unrestricted diagonals.
				// Disable that pruning, storing actual parents in our separate search table.
				graph[cell] = new CellInfo(CellStatus.Open, 0, 0, cell);
				foreach (var edge in graph.GetConnections(cell, goals.Contains))
				{
					var to = edge.Destination;
					if (cell.Layer == 0 && to.Layer == 0 && Math.Abs(cell.X - to.X) == 1 && Math.Abs(cell.Y - to.Y) == 1)
					{
						bool Unsafe(CPos corner) => !map.Contains(corner) || locomotor.MovementCostForCell(corner) == PathGraph.MovementCostForUnreachableCell ||
							Math.Abs(map.Height[corner] - map.Height[cell]) > 1 || Math.Abs(map.Height[corner] - map.Height[to]) > 1;
						if (Unsafe(new CPos(cell.X, to.Y)) || Unsafe(new CPos(to.X, cell.Y))) continue;
					}
					yield return edge;
				}
			}
			var complete = TrySearch(source, goals, Edges, Estimate, 2048, out path);
			TerrainBuildMetrics.Count(map, complete ? "range-search-complete" : "range-search-fallback", 1);
			return complete;
		}
	}
}
