using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckPlateauRemovalPlayTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-plateau-removal-playtest";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 4;
		[Desc("ORIGINAL SAVED_EDITOR_MAP OUTPUT", "Verify an editor removal against its source and playtest former blocked cliff feet.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[3]);
			Directory.CreateDirectory(output);
			using var oldStream = File.OpenRead(args[1]);
			using var oldZip = new ZipFileLoader.ReadOnlyZipFile(oldStream, args[1]);
			using var original = new Map(utility.ModData, oldZip);
			using var stream = File.OpenRead(args[2]);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[2]);
			using var map = new Map(utility.ModData, zip);
			var clicked = original.AllCells.First(c => original.Height[c] == 4 && original.Tiles[c].Type == 1000 && map.Height[c] == 0);
			var plan = PlateauRemovalPlan.Create(original, original.ActorDefinitions, clicked);
			foreach (var c in map.AllCells)
			{
				if (plan.Patch.ContainsKey(c))
				{
					if (map.Height[c] != 0 || map.Ramp[c] != 0 || map.Tiles[c].Type != 1000) throw new InvalidDataException("Saved plateau not fully lowered.");
				}
				else if (original.Height[c] != map.Height[c] || !original.Tiles[c].Equals(map.Tiles[c]))
					throw new InvalidDataException("Editor removal changed unrelated terrain.");
				if (!original.Resources[c].Equals(map.Resources[c])) throw new InvalidDataException("Editor removal changed resources.");
			}
			var removed = plan.Decorations.Select(n => n.Key).ToHashSet();
			if (original.ActorDefinitions.Where(n => !removed.Contains(n.Key)).OrderBy(n => n.Key).WriteToString() !=
				map.ActorDefinitions.OrderBy(n => n.Key).WriteToString()) throw new InvalidDataException("Editor removal changed unrelated actors.");
			var resultPath = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
			File.WriteAllText(resultPath, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
			var center = new CPos((int)plan.Patch.Keys.Average(c => c.X), (int)plan.Patch.Keys.Average(c => c.Y));
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {center}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: 0.5\n\t\tScreenshotTicks: 900\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n");
			var actors = map.ActorDefinitions.ToList();
			var used = new HashSet<CPos>();
			var report = new StringBuilder("direction\tkind\tformer-roof\tformer-foot\n");
			for (var direction = 0; direction < 4; direction++)
				for (var kind = 0; kind < 2; kind++)
				{
					var d = PlateauTopology.Directions[direction];
					var candidates = plan.Patch.Keys.Where(c => original.Height[c] == 4 && original.Ramp[c] == 0 && original.Tiles[c].Type == 1000 &&
						plan.Patch.ContainsKey(c + d) && original.Height[c + d] == 0 && original.Tiles[c + d].Type == 3992 &&
						!used.Contains(c) && !used.Contains(c + d)).OrderBy(c => c.X).ThenBy(c => c.Y).ToArray();
					if (candidates.Length == 0) throw new InvalidDataException("No unused former cliff foot for direction " + direction);
					var top = candidates[0]; var foot = top + d;
					used.Add(top); used.Add(foot);
					var name = $"calibration.loweredprobe{direction}-{kind}";
					rules.AppendLine($"{name}:\n\tInherits: {(kind == 0 ? "Light_Infantry" : "Heavy_Tank")}\n\t-ArmyFollower:\n\t-Autobattler:\n\tRenderSprites:\n\t\tImage: {(kind == 0 ? "light_infantry" : "heavytank")}\n\tPlateauMovementProbe:\n\t\tLoweredCliffFoot: true\n\t\tCliffTop: {top}\n\t\tCliffBottom: {foot}\n\t\tResultPath: {resultPath}");
					var actor = new ActorReference(name) { new LocationInit(top), new OwnerInit("Multi0") };
					actors.Add(new MiniYamlNode("LoweredMovement" + actors.Count, actor.Save()));
					report.AppendLine($"{direction}\t{kind}\t{top}\t{foot}");
				}
			map.ActorDefinitions = actors;
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "removal-playtest"));
			map.Title = "Playtest of editor-lowered native plateau";
			var path = Path.Combine(output, "rubberduck-plateau-removal-playtest.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			File.WriteAllText(Path.Combine(output, "former-cliff-edges.tsv"), report.ToString());
			Console.WriteLine($"Source comparison passed; eight round trips across former cliff feet: {path}");
		}
	}
}
