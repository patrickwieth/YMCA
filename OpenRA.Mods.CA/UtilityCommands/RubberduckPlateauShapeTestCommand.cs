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
	sealed class RubberduckPlateauShapeTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-plateau-shape-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		internal static CPos First(Map map) => new MPos(40, 48).ToCPos(map);
		internal static CPos Second(Map map) => First(map) + new CVec(24, 0);
		internal static CPos[] Bridge(Map map) => Enumerable.Range(5, 15).SelectMany(x => Enumerable.Range(5, 3).Select(y => First(map) + new CVec(x, y))).ToArray();
		internal static CPos[] Trim(Map map) => Enumerable.Range(-7, 3).SelectMany(x => Enumerable.Range(-7, 3).Select(y => First(map) + new CVec(x, y))).ToArray();
		internal static HashSet<CPos> Protected(Map map) => Enumerable.Range(-2, 5).SelectMany(x => Enumerable.Range(-2, 5).Select(y => Second(map) + new CVec(x, y))).ToHashSet();

		[Desc("OUTPUT", "Test native plateau shaping/merging preflight and export a real-editor transaction fixture.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
			using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 112, 128);
			foreach (var c in map.AllCells) map.Tiles[c] = new TerrainTile(1000, 0);
			map.SetBounds(new PPos(1, 17), new PPos(110, 126));
			map.Title = "Plateau shape and join editor test"; map.Author = "YMCA terrain calibration"; map.RequiresMod = "ca";
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			var surfaces = PlateauTopology.CalibrationPlateau(First(map));
			foreach (var p in PlateauTopology.CalibrationPlateau(Second(map))) surfaces.Add(p.Key, p.Value);
			var actors = new List<MiniYamlNode>();
			RubberduckPlateauRenderer.Apply(map, surfaces, actors);
			void Add(string id, string type, CPos c) => actors.Add(new MiniYamlNode(id, new ActorReference(type) { new LocationInit(c), new OwnerInit("Neutral") }.Save()));
			Add("Spawn0", "mpspawn", new MPos(12, 28).ToCPos(map));
			Add("Spawn1", "mpspawn", new MPos(90, 110).ToCPos(map));
			Add("ProtectedRefinery", "proc", Second(map));
			map.ActorDefinitions = actors;
			for (var x = -1; x <= 1; x++) for (var y = -1; y <= 1; y++) map.Resources[First(map) + new CVec(x, y)] = new ResourceTile(1, 5);
			var report = new StringBuilder();
			void Reject(IEnumerable<CPos> stroke, bool grow, string label)
			{
				try { PlateauShapePlan.Create(map, actors, stroke, grow, Protected(map)); }
				catch (InvalidOperationException) { report.AppendLine("PASS rejected " + label); return; }
				throw new InvalidDataException("Unsafe stroke accepted: " + label);
			}
			Reject(new[] { First(map) }, false, "resource roof");
			Reject(new[] { Second(map) + new CVec(1, 1) }, false, "building footprint");
			Reject(new[] { First(map) + new CVec(7, 0) }, true, "ramp");
			Reject(new[] { First(map) + new CVec(9, 0) }, true, "ramp approach");
			Reject(new[] { First(map) + new CVec(10, -6) }, true, "inaccessible detached roof");
			Reject(new[] { new MPos(1, 17).ToCPos(map) }, true, "boundary");
			var plan = PlateauShapePlan.Create(map, actors, Bridge(map), true, Protected(map));
			if (plan.NativePieces == 0) throw new InvalidDataException("Shape planning lost native cliff composition.");
			report.AppendLine($"PASS join: {plan.Patch.Count} terrain changes, {plan.NativePieces} native pieces.");
			var action = new PlateauTerrainEditAction(map, plan.Patch, _ => { }, "Pure join test");
			action.Do();
			var mergedActors = actors.Where(n => !plan.BeforeActors.Any(old => old.Key == n.Key)).Concat(plan.AfterActors).ToArray();
			var trim = PlateauShapePlan.Create(map, mergedActors, Trim(map), false, Protected(map));
			report.AppendLine($"PASS trim: {trim.Patch.Count} terrain changes, {trim.NativePieces} native pieces.");
			// Close only the inward-facing ramps in this temporary negative fixture.
			// A second high bridge would now trap the low courtyard between the roofs.
			var closed = new Dictionary<CPos, (TerrainTile Tile, byte Height)>();
			foreach (var entry in new[] { (Center: First(map), Direction: 0), (Center: Second(map), Direction: 2) })
				for (var depth = 4; depth <= 7; depth++)
					for (var lane = -2; lane <= 2; lane++)
					{
						var c = entry.Center + PlateauTopology.Directions[entry.Direction] * depth + new CVec(0, lane);
						closed.Add(c, (map.Tiles[c], map.Height[c])); map.Height[c] = 4; map.Tiles[c] = new TerrainTile(1000, 0);
					}
			var refusedDisconnect = false;
			try
			{
				var north = Bridge(map).Select(c => new CPos(c.X, 2 * First(map).Y - c.Y));
				PlateauShapePlan.Create(map, mergedActors, north, true, Protected(map));
			}
			catch (InvalidOperationException e) when (e.Message.Contains("disconnect", StringComparison.Ordinal)) { refusedDisconnect = true; }
			finally { foreach (var p in closed) { map.Tiles[p.Key] = p.Value.Tile; map.Height[p.Key] = p.Value.Height; } }
			if (!refusedDisconnect) throw new InvalidDataException("A stroke that seals the low courtyard was accepted.");
			report.AppendLine("PASS rejected disconnected low courtyard");
			action.Undo();
			File.WriteAllText(Path.Combine(output, "preflight-results.txt"), report.ToString());
			var resultPath = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(resultPath)); File.WriteAllText(resultPath, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"World:\n\tPlateauShapeProbe:\n\t\tResultPath: {resultPath}\nEditorWorld:\n\tPlateauShapeProbe:\n\t\tResultPath: {resultPath}\n", "shape-test"));
			var path = Path.Combine(output, "rubberduck-plateau-shape-test.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			Console.WriteLine(report + "Editor fixture: " + path);
		}
	}
}
