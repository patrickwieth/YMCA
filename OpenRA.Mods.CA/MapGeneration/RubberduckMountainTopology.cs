using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Fit raw mountain outlines to the shared authored contour vocabulary BEFORE
	// economy placement. Never leave invisible blocked leftovers or fill water.
	static class RubberduckMountainTopology
	{
		const PlannedFeature Protected = PlannedFeature.Spawn | PlannedFeature.BuildClearance | PlannedFeature.Bottleneck |
			PlannedFeature.Entrance | PlannedFeature.Resource | PlannedFeature.RichResource |
			PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding | PlannedFeature.DestructibleBridge;

		public static bool Prepare(MapPlan plan)
		{
			var raw = new HashSet<CPos>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					if (plan.TerrainAt(x, y) == PlannedTerrain.Mountain && !plan.PlateauSurfaces.ContainsKey(new MPos(x, y))) raw.Add(MountainPlateauPlanner.Cell(x, y));
			if (raw.Count == 0) return true;
			var patch = new Dictionary<CPos, PlateauSurface>();
			foreach (var component in Components(raw, true))
			{
				Dictionary<CPos, PlateauSurface> selected = null;
				for (var phase = 0; phase < 4 && selected == null; phase++) selected = Fit(plan, component, patch, phase);
				if (selected == null) return false;
				foreach (var p in selected) patch.Add(p.Key, p.Value);
			}
			// Commit complete contours only. Removed outline cells become ordinary
			// ground, rather than retaining an invisible collision footprint.
			foreach (var c in raw)
			{
				var p = MountainPlateauPlanner.Point(c); plan.SetTerrain(p.U, p.V, PlannedTerrain.Land);
			}
			foreach (var pair in patch)
			{
				var p = MountainPlateauPlanner.Point(pair.Key);
				plan.SetTerrain(p.U, p.V, PlannedTerrain.Mountain); plan.PlateauSurfaces[p] = pair.Value;
			}
			plan.RequireClosedPlateaus = true;
			plan.ValidationMessages.Add($"Shared mountain contours: {patch.Count(p => p.Value.Height == 4)} roof cells and {patch.Count(p => p.Value.Height == 0)} reserved feet; no legacy wall fallback.");
			return true;
		}

		static Dictionary<CPos, PlateauSurface> Fit(MapPlan plan, HashSet<CPos> raw, Dictionary<CPos, PlateauSurface> committed, int phase)
		{
			var px = phase % 2; var py = phase / 2;
			int Band(int value) => (int)Math.Floor(value / 2.0);
			IEnumerable<CPos> Block((int X, int Y) b)
			{
				for (var x = 0; x < 2; x++) for (var y = 0; y < 2; y++) yield return new CPos(b.X * 2 + px + x, b.Y * 2 + py + y);
			}
			bool Safe(CPos c)
			{
				var p = MountainPlateauPlanner.Point(c);
				return plan.Contains(p.U, p.V) && (plan.TerrainAt(p.U, p.V) == PlannedTerrain.Land || raw.Contains(c)) &&
					!plan.HasFeature(p.U, p.V, Protected) && !plan.StrategicReservations.Contains(p) &&
					!plan.PlateauSurfaces.ContainsKey(p) && !committed.ContainsKey(c);
			}
			bool Fits((int X, int Y) b) => Block(b).All(c => Enumerable.Range(-1, 3).All(x => Enumerable.Range(-1, 3).All(y => Safe(c + new CVec(x, y)))));
			var blocks = raw.GroupBy(c => (X: Band(c.X - px), Y: Band(c.Y - py))).Where(g => g.Count() >= 2 && Fits(g.Key)).Select(g => g.Key).ToHashSet();
			if (blocks.Count == 0) return null;
			// A diagonal-only contact cannot carry two closed corner sockets. Join
			// it with the best supported macro-cell, or reject the whole proposal.
			bool changed;
			do
			{
				changed = false;
				foreach (var b in blocks.OrderBy(b => b.X).ThenBy(b => b.Y).ToArray())
					foreach (var dy in new[] { -1, 1 })
					{
						if (!blocks.Contains((b.X + 1, b.Y + dy)) || blocks.Contains((b.X + 1, b.Y)) || blocks.Contains((b.X, b.Y + dy))) continue;
						var choices = new[] { (X: b.X + 1, Y: b.Y), (X: b.X, Y: b.Y + dy) }.Where(Fits)
							.OrderByDescending(c => Block(c).Count(raw.Contains)).ThenBy(c => c.X).ThenBy(c => c.Y).ToArray();
						if (choices.Length == 0) return null;
						blocks.Add(choices[0]); changed = true;
					}
			} while (changed);
			var roofs = blocks.SelectMany(Block).ToHashSet();
			var surfaces = PlateauTopology.WithCliffFeet(roofs.ToDictionary(c => c, _ => new PlateauSurface(4, 0, true)));
			if (!surfaces.Keys.All(Safe) || raw.Count(surfaces.ContainsKey) < raw.Count * 0.65) return null;
			// Do not open an unintended bypass at the end of a boundary-reaching ridge.
			int Edges(IEnumerable<CPos> cells)
			{
				var edges = 0;
				foreach (var c in cells)
				{
					var p = MountainPlateauPlanner.Point(c);
					if (p.U == 0) edges |= 1; if (p.U == plan.Width - 1) edges |= 2;
					if (p.V == 0) edges |= 4; if (p.V == plan.Height - 1) edges |= 8;
				}
				return edges;
			}
			if ((Edges(surfaces.Keys) & Edges(raw)) != Edges(raw)) return null;
			var connectedRoofs = Components(roofs, false).ToArray();
			if (connectedRoofs.Length != 1) return null;
			foreach (var component in connectedRoofs)
			{
				bool Low(CPos c, bool blocked) => surfaces.TryGetValue(c, out var s) && s.Height == 0 && s.Blocked == blocked;
				if (!RubberduckClosedCliffSelection.TryPieces(surfaces, component, Low, out _)) return null;
			}
			return surfaces;
		}

		static IEnumerable<HashSet<CPos>> Components(IEnumerable<CPos> cells, bool diagonal)
		{
			var remaining = cells.ToHashSet();
			while (remaining.Count > 0)
			{
				var result = new HashSet<CPos>(); var pending = new Stack<CPos>(); pending.Push(remaining.OrderBy(c => c.X).ThenBy(c => c.Y).First());
				while (pending.Count > 0)
				{
					var c = pending.Pop(); if (!remaining.Remove(c)) continue; result.Add(c);
					for (var x = -1; x <= 1; x++) for (var y = -1; y <= 1; y++)
						if ((x != 0 || y != 0) && (diagonal || x == 0 || y == 0) && remaining.Contains(c + new CVec(x, y))) pending.Push(c + new CVec(x, y));
				}
				yield return result;
			}
		}
	}
}
