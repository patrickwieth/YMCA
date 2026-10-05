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
	sealed class RubberduckRampMoveTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-ramp-move-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		internal static CPos Center(Map map) => new MPos(48, 64).ToCPos(map);
		internal static CPos Source(Map map) => Center(map) + new CVec(12, 0);
		internal static CPos Target(Map map) => Center(map) + new CVec(6, -12);
		[Desc("OUTPUT", "Validate all 16 ramp relocation orientations and export a real editor fixture.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
			using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 112, 128);
			foreach (var c in map.AllCells) map.Tiles[c] = new TerrainTile(1000, 0);
			map.SetBounds(new PPos(1, 17), new PPos(110, 126));
			map.Title = "Ramp relocation editor test"; map.Author = "YMCA terrain calibration"; map.RequiresMod = "ca";
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			var actors = new List<MiniYamlNode>();
			RubberduckPlateauRenderer.Apply(map, PlateauTopology.CalibrationPlateau(Center(map), 12), actors);
			foreach (var uv in new[] { new MPos(10, 28), new MPos(95, 110) })
				actors.Add(new MiniYamlNode("Spawn" + actors.Count, new ActorReference("mpspawn") { new LocationInit(uv.ToCPos(map)), new OwnerInit("Neutral") }.Save()));
			map.ActorDefinitions = actors;
			var report = new StringBuilder();
			for (var from = 0; from < 4; from++)
				for (var to = 0; to < 4; to++)
				{
					var toe = Center(map) + PlateauTopology.Directions[from] * 12;
					var target = Center(map) + PlateauTopology.Directions[to] * 12 + PlateauTopology.Directions[(to + 1) % 4] * 6;
					var source = PlateauRampMovePlan.Find(map, toe - PlateauTopology.Directions[from] * 2 + PlateauTopology.Directions[(from + 1) % 4]);
					if (source.Toe != toe) throw new InvalidDataException("Ramp recognition failed from an interior side lane.");
					var plan = source.Move(map, actors, target, Array.Empty<CPos>());
					var action = new PlateauTerrainEditAction(map, plan.Patch, _ => { }, "Ramp matrix");
					action.Do();
					if (map.Ramp[toe] != 0 || map.Height[toe] != 4 || map.Ramp[target] != (to + 2) % 4 + 1 || map.Height[target] != 0)
						throw new InvalidDataException("Relocation did not close/open the expected ramps.");
					action.Undo();
					if (map.Ramp[toe] != (from + 2) % 4 + 1 || map.Height[target] != 4) throw new InvalidDataException("Relocation undo failed.");
					report.AppendLine($"PASS {from}->{to}: {plan.Patch.Count} cells, {plan.NativePieces} native pieces");
				}
			for (var direction = 0; direction < 4; direction++)
				foreach (var shift in new[] { -1, 1 })
				{
					var toe = Center(map) + PlateauTopology.Directions[direction] * 12;
					var target = toe + PlateauTopology.Directions[(direction + 1) % 4] * shift;
					var plan = PlateauRampMovePlan.Find(map, toe).Move(map, actors, target, Array.Empty<CPos>());
					var action = new PlateauTerrainEditAction(map, plan.Patch, _ => { }, "Overlapping shift");
					action.Do();
					if (PlateauRampMovePlan.Find(map, target).Toe != target) throw new InvalidDataException("Overlapping ramp shift is incomplete.");
					action.Undo();
					report.AppendLine($"PASS overlapping shift {direction}: {shift}");
				}
			for (var direction = 0; direction < 4; direction++)
			{
				var toe = Center(map) + PlateauTopology.Directions[direction] * 12;
				var target = toe + PlateauTopology.Directions[(direction + 1) % 4] * 6;
				var add = PlateauRampMovePlan.Add(map, actors, target, Array.Empty<CPos>());
				var action = new PlateauTerrainEditAction(map, add.Patch, _ => { }, "Add ramp");
				action.Do();
				if (PlateauRampMovePlan.Find(map, target).Toe != target || map.AllCells.Count(c => map.Ramp[c] != 0) != 60)
					throw new InvalidDataException("Additional ramp did not preserve all four original ramps.");
				action.Undo();
				var close = PlateauRampMovePlan.Find(map, toe).Close(map, actors, Array.Empty<CPos>());
				action = new PlateauTerrainEditAction(map, close.Patch, _ => { }, "Close ramp");
				action.Do();
				if (map.Ramp[toe] != 0 || map.Height[toe] != 4 || map.AllCells.Count(c => map.Ramp[c] != 0) != 36)
					throw new InvalidDataException("Ramp closure changed another ramp.");
				action.Undo();
				report.AppendLine($"PASS add/close direction {direction}");
			}
			var closures = new Stack<PlateauTerrainEditAction>();
			var remainingActors = actors.ToList();
			for (var direction = 0; direction < 3; direction++)
			{
				var toe = Center(map) + PlateauTopology.Directions[direction] * 12;
				var close = PlateauRampMovePlan.Find(map, toe).Close(map, remainingActors, Array.Empty<CPos>());
				var removed = close.BeforeActors.Select(n => n.Key).ToHashSet();
				remainingActors.RemoveAll(n => removed.Contains(n.Key)); remainingActors.AddRange(close.AfterActors);
				var action = new PlateauTerrainEditAction(map, close.Patch, _ => { }, "Close ramp");
				action.Do(); closures.Push(action);
			}
			var last = Center(map) + PlateauTopology.Directions[3] * 12;
			var lastRejected = false;
			try { PlateauRampMovePlan.Find(map, last).Close(map, remainingActors, Array.Empty<CPos>()); }
			catch (InvalidOperationException) { lastRejected = true; }
			if (!lastRejected) throw new InvalidDataException("Closing the last accessible ramp was accepted.");
			while (closures.Count != 0) closures.Pop().Undo();
			report.AppendLine("PASS last accessible ramp protected");
			var selected = PlateauRampMovePlan.Find(map, Source(map));
			map.Resources[Source(map)] = new ResourceTile(1, 5);
			var rejected = false;
			try { selected.Move(map, actors, Target(map), Array.Empty<CPos>()); }
			catch (InvalidOperationException) { rejected = true; }
			map.Resources[Source(map)] = default;
			if (!rejected) throw new InvalidDataException("Source resource was not protected.");
			report.AppendLine("PASS protected source resource");
			File.WriteAllText(Path.Combine(output, "preflight-results.txt"), report.ToString());
			var result = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(result)); File.WriteAllText(result, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"World:\n\tPlateauRampMoveProbe:\n\t\tResultPath: {result}\nEditorWorld:\n\tPlateauRampMoveProbe:\n\t\tResultPath: {result}\n", "ramp-move"));
			using (var package = ZipFileLoader.Create(Path.Combine(output, "rubberduck-ramp-move-test.oramap"))) map.Save(package);
			Console.WriteLine(report);
		}
	}
}
