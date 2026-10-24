using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class WaterBasinsShoreTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--water-basins-shore-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT", "Check open Water Basins shores in all three modes, including physical exported terrain.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var cases = 0;
			foreach (var mode in new[] { "tactical", "operational", "strategic" })
				foreach (var players in new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 16 })
					foreach (var seed in new[] { 42, 43 })
					{
						var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "preset=water-basins", $"mode={mode}", $"players={players}", $"seed={seed}", "size=auto" });
						var plan = MapPlanGenerator.Generate(options);
						var water = 0; var landings = 0;
						for (var y = 0; y < plan.Height; y++)
							for (var x = 0; x < plan.Width; x++)
							{
								if (plan.HasFeature(x, y, PlannedFeature.CliffShore) || plan.TerrainAt(x, y) == PlannedTerrain.Mountain)
									throw new InvalidDataException("Water Basins reserved a cliff obstacle.");
								if (plan.TerrainAt(x, y) == PlannedTerrain.Water) water++;
								if (plan.HasFeature(x, y, PlannedFeature.LaunchShore)) landings++;
							}
						if (water == 0 || landings == 0) throw new InvalidDataException("Basin water or landing reservations disappeared.");
						var path = new RubberduckMapExporter(utility.ModData).Export(plan, options, Path.Combine(args[1], $"{mode}-{players}-{seed}"));
						using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var map = new Map(utility.ModData, zip);
						if (map.AllCells.Any(c => map.Height[c] != 0 || map.Tiles[c].Type == 3992 || map.Tiles[c].Type >= 14010 && map.Tiles[c].Type <= 14012) ||
							map.ActorDefinitions.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.coast-", StringComparison.Ordinal)))
							throw new InvalidDataException("Export retained hidden cliff collision or coast actors.");
						if (mode == "strategic")
						{
							foreach (var p in plan.StrategicReservations)
								if (plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land || plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator))
									throw new InvalidDataException("Objective reservations were overwritten by water/economy.");
							Console.WriteLine(plan.ValidationMessages.Last(m => m.StartsWith("Basin home-to-own-front", StringComparison.Ordinal)));
							if (players == 4 && seed == 42)
							{
								void Reject(MapPlan invalid)
								{
									try { WaterBasinsStrategicPlanner.Validate(invalid, options); }
									catch (InvalidOperationException) { return; }
									throw new InvalidDataException("Invalid basin objective layout was accepted.");
								}
								var reversed = plan.Clone(); reversed.StrategicCheckpoints.Reverse(); Reject(reversed);
								var flooded = plan.Clone(); var pad = flooded.StrategicReservations.First();
								flooded.SetTerrain(pad.U, pad.V, PlannedTerrain.Water); Reject(flooded);
								if (flooded.TerrainAt(pad.U, pad.V) != PlannedTerrain.Water) throw new InvalidDataException("Validation filled water to rescue an objective.");
								var repeat = MapPlanGenerator.Generate(options);
								if (!plan.Spawns.SequenceEqual(repeat.Spawns) || !plan.StrategicCheckpoints.SequenceEqual(repeat.StrategicCheckpoints))
									throw new InvalidDataException("Basin front is not deterministic.");
								Console.WriteLine("BASIN FRONT NEGATIVE CONTROLS PASS reversed chain/flooded pad; deterministic repeat PASS");
							}
						}
						Console.WriteLine($"OPEN BASINS PASS mode={mode} players={players} seed={seed} water={water} landing-reservations={landings}");
						cases++;
					}
			Console.WriteLine($"OPEN BASINS COMPLETE {cases} generated and exported layouts.");
		}
	}
}
