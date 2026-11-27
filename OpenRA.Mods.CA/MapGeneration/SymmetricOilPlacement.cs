using System;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Oil has a 2x2 native CPos footprint. Reflect its CENTER, not its top-left
	// anchor, and commit both placements together only after checking both sides.
	static class SymmetricOilPlacement
	{
		public static PlanPoint Mirror(MapPlan plan, PlanPoint point)
		{
			var first = MountainPlateauPlanner.Cell(0, 0);
			var last = MountainPlateauPlanner.Cell(plan.Width - 1, plan.Height - 1);
			var cell = MountainPlateauPlanner.Cell(point.X, point.Y);
			var p = MountainPlateauPlanner.Point(new CPos(first.X + last.X - cell.X - 1, first.Y + last.Y - cell.Y - 1));
			return new PlanPoint(p.U, p.V);
		}

		public static bool CanPlace(MapPlan plan, PlanPoint point, bool allowResourceFootprint = false)
		{
			var cell = MountainPlateauPlanner.Cell(point.X, point.Y);
			for (var y = -1; y <= 2; y++)
				for (var x = -1; x <= 2; x++)
				{
					var p = MountainPlateauPlanner.Point(cell + new CVec(x, y));
					if (!plan.ContainsPlayable(p.U, p.V) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land ||
						plan.PlateauSurfaces.ContainsKey(p) || plan.StrategicReservations.Contains(p) ||
						plan.HasFeature(p.U, p.V, PlannedFeature.Bottleneck | PlannedFeature.BuildClearance | PlannedFeature.Entrance |
							PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding)) return false;
					if (!allowResourceFootprint && x >= 0 && x <= 1 && y >= 0 && y <= 1 &&
						plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource)) return false;
				}
			return true;
		}

		public static bool TryPlacePair(MapPlan plan, PlanPoint point)
		{
			var mirror = Mirror(plan, point);
			var a = MountainPlateauPlanner.Cell(point.X, point.Y);
			var b = MountainPlateauPlanner.Cell(mirror.X, mirror.Y);
			if (Math.Abs(a.X - b.X) < 3 && Math.Abs(a.Y - b.Y) < 3 || !CanPlace(plan, point) || !CanPlace(plan, mirror)) return false;
			plan.AddFeature(point.X, point.Y, PlannedFeature.TechBuilding);
			plan.AddFeature(mirror.X, mirror.Y, PlannedFeature.TechBuilding);
			return true;
		}

		public static void Validate(MapPlan plan)
		{
			var occupied = new System.Collections.Generic.HashSet<CPos>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (!plan.HasFeature(x, y, PlannedFeature.TechBuilding)) continue;
					var mirror = Mirror(plan, new PlanPoint(x, y));
					if (!plan.Contains(mirror.X, mirror.Y) || !plan.HasFeature(mirror.X, mirror.Y, PlannedFeature.TechBuilding))
						throw new InvalidOperationException("Oil placement lost its mirrored counterpart.");
					var c = MountainPlateauPlanner.Cell(x, y);
					for (var dy = 0; dy < 2; dy++) for (var dx = 0; dx < 2; dx++)
					{
						var cell = c + new CVec(dx, dy); var p = MountainPlateauPlanner.Point(cell);
						if (!plan.ContainsPlayable(p.U, p.V) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land ||
							plan.PlateauSurfaces.ContainsKey(p) || plan.StrategicReservations.Contains(p) || !occupied.Add(cell))
							throw new InvalidOperationException("Oil footprint overlaps protected terrain or another derrick.");
					}
				}
		}
	}
}
