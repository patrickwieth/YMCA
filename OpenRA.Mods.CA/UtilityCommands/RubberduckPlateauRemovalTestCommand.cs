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
	sealed class RubberduckPlateauRemovalTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-plateau-removal-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("MAP OUTPUT", "Validate protected plateau removal and export an actual editor Undo/Redo fixture.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[2]);
			Directory.CreateDirectory(output);
			using var stream = File.OpenRead(args[1]);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]);
			using var map = new Map(utility.ModData, zip);
			PlateauRemovalPlan plan = null;
			var clicked = default(CPos);
			var seen = new HashSet<CPos>();
			var messages = new StringBuilder();
			foreach (var c in map.AllCells)
			{
				if (!map.Contains(c) || seen.Contains(c) || map.Height[c] != 4 || map.Tiles[c].Type != 1000) continue;
				var queue = new Queue<CPos>(); queue.Enqueue(c);
				while (queue.Count > 0)
				{
					var next = queue.Dequeue();
					if (!map.Contains(next) || (map.Height[next] == 0 && map.Ramp[next] == 0) || !seen.Add(next)) continue;
					for (var dx = -1; dx <= 1; dx++)
						for (var dy = -1; dy <= 1; dy++) queue.Enqueue(next + new CVec(dx, dy));
				}
				try { plan = PlateauRemovalPlan.Create(map, map.ActorDefinitions, c); clicked = c; break; }
				catch (InvalidOperationException e) { messages.AppendLine("Protected component: " + e.Message); }
			}
			if (plan == null) throw new InvalidDataException("No isolated empty plateau: " + messages);
			if (!plan.Decorations.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.nativecliff", StringComparison.Ordinal)))
				throw new InvalidDataException("This regression requires production native cliff pieces.");
			messages.AppendLine($"PASS discovered {plan.Patch.Count} cells, {plan.Decorations.Count} cliff objects at {clicked}.");
			void Reject(IEnumerable<MiniYamlNode> actors, string name)
			{
				try { PlateauRemovalPlan.Create(map, actors, clicked); }
				catch (InvalidOperationException) { messages.AppendLine("PASS rejection: " + name); return; }
				throw new InvalidDataException("Unsafe removal accepted: " + name);
			}
			map.Resources[clicked] = new ResourceTile(1, 5);
			Reject(map.ActorDefinitions, "resources");
			map.Resources[clicked] = default;
			var spawn = new ActorReference("mpspawn") { new LocationInit(clicked), new OwnerInit("Neutral") };
			Reject(map.ActorDefinitions.Append(new MiniYamlNode("ProtectedSpawn", spawn.Save())), "spawn/actor");
			var other = plan.Clearance.First(c => !plan.Patch.ContainsKey(c) &&
				!plan.Patch.Keys.Any(p => Math.Abs(p.X - c.X) <= 1 && Math.Abs(p.Y - c.Y) <= 1));
			map.Height[other] = 4;
			Reject(map.ActorDefinitions, "neighbor plateau");
			map.Height[other] = 0;
			var originalTile = map.Tiles[other];
			map.Tiles[other] = new TerrainTile(3992, 0);
			Reject(map.ActorDefinitions, "extended/shared cliff foot");
			map.Tiles[other] = originalTile;
			var decorated = plan.Decorations.First();
			Reject(map.ActorDefinitions.Select(n => n.Key == decorated.Key ? new MiniYamlNode(n.Key,
				new MiniYaml(n.Value.Value, n.Value.Nodes.Append(new MiniYamlNode("Health", "99")).ToList())) : n), "customized decoration");
			var renamed = PlateauRemovalPlan.Create(map, map.ActorDefinitions.Select((n, i) => new MiniYamlNode("Renamed" + i, n.Value)), clicked);
			if (renamed.Patch.Count != plan.Patch.Count || renamed.Decorations.Count != plan.Decorations.Count)
				throw new InvalidDataException("Removal depends on generated actor IDs.");
			messages.AppendLine("PASS arbitrary actor IDs");
			File.WriteAllText(Path.Combine(output, "preflight-results.txt"), messages.ToString());
			var resultPath = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
			File.WriteAllText(resultPath, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"World:\n\tPlateauRemovalProbe:\n\t\tCell: {clicked}\n\t\tResultPath: {resultPath}\nEditorWorld:\n\tPlateauRemovalProbe:\n\t\tCell: {clicked}\n\t\tResultPath: {resultPath}\n", "removal-test"));
			map.Title = "Generated plateau removal editor test";
			var path = Path.Combine(output, "rubberduck-plateau-removal-test.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			Console.WriteLine(messages + "Editor fixture: " + path);
		}
	}
}
