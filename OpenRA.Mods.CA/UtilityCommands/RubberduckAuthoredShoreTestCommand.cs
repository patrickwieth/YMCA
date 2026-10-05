using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckAuthoredShoreTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-authored-shore-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2 && args.Length <= 7 && args.Skip(2).All(a => a == "shaded" || a == "mixed" || a == "baseline" || a == "editor" || a == "fog") && !(args.Contains("editor") && args.Contains("fog"));
		[Desc("OUTPUT [shaded] [mixed] [baseline] [editor|fog]", "Export an actual-client ground-edge gallery; collision remains ordinary land/water.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
			var tileset = "RUBBERDUCK-TEMPERATE" + (args.Contains("shaded") ? "-SHADED" : "");
			using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo[tileset], 82, 98);
			map.Title = "Authored shore acceptance gallery"; map.Author = "YMCA diagnostics"; map.RequiresMod = "ca";
			map.SetBounds(new PPos(1, 1), new PPos(80, 96));
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			foreach (var cell in map.AllCells) map.Tiles[cell] = new TerrainTile(1050, 0);
			var middle = new MPos(40, 56).ToCPos(map);
			var types = new ushort[] { 1000, 1010, 1020, 1030, 1040 };
			var centers = new List<CPos>();
			for (var m = 0; m < types.Length; m++)
			{
				var center = middle + new CVec((m % 3 - 1) * 14, (m / 3 - 1) * 14);
				centers.Add(center);
				for (var x = -4; x <= 4; x++)
					for (var y = -4; y <= 4; y++)
						if (!(x >= 1 && y >= 1)) map.Tiles[center + new CVec(x, y)] = new TerrainTile(types[m], (byte)((x * x + y * y) % 16));
				map.Tiles[center + new CVec(-2, -2)] = new TerrainTile(1050, 0);
				map.Tiles[center + new CVec(3, 3)] = new TerrainTile(types[m], 0);
			}
			// Adjacent land families and the secondary sea deliberately exercise junctions,
			// not just five disconnected straight edges.
			for (var x = 0; x < 8; x++)
				for (var y = 0; y < 8; y++)
					map.Tiles[middle + new CVec(11 + x, 11 + y)] = new TerrainTile(types[x % 5], 0);
			foreach (var cell in map.AllCells.ToArray())
				if (map.Tiles[cell].Type == 1050 && cell.X > middle.X + 8) map.Tiles[cell] = new TerrainTile(1060, 0);
			var actors = new List<MiniYamlNode>
			{
				new MiniYamlNode("Spawn0", new ActorReference("mpspawn") { new LocationInit(centers[0]), new OwnerInit("Neutral") }.Save()),
				new MiniYamlNode("Spawn1", new ActorReference("mpspawn") { new LocationInit(centers[1]), new OwnerInit("Neutral") }.Save())
			};
			var logs = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes"); Directory.CreateDirectory(logs);
			var result = Path.Combine(logs, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			File.WriteAllText(result, ""); File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			var probes = new StringBuilder();
			for (var m = 0; m < centers.Count; m++)
				for (var water = 0; water < 2; water++)
				{
					var name = $"calibration.authoredshore{m}-{water}";
					var center = centers[m];
					var start = center + (water == 0 ? new CVec(-4, -4) : new CVec(-6, -6));
					var target = center + (water == 0 ? new CVec(-4, 4) : new CVec(6, -6));
					var forbidden = water == 0 ? center + new CVec(-5, -4) : center;
					if (!map.Contains(start) || !map.Contains(target) || !map.Contains(forbidden))
						throw new InvalidDataException("Shore probe leaves playable bounds.");
					actors.Add(new MiniYamlNode("Probe" + name, new ActorReference(name) { new LocationInit(start), new OwnerInit("Multi0") }.Save()));
					var parent = water == 0 ? "Challenger_Tank" : "DD";
					probes.AppendLine($"{name}:\n\tInherits: {parent}\n\tRenderSprites:\n\t\tImage: {parent.ToLowerInvariant()}\n\tShoreMovementProbe:\n\t\tTarget: {target}\n\t\tForbidden: {forbidden}\n\t\tWater: {water != 0}\n\t\tResultPath: {result}");
				}
			map.ActorDefinitions = actors;
			var editor = args.Contains("editor") ? $"\tAuthoredShoreEditorProbe:\n\t\tCell: {centers[0] + new CVec(-4, -4)}\n\t\tResultPath: {result}\n" : "";
			var editorWorld = args.Contains("editor") ? $"EditorWorld:\n{editor}\tRubberduckMaterialTransitions:\n\t\tAuthoredShores: true\n" : "";
			var fog = args.Contains("fog");
			var victoryRules = fog ? "\t-ConquestVictoryConditions:\n" : "";
			var sightCells = centers.SelectMany(c => PlateauTopology.Directions.Select(d => c + d * 4)).ToArray();
			var sightRules = fog ? $"\tShoreVisibilityProbe:\n\t\tHiddenCell: {new MPos(3, 90).ToCPos(map)}\n\t\tCells: {string.Join(", ", sightCells.Select(c => c.ToString()))}\n\t\tResultPath: {result}\n" : "";
			if (fog) probes.AppendLine("calibration.shore-vision:\n\tImmobile:\n\t\tOccupiesSpace: false\n\tRevealsShroud:\n\t\tType: CenterPosition\n\t\tRange: 3c0");
			var camera = args.Contains("mixed") ? middle + new CVec(14, 14) : middle + new CVec(-3, -3);
			var zoom = args.Contains("mixed") ? "0.75" : "0.4";
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"{editorWorld}World:\n{editor}{sightRules}\tRubberduckMaterialTransitions:\n\t\tAuthoredShores: {!args.Contains("baseline")}\n\tTerrainCalibrationView:\n\t\tValidateMixedWater: true\n\t\tResultPath: {result}\n\t\tCenter: {camera}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: {zoom}\n\t\tScreenshotTicks: 1200\nPlayer:\n{victoryRules}\tShroud:\n\t\tFogCheckboxEnabled: {fog}\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: {!fog}\n\t\tExploredMapCheckboxLocked: true\n" + probes, "authored-shore-gallery"));
			var path = Path.Combine(output, "rubberduck-authored-shores.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			Console.WriteLine(path);
		}
	}
}
