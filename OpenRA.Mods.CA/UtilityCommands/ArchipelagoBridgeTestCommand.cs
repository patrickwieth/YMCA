using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class ArchipelagoBridgeTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--archipelago-bridge-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2 || args.Length == 3 &&
			args[2].StartsWith("skip=", StringComparison.Ordinal) && int.TryParse(args[2].Substring(5), out var skip) && skip >= 0;
		[Desc("OUTPUT [skip=N]", "Check same-team island bridges, real water, deterministic exports and protected-bank rejection.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var cases = 0;
			var index = 0;
			var skip = args.Length == 3 ? int.Parse(args[2].Substring(5)) : 0;
			foreach (var mode in new[] { "tactical", "operational" })
				foreach (var players in new[] { 2, 3, 4, 6, 8, 12, 16 })
					foreach (var seed in new[] { 42, 43 })
					{
						if (index++ < skip) continue;
						var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "preset=archipelago", $"mode={mode}", $"players={players}", $"seed={seed}", "size=auto" });
						Check(utility, options, args[1]); cases++;
					}
			foreach (var teams in new[] { 1, 3, 6 })
			{
				if (index++ < skip) continue;
				var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "preset=archipelago", "players=6", $"teams={teams}", "seed=42", "size=auto" });
				Check(utility, options, args[1]); cases++;
			}

			var blocked = new MapPlan(48, 48);
			for (var y = 0; y < 48; y++) for (var x = 0; x < 48; x++)
			{
				blocked.SetTerrain(x, y, x >= 20 && x <= 28 ? PlannedTerrain.Water : PlannedTerrain.Land);
				blocked.AddFeature(x, y, PlannedFeature.Resource);
			}
			if (RubberduckBridgePlanner.Connect(blocked, new[] { new PlanPoint(10, 24), new PlanPoint(38, 24) }, new[] { 0, 0 }) || blocked.Bridges.Count != 0)
				throw new InvalidDataException("Protected bank was not rejected atomically.");
			for (var y = 0; y < 48; y++) for (var x = 0; x < 48; x++)
				if (blocked.TerrainAt(x, y) != (x >= 20 && x <= 28 ? PlannedTerrain.Water : PlannedTerrain.Land) || blocked.FeaturesAt(x, y) != PlannedFeature.Resource)
					throw new InvalidDataException("Rejected bridge mutated protected terrain.");
			Console.WriteLine($"ARCHIPELAGO BRIDGES COMPLETE: {cases} twice-generated exports (skipped {Math.Min(index, skip)}) and atomic protected-bank negative control.");
		}

		static void Check(Utility utility, MapGenerationOptions options, string output)
		{
			var a = MapPlanGenerator.Generate(options); var b = MapPlanGenerator.Generate(options);
			if (!a.Spawns.SequenceEqual(b.Spawns) || a.GenerationAttempt != b.GenerationAttempt || a.Bridges.Count != b.Bridges.Count)
				throw new InvalidDataException("Nondeterministic island layout.");
			for (var y = 0; y < a.Height; y++) for (var x = 0; x < a.Width; x++)
			{
				if (a.TerrainAt(x, y) != b.TerrainAt(x, y) || a.FeaturesAt(x, y) != b.FeaturesAt(x, y)) throw new InvalidDataException("Nondeterministic terrain/economy.");
				if (a.TerrainAt(x, y) == PlannedTerrain.ShallowWater && !a.HasFeature(x, y, PlannedFeature.DestructibleBridge))
					throw new InvalidDataException("Permanent shallow-water causeway remains.");
			}
			var owners = new Dictionary<CPos, int>();
			for (var player = 0; player < options.Players; player++)
			{
				var team = Math.Min(options.Teams - 1, player * options.Teams / options.Players);
				var start = PlannedRubberduckBridge.Cell(a.Spawns[player].X, a.Spawns[player].Y);
				foreach (var c in Flood(a, start, false))
				{
					if (owners.TryGetValue(c, out var existing) && existing != team) throw new InvalidDataException("Enemy home islands touch.");
					owners[c] = team;
				}
				var reachable = Flood(a, start, true);
				for (var other = 0; other < options.Players; other++)
					if (Math.Min(options.Teams - 1, other * options.Teams / options.Players) != team && reachable.Contains(PlannedRubberduckBridge.Cell(a.Spawns[other].X, a.Spawns[other].Y)))
						throw new InvalidDataException("Bridge network connects enemy teams.");
			}
			var counts = new int[options.Teams];
			for (var i = 0; i < a.Bridges.Count; i++)
			{
				var bridge = a.Bridges[i]; var cells = bridge.Cells().ToArray();
				if (!cells.SequenceEqual(b.Bridges[i].Cells()) || owners[cells[0]] != owners[cells[cells.Length - 1]])
					throw new InvalidDataException("Bridge endpoints or determinism invalid.");
				counts[owners[cells[0]]]++;
				var middle = cells[cells.Length / 2];
				var side = bridge.AlongY ? new CVec(1, 0) : new CVec(0, 1);
				for (var along = -1; along <= 1; along++) for (var lateral = -2; lateral <= 2; lateral++)
				{
					var p = PlannedRubberduckBridge.Point(middle + bridge.Step * along + side * lateral);
					if (a.TerrainAt(p.U, p.V) != PlannedTerrain.Water && !(a.TerrainAt(p.U, p.V) == PlannedTerrain.ShallowWater && a.HasFeature(p.U, p.V, PlannedFeature.DestructibleBridge)))
						throw new InvalidDataException("Bridge has no navigable deep-water cross-section.");
				}
				foreach (var c in cells.Concat(bridge.Approaches()))
				{
					var p = PlannedRubberduckBridge.Point(c);
					if (a.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.Spawn | PlannedFeature.BuildClearance | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator))
						throw new InvalidDataException("Crossing overlaps protected economy/home.");
				}
			}
			if (counts.Distinct().Count() != 1 || options.Players / options.Teams >= 2 && counts[0] == 0)
				throw new InvalidDataException("Missing or unequal per-team bridge counts.");
			var path = new RubberduckMapExporter(utility.ModData).Export(a, options, Path.Combine(output, $"{options.Mode}-{options.Players}-{options.Teams}-{options.Seed}"));
			using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var map = new Map(utility.ModData, zip);
			if (map.ActorDefinitions.Count(n => n.Value.Value.StartsWith("terrain.rubberduck.bridge.", StringComparison.Ordinal)) != a.Bridges.Count)
				throw new InvalidDataException("Export lost bridge actors.");
			if (map.ActorDefinitions.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.coast", StringComparison.Ordinal)) ||
				map.AllCells.Any(c => map.Tiles[c].Type >= 14010 && map.Tiles[c].Type <= 14012))
				throw new InvalidDataException("Archipelago retained rock-coast actors or invisible blocked coast tiles.");
			// Export itself validates every real water tile, dry landing and approach.
			Console.WriteLine($"ARCHIPELAGO BRIDGE PASS {options.Mode}/{options.Players}/{options.Teams}/{options.Seed} bridges={a.Bridges.Count} attempt={a.GenerationAttempt}");
		}

		static HashSet<CPos> Flood(MapPlan plan, CPos start, bool bridges)
		{
			var seen = new HashSet<CPos>(); var pending = new Queue<CPos>(); pending.Enqueue(start);
			while (pending.Count != 0)
			{
				var c = pending.Dequeue(); if (seen.Contains(c)) continue;
				var p = PlannedRubberduckBridge.Point(c);
				if (!plan.Contains(p.U, p.V) || !(bridges ? plan.IsTraversable(p.U, p.V) : plan.TerrainAt(p.U, p.V) == PlannedTerrain.Land)) continue;
				seen.Add(c);
				foreach (var d in PlateauTopology.Directions) pending.Enqueue(c + d);
			}
			return seen;
		}
	}
}
