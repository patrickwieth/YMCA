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
	sealed class RubberduckRampMovePlayTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-ramp-move-playtest";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("EDITOR_SAVE OUTPUT", "Playtest the relocated and retained ramps on the actual editor save.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[2]); Directory.CreateDirectory(output);
			using var stream = File.OpenRead(args[1]); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]);
			using var map = new Map(utility.ModData, zip);
			var source = RubberduckRampMoveTestCommand.Source(map); var target = RubberduckRampMoveTestCommand.Target(map);
			if (map.Ramp[source] != 0 || map.Height[source] != 4 || map.Ramp[target] != 2) throw new InvalidDataException("Not the moved-ramp editor save.");
			var result = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(result)); File.WriteAllText(result, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {RubberduckRampMoveTestCommand.Center(map) + new CVec(9, -6)}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: 0.4\n\t\tScreenshotTicks: 1200\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n");
			var actors = map.ActorDefinitions.ToList();
			for (var ascent = 0; ascent < 4; ascent++)
				for (var kind = 0; kind < 2; kind++)
				{
					var direction = ascent == 0 ? 3 : ascent;
					var outward = PlateauTopology.Directions[direction]; var lateral = PlateauTopology.Directions[(direction + 1) % 4] * (kind == 0 ? -1 : 1);
					var toe = ascent == 0 ? target : RubberduckRampMoveTestCommand.Center(map) + outward * 12;
					var name = $"calibration.movedramp{ascent}-{kind}";
					rules.AppendLine($"{name}:\n\tInherits: {(kind == 0 ? "Light_Infantry" : "Heavy_Tank")}\n\t-ArmyFollower:\n\t-Autobattler:\n\tRenderSprites:\n\t\tImage: {(kind == 0 ? "light_infantry" : "heavytank")}\n\tPlateauMovementProbe:\n\t\tTarget: {toe - outward * 5 + lateral}\n\t\tRamp: {(direction + 2) % 4 + 1}\n\t\tCliffTop: {source}\n\t\tCliffBottom: {source + new CVec(1, 0)}\n\t\tBlockedCliffFoot: {(map.Tiles[source + new CVec(1, 0)].Type == 3992 ? "true" : "false")}\n\t\tResultPath: {result}");
					actors.Add(new MiniYamlNode("MovedRampProbe" + actors.Count, new ActorReference(name) { new LocationInit(toe + outward * 2 + lateral), new OwnerInit("Multi0") }.Save()));
				}
			map.ActorDefinitions = actors; map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "moved-ramp-playtest"));
			map.Title = "Playtest of editor-relocated ramp";
			var path = Path.Combine(output, "rubberduck-ramp-move-playtest.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var checkStream = File.OpenRead(path); using var checkZip = new ZipFileLoader.ReadOnlyZipFile(checkStream, path);
			using var check = new Map(utility.ModData, checkZip);
			if (check.InvalidCustomRules) throw new InvalidDataException("Ramp playtest rules failed.", check.InvalidCustomRulesException);
			foreach (var c in map.AllCells)
				if (map.Height[c] != check.Height[c] || !map.Tiles[c].Equals(check.Tiles[c]) || !map.Resources[c].Equals(check.Resources[c])) throw new InvalidDataException("Playtest export changed editor terrain.");
			Console.WriteLine("Saved ramp geometry retained: " + path);
		}
	}
}
