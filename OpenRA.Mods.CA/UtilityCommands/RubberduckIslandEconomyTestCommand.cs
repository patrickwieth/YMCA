using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckIslandEconomyTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-island-economy-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("", "Regression: complete deterministic Archipelago home economies must never be placed in water.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var cases = 0;
			foreach (var players in new[] { 4, 12, 16 })
				foreach (var seed in new[] { 42, 43, 101, 202 })
				{
					var size = players == 4 ? 96 : players == 12 ? 160 : 192;
					var options = new MapGenerationOptions { Preset = MapGenerationPreset.Archipelago,
						Players = players, Width = size, Height = size, Seed = seed, Resources = ResourceFieldLayout.Patches, TechBuildings = TechBuildingDensity.Dense };
					var a = MapPlanGenerator.Generate(options);
					var b = MapPlanGenerator.Generate(options);
					if (a.GenerationAttempt != b.GenerationAttempt || !a.Spawns.SequenceEqual(b.Spawns))
						throw new InvalidDataException("Archipelago retry changed deterministic spawn selection.");
					var resources = 0; var generators = 0;
					for (var y = 0; y < size; y++)
						for (var x = 0; x < size; x++)
						{
							if (a.TerrainAt(x, y) != b.TerrainAt(x, y) || a.FeaturesAt(x, y) != b.FeaturesAt(x, y) || a.HomeOwnerAt(x, y) != b.HomeOwnerAt(x, y))
								throw new InvalidDataException("Archipelago retry is not deterministic.");
							if (a.HasFeature(x, y, PlannedFeature.Resource)) resources++;
							if (a.HasFeature(x, y, PlannedFeature.ResourceGenerator)) generators++;
							if (a.HasFeature(x, y, PlannedFeature.Resource | PlannedFeature.ResourceGenerator) && a.TerrainAt(x, y) != PlannedTerrain.Land)
								throw new InvalidDataException("Home economy left its dry island.");
						}
					if (resources != players * 13 || generators != players)
						throw new InvalidDataException("Retry removed or enlarged an Archipelago home resource patch.");
					Console.WriteLine($"ISLAND ECONOMY PASS players={players} seed={seed} attempt={a.GenerationAttempt} resources={resources} generators={generators}");
					cases++;
				}
			Console.WriteLine($"PASS: {cases} twice-generated economies, complete equal-sized dry patches and deterministic retries.");
		}
	}
}
