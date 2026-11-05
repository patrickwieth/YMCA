using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class HillFortressPlateauPlanner
	{
		public static bool Prepare(MapPlan plan, MapGenerationOptions options, MersenneTwister random)
		{
			var alongX = random.Next(2) == 0;
			int Axis(CPos c) => alongX ? c.X : c.Y;
			int Lateral(CPos c) => alongX ? c.Y : c.X;
			var first = MountainPlateauPlanner.Cell(0, 0);
			var last = MountainPlateauPlanner.Cell(plan.Width - 1, plan.Height - 1);
			var middle = (Axis(first) + Axis(last)) / 2.0;
			var halfValley = Math.Max(8, Math.Min(options.Width, options.Height) / 10);
			plan.RequireClosedPlateaus = true;
			for (var team = 0; team < 2; team++)
			{
				var footprint = new List<PlanPoint>();
				// Close the backing contour beyond the playable rectangle. The roof
				// continues across the visible map boundary without a peripheral cliff.
				for (var y = 6; y < plan.Height - 6; y++) for (var x = 6; x < plan.Width - 6; x++)
				{
					var axis = Axis(MountainPlateauPlanner.Cell(x, y));
					if (team == 0 ? axis < middle - halfValley : axis > middle + halfValley)
						footprint.Add(new PlanPoint(x, y));
				}
				if (footprint.Count == 0) return false;
				var center = new PlanPoint((int)Math.Round(footprint.Average(p => p.X)), (int)Math.Round(footprint.Average(p => p.Y)));
				if (!MountainPlateauPlanner.AddHome(plan, footprint, center, team, insetRamp: true,
					rampCount: options.Custom?.BattleInTheCenter == false ? 2 : 1)) return false;
			}

			foreach (var pair in plan.PlateauSurfaces.Where(p => p.Value.Ramp != 0))
				plan.AddFeature(pair.Key.U, pair.Key.V, PlannedFeature.Bottleneck);
			var radius = 9;
			for (var team = 0; team < 2; team++)
			{
				bool Safe(PlanPoint p)
				{
					for (var y = -radius; y <= radius; y++) for (var x = -radius; x <= radius; x++)
						if (x * x + y * y <= radius * radius && !FlatHome(plan, p.X + x, p.Y + y, team)) return false;
					return true;
				}
				var candidates = plan.PlateauSurfaces.Where(p => plan.HomeOwnerAt(p.Key.U, p.Key.V) == team)
					.Select(p => new PlanPoint(p.Key.U, p.Key.V)).Where(Safe).ToArray();
				if (candidates.Length == 0) return false;
				var cells = candidates.Select(p => MountainPlateauPlanner.Cell(p.X, p.Y)).ToArray();
				var axis = cells.Average(c => Axis(c));
				var low = cells.Min(Lateral); var high = cells.Max(Lateral);
				var count = team == 0 ? (options.Players + 1) / 2 : options.Players / 2;
				for (var player = 0; player < count; player++)
				{
					var target = low + (high - low) * (player + 1.0) / (count + 1.0);
					var eligible = candidates.Where(p => plan.Spawns.All(s => (s.X - p.X) * (s.X - p.X) + (s.Y - p.Y) * (s.Y - p.Y) >= 64))
						.OrderBy(p =>
						{
							var c = MountainPlateauPlanner.Cell(p.X, p.Y);
							return Math.Pow(Axis(c) - axis, 2) + Math.Pow(Lateral(c) - target, 2);
						}).ThenBy(p => p.X).ThenBy(p => p.Y).ToArray();
					if (eligible.Length == 0) return false;
					var spawn = eligible[0]; plan.Spawns.Add(spawn);
					for (var y = -radius; y <= radius; y++) for (var x = -radius; x <= radius; x++)
						if (x * x + y * y <= radius * radius) plan.AddFeature(spawn.X + x, spawn.Y + y, PlannedFeature.BuildClearance);
					plan.AddFeature(spawn.X, spawn.Y, PlannedFeature.Spawn);
				}
			}

			var centerX = (plan.Width - 1) / 2.0; var centerY = (plan.Height - 1) / 2.0;
			var centerRadius = Math.Min(options.Width, options.Height) * .23;
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				if (plan.TerrainAt(x, y) == PlannedTerrain.Land && !plan.PlateauSurfaces.ContainsKey(new MPos(x, y)) &&
					Math.Abs(Axis(MountainPlateauPlanner.Cell(x, y)) - middle) < halfValley - 3 &&
					(x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= centerRadius * centerRadius)
					plan.AddFeature(x, y, PlannedFeature.ContestedCenter);
			return true;
		}

		internal static bool FlatHome(MapPlan plan, int x, int y, int owner = -1) =>
			plan.Contains(x, y) && x >= plan.PlayableInset && x < plan.Width - plan.PlayableInset &&
			y - 4 >= plan.PlayableInset && y - 4 < plan.Height - plan.PlayableInset &&
			(owner < 0 || plan.HomeOwnerAt(x, y) == owner) &&
			plan.HasFeature(x, y, PlannedFeature.Home) && plan.PlateauSurfaces.TryGetValue(new MPos(x, y), out var surface) &&
			surface.Height == 4 && surface.Ramp == 0 && !surface.Blocked;
	}
}
