using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Disjoint, straight native-grid rock islands. Complete closed contours replace
	// intersecting rotated bent lines: no hooked arms, shared sockets or narrow gaps.
	static class ArabiaMountainPlanner
	{
		internal const int Clearance = 6;

		public static bool Place(MapPlan plan, int players, MersenneTwister random)
		{
			var order = PlayerSpawnLayout.SymmetryOrder(players);
			var centerX = (plan.Width - 1) / 2.0;
			var centerY = (plan.Height - 1) / 2.0;
			var middle = MountainPlateauPlanner.Cell(plan.Width / 2, plan.Height / 2);
			var groups = 0;
			for (var group = 0; group < (players >= 8 ? 2 : 1); group++)
				for (var attempt = 0; attempt < 500; attempt++)
				{
					var radius = Math.Min(plan.Width, plan.Height) * (0.20 + random.NextFloat() * 0.22);
					var phase = random.NextFloat() * Math.PI * 2;
					var halfLength = random.Next(3, 6);
					var draft = plan.Clone();
					var fits = true;
					for (var rotation = 0; rotation < order && fits; rotation++)
					{
						var angle = phase + rotation * Math.PI * 2 / order;
						var center = MountainPlateauPlanner.Cell((int)Math.Round(centerX + Math.Cos(angle) * radius),
							(int)Math.Round(centerY + Math.Sin(angle) * radius));
						var alongX = Math.Abs(center.X - middle.X) >= Math.Abs(center.Y - middle.Y);
						var roof = new Dictionary<CPos, PlateauSurface>();
						for (var u = -halfLength; u <= halfLength; u++)
							for (var v = -2; v <= 2; v++)
								roof.Add(center + new CVec(alongX ? u : v, alongX ? v : u), new PlateauSurface(4, 0, true));
						foreach (var c in roof.Keys)
						{
							var p = MountainPlateauPlanner.Point(c);
							if (!draft.Contains(p.U, p.V) || draft.TerrainAt(p.U, p.V) != PlannedTerrain.Land ||
								draft.FeaturesAt(p.U, p.V) != PlannedFeature.None) { fits = false; break; }
						}
						if (!fits) break;
						var patch = MountainPlateauPlanner.ReserveClosedFeet(draft, roof);
						if (ReferenceEquals(patch, roof)) { fits = false; break; }
						foreach (var c in patch.Keys)
						{
							if (draft.StrategicReservations.Contains(MountainPlateauPlanner.Point(c))) { fits = false; break; }
							// Keep a genuinely open center and six ground cells between
							// reserved feet or map boundaries, not just between roof pixels.
							if (Math.Max(Math.Abs(c.X - middle.X), Math.Abs(c.Y - middle.Y)) <= Clearance + 1) { fits = false; break; }
							for (var x = -Clearance; x <= Clearance && fits; x++)
								for (var y = -Clearance; y <= Clearance; y++)
								{
									var p = MountainPlateauPlanner.Point(c + new CVec(x, y));
									if (!draft.Contains(p.U, p.V) || draft.TerrainAt(p.U, p.V) != PlannedTerrain.Land)
									{ fits = false; break; }
								}
							if (!fits) break;
						}
						if (!fits) break;
						foreach (var pair in patch)
						{
							var p = MountainPlateauPlanner.Point(pair.Key);
							draft.PlateauSurfaces.Add(p, pair.Value);
							draft.SetTerrain(p.U, p.V, PlannedTerrain.Mountain);
						}
					}
					if (!fits || !HasWideRoutes(draft)) continue;
					foreach (var pair in draft.PlateauSurfaces)
					{
						plan.PlateauSurfaces[pair.Key] = pair.Value;
						plan.SetTerrain(pair.Key.U, pair.Key.V, PlannedTerrain.Mountain);
					}
					groups++;
					break;
				}
			plan.RequireClosedPlateaus = true;
			if (groups == 0) return false;
			plan.ValidationMessages.Add($"Arabia: {groups * order} disjoint straight closed rock islands; six-cell foot clearance and five-cell-wide home-to-center routes.");
			return true;
		}

		internal static bool HasWideRoutes(MapPlan plan)
		{
			bool Clear(CPos c)
			{
				for (var x = -2; x <= 2; x++)
					for (var y = -2; y <= 2; y++)
					{
						var p = MountainPlateauPlanner.Point(c + new CVec(x, y));
						if (!plan.IsTraversable(p.U, p.V)) return false;
					}
				return true;
			}
			var first = MountainPlateauPlanner.Cell(plan.Width / 2, plan.Height / 2);
			var seen = new HashSet<CPos>(); var rejected = new HashSet<CPos>(); var pending = new Queue<CPos>();
			pending.Enqueue(first);
			while (pending.Count != 0)
			{
				var c = pending.Dequeue();
				if (seen.Contains(c) || rejected.Contains(c)) continue;
				if (!Clear(c)) { rejected.Add(c); continue; }
				seen.Add(c);
				foreach (var d in PlateauTopology.Directions) pending.Enqueue(c + d);
			}
			return plan.Spawns.All(p => seen.Contains(MountainPlateauPlanner.Cell(p.X, p.Y)));
		}
	}
}
