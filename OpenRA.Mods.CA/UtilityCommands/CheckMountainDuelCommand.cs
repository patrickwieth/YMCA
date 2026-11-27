using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckMountainDuelCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-mountain-duel";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;

		[Desc("OUTPUT_DIRECTORY", "Check enlarged mountain duels, non-forward home ramps and native path/resource access.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			foreach (var seed in new[] { 1, 2, 3, 7, 11, 42, 43, 101 })
			foreach (var resources in new[] { "Patches", "Glitter" })
			{
				var options = DebugMapGeneratorCommand.ParseOptions(new[] { "check", args[1], "preset=MountainValleys",
					"players=2", "seed=" + seed, "resources=" + resources, "finite=" + (seed % 2 == 0), "size=auto" });
				if (options.Width != 112 || options.Height != 112)
					throw new InvalidDataException("Mountain duel must be 112x112.");
				var plan = MapPlanGenerator.Generate(options);
				MapPlanGameplayValidator.Validate(plan, options);
				for (var player = 0; player < 2; player++)
				{
					var home = plan.Spawns[player];
					var other = plan.Spawns[1 - player];
					var origin = MountainPlateauPlanner.Cell(home.X, home.Y);
					var target = MountainPlateauPlanner.Cell(other.X, other.Y);
					var ramps = plan.Plateaus.Where(p => plan.HomeOwnerAt(p.Landing.X, p.Landing.Y) == player).ToArray();
					if (ramps.Length != 1) throw new InvalidDataException("Expected exactly one home ramp.");
					var distances = StartBalanceMetrics.Distances(plan, home);
					foreach (var ramp in ramps)
					{
						var top = MountainPlateauPlanner.Cell(ramp.Landing.X, ramp.Landing.Y);
						var toe = MountainPlateauPlanner.Cell(ramp.Approach.X, ramp.Approach.Y);
						var dot = (toe.X - top.X) * (target.X - origin.X) + (toe.Y - top.Y) * (target.Y - origin.Y);
						if (dot > 0 || distances[ramp.Approach.X + ramp.Approach.Y * plan.Width] < 0)
							throw new InvalidDataException("Forward-facing or inaccessible home ramp.");
					}
					var path = distances[other.X + other.Y * plan.Width];
					if (path < 0) throw new InvalidDataException("Enemy home is unreachable.");
					Console.WriteLine($"PASS seed={seed} {resources} finite={options.FiniteResources} player={player + 1}: 112x112, non-forward ramp, opponent path={path} native cardinal steps.");
				}
				if (seed == 42)
					new RubberduckMapExporter(utility.ModData).Export(plan, options, Path.Combine(args[1], resources));
			}
			var desert = DebugMapGeneratorCommand.ParseOptions(new[] { "check", args[1], "preset=Desert", "players=2", "size=auto" });
			if (desert.Width != 80 || desert.Height != 80) throw new InvalidDataException("Desert rush map must stay 80x80.");
			Console.WriteLine("PASS Desert remains 80x80.");
		}
	}
}
