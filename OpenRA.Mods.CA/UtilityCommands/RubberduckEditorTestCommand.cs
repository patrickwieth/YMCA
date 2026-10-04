using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckEditorTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-editor-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2 || args.Length == 3 && args[2] == "shaded=true";
		[Desc("OUTPUT [shaded=true]", "Test atomic terrain rollback and export an automated real-editor stamp/undo fixture.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[1]);
			Directory.CreateDirectory(output);
			using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo[args.Length == 3 ? "RUBBERDUCK-TEMPERATE-SHADED" : "RUBBERDUCK-TEMPERATE"], 82, 98);
			foreach (var c in map.AllCells) map.Tiles[c] = new TerrainTile(1000, 0);
			map.SetBounds(new PPos(1, 17), new PPos(80, 96));
			map.Title = "Rubberduck editor transaction test";
			map.Author = "YMCA editor calibration";
			map.RequiresMod = "ca";
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			var center = new MPos(40, 48).ToCPos(map);
			var patch = PlateauTopology.CalibrationPlateau(center);
			var applied = false;
			var action = new PlateauTerrainEditAction(map, patch, forward => applied = forward, "Test plateau");
			for (var iteration = 0; iteration < 3; iteration++)
			{
				action.Do();
				if (!applied || map.Height[center] != 4) throw new InvalidOperationException("Terrain action did not apply.");
				action.Undo();
				foreach (var c in patch.Keys)
					if (map.Height[c] != 0 || map.Tiles[c].Type != 1000 || map.Ramp[c] != 0 || applied)
						throw new InvalidOperationException("Terrain action undo was not exact.");
			}
			var failed = false;
			try { new PlateauTerrainEditAction(map, patch, _ => throw new InvalidOperationException("Injected actor failure"), "Failure test").Do(); }
			catch (InvalidOperationException) { failed = true; }
			if (!failed) throw new InvalidOperationException("Failure injection did not execute.");
			foreach (var c in patch.Keys)
				if (map.Height[c] != 0 || map.Tiles[c].Type != 1000 || map.Ramp[c] != 0)
					throw new InvalidOperationException("Failed actor patch left partial terrain changes.");
			File.WriteAllText(Path.Combine(output, "transaction-results.txt"), "PASS three Do/Undo cycles.\nPASS failed actor patch rolls terrain back.\n");
			var resultPath = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
			File.WriteAllText(resultPath, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"World:\n\tPlateauEditorProbe:\n\t\tResultPath: {resultPath}\nEditorWorld:\n\tPlateauEditorProbe:\n\t\tResultPath: {resultPath}\n", "editor-test-rules"));
			var actors = new System.Collections.Generic.List<MiniYamlNode>();
			foreach (var uv in new[] { new MPos(8, 24), new MPos(72, 88) })
			{
				var actor = new ActorReference("mpspawn") { new LocationInit(uv.ToCPos(map)), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("Spawn" + actors.Count, actor.Save()));
			}
			map.ActorDefinitions = actors;
			var path = Path.Combine(output, "rubberduck-editor-test.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			Console.WriteLine("Atomic terrain checks passed; real editor fixture: " + path);
		}
	}
}
