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
	sealed class RubberduckExistingShapePlayTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-existing-shape-playtest";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 4;
		[Desc("ORIGINAL EDITOR_SAVE OUTPUT", "Playtest infantry/tank access to an actual generated plateau extension.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[3]); Directory.CreateDirectory(output);
			using var oldStream = File.OpenRead(args[1]); using var oldZip = new ZipFileLoader.ReadOnlyZipFile(oldStream, args[1]);
			using var original = new Map(utility.ModData, oldZip);
			using var stream = File.OpenRead(args[2]); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[2]);
			using var map = new Map(utility.ModData, zip);
			var grown = map.AllCells.Single(c => map.Contains(c) && original.Height[c] == 0 && map.Height[c] == 4 && map.Tiles[c].Type == 1000);
			foreach (var c in map.AllCells)
			{
				if (!original.Resources[c].Equals(map.Resources[c])) throw new InvalidDataException("Generated shape changed resources.");
				if (original.Ramp[c] != 0 && (map.Ramp[c] != original.Ramp[c] || map.Height[c] != original.Height[c] || !map.Tiles[c].Equals(original.Tiles[c])))
					throw new InvalidDataException("Generated shape changed a ramp.");
			}
			string ProtectedActors(Map source) => source.ActorDefinitions.Where(n => !PlateauRemovalPlan.Decoration(n.Value.Value)).OrderBy(n => n.Key).WriteToString();
			if (ProtectedActors(map) != ProtectedActors(original)) throw new InvalidDataException("Generated shape changed non-cliff actors.");
			bool Roof(CPos c) => map.Contains(c) && map.Height[c] == 4 && map.Ramp[c] == 0 && map.Tiles[c].Type == 1000;
			var top = PlateauTopology.Directions.Select(d => grown + d).First(Roof);
			var other = PlateauTopology.Directions.Select(d => top + d).First(c => c != grown && Roof(c));
			var resultPath = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(resultPath)); File.WriteAllText(resultPath, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {grown}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: 0.5\n\t\tScreenshotTicks: 1200\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n");
			var actors = map.ActorDefinitions.ToList();
			for (var kind = 0; kind < 2; kind++)
			{
				var name = "calibration.existingextension" + kind;
				// Park the inactive tank away from the infantry route until its turn.
				var start = kind == 0 ? top : PlateauTopology.Directions.Select(d => other + d).First(c => c != top && c != grown && Roof(c));
				rules.AppendLine($"{name}:\n\tInherits: {(kind == 0 ? "Light_Infantry" : "Heavy_Tank")}\n\t-ArmyFollower:\n\t-Autobattler:\n\tRenderSprites:\n\t\tImage: {(kind == 0 ? "light_infantry" : "heavytank")}\n\tPlateauBridgeProbe:\n\t\tWaypoint: {grown}\n\t\tTarget: {other}\n\t\tStartDelay: {kind * 300}\n\t\tResultPath: {resultPath}");
				actors.Add(new MiniYamlNode("ExtensionMovement" + kind, new ActorReference(name) { new LocationInit(start), new OwnerInit("Multi0") }.Save()));
			}
			map.ActorDefinitions = actors; map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "extension-playtest"));
			map.Title = "Playtest of edited generated plateau extension";
			var path = Path.Combine(output, "rubberduck-existing-shape-playtest.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var checkStream = File.OpenRead(path); using var checkZip = new ZipFileLoader.ReadOnlyZipFile(checkStream, path);
			using var check = new Map(utility.ModData, checkZip);
			if (check.InvalidCustomRules) throw new InvalidDataException("Extension playtest rules failed.", check.InvalidCustomRulesException);
			foreach (var c in map.AllCells)
				if (map.Height[c] != check.Height[c] || !map.Tiles[c].Equals(check.Tiles[c]) || !map.Resources[c].Equals(check.Resources[c]))
					throw new InvalidDataException("Extension playtest changed saved geometry.");
			Console.WriteLine($"Protected actors, resources and ramps retained; extension {grown}: {path}");
		}
	}
}
