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
	sealed class RubberduckPlateauShapePlayTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-plateau-shape-playtest";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("EDITOR_SAVE OUTPUT", "Playtest the actual joined/trimmed editor save: raised bridge, ramps and protected resources/building.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[2]); Directory.CreateDirectory(output);
			using var stream = File.OpenRead(args[1]); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]);
			using var map = new Map(utility.ModData, zip);
			var center = RubberduckPlateauShapeTestCommand.First(map);
			if (RubberduckPlateauShapeTestCommand.Bridge(map).Any(c => map.Height[c] != 4 || map.Ramp[c] != 0) ||
				RubberduckPlateauShapeTestCommand.Trim(map).Any(c => map.Height[c] != 0)) throw new InvalidDataException("Save is not the joined/trimmed editor fixture.");
			if (map.Resources[center].Type != 1 || !map.ActorDefinitions.Any(n => n.Key == "ProtectedRefinery"))
				throw new InvalidDataException("Protected resource/building is missing.");
			var resultPath = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(resultPath)); File.WriteAllText(resultPath, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {center + new CVec(12, 0)}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: 0.35\n\t\tScreenshotTicks: 1400\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n");
			var actors = map.ActorDefinitions.ToList();
			void Probe(string name, int kind, CPos start, string trait)
			{
				rules.AppendLine($"{name}:\n\tInherits: {(kind == 0 ? "Light_Infantry" : "Heavy_Tank")}\n\t-ArmyFollower:\n\t-Autobattler:\n\tRenderSprites:\n\t\tImage: {(kind == 0 ? "light_infantry" : "heavytank")}\n{trait}\n\t\tResultPath: {resultPath}");
				actors.Add(new MiniYamlNode("ShapeMovement" + actors.Count, new ActorReference(name) { new LocationInit(start), new OwnerInit("Multi0") }.Save()));
			}
			for (var kind = 0; kind < 2; kind++)
			{
				var lane = kind == 0 ? 5 : 7;
				Probe("calibration.bridge" + kind, kind, center + new CVec(kind == 0 ? 5 : 19, lane),
					$"\tPlateauBridgeProbe:\n\t\tWaypoint: {center + new CVec(12, lane)}\n\t\tTarget: {center + new CVec(kind == 0 ? 19 : 5, lane)}");
			}
			for (var direction = 0; direction < 4; direction++)
				for (var kind = 0; kind < 2; kind++)
				{
					var outward = PlateauTopology.Directions[direction];
					var lateral = PlateauTopology.Directions[(direction + 1) % 4] * (kind == 0 ? -1 : 1);
					// Use unchanged clear-ground steep edges away from the trimmed NW corner.
					var top = center + outward * 7 + lateral * 3;
					var bottom = top + outward;
					if (map.Tiles[bottom].Type != 1000 || map.Height[top] != 4 || map.Ramp[top] != 0)
						throw new InvalidDataException("Ramp test's untouched paired edge is unavailable.");
					Probe($"calibration.shaperamp{direction}-{kind}", kind, center + outward * 9 + lateral,
						$"\tPlateauMovementProbe:\n\t\tTarget: {center + outward * 3 + lateral}\n\t\tRamp: {(direction + 2) % 4 + 1}\n\t\tCliffTop: {top}\n\t\tCliffBottom: {bottom}");
				}
			map.ActorDefinitions = actors; map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "shape-playtest"));
			map.Title = "Playtest of editor-shaped joined plateaus";
			var path = Path.Combine(output, "rubberduck-plateau-shape-playtest.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var checkStream = File.OpenRead(path); using var checkZip = new ZipFileLoader.ReadOnlyZipFile(checkStream, path);
			using var check = new Map(utility.ModData, checkZip);
			if (check.InvalidCustomRules) throw new InvalidDataException("Shape playtest rules failed.", check.InvalidCustomRulesException);
			foreach (var c in map.AllCells)
				if (map.Height[c] != check.Height[c] || !map.Tiles[c].Equals(check.Tiles[c]) || !map.Resources[c].Equals(check.Resources[c]))
					throw new InvalidDataException("Playtest export changed shaped terrain/resources.");
			Console.WriteLine("Saved geometry retained: bridge + four-ramp movement fixture " + path);
		}
	}
}
