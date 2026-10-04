using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.TerrainOverrides
{
	// CA precedes Common in mod.yaml. Retain the YAML names so inherited overrides
	// and old saved maps resolve one trait, not an additional reveal/jammer source.
	// Other tilesets instantiate the original Common implementations.
	public sealed class RevealsShroudInfo : OpenRA.Mods.Common.Traits.RevealsShroudInfo
	{
		public override object Create(ActorInitializer init) => init.World.Map.Tileset.StartsWith("RUBBERDUCK-", StringComparison.Ordinal)
			? new RubberduckRevealsShroud(this) : base.Create(init);
	}

	public sealed class CreatesShroudInfo : OpenRA.Mods.Common.Traits.CreatesShroudInfo
	{
		public override object Create(ActorInitializer init) => init.World.Map.Tileset.StartsWith("RUBBERDUCK-", StringComparison.Ordinal)
			? new RubberduckCreatesShroud(this) : base.Create(init);
	}

	public sealed class RubberduckRevealsShroud : OpenRA.Mods.Common.Traits.RevealsShroud
	{
		readonly RubberduckShroudProjection projection = new RubberduckShroudProjection();
		public RubberduckRevealsShroud(RevealsShroudInfo info) : base(info) { }
		protected override void AddCellsToPlayerShroud(Actor self, Player player, PPos[] cells) =>
			base.AddCellsToPlayerShroud(self, player, projection.Cells(self, Info, Range, cells));
	}

	public sealed class RubberduckCreatesShroud : OpenRA.Mods.Common.Traits.CreatesShroud
	{
		readonly RubberduckShroudProjection projection = new RubberduckShroudProjection();
		public RubberduckCreatesShroud(CreatesShroudInfo info) : base(info) { }
		protected override void AddCellsToPlayerShroud(Actor self, Player player, PPos[] cells) =>
			base.AddCellsToPlayerShroud(self, player, projection.Cells(self, Info, Range, cells));
	}

	sealed class RubberduckShroudProjection
	{
		PPos[] previousInput;
		PPos[] physicalCells;
		WPos previousPosition;
		WDist previousRange;
		long previousRevision = -1;

		sealed class RangeCache
		{
			public long Revision = -1;
			public Rectangle Bounds;
			public int RetainedCells;
			public readonly Dictionary<(WPos Source, int Minimum, int Maximum, int Delta), PPos[]> Cells = new();
		}
		static readonly ConditionalWeakTable<Map, RangeCache> RangeCaches = new();

		internal static int CachedRangeCount(Map map) => RangeCaches.TryGetValue(map, out var cache) ? cache.Cells.Count : 0;
		internal static int CachedCellCount(Map map) => RangeCaches.TryGetValue(map, out var cache) ? cache.RetainedCells : 0;

		internal static PPos[] CachedCells(Map map, WPos source, WDist minimum, WDist maximum, int delta)
		{
			var cache = RangeCaches.GetValue(map, _ => new RangeCache());
			var revision = TerrainRenderRevision.Get(map);
			if (cache.Revision != revision || cache.Bounds != map.Bounds)
			{ cache.Cells.Clear(); cache.RetainedCells = 0; cache.Revision = revision; cache.Bounds = map.Bounds; }
			var key = (source, minimum.Length, maximum.Length, delta);
			if (cache.Cells.TryGetValue(key, out var cells)) return cells;
			// Footprint origins are cell centers, shared by nearby infantry and repeated
			// subcell updates. Bound retention; do not cache continuously moving air origins.
			cells = TerrainBuildMetrics.Measure(map, "vision-range", () => CellsInPhysicalRange(map, source, minimum, maximum, delta).Distinct().ToArray());
			const int cellBudget = 1024 * 1024;
			if (cells.Length > cellBudget) return cells;
			if (cache.Cells.Count >= 4096 || cache.RetainedCells + cells.Length > cellBudget)
			{ cache.Cells.Clear(); cache.RetainedCells = 0; }
			cache.Cells.Add(key, cells);
			cache.RetainedCells += cells.Length;
			return cells;
		}
		public PPos[] Cells(Actor self, AffectsShroudInfo info, WDist range, PPos[] input)
		{
			// AffectsShroud passes the same array to each player. Compute once per update,
			// retaining its conditions, modifiers, sharing, movement and source lifecycle.
			var revision = TerrainRenderRevision.Get(self.World.Map);
			if (input.Length != 0 && ReferenceEquals(previousInput, input) && previousPosition == self.CenterPosition &&
				previousRange == range && previousRevision == revision) return physicalCells;
			previousInput = input;
			previousPosition = self.CenterPosition;
			previousRange = range;
			previousRevision = revision;
			if (range <= info.MinRange) return physicalCells = Array.Empty<PPos>();
			var positions = info.Type == VisibilityType.Footprint
				? self.OccupiesSpace.OccupiedCells().Select(c => self.World.Map.CenterOfCell(c.Cell))
				: new[] { self.CenterPosition }.AsEnumerable();
			if (info.Type == VisibilityType.GroundPosition)
				positions = positions.Select(p => p - new WVec(0, 0, self.World.Map.DistanceAboveTerrain(p).Length));
			return physicalCells = TerrainBuildMetrics.Measure(self.World.Map, "vision-project", () => positions.SelectMany(p =>
				info.Type == VisibilityType.Footprint ? CachedCells(self.World.Map, p, info.MinRange, range, info.MaxHeightDelta) :
				CellsInPhysicalRange(self.World.Map, p, info.MinRange, range, info.MaxHeightDelta)).Distinct().ToArray());
		}

		internal static IEnumerable<PPos> CellsInPhysicalRange(Map map, WPos source, WDist minimum, WDist maximum, int maximumHeightDelta)
		{
			// Measure before projection: a circle in the screen/shroud plane shifts raised
			// targets towards one compass direction and away from the opposite direction.
			if (maximum <= minimum) yield break;
			var center = map.CellContaining(source);
			var radius = (maximum.Length + 1023) / 1024 + 1;
			foreach (var cell in map.FindTilesInAnnulus(center, 0, radius, true))
			{
				if (!map.Contains(cell)) continue;
				var position = map.CenterOfCell(cell);
				var distance = (position - source).HorizontalLengthSquared;
				if (distance > maximum.LengthSquared || distance != 0 && distance <= minimum.LengthSquared) continue;
				if (maximumHeightDelta >= 0 && position.Z >= source.Z + maximumHeightDelta * 724) continue;
				var uv = cell.ToMPos(map);
				foreach (var projected in map.ProjectedCellsCovering(uv))
					// A raised cell projects its roof, but also owns the vertical face down
					// to its ground-plane footprint. Roof-only projection leaves black holes
					// immediately below otherwise visible cliffs.
					for (var v = projected.V; v <= Math.Max(projected.V, uv.V); v++)
					{
						var column = new PPos(projected.U, v);
						if (map.Contains(column) && (maximumHeightDelta < 0 || map.ProjectedHeight(column) * 724 < source.Z + maximumHeightDelta * 724))
							yield return column;
					}
			}
		}
	}
}
