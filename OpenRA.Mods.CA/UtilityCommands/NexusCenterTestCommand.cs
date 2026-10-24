using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class NexusCenterTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--nexus-center-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT", "Check full Dirt centers, unchanged planned gem placement, editor/game rules and export a live resource probe.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[1]);
			foreach (var mode in new[] { "tactical", "operational" })
				foreach (var seed in new[] { 42, 43 })
					foreach (var resources in new[] { "glitter", "patches", "none" })
					{
						var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", output, "preset=battle-nexus", "players=4", $"mode={mode}", $"seed={seed}", $"resources={resources}" });
						if (resources == "none") options.FiniteResources = true;
						var plan = MapPlanGenerator.Generate(options);
						var path = new RubberduckMapExporter(utility.ModData).Export(plan, options, Path.Combine(output, $"{mode}-{seed}-{resources}"));
						using var stream = File.OpenRead(path);
						using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
						using var map = new Map(utility.ModData, zip);
						var count = 0; var gems = 0;
						for (var y = 0; y < plan.Height; y++)
							for (var x = 0; x < plan.Width; x++)
							{
								if (plan.TerrainAt(x, y) != PlannedTerrain.Land || !plan.HasFeature(x, y, PlannedFeature.ContestedCenter)) continue;
								var cell = new MPos(x + 1, y + map.Grid.MaximumTerrainHeight + 1).ToCPos(map);
								if (map.GetTerrainInfo(cell).Type != "Dirt") throw new InvalidDataException("Center contains buildable/non-Dirt ground.");
								var rich = plan.HasFeature(x, y, PlannedFeature.RichResource);
								if ((map.Resources[cell].Type == 4) != rich) throw new InvalidDataException("Planned gem placement changed during export.");
								count++; if (rich) gems++;
							}
						if (count == 0 || (resources == "none" ? gems != 0 : gems == 0)) throw new InvalidDataException("Invalid center resource coverage.");
						var game = map.Rules.Actors["world"].TraitInfo<ResourceLayerInfo>().ResourceTypes;
						var editor = map.Rules.Actors["editorworld"].TraitInfo<EditorResourceLayerInfo>().ResourceTypes;
						if (new[] { "Tiberium", "BlueTiberium" }.Any(t => !game[t].AllowedTerrainTypes.Contains("Dirt") || !editor[t].AllowedTerrainTypes.Contains("Dirt")) ||
							game.ContainsKey("Ore") || editor.ContainsKey("Ore") || game.ContainsKey("Gems") || editor.ContainsKey("Gems"))
							throw new InvalidDataException("Game/editor Dirt permissions disagree or permit ore.");
						Console.WriteLine($"NEXUS CENTER PASS {mode} seed={seed} resources={resources}: dirt={count}, planned/exported gems={gems}.");
						if (mode != "tactical" || seed != 42 || resources != "glitter") continue;
						var logs = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes"); Directory.CreateDirectory(logs);
						var result = Path.Combine(logs, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output + "-nexus"))) + ".log");
						File.WriteAllText(result, ""); File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
						var center = new MPos(plan.Width / 2 + 1, plan.Height / 2 + map.Grid.MaximumTerrainHeight + 1).ToCPos(map);
						map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"World:\n\tNexusResourceProbe:\n\t\tResultPath: {result}\n\tTerrainCalibrationView:\n\t\tCenter: {center}\n\t\tZoomScale: 0.7\n\t\tMinimumZoomScale: 0.1\n\t\tScreenshotTicks: 160\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n", "nexus-center-probe"));
						using var package = ZipFileLoader.Create(Path.Combine(output, "nexus-center-probe.oramap"));
						map.Save(package);
						// Exercise old packages without rewriting them on load.
						foreach (var cell in map.AllCells)
						{
							var r = map.Resources[cell];
							if (r.Type == 3 || r.Type == 4) map.Resources[cell] = new ResourceTile((byte)(r.Type - 2), r.Index);
						}
						map.ActorDefinitions = map.ActorDefinitions.Select(actor => new MiniYamlNode(actor.Key,
							new MiniYaml(actor.Value.Value == "splitblue" ? "gmine" : actor.Value.Value == "split2" ? "mine" : actor.Value.Value, actor.Value.Nodes))).ToList();
						using var legacy = ZipFileLoader.Create(Path.Combine(output, "nexus-legacy-resource-probe.oramap"));
						map.Save(legacy);
					}
		}
	}
}
