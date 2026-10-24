using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Plan whole native-grid rock contours and their feet before any valley economy.
	// Failed proposals are transactional: no partial walls or legacy-renderer fallback.
	static class ClosedExpansionRimPlanner
	{
		public static bool Place(MapPlan plan, IReadOnlyList<PlanPoint> rim, ISet<MPos> protectedCells)
		{
			if (rim.Count == 0) return true;
			var original = rim.Select(p => MountainPlateauPlanner.Cell(p.X, p.Y)).ToHashSet();
			bool Available(CPos cell)
			{
				var p = MountainPlateauPlanner.Point(cell);
				return plan.Contains(p.U, p.V) && plan.TerrainAt(p.U, p.V) == PlannedTerrain.Land &&
					plan.FeaturesAt(p.U, p.V) == PlannedFeature.None && !plan.PlateauSurfaces.ContainsKey(p) &&
					!plan.StrategicReservations.Contains(p) && !protectedCells.Contains(p);
			}

			for (var phase = 0; phase < 4; phase++)
			{
				var px = phase % 2; var py = phase / 2;
				CPos Block(CPos c) => new CPos(2 * (int)Math.Floor((c.X - px) / 2.0) + px, 2 * (int)Math.Floor((c.Y - py) / 2.0) + py);
				var roof = new HashSet<CPos>();
				foreach (var group in original.GroupBy(Block).OrderBy(g => g.Key.X).ThenBy(g => g.Key.Y))
				{
					if (group.Count() < 2) continue;
					// Paired runs give the authored corners complete sockets. Shorten an
					// uncommitted proposal at gates instead of consuming their approach.
					var safe = true;
					for (var x = -1; x <= 2; x++) for (var y = -1; y <= 2; y++)
						if (!Available(group.Key + new CVec(x, y))) safe = false;
					if (!safe) continue;
					for (var x = 0; x < 2; x++) for (var y = 0; y < 2; y++) roof.Add(group.Key + new CVec(x, y));
				}
				if (roof.Count < Math.Max(4, original.Count / 3)) continue;
				var draft = plan.Clone();
				var pending = new HashSet<CPos>(roof);
				var fits = true;
				while (pending.Count != 0 && fits)
				{
					var component = new Dictionary<CPos, PlateauSurface>();
					var queue = new Queue<CPos>(); queue.Enqueue(pending.OrderBy(c => c.X).ThenBy(c => c.Y).First());
					while (queue.Count != 0)
					{
						var cell = queue.Dequeue(); if (!pending.Remove(cell)) continue;
						component.Add(cell, new PlateauSurface(4, 0, true));
						foreach (var direction in PlateauTopology.Directions) if (pending.Contains(cell + direction)) queue.Enqueue(cell + direction);
					}
					var patch = MountainPlateauPlanner.ReserveClosedFeet(draft, component);
					if (ReferenceEquals(patch, component)) { fits = false; break; }
					foreach (var pair in patch)
					{
						var p = MountainPlateauPlanner.Point(pair.Key);
						draft.PlateauSurfaces.Add(p, pair.Value);
						draft.SetTerrain(p.U, p.V, PlannedTerrain.Mountain);
					}
				}
				if (!fits) continue;
				foreach (var pair in draft.PlateauSurfaces.Where(p => !plan.PlateauSurfaces.ContainsKey(p.Key)))
				{
					plan.PlateauSurfaces.Add(pair.Key, pair.Value);
					plan.SetTerrain(pair.Key.U, pair.Key.V, PlannedTerrain.Mountain);
				}
				plan.ValidationMessages.Add($"Closed expansion rim: {roof.Count} blocked roof cells; complete native contours and reserved feet.");
				return true;
			}
			return false;
		}
	}
}
