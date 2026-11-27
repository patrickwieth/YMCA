using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class SymmetricOilTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--symmetric-oil-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Validate atomic Two Sides oil pairs, full native footprints and protected-layout symmetry.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var empty = new MapPlan(64, 64);
			var candidate = new PlanPoint(12, 12);
			var reflected = SymmetricOilPlacement.Mirror(empty, candidate);
			empty.SetTerrain(reflected.X, reflected.Y, PlannedTerrain.Water);
			if (SymmetricOilPlacement.TryPlacePair(empty, candidate) || empty.HasFeature(candidate.X, candidate.Y, PlannedFeature.TechBuilding))
				throw new InvalidDataException("Rejected mirror left an orphan oil actor.");
			empty.SetTerrain(reflected.X, reflected.Y, PlannedTerrain.Land);
			if (!SymmetricOilPlacement.TryPlacePair(empty, candidate) || SymmetricOilPlacement.TryPlacePair(empty, candidate))
				throw new InvalidDataException("Atomic placement failed or allowed overlapping oil.");
			var edge = new MapPlan(64, 64);
			var corner = MountainPlateauPlanner.Point(MountainPlateauPlanner.Cell(candidate.X, candidate.Y) + new CVec(1, 1));
			edge.SetTerrain(corner.U, corner.V, PlannedTerrain.Water);
			if (SymmetricOilPlacement.TryPlacePair(edge, candidate) || edge.HasFeature(reflected.X, reflected.Y, PlannedFeature.TechBuilding))
				throw new InvalidDataException("A clear anchor concealed water under the rest of the footprint.");
			var cases = 0;
			foreach (var players in new[] { 2, 4, 8 })
			foreach (var mode in new[] { "tactical", "operational", "strategic" })
			foreach (var density in new[] { "none", "sparse", "dense" })
			foreach (var seed in new[] { 42, 811863718 })
			{
				var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "preset=two-sides", $"mode={mode}",
					$"players={players}", $"seed={seed}", $"tech={density}", "size=auto" });
				var plan = MapPlanGenerator.Generate(options);
				var anchors = new HashSet<CPos>();
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
					if (plan.HasFeature(x, y, PlannedFeature.TechBuilding)) anchors.Add(MountainPlateauPlanner.Cell(x, y));
				if (density != "none" && anchors.Count == 0) throw new InvalidDataException("Requested oils were omitted.");
				if (density == "none" && anchors.Count != 0) throw new InvalidDataException("Unexpected oil actors.");
				var bank = new HashSet<CPos>();
				var pending = new Queue<CPos>();
				pending.Enqueue(MountainPlateauPlanner.Cell(plan.Spawns[0].X, plan.Spawns[0].Y));
				while (pending.Count != 0)
				{
					var c = pending.Dequeue(); var p = MountainPlateauPlanner.Point(c);
					if (!plan.Contains(p.U, p.V) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land || !bank.Add(c)) continue;
					foreach (var d in PlateauTopology.Directions) pending.Enqueue(c + d);
				}
				if (anchors.Count(bank.Contains) * 2 != anchors.Count)
					throw new InvalidDataException("Dry river banks have unequal oil counts.");
				var first = MountainPlateauPlanner.Cell(0, 0); var last = MountainPlateauPlanner.Cell(plan.Width - 1, plan.Height - 1);
				var occupied = new HashSet<CPos>();
				foreach (var c in anchors)
				{
					// Independent center-sum check, not a call to the placement mirror helper.
					if (!anchors.Any(other => other != c && other.X + c.X + 1 == first.X + last.X && other.Y + c.Y + 1 == first.Y + last.Y))
						throw new InvalidDataException("Oil centers are not natively symmetric.");
					for (var dy = 0; dy < 2; dy++) for (var dx = 0; dx < 2; dx++)
					{
						var cell = c + new CVec(dx, dy); var p = MountainPlateauPlanner.Point(cell);
						if (!occupied.Add(cell) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land ||
							plan.HasFeature(p.U, p.V, PlannedFeature.BuildClearance | PlannedFeature.Bottleneck | PlannedFeature.Resource | PlannedFeature.RichResource) ||
							plan.StrategicReservations.Contains(p) || plan.PlateauSurfaces.ContainsKey(p))
							throw new InvalidDataException("Oil footprint intersects protected terrain, resources or another derrick.");
					}
				}
				Console.WriteLine($"OIL PASS {players}P {mode} {density} seed={seed} oil={anchors.Count}");
				cases++;
			}
			Console.WriteLine($"OIL COMPLETE: {cases} full layouts plus atomic-rejection and overlap regressions.");
		}
	}
}
