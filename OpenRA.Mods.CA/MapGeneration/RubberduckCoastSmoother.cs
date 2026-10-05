using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Conservative, area-neutral corner reduction in the actual isometric cell grid.
	// Changes are paired in the generator's symmetry space and never drift inland.
	static class RubberduckCoastSmoother
	{
		public static int Apply(MapPlan plan, bool integerCenter = false, int symmetryOrder = 2)
		{
			// Non-grid rotational symmetries would require rounding cell positions.
			// Keep those layouts unchanged instead of introducing asymmetric edits.
			if ((plan.Height & 1) != 0 || plan.PlateauSurfaces.Count != 0 ||
				(symmetryOrder != 2 && symmetryOrder != 4) || symmetryOrder == 4 && plan.Width != plan.Height) return 0;
			var terrain = new Dictionary<CPos, PlannedTerrain>();
			var protectedCells = new HashSet<CPos>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var c = MountainPlateauPlanner.Cell(x, y);
					terrain[c] = plan.TerrainAt(x, y);
					if (plan.FeaturesAt(x, y) != PlannedFeature.None || plan.HomeOwnerAt(x, y) >= 0 ||
						(plan.TerrainAt(x, y) != PlannedTerrain.Land && plan.TerrainAt(x, y) != PlannedTerrain.Water) ||
						x < 4 || y < 4 || x >= plan.Width - 4 || y >= plan.Height - 4)
						for (var dx = -2; dx <= 2; dx++)
							for (var dy = -2; dy <= 2; dy++) protectedCells.Add(c + new CVec(dx, dy));
				}
			var boundary = terrain.Keys.Where(c => PlateauTopology.Directions.Any(d => terrain.TryGetValue(c + d, out var t) &&
				(t == PlannedTerrain.Land || t == PlannedTerrain.Water) && t != terrain[c])).ToHashSet();
			var offsets = (from x in Enumerable.Range(-3, 7) from y in Enumerable.Range(-3, 7)
				where Math.Abs(x) + Math.Abs(y) > 0 && Math.Abs(x) + Math.Abs(y) <= 3
				orderby Math.Abs(x) + Math.Abs(y), x, y select new CVec(x, y)).ToArray();
			var touched = new HashSet<CPos>();
			var first = MountainPlateauPlanner.Cell(0, 0); var last = MountainPlateauPlanner.Cell(plan.Width - 1, plan.Height - 1);
			CPos Mirror(CPos c)
			{
				if (!integerCenter) return new CPos(first.X + last.X - c.X, first.Y + last.Y - c.Y);
				// Island generators use integer (size - 1) / 2 centers, whereas
				// Open Plains uses the half-cell center of the complete rectangle.
				var p = MountainPlateauPlanner.Point(c);
				return MountainPlateauPlanner.Cell(2 * ((plan.Width - 1) / 2) - p.U, 2 * ((plan.Height - 1) / 2) - p.V);
			}
			CPos QuarterTurn(CPos c)
			{
				var p = MountainPlateauPlanner.Point(c);
				var twiceCenter = integerCenter ? 2 * ((plan.Width - 1) / 2) : plan.Width - 1;
				return MountainPlateauPlanner.Cell(twiceCenter - p.V, p.U);
			}
			bool Editable(CPos c) => terrain.TryGetValue(c, out var t) && (t == PlannedTerrain.Land || t == PlannedTerrain.Water) &&
				boundary.Contains(c) && !protectedCells.Contains(c) && !touched.Contains(c) &&
				PlateauTopology.Directions.All(d => terrain.TryGetValue(c + d, out var n) && (n == PlannedTerrain.Land || n == PlannedTerrain.Water));
			var swaps = 0;
			foreach (var a in terrain.Keys.OrderBy(c => c.X).ThenBy(c => c.Y).ToArray())
			{
				if (!Editable(a) || terrain[a] != PlannedTerrain.Land) continue;
				foreach (var offset in offsets)
				{
					var b = a + offset; var ma = Mirror(a); var mb = Mirror(b);
					var cells = symmetryOrder == 2 ? new[] { a, b, ma, mb } :
						new[] { a, b, QuarterTurn(a), QuarterTurn(b), ma, mb, QuarterTurn(ma), QuarterTurn(mb) };
					if (cells.Distinct().Count() != symmetryOrder * 2 || cells.Any(c => !Editable(c)) ||
						cells.Where((c, i) => terrain[c] != (i % 2 == 0 ? PlannedTerrain.Land : PlannedTerrain.Water)).Any()) continue;
					var quads = cells.SelectMany(c => new[] { c, c - new CVec(1, 0), c - new CVec(0, 1), c - new CVec(1, 1) }).ToHashSet();
					var beforeCorners = quads.Sum(c => Corners(terrain, c));
					var edges = cells.SelectMany(c => new[] { c }.Concat(PlateauTopology.Directions.Select(d => c + d))).ToHashSet();
					var beforeEdges = edges.Sum(c => Edges(terrain, c));
					var changed = new List<CPos>(); var valid = true;
					foreach (var c in cells)
					{
						if (!Simple(terrain, c)) { valid = false; break; }
						terrain[c] = terrain[c] == PlannedTerrain.Land ? PlannedTerrain.Water : PlannedTerrain.Land;
						changed.Add(c);
					}
					valid &= quads.Sum(c => Corners(terrain, c)) < beforeCorners && edges.Sum(c => Edges(terrain, c)) <= beforeEdges;
					if (!valid)
					{
						foreach (var c in changed) terrain[c] = terrain[c] == PlannedTerrain.Land ? PlannedTerrain.Water : PlannedTerrain.Land;
						continue;
					}
					foreach (var c in cells)
					{
						var p = MountainPlateauPlanner.Point(c); plan.SetTerrain(p.U, p.V, terrain[c]); touched.Add(c);
					}
					swaps += symmetryOrder;
					break;
				}
			}
			plan.ValidationMessages.Add($"Coast smoothing: {swaps} area-neutral swaps in {symmetryOrder}-fold groups; protected features and cardinal components retained.");
			return swaps;
		}

		internal static int CornerScore(MapPlan plan)
		{
			var terrain = new Dictionary<CPos, PlannedTerrain>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++) terrain[MountainPlateauPlanner.Cell(x, y)] = plan.TerrainAt(x, y);
			return terrain.Keys.Sum(c => Corners(terrain, c));
		}

		static int Corners(Dictionary<CPos, PlannedTerrain> terrain, CPos c)
		{
			var offsets = new[] { CVec.Zero, new CVec(1, 0), new CVec(1, 1), new CVec(0, 1) };
			var mask = 0;
			for (var i = 0; i < 4; i++)
			{
				if (!terrain.TryGetValue(c + offsets[i], out var t) || (t != PlannedTerrain.Land && t != PlannedTerrain.Water)) return 0;
				if (t == PlannedTerrain.Land) mask |= 1 << i;
			}
			if (mask == 0 || mask == 15 || mask == 3 || mask == 6 || mask == 12 || mask == 9) return 0;
			return mask == 5 || mask == 10 ? 4 : 1;
		}

		static int Edges(Dictionary<CPos, PlannedTerrain> terrain, CPos c)
		{
			if (!terrain.TryGetValue(c, out var t)) return 0;
			return PlateauTopology.Directions.Count(d => terrain.TryGetValue(c + d, out var n) && n != t);
		}

		// A simple flip neither splits nor merges either cardinal terrain component.
		// Requiring connected rings is conservative: routes outside this 3x3 area
		// are never assumed to make a locally unsafe change acceptable.
		static bool Simple(Dictionary<CPos, PlannedTerrain> terrain, CPos center)
		{
			foreach (var kind in new[] { PlannedTerrain.Land, PlannedTerrain.Water })
			{
				var ring = new HashSet<CPos>();
				for (var x = -1; x <= 1; x++)
					for (var y = -1; y <= 1; y++)
					{
						var c = center + new CVec(x, y);
						if (c != center && terrain.TryGetValue(c, out var t) && t == kind) ring.Add(c);
					}
				if (!PlateauTopology.Directions.Any(d => ring.Contains(center + d))) return false;
				var queue = new Queue<CPos>(); queue.Enqueue(ring.First());
				while (queue.Count != 0)
				{
					var c = queue.Dequeue(); if (!ring.Remove(c)) continue;
					foreach (var d in PlateauTopology.Directions) if (ring.Contains(c + d)) queue.Enqueue(c + d);
				}
				if (ring.Count != 0) return false;
			}
			return true;
		}
	}
}
