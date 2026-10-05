using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.Mods.CA.Traits;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	// Diagnostic copy for the shared legacy renderer; original packages remain untouched.
	sealed class RubberduckLegacyFootTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-legacy-foot-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 3 && args.Length <= 7 && args.Skip(3).All(a => a == "shaded" || a == "manual" || a == "editor" || a == "east");
		[Desc("LEGACY_MAP OUTPUT [shaded] [manual] [editor] [east]",  "Review original rock-pile abutments on existing blocked legacy feet, without terrain changes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			Directory.CreateDirectory(args[2]);
			using var input = File.OpenRead(args[1]);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(input, args[1]);
			using var map = new Map(utility.ModData, zip);
			if (args.Contains("shaded")) map.Tileset = "RUBBERDUCK-TEMPERATE-SHADED";
			var east = args.Contains("east");
			var cells = east ? new[] { new CPos(52, -8) } : new[] { new CPos(86, -22), new CPos(87, -22) };
			var center = east ? new CPos(51, -8) : new CPos(87, -23);
			var start = east ? new CPos(52, -6) : new CPos(89, -22);
			var target = east ? new CPos(52, -7) : new CPos(88, -22);
			foreach (var cell in cells)
				if (!map.Contains(cell) || map.Height[cell] != 0 || map.Ramp[cell] != 0 || map.Tiles[cell].Type != 3992 || map.Resources[cell].Type != 0)
					throw new InvalidDataException("Expected an already blocked/resource-free legacy foot: " + cell);
			const string type = "calibration.legacytalus";
			var actors = map.ActorDefinitions.ToList();
			var entries = actors.Select(n =>
			{
				var actor = new ActorReference(n.Value.Value, n.Value.ToDictionary());
				return (map.Rules.Actors[actor.Type], actor.Get<LocationInit>().Value);
			}).ToArray();
			var selected = LegacyCliffAbutments.Select(map, entries);
			var protectedCell = cells[cells.Length - 1];
			var resource = map.Resources[protectedCell];
			try
			{
				map.Resources[protectedCell] = new ResourceTile(1, 1);
				if (LegacyCliffAbutments.Select(map, entries).Contains(protectedCell)) throw new InvalidDataException("Abutment covered a resource.");
			}
			finally { map.Resources[protectedCell] = resource; }
			var withBuilding = entries.Concat(new[] { (map.Rules.Actors["syrd"], protectedCell - new CVec(1, 1)) });
			if (LegacyCliffAbutments.Select(map, withBuilding).Contains(protectedCell)) throw new InvalidDataException("Abutment covered a full building footprint.");
			if (cells.Any(c => !selected.Contains(c))) throw new InvalidDataException("Known legacy feet were not selected.");
			if (args.Contains("manual"))
				for (var i = 0; i < cells.Length; i++)
					actors.Add(new MiniYamlNode("LegacyTalus" + i, new ActorReference(type) { new LocationInit(cells[i]), new OwnerInit("Neutral") }.Save()));
			var logs = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes");
			Directory.CreateDirectory(logs);
			var result = Path.Combine(logs, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(args[2])))) + ".log");
			File.WriteAllText(result, "");
			File.WriteAllText(Path.Combine(args[2], "runtime-results-path.txt"), result);
			actors.Add(new MiniYamlNode("LegacyFootVehicle", new ActorReference("calibration.legacyvehicle") { new LocationInit(start), new OwnerInit("Multi0") }.Save()));
			map.ActorDefinitions = actors;
			var vehicle = map.Rules.Actors["challenger_tank"];
			var image = vehicle.TraitInfo<RenderSpritesInfo>().Image ?? "challenger_tank";
			var voxel = vehicle.TraitInfo<RenderVoxelsInfo>().Image ?? "challenger_tank";
			var editorProbe = args.Contains("editor") ? $"\tLegacyFootEditorProbe:\n\t\tCell: {cells[cells.Length - 1]}\n\t\tCenter: {center}\n\t\tResultPath: {result}\n" : "";
			var editorWorld = args.Contains("editor") ? $"EditorWorld:\n{editorProbe}" : "";
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"{editorWorld}World:\n{editorProbe}\tTerrainCalibrationView:\n\t\tCenter: {center}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: 0.65\n\t\tScreenshotTicks: -1\ncalibration.legacyvehicle:\n\tInherits: Challenger_Tank\n\tRenderSprites:\n\t\tImage: {image}\n\tRenderVoxels:\n\t\tImage: {voxel}\n\tShoreMovementProbe:\n\t\tTarget: {target}\n\t\tForbidden: {cells[cells.Length - 1]}\n\t\tForbiddenTerrainType: 3992\n\t\tResultPath: {result}\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n{type}:\n\tInherits: terrain.rubberduck.nativecliff0\n\tNativeCliffBody:\n\t\tGroundAbutment: true\n", "legacy-talus"));
			map.SequenceDefinitions = new MiniYaml("", MiniYaml.FromString($"{type}:\n\tidle:\n\t\tFilename: bits/terrain/rubberduck/derived/coasts/coast-abutment-0.png\n", "legacy-talus"));
			map.Title = "Legacy blocked-foot abutment review";
			var output = Path.Combine(args[2], "rubberduck-legacy-foot.oramap");
			using (var package = ZipFileLoader.Create(output)) map.Save(package);
			using var saved = File.OpenRead(output);
			using var savedZip = new ZipFileLoader.ReadOnlyZipFile(saved, output);
			using var reload = new Map(utility.ModData, savedZip);
			if (reload.InvalidCustomRules) throw new InvalidDataException("Invalid legacy review rules.", reload.InvalidCustomRulesException);
			foreach (var cell in map.AllCells)
				if (!map.Tiles[cell].Equals(reload.Tiles[cell]) || map.Height[cell] != reload.Height[cell] || !map.Resources[cell].Equals(reload.Resources[cell]))
					throw new InvalidDataException("Legacy foot review altered terrain.");
			File.WriteAllText(Path.Combine(args[2], "scope.txt"), $"Shared legacy renderer review using the existing unchanged authored rock_cliffs slot-16 crop. All original terrain, collision, resources, starts and actors retained; one diagnostic vehicle added. Manual duplicate-decoration mode: {args.Contains("manual")}. Selected blocked feet: {selected.Count}. Native closed families and blocked mountain roofs are excluded.\n");
			Console.WriteLine(output);
		}
	}
}
