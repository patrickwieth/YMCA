using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Uses the standard pathfinder, but routes around unsafe cliff and shore corners on Rubberduck maps.")]
	public class PlateauPathFinderInfo : PathFinderInfo
	{
		public override object Create(ActorInitializer init) => new PlateauPathFinder(init.Self);
	}

	public class PlateauPathFinder : PathFinder, IPathFinder
	{
		readonly Map map;
		readonly bool enabled;

		public PlateauPathFinder(Actor self) : base(self)
		{
			map = self.World.Map;
			// Flat shores need the same corner protection: ships otherwise cut across
			// land while following diagonal paths on height-zero Archipelago maps.
			enabled = map.Rules.TerrainInfo.Id.StartsWith("RUBBERDUCK-", StringComparison.Ordinal);
		}

		List<CPos> IPathFinder.FindPathToTargetCell(Actor self, IEnumerable<CPos> sources, CPos target,
			BlockedByActor check, Func<CPos, int> customCost, Actor ignoreActor, bool laneBias)
		{
			var starts = sources.ToArray();
			return SafePath(self, check, customCost, ignoreActor,
				cost => base.FindPathToTargetCell(self, starts, target, check, cost, ignoreActor, laneBias));
		}

		List<CPos> IPathFinder.FindPathToTargetCells(Actor self, CPos source, IEnumerable<CPos> targets,
			BlockedByActor check, Func<CPos, int> customCost, Actor ignoreActor, bool laneBias)
		{
			var ends = targets.ToArray();
			if (enabled && customCost == null && ends.Length >= 16)
			{
				List<CPos> fastPath = null;
				var complete = TerrainBuildMetrics.Measure(map, "range-search", () =>
					SafeRangePathSearch.TryFind(self, source, ends, check, ignoreActor, laneBias, out fastPath));
				if (complete) return fastPath;
			}
			return SafePath(self, check, customCost, ignoreActor,
				cost => base.FindPathToTargetCells(self, source, ends, check, cost, ignoreActor, laneBias));
		}

		List<CPos> IPathFinder.FindPathToTargetCellByPredicate(Actor self, IEnumerable<CPos> sources, Func<CPos, bool> targetPredicate,
			BlockedByActor check, Func<CPos, int> customCost, Actor ignoreActor, bool laneBias)
		{
			var starts = sources.ToArray();
			return SafePath(self, check, customCost, ignoreActor,
				cost => base.FindPathToTargetCellByPredicate(self, starts, targetPredicate, check, cost, ignoreActor, laneBias));
		}

		List<CPos> SafePath(Actor self, BlockedByActor check, Func<CPos, int> customCost, Actor ignoreActor,
			Func<Func<CPos, int>, List<CPos>> search) =>
			TerrainBuildMetrics.Measure(map, "path-request", () => SafePathCore(self, check, customCost, ignoreActor, search));

		List<CPos> SafePathCore(Actor self, BlockedByActor check, Func<CPos, int> customCost, Actor ignoreActor,
			Func<Func<CPos, int>, List<CPos>> search)
		{
			if (!enabled) return search(customCost);
			var locomotor = self.Trait<Mobile>().Locomotor;
			var avoid = new HashSet<CPos>();
			for (var attempt = 0; attempt < 16; attempt++)
			{
				int Cost(CPos cell) => avoid.Contains(cell) ? PathGraph.PathCostForInvalidPath : customCost?.Invoke(cell) ?? 0;
				var path = TerrainBuildMetrics.Measure(map, "path-search", () => search(avoid.Count == 0 ? customCost : Cost));
				if (path.Count < 2) return path;
				var result = new List<CPos> { path[0] };
				var retry = false;
				// Engine paths are reversed: destination first, source last.
				for (var i = 1; i < path.Count; i++)
				{
					var from = path[i];
					var to = path[i - 1];
					if (from.Layer == 0 && to.Layer == 0 && Math.Abs(from.X - to.X) == 1 && Math.Abs(from.Y - to.Y) == 1)
					{
						var a = new CPos(from.X, to.Y);
						var b = new CPos(to.X, from.Y);
						bool Unsafe(CPos corner) => !map.Contains(corner) ||
							locomotor.MovementCostForCell(corner) == PathGraph.MovementCostForUnreachableCell ||
							Math.Abs(map.Height[corner] - map.Height[from]) > 1 || Math.Abs(map.Height[corner] - map.Height[to]) > 1;
						if (Unsafe(a) || Unsafe(b))
						{
							bool Via(CPos corner) => map.Contains(corner) && Cost(corner) != PathGraph.PathCostForInvalidPath &&
								locomotor.MovementCostToEnterCell(self, from, corner, check, ignoreActor) != PathGraph.MovementCostForUnreachableCell &&
								locomotor.MovementCostToEnterCell(self, corner, to, check, ignoreActor) != PathGraph.MovementCostForUnreachableCell;
							if (Via(a)) result.Add(a);
							else if (Via(b)) result.Add(b);
							else
							{
								// A logical diagonal across a blocked saddle is not a physical route.
								// Exclude an intermediate node and let the standard finder choose a detour.
								var blocked = i > 1 ? to : from;
								if (blocked == path[0] || blocked == path[path.Count - 1] || !avoid.Add(blocked)) return NoPath;
								retry = true;
								break;
							}
						}
					}
					result.Add(from);
				}
				if (!retry) return result;
			}
			return NoPath;
		}
	}
}
