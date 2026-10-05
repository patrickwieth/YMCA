using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckBridgeTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-bridge-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2 && args.Length <= 4;
		[Desc("OUTPUT [shaded] [focus=x|y]",  "Six bridge spans: both axes, short/repeated decks, real crossing and collapse/water restoration.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
			var plan = new MapPlan(82, 98);
			plan.Spawns.Add(new PlanPoint(22, 70)); plan.Spawns.Add(new PlanPoint(45, 70));
			foreach (var alongY in new[] { false, true })
				for (var i = 0; i < 3; i++)
				{
					var length = new[] { 12, 6, 3 }[i];
					var start = new CPos(36 + i * 16, alongY ? 8 : -16);
					var bridge = new PlannedRubberduckBridge(start, alongY, length);
					var side = alongY ? new CVec(1, 0) : new CVec(0, 1);
					for (var n = 1; n < length - 1; n++)
						for (var lateral = -3; lateral <= 3; lateral++)
						{
							var p = PlannedRubberduckBridge.Point(start + bridge.Step * n + side * lateral);
							plan.SetTerrain(p.U, p.V, lateral == 0 ? PlannedTerrain.ShallowWater : PlannedTerrain.Water);
							if (lateral == 0) plan.AddFeature(p.U, p.V, PlannedFeature.DestructibleBridge);
						}
					plan.Bridges.Add(bridge);
				}
			var options = new MapGenerationOptions { Players = 2, Width = 82, Height = 98, Preset = MapGenerationPreset.Continents };
			var source = new RubberduckMapExporter(utility.ModData).Export(plan, options, output);
			using var stream = File.OpenRead(source); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, source);
			using var map = new Map(utility.ModData, zip);
			if (args.Contains("shaded")) map.Tileset = "RUBBERDUCK-TEMPERATE-SHADED";
			var logs = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes"); Directory.CreateDirectory(logs);
			var result = Path.Combine(logs, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			if (File.Exists(result)) File.Delete(result);
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			var focus = args.FirstOrDefault(a => a.StartsWith("focus=", StringComparison.Ordinal));
			var camera = focus == "focus=x" ? "50,-8" : focus == "focus=y" ? "44,21" : "64,8";
			var zoom = focus == null ? "0.25" : "0.8";
			var rules = map.RuleDefinitions.Nodes.ToList();
			rules.AddRange(MiniYaml.FromString($"World:\n\tRubberduckBridgeProbe:\n\t\tResultPath: {result}\n\tTerrainCalibrationView:\n\t\tCenter: {camera}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: {zoom}\n\t\tScreenshotTicks: 1200\nPlayer:\n\t-ConquestVictoryConditions:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n", "bridge-diagnostics"));
			map.RuleDefinitions = new MiniYaml("", rules);
			var path = Path.Combine(output, "rubberduck-bridge-test.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			Console.WriteLine(path);
		}
	}
}
