using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckBridgePlayTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-bridge-playtest";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 3 && args.Length <= 5;
		[Desc("MAP OUTPUT [editor] [shaded]", "Keep generated bridge terrain/rules/actors; add runtime or editor assertions and a closeup.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[2]); Directory.CreateDirectory(output);
			using var stream = File.OpenRead(args[1]); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]); using var map = new Map(utility.ModData, zip);
			if (!map.Tileset.StartsWith("RUBBERDUCK-", StringComparison.Ordinal) || !RubberduckBridgeExporter.OnlyBridgeRules(map.RuleDefinitions))
				throw new InvalidDataException("Expected production Rubberduck bridge rules only.");
			var bridge = map.ActorDefinitions.First(n => n.Value.Value.StartsWith("terrain.rubberduck.bridge.", StringComparison.Ordinal));
			var cell = new ActorReference(bridge.Value.Value, bridge.Value.ToDictionary()).Get<LocationInit>().Value;
			var body = map.Rules.Actors[bridge.Value.Value].TraitInfo<RubberduckBridgeBodyInfo>();
			var center = cell + (body.AlongY ? new CVec(0, body.Length / 2) : new CVec(body.Length / 2, 0));
			if (args.Contains("shaded")) map.Tileset = "RUBBERDUCK-TEMPERATE-SHADED";
			var logs = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes"); Directory.CreateDirectory(logs);
			var result = Path.Combine(logs, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			File.WriteAllText(result, ""); File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			var editor = args.Contains("editor");
			var probe = editor ? $"\tRubberduckBridgeEditorProbe:\n\t\tCell: {cell}\n\t\tResultPath: {result}\n" : $"\tRubberduckBridgeProbe:\n\t\tResultPath: {result}\n";
			var nodes = map.RuleDefinitions.Nodes.ToList();
			nodes.AddRange(MiniYaml.FromString((editor ? "EditorWorld:\n" + probe : "") + $"World:\n{probe}\tTerrainCalibrationView:\n\t\tCenter: {center}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: 0.7\n\t\tScreenshotTicks: 1200\nPlayer:\n\t-ConquestVictoryConditions:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n", "bridge-production-diagnostic"));
			map.RuleDefinitions = new MiniYaml("", nodes);
			var path = Path.Combine(output, "rubberduck-bridge-playtest.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			Console.WriteLine(path);
		}
	}
}
