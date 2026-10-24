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
	sealed class StrategicFrontPlayTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--strategic-front-playtest";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2;
		[Desc("OUTPUT [players=N] [seed=N] [size=auto] [preset=mountain-valleys|two-sides]", "Export a Strategic front and a separate autonomous two-team diagnostic copy.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var preset = args.FirstOrDefault(a => a.StartsWith("preset=", StringComparison.Ordinal)) ?? "preset=mountain-valleys";
			var options = DebugMapGeneratorCommand.ParseOptions(args.Concat(new[] { "mode=strategic", preset }).ToArray());
			var output = Path.GetFullPath(args[1]);
			var plan = MapPlanGenerator.Generate(options);
			var source = new RubberduckMapExporter(utility.ModData).Export(plan, options, output);
			using var stream = File.OpenRead(source);
			using var package = new ZipFileLoader.ReadOnlyZipFile(stream, source);
			using var map = new Map(utility.ModData, package);
			var players = new MapPlayers(map.PlayerDefinitions);
			foreach (var player in players.Players.Values.Where(p => p.Playable)) { player.Faction = "blackh"; player.LockFaction = true; }
			map.PlayerDefinitions = players.ToMiniYaml();
			var directory = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes"); Directory.CreateDirectory(directory);
			var result = Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			File.WriteAllText(result, ""); File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			var middle = plan.StrategicCheckpoints[plan.StrategicCheckpoints.Count / 2].Point;
			var center = new MPos(middle.X + 1, middle.Y + map.Grid.MaximumTerrainHeight + 1).ToCPos(map);
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($@"World:
	StrategicFrontProbe:
		ResultPath: {result}
	TerrainCalibrationView:
		Center: {center}
		ScreenshotTicks: 6500
		MinimumZoomScale: 0.25
		ZoomSteps: -15
	StartingUnits@FRONT-PROBE:
		Class: front-probe
		ClassName: Front diagnostic
		Factions: blackh
	SpawnStartingUnits:
		StartingUnitsClass: front-probe
		DropdownLocked: true
Player:
	-ConquestVictoryConditions:
	-CheckpointVictoryConditions:
	Shroud:
		FogCheckboxEnabled: false
		FogCheckboxLocked: true
		ExploredMapCheckboxEnabled: true
		ExploredMapCheckboxLocked: true
^Autobattler:
	Autobattler:
		waveInterval: 100
^Checkpoint:
	Checkpoint:
		waveInterval: 100
", "front-probe"));
			var target = Path.Combine(output, "strategic-front-playtest.oramap");
			using (var zip = ZipFileLoader.Create(target)) map.Save(zip);
			Console.WriteLine(target);
		}
	}
}
