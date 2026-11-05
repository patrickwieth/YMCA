using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class MigrationIslandTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--migration-island-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT", "Check larger Migration maps, isolated oil islands, open seas and cliff-free homes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var cases = 0;
			foreach (var mode in new[] { "tactical", "operational" })
				foreach (var players in new[] { 2, 3, 4, 6, 8, 12, 16 })
					foreach (var seed in new[] { 42, 43 })
					{
						var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "preset=migration", $"mode={mode}", $"players={players}", $"seed={seed}", "size=auto" });
						var oldSize = players <= 3 ? 80 : players == 4 ? 96 : players == 6 ? 112 : players == 8 ? 128 : players == 12 ? 160 : 192;
						if (options.Width != oldSize + 32 || options.Height != oldSize + 32) throw new InvalidDataException("Migration auto-size did not grow.");
						var plan = MapPlanGenerator.Generate(options); var repeat = MapPlanGenerator.Generate(options);
						if (!IslandsGenerator.ValidateMigrationIslands(plan, players) || !plan.Spawns.SequenceEqual(repeat.Spawns)) throw new InvalidDataException("Island separation/economy failed.");
						var water = new HashSet<CPos>(); var oilCount = 0;
						for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
						{
							if (plan.TerrainAt(x, y) != repeat.TerrainAt(x, y) || plan.FeaturesAt(x, y) != repeat.FeaturesAt(x, y)) throw new InvalidDataException("Nondeterministic layout/economy.");
							if (plan.HasFeature(x, y, PlannedFeature.CliffShore | PlannedFeature.ResourceGenerator | PlannedFeature.DestructibleBridge)) throw new InvalidDataException("Cliff bank, generator or causeway remains.");
							if (plan.HasFeature(x, y, PlannedFeature.TechBuilding)) oilCount++;
							if (plan.TerrainAt(x, y) == PlannedTerrain.Water) water.Add(PlannedRubberduckBridge.Cell(x, y));
						}
						var pending = new Queue<CPos>(); pending.Enqueue(water.First());
						while (pending.Count != 0)
						{
							var cell = pending.Dequeue(); if (!water.Remove(cell)) continue;
							foreach (var d in PlateauTopology.Directions) if (water.Contains(cell + d)) pending.Enqueue(cell + d);
						}
						if (water.Count != 0) throw new InvalidDataException("New islands split the open sea.");
						var path = new RubberduckMapExporter(utility.ModData).Export(plan, options, Path.Combine(args[1], $"{mode}-{players}-{seed}"));
						using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var map = new Map(utility.ModData, zip);
						if (map.ActorDefinitions.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.", StringComparison.Ordinal))) throw new InvalidDataException("Migration exported rock/bridge decorations.");
						var oils = map.ActorDefinitions.Where(n => n.Value.Value == "oilb").ToArray();
						if (oils.Length != oilCount || oilCount < players * 9 || oilCount > players * 10) throw new InvalidDataException("Export lost oil derricks.");
						foreach (var oil in oils)
						{
							var cell = new ActorReference(oil.Value.Value, oil.Value.ToDictionary()).Get<LocationInit>().Value;
							foreach (var c in map.Rules.Actors["oilb"].TraitInfo<BuildingInfo>().OccupiedTiles(cell))
								if (!map.Contains(c) || map.GetTerrainInfo(c).Type == "Water") throw new InvalidDataException("Oil derrick footprint overhangs water.");
						}
						var corrupt = plan.Clone(); var spawn = corrupt.Spawns[0]; corrupt.AddFeature(spawn.X, spawn.Y, PlannedFeature.TechBuilding);
						if (IslandsGenerator.ValidateMigrationIslands(corrupt, players)) throw new InvalidDataException("Extra home-oil negative control passed.");
						Console.WriteLine($"MIGRATION PASS {mode}/{players}/{seed} size={plan.Width} attempt={plan.GenerationAttempt} oilIslands={players} oils={oilCount} openSea=true"); cases++;
					}
			Console.WriteLine($"MIGRATION COMPLETE {cases} twice-generated physical exports; extra home-oil negative controls passed.");
		}
	}
}
