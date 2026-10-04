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
	sealed class RubberduckEditorPlayTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-editor-playtest";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("SAVED_EDITOR_FIXTURE OUTPUT", "Run movement probes on the actual editor-saved plateau without regenerating terrain.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[2]);
			Directory.CreateDirectory(output);
			using var source = File.OpenRead(args[1]);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(source, args[1]);
			using var map = new Map(utility.ModData, zip);
			var center = new MPos(40, 48).ToCPos(map);
			var expected = PlateauTopology.CalibrationPlateau(center);
			foreach (var pair in expected)
				if (map.Height[pair.Key] != pair.Value.Height || map.Ramp[pair.Key] != pair.Value.Ramp)
					throw new InvalidDataException("Saved editor fixture does not contain the expected plateau.");
			var resultPath = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
			File.WriteAllText(resultPath, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {center}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: 0.5\n\t\tScreenshotTicks: 900\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n");
			var actors = map.ActorDefinitions.ToList();
			for (var direction = 0; direction < 4; direction++)
				for (var kind = 0; kind < 2; kind++)
				{
					var outward = PlateauTopology.Directions[direction];
					var lateral = PlateauTopology.Directions[(direction + 1) % 4] * (kind == 0 ? -1 : 1);
					var name = $"calibration.editorprobe{direction}-{kind}";
					var blockedFoot = map.Tiles[center + outward * 8 + lateral * 5].Type == 3992;
					rules.AppendLine($"{name}:\n\tInherits: {(kind == 0 ? "Light_Infantry" : "Heavy_Tank")}\n\t-ArmyFollower:\n\t-Autobattler:\n\tRenderSprites:\n\t\tImage: {(kind == 0 ? "light_infantry" : "heavytank")}\n\tPlateauMovementProbe:\n\t\tTarget: {center + outward * 2 + lateral}\n\t\tRamp: {(direction + 2) % 4 + 1}\n\t\tCliffTop: {center + outward * 7 + lateral * 5}\n\t\tCliffBottom: {center + outward * 8 + lateral * 5}\n\t\tBlockedCliffFoot: {blockedFoot.ToString().ToLowerInvariant()}\n\t\tResultPath: {resultPath}");
					var actor = new ActorReference(name) { new LocationInit(center + outward * 9 + lateral), new OwnerInit("Multi0") };
					actors.Add(new MiniYamlNode("EditorMovement" + actors.Count, actor.Save()));
				}
			map.ActorDefinitions = actors;
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "editor-playtest"));
			map.Title = "Playtest of editor-saved plateau";
			var path = Path.Combine(output, "rubberduck-editor-playtest.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var checkStream = File.OpenRead(path);
			using var checkZip = new ZipFileLoader.ReadOnlyZipFile(checkStream, path);
			using var check = new Map(utility.ModData, checkZip);
			if (check.InvalidCustomRules) throw new InvalidDataException("Editor playtest rules failed.", check.InvalidCustomRulesException);
			foreach (var cell in map.AllCells)
				if (map.Height[cell] != check.Height[cell] || map.Ramp[cell] != check.Ramp[cell] || !map.Tiles[cell].Equals(check.Tiles[cell]))
					throw new InvalidDataException("Playtest export changed editor terrain.");
			Console.WriteLine("Editor-saved terrain retained for runtime probes: " + path);
		}
	}
}
