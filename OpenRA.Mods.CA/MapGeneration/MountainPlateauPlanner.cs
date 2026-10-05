using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	readonly struct PlannedPlateau
	{
		public readonly PlanPoint Approach;
		public readonly PlanPoint Landing;
		public readonly byte Ramp;
		public PlannedPlateau(PlanPoint approach, PlanPoint landing, byte ramp)
		{
			Approach = approach;
			Landing = landing;
			Ramp = ramp;
		}
	}

	static class MountainPlateauPlanner
	{
		// Match the exporter's one-cell padding and stagger parity. The additional
		// Rubberduck height border is even, so export is a pure CPos translation.
		public static CPos Cell(int x, int y) => new MPos(x + 1, y + 1).ToCPos(MapGridType.RectangularIsometric);
		public static MPos Point(CPos cell)
		{
			var uv = cell.ToMPos(MapGridType.RectangularIsometric);
			return new MPos(uv.U - 1, uv.V - 1);
		}

		public static bool AddIsland(MapPlan plan, IEnumerable<PlanPoint> footprint, double rotation)
		{
			var cells = footprint.Select(p => Cell(p.X, p.Y)).ToHashSet();
			if (cells.Count == 0) return false;
			var centerX = cells.Average(c => c.X);
			var centerY = cells.Average(c => c.Y);
			// Prefer the rotated outward direction, but require a complete corridor,
			// retaining strips and landing rather than forcing slopes through a thin rim.
			var preferred = ((int)Math.Round(rotation / (Math.PI / 2)) % 4 + 4) % 4;
			var originalCells = cells;
			var coarse = CoarseRoof(cells, new CPos((int)Math.Floor(centerX), (int)Math.Floor(centerY)));
			// Try bands, a small-island rectangle, then the exact original outline.
			// All three paths require complete reserved sockets; none is a legacy fallback.
			foreach (var (fit, turn) in Enumerable.Range(0, 3).SelectMany(fit => Enumerable.Range(0, 4).Select(t => (fit, t))))
			{
				cells = fit == 0 ? coarse : originalCells;
				var direction = (preferred + turn) % 4;
				var outward = PlateauTopology.Directions[direction];
				var lateral = PlateauTopology.Directions[(direction + 1) % 4];
				foreach (var toe in cells.OrderBy(c => Math.Abs((c.X - centerX) * lateral.X + (c.Y - centerY) * lateral.Y))
					.ThenByDescending(c => c.X * outward.X + c.Y * outward.Y).ThenBy(c => c.X).ThenBy(c => c.Y))
				{
					var fits = true;
					for (var step = 0; step <= 5; step++)
						for (var lane = step < 4 ? -2 : -1; lane <= (step < 4 ? 2 : 1); lane++)
							if (!cells.Contains(toe - outward * step + lateral * lane)) fits = false;
					if (!fits) continue;
					var roof = fit == 1 ? RampRectangle(originalCells, toe, outward, lateral) : cells;
					if (roof == null) continue;

					var approach = toe;
					var notch = new HashSet<CPos>();
					var outsideRows = 0;
					for (var distance = 1; distance <= 32 && outsideRows < 2; distance++)
					{
						approach = toe + outward * distance;
						var outside = true;
						for (var lane = -1; lane <= 1; lane++)
						{
							var cell = approach + lateral * lane;
							var point = Point(cell);
							if (!plan.Contains(point.U, point.V) || (!roof.Contains(cell) &&
								(plan.TerrainAt(point.U, point.V) != PlannedTerrain.Land || plan.HasFeature(point.U, point.V,
									PlannedFeature.Home | PlannedFeature.Expansion | PlannedFeature.Enclosed | PlannedFeature.BuildClearance))))
								fits = false;
							outside &= !roof.Contains(cell);
							notch.Add(cell);
						}
						outsideRows = outside ? outsideRows + 1 : 0;
					}
					if (!fits || outsideRows < 2) continue;
					var surfaces = roof.ToDictionary(c => c, _ => new PlateauSurface(4));
					foreach (var cell in notch) surfaces.Remove(cell);
					var ramp = (byte)((direction + 2) % 4 + 1);
					for (var step = 0; step < 4; step++)
					{
						for (var lane = -1; lane <= 1; lane++)
							surfaces[toe - outward * step + lateral * lane] = new PlateauSurface((byte)step, ramp);
						foreach (var lane in new[] { -2, 2 })
							surfaces[toe - outward * step + lateral * lane] = new PlateauSurface(4, 0, true);
					}
					for (var step = -1; step < 4; step++)
						for (var lane = -1; lane <= 1; lane++)
						{
							var cell = toe - outward * step + lateral * lane;
							if (!PlateauTopology.Continuous(PlateauTopology.Get(surfaces, cell),
								PlateauTopology.Get(surfaces, cell - outward), (direction + 2) % 4))
								throw new InvalidOperationException("Planned island has a discontinuous ascent.");
						}
					if (!SealDisconnectedRim(surfaces, notch, toe - outward * 4)) continue;
					var reserved = ReserveClosedFeet(plan, surfaces);
					if (ReferenceEquals(reserved, surfaces)) continue;
					surfaces = reserved;
					foreach (var cell in roof.Concat(notch))
					{
						var p = Point(cell);
						plan.SetTerrain(p.U, p.V, PlannedTerrain.Land);
						// Reserve the approach too: economy and checkpoint placement must not
						// obstruct or flatten a committed plateau access route.
						plan.AddFeature(p.U, p.V, PlannedFeature.Enclosed);
					}
					foreach (var pair in surfaces)
					{
						var p = Point(pair.Key);
						plan.PlateauSurfaces.Add(p, pair.Value);
						if (pair.Value.Blocked) plan.SetTerrain(p.U, p.V, PlannedTerrain.Mountain);
					}
					var a = Point(approach);
					var landing = Point(toe - outward * 4);
					plan.Plateaus.Add(new PlannedPlateau(new PlanPoint(a.U, a.V), new PlanPoint(landing.U, landing.V), ramp));
					return true;
				}
			}
			return false;
		}

		public static bool AddHome(MapPlan plan, IEnumerable<PlanPoint> footprint, PlanPoint center, int owner)
		{
			var cells = footprint.Select(p => Cell(p.X, p.Y)).ToHashSet();
			if (cells.Any(c => plan.PlateauSurfaces.ContainsKey(Point(c)) ||
				plan.HasFeature(Point(c).U, Point(c).V, PlannedFeature.Enclosed))) return false;
			var origin = Cell(center.X, center.Y);
			// Author the roof in two-cell runs so adjacent inward/outward corners do
			// not demand overlapping one-cell source-art sockets. Only remove fringe
			// cells; never expand into another component or change the starting fields.
			var coarse = CoarseRoof(cells, origin);
			var fieldsFit = true;
			foreach (var side in new[] { -1, 1 })
				for (var dy = -2; dy <= 2; dy++)
					for (var dx = -2; dx <= 2; dx++)
						if (!coarse.Contains(Cell(center.X + side * 5 + dx, center.Y + dy))) fieldsFit = false;
			for (var x = -2; x <= 2; x++)
				for (var y = -2; y <= 2; y++)
					if (!coarse.Contains(origin + new CVec(x, y))) fieldsFit = false;
			var originalCells = cells;
			var middle = Cell(plan.Width / 2, plan.Height / 2);
			var directions = Enumerable.Range(0, 4).OrderByDescending(d =>
				(middle.X - origin.X) * PlateauTopology.Directions[d].X + (middle.Y - origin.Y) * PlateauTopology.Directions[d].Y);
			foreach (var (fitCoarse, direction) in new[] { true, false }.SelectMany(coarseFit => directions.Select(d => (coarseFit, d))))
			{
				cells = fitCoarse && fieldsFit && coarse.Contains(origin) ? coarse : originalCells;
				var outward = PlateauTopology.Directions[direction];
				var lateral = PlateauTopology.Directions[(direction + 1) % 4];
				foreach (var landing in cells.OrderBy(c => Math.Abs((c.X - origin.X) * lateral.X + (c.Y - origin.Y) * lateral.Y))
					.ThenByDescending(c => c.X * outward.X + c.Y * outward.Y).ThenBy(c => c.X).ThenBy(c => c.Y))
				{
					if (Enumerable.Range(-2, 5).Any(lane => !cells.Contains(landing + lateral * lane) ||
						!cells.Contains(landing - outward + lateral * lane))) continue;
					var rampCells = new HashSet<CPos>();
					var fits = true;
					for (var step = 1; step <= 6; step++)
						for (var lane = -2; lane <= 2; lane++)
						{
							var cell = landing + outward * step + lateral * lane;
							var p = Point(cell);
							if ((step >= 4 && cells.Contains(cell)) || !plan.Contains(p.U, p.V) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land ||
								plan.HasFeature(p.U, p.V, PlannedFeature.Home | PlannedFeature.Enclosed | PlannedFeature.Expansion)) fits = false;
							rampCells.Add(cell);
						}
					if (!fits) continue;
					var surfaces = cells.ToDictionary(c => c, _ => new PlateauSurface(4));
					var ramp = (byte)((direction + 2) % 4 + 1);
					for (var step = 1; step <= 4; step++)
						for (var lane = -2; lane <= 2; lane++)
							surfaces[landing + outward * step + lateral * lane] = Math.Abs(lane) == 2
								? new PlateauSurface((byte)(5 - step), 0, true) : new PlateauSurface((byte)(4 - step), ramp);
					// A curved rim may need a shallow lip cut. Keep both starting fields
					// intact and put the foot of the ramp entirely outside the footprint.
					foreach (var side in new[] { -1, 1 })
						for (var dy = -2; dy <= 2; dy++)
							for (var dx = -2; dx <= 2; dx++)
							{
								var field = PlateauTopology.Get(surfaces, Cell(center.X + side * 5 + dx, center.Y + dy));
								if (field.Height != 4 || field.Ramp != 0 || field.Blocked) fits = false;
							}
					if (!fits || surfaces.Any(p => Point(p.Key).V < p.Value.Height + (p.Value.Height & 1) ||
						((p.Value.Height & 1) != 0 && p.Value.Ramp == 0 &&
							(Point(p.Key).U < 1 || Point(p.Key).U >= plan.Width - 1)))) continue;
					for (var step = 0; step <= 4; step++)
						for (var lane = -1; lane <= 1; lane++)
						{
							var cell = landing + outward * step + lateral * lane;
							if (!PlateauTopology.Continuous(PlateauTopology.Get(surfaces, cell),
								PlateauTopology.Get(surfaces, cell + outward), direction))
								throw new InvalidOperationException("Discontinuous home plateau ramp.");
						}
					if (!SealDisconnectedRim(surfaces, rampCells, origin)) continue;
					var reserved = ReserveClosedFeet(plan, surfaces);
					if (ReferenceEquals(reserved, surfaces)) continue;
					surfaces = reserved;
					foreach (var pair in surfaces)
					{
						var p = Point(pair.Key);
						plan.PlateauSurfaces.Add(p, pair.Value);
						plan.SetTerrain(p.U, p.V, pair.Value.Blocked ? PlannedTerrain.Mountain : PlannedTerrain.Land);
						if (cells.Contains(pair.Key))
						{
							plan.AddFeature(p.U, p.V, PlannedFeature.Home);
							if (owner >= 0) plan.SetHomeOwner(p.U, p.V, owner);
						}
					}
					foreach (var cell in rampCells)
					{
						var p = Point(cell);
						plan.AddFeature(p.U, p.V, PlannedFeature.Enclosed | PlannedFeature.Entrance);
					}
					var approach = Point(landing + outward * 6);
					var top = Point(landing);
					plan.Plateaus.Add(new PlannedPlateau(new PlanPoint(approach.U, approach.V), new PlanPoint(top.U, top.V), ramp));
					return true;
				}
			}
			return false;
		}

		// Small islands can fit a six-row ascent but not the odd-width centered
		// bands. Fit a ramp-oriented rectangle without removing any interior cell.
		internal static HashSet<CPos> RampRectangle(HashSet<CPos> original, CPos toe, CVec outward, CVec lateral)
		{
			var interior = original.Where(c => Enumerable.Range(-1, 3).All(x =>
				Enumerable.Range(-1, 3).All(y => original.Contains(c + new CVec(x, y))))).ToArray();
			HashSet<CPos> best = null;
			for (var halfWidth = 2; halfWidth <= 16; halfWidth++)
			{
				var rectangle = new HashSet<CPos>();
				for (var depth = 0; depth < 64; depth++)
				{
					var row = Enumerable.Range(-halfWidth, 2 * halfWidth + 1).Select(lane => toe - outward * depth + lateral * lane).ToArray();
					if (row.Any(c => !original.Contains(c))) break;
					rectangle.UnionWith(row);
					if (depth >= 5 && (best == null || rectangle.Count > best.Count) && interior.All(rectangle.Contains))
						best = new HashSet<CPos>(rectangle);
				}
			}
			return best;
		}

		internal static HashSet<CPos> CoarseRoof(HashSet<CPos> cells, CPos origin)
		{
			// A centered five-cell band and paired two-cell outer bands commute
			// with rotation/reflection. A plain 2x2 lattice biases one compass side.
			int Band(int offset) => offset < -2 ? -((-offset - 1) / 2) : offset > 2 ? (offset - 1) / 2 : 0;
			return cells.GroupBy(c => (X: Band(c.X - origin.X), Y: Band(c.Y - origin.Y)))
				.Where(g => g.Count() == (g.Key.X == 0 ? 5 : 2) * (g.Key.Y == 0 ? 5 : 2)).SelectMany(g => g).ToHashSet();
		}

		// Run before committing homes, entrances or economy. A rejected candidate has
		// no side effects; rendering is never allowed to reserve these cells later.
		internal static Dictionary<CPos, PlateauSurface> ReserveClosedFeet(MapPlan plan, Dictionary<CPos, PlateauSurface> original)
		{
			var fullGuards = original.ToDictionary(p => p.Key, p => p.Value.Blocked && p.Value.Height > 0 && p.Value.Ramp == 0
				? new PlateauSurface(4, 0, true) : p.Value);
			var candidate = PlateauTopology.WithCliffFeet(fullGuards);
			foreach (var cell in candidate.Keys.Where(c => !original.ContainsKey(c)))
			{
				var p = Point(cell);
				if (!plan.Contains(p.U, p.V) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land ||
					plan.FeaturesAt(p.U, p.V) != PlannedFeature.None || plan.PlateauSurfaces.ContainsKey(p))
				{
					if (Environment.GetEnvironmentVariable("YMCA_MAPGEN_DIAGNOSTICS") == "1") Console.Error.WriteLine($"Closed foot rejected at {p}: bounds/terrain/features.");
					return original;
				}
			}
			bool FlatLow(CPos cell, bool blocked)
			{
				var p = Point(cell);
				if (!plan.Contains(p.U, p.V)) return false;
				if (candidate.TryGetValue(cell, out var surface)) return surface.Height == 0 && surface.Ramp == 0 && surface.Blocked == blocked;
				return !blocked && plan.TerrainAt(p.U, p.V) == PlannedTerrain.Land && !plan.PlateauSurfaces.ContainsKey(p);
			}
			if (!RubberduckClosedCliffSelection.TryPieces(candidate,
				fullGuards.Where(p => RubberduckClosedCliffSelection.Elevated(p.Value)).Select(p => p.Key).ToHashSet(), FlatLow, out var pieces,
				reason => { if (Environment.GetEnvironmentVariable("YMCA_MAPGEN_DIAGNOSTICS") == "1") Console.Error.WriteLine(reason); }))
			{
				if (Environment.GetEnvironmentVariable("YMCA_MAPGEN_DIAGNOSTICS") == "1") Console.Error.WriteLine("Closed plateau contour unsupported.");
				return original;
			}
			plan.ValidationMessages.Add($"Closed plateau footprint reserved before economy: {candidate.Count - original.Count} feet, {pieces.Count} pieces.");
			return candidate;
		}

		static bool SealDisconnectedRim(Dictionary<CPos, PlateauSurface> surfaces, HashSet<CPos> notch, CPos landing)
		{
			var reachable = new HashSet<CPos> { landing };
			var queue = new Queue<CPos>();
			queue.Enqueue(landing);
			while (queue.Count > 0)
			{
				var cell = queue.Dequeue();
				var current = PlateauTopology.Get(surfaces, cell);
				foreach (var direction in PlateauTopology.Directions)
				{
					var next = cell + direction;
					if (!surfaces.ContainsKey(next) && !notch.Contains(next)) continue;
					var value = PlateauTopology.Get(surfaces, next);
					if (value.Blocked || Math.Abs(current.Height - value.Height) > 1 || !reachable.Add(next)) continue;
					queue.Enqueue(next);
				}
			}
			var tips = surfaces.Where(p => !p.Value.Blocked && !reachable.Contains(p.Key)).ToArray();
			// Rasterization can leave tiny corner-only protrusions. They are cliff rim,
			// not usable ground. Never hide a disconnected interior or an entire lobe.
			if (tips.Length > Math.Max(4, surfaces.Count / 20) || tips.Any(p => p.Value.Ramp != 0 ||
				!PlateauTopology.Directions.Any(d => !surfaces.ContainsKey(p.Key + d)))) return false;
			foreach (var tip in tips) surfaces[tip.Key] = new PlateauSurface(tip.Value.Height, 0, true);
			return true;
		}

		public static bool[] Reachable(MapPlan plan, PlanPoint start)
		{
			var visited = new bool[plan.Width * plan.Height];
			var queue = new Queue<CPos>();
			queue.Enqueue(Cell(start.X, start.Y));
			visited[start.X + start.Y * plan.Width] = true;
			while (queue.Count != 0)
			{
				var cell = queue.Dequeue();
				var p = Point(cell);
				var height = plan.PlateauSurfaces.TryGetValue(p, out var current) ? current.Height : 0;
				for (var dx = -1; dx <= 1; dx++)
					for (var dy = -1; dy <= 1; dy++)
					{
						// A safe diagonal can always be replaced by cardinal steps. Using
						// cardinal reachability excludes corner-only islands that a unit's
						// subcell offset cannot physically traverse without crossing a cliff.
						if (dx != 0 && dy != 0 || dx == 0 && dy == 0) continue;
						var nextCell = cell + new CVec(dx, dy);
						var next = Point(nextCell);
						if (!plan.IsTraversable(next.U, next.V)) continue;
						var nextHeight = plan.PlateauSurfaces.TryGetValue(next, out var surface) ? surface.Height : 0;
						if (Math.Abs(height - nextHeight) > 1) continue;
						var index = next.U + next.V * plan.Width;
						if (visited[index]) continue;
						visited[index] = true;
						queue.Enqueue(nextCell);
					}
			}
			return visited;
		}
	}
}
