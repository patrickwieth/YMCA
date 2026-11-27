using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class PlainsOilClusterTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--test-plains-oil-clusters";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT.json", "Check outer Plains oil clusters across player counts, densities and seeds.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var results = new List<object>();
			foreach (var players in Enumerable.Range(2, 11).Append(16))
			foreach (var density in new[] { TechBuildingDensity.Sparse, TechBuildingDensity.Dense })
			foreach (var seed in new[] { 1, 42, 43 })
			{
				var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "preset=plains", "mode=tactical", $"players={players}", $"seed={seed}", $"tech={density}", "size=auto" });
				var plan = MapPlanGenerator.Generate(options);
				PlainsOilClusters.Validate(plan, density);
				if (plan.OilClusters.Count != players || plan.OilClusters.Any(c => c.Towers.Length != (density == TechBuildingDensity.Sparse ? 3 : 4)))
					throw new InvalidDataException("Wrong cluster count or size.");
				if (players == 2 && plan.OilClusters.Any(c => (c.Target.X != 7 && c.Target.X != plan.Width - 8) || (c.Target.Y != 7 && c.Target.Y != plan.Height - 8)))
					throw new InvalidDataException("1v1 oil targets must be unused corners, not side-midpoint targets.");
				var clone = plan.Clone();
				var first = clone.OilClusters[0].Towers[0]; clone.RemoveFeature(first.X, first.Y, PlannedFeature.TechBuilding);
				var rejected = false;
				try { PlainsOilClusters.Validate(clone, density); } catch (InvalidOperationException) { rejected = true; }
				if (!rejected || !plan.HasFeature(first.X, first.Y, PlannedFeature.TechBuilding)) throw new InvalidDataException("Lost cluster accepted, or clone damaged original.");
				results.Add(new { Players = players, Seed = seed, Density = density.ToString(), plan.GenerationAttempt, Clusters = plan.OilClusters.Select(c => new {
					Target = new[] { c.Target.X, c.Target.Y }, Towers = c.Towers.Select(p => new[] { p.X, p.Y }).ToArray() }).ToArray() });
				File.WriteAllText(args[1], JsonConvert.SerializeObject(results, Formatting.Indented));
				Console.WriteLine($"PASS Plains oil {players}P {density} seed={seed}: {plan.OilClusters.Count} clusters");
			}
		}
	}
}
