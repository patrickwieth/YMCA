using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class StrategicFrontTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--strategic-front-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("", "Validate reserved Strategic fronts, team homes, deterministic objectives and independent cloning.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			TestWholeExpansionRims();
			var cases = 0;
			foreach (var players in new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 16 })
				foreach (var seed in new[] { 42, 43 })
				{
					var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "mode=strategic", "preset=mountain-valleys", "size=auto", $"players={players}", $"seed={seed}" });
					var a = MapPlanGenerator.Generate(options); var b = MapPlanGenerator.Generate(options);
					if (a.StrategicRoute.Count == 0 || a.StrategicReservations.Count == 0 || a.GenerationAttempt != b.GenerationAttempt ||
						!a.StrategicRoute.SequenceEqual(b.StrategicRoute) || !a.StrategicCheckpoints.SequenceEqual(b.StrategicCheckpoints) ||
						!a.Spawns.SequenceEqual(b.Spawns) || !a.StrategicReservations.SetEquals(b.StrategicReservations))
						throw new InvalidDataException("Strategic front is missing or nondeterministic.");
					for (var i = 0; i < players; i++)
						if (a.HomeOwnerAt(a.Spawns[i].X, a.Spawns[i].Y) != i) throw new InvalidDataException("Team slot and home owner disagree.");
					for (var y = 0; y < a.Height; y++)
						for (var x = 0; x < a.Width; x++)
							if (a.TerrainAt(x, y) != b.TerrainAt(x, y) || a.FeaturesAt(x, y) != b.FeaturesAt(x, y))
								throw new InvalidDataException("Strategic terrain/economy is nondeterministic.");
					var copy = a.Clone(); copy.StrategicRoute.Clear(); copy.StrategicReservations.Clear();
					if (a.StrategicRoute.Count == 0 || a.StrategicReservations.Count == 0) throw new InvalidDataException("Front clone aliases original.");
					Console.WriteLine($"STRATEGIC FRONT PASS players={players} seed={seed} attempt={a.GenerationAttempt} checkpoints={a.StrategicCheckpoints.Count}"); cases++;
				}
			Console.WriteLine($"STRATEGIC FRONT COMPLETE cases={cases}; each generated twice.");
		}

		static void TestWholeExpansionRims()
		{
			var origin = MountainPlateauPlanner.Cell(15, 15);
			var rim = (from x in Enumerable.Range(0, 4) from y in Enumerable.Range(0, 2)
				let p = MountainPlateauPlanner.Point(origin + new CVec(x, y)) select new PlanPoint(p.U, p.V)).ToArray();
			foreach (var blocked in rim)
			{
				var plan = new MapPlan(32, 32);
				plan.StrategicReservations.Add(new MPos(blocked.X, blocked.Y));
				plan.AddFeature(4, 4, PlannedFeature.Expansion | PlannedFeature.Resource);
				plan.SetTerrain(0, 0, PlannedTerrain.Mountain);
				var before = plan.Clone();
				var accepted = MountainValleysGenerator.PaintExpansionRim(plan, rim);
				if (accepted && (plan.PlateauSurfaces.Count == 0 || plan.PlateauSurfaces.ContainsKey(new MPos(blocked.X, blocked.Y))))
					throw new InvalidDataException("Replanned rim consumed its protected route or lacks closed surfaces.");
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if ((!accepted || before.TerrainAt(x, y) != PlannedTerrain.Land || before.FeaturesAt(x, y) != PlannedFeature.None ||
							plan.StrategicReservations.Contains(new MPos(x, y))) &&
							(plan.TerrainAt(x, y) != before.TerrainAt(x, y) || plan.FeaturesAt(x, y) != before.FeaturesAt(x, y)))
							throw new InvalidDataException("Rejected rim left fragments or replanning modified protected terrain/economy.");
			}

			var intact = new MapPlan(32, 32);
			// A route outside the complete reserved feet must remain open.
			var gate = MountainPlateauPlanner.Point(origin + new CVec(-3, 0));
			intact.StrategicReservations.Add(gate);
			if (!MountainValleysGenerator.PaintExpansionRim(intact, rim) ||
				rim.Any(p => intact.TerrainAt(p.X, p.Y) != PlannedTerrain.Mountain) ||
				intact.TerrainAt(gate.U, gate.V) != PlannedTerrain.Land || intact.PlateauSurfaces.Count == 0)
				throw new InvalidDataException("Uncut expansion rim or its existing gate changed.");
			Console.WriteLine("STRATEGIC RIM PASS eight protected-route replanning/rejection checks and closed-contour/open-route control.");
		}
	}
}
