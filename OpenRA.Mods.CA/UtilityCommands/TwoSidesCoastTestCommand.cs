using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class TwoSidesCoastTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--two-sides-coast-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT", "Export recent Two Sides seeds and check every blocked bank face has its native actor, a map-edge exit and one interior end.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			foreach (var seed in new[] { 42, 43, 1226359626, 1652607081, 1494919500, 65690028, 165374254, 1775378146 })
			{
				var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "mode=strategic", "preset=two-sides", "players=4", "size=auto", $"seed={seed}" });
				var plan = MapPlanGenerator.Generate(options);
				var path = new RubberduckMapExporter(utility.ModData).Export(plan, options, Path.Combine(args[1], seed.ToString()));
				foreach (var message in plan.ValidationMessages.Where(m => m.StartsWith("Rock coast candidate skipped", StringComparison.Ordinal))) Console.WriteLine(message);
				using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
				using var map = new Map(utility.ModData, zip);
				var actors = map.ActorDefinitions.Where(n => n.Key.StartsWith("RockCoast", StringComparison.Ordinal)).ToArray();
				if (actors.Length == 0) throw new InvalidDataException("Two Sides lost both defensive coast runs.");
				var faces = actors.Where(n => n.Value.Value.Contains("coast-native-", StringComparison.Ordinal))
					.Select(n => (Cell: new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value,
						Direction: int.Parse(n.Value.Value.Split('-')[2]))).ToHashSet();
				var expected = 0;
				foreach (var cell in map.AllCells.Where(map.Contains))
				{
					if (map.Tiles[cell].Type != 14011 && map.Tiles[cell].Type != 14012) continue;
					for (var d = 0; d < 4; d++)
					{
						var n = cell + PlateauTopology.Directions[d];
						if (!map.Contains(n) || map.Tiles[n].Type != 14010 && map.Tiles[n].Type != 1050) continue;
						expected++;
						if (!faces.Contains((cell, d))) throw new InvalidDataException($"Unrendered bank face at {cell}/{d}.");
					}
				}
				foreach (var group in actors.GroupBy(n => n.Key.Split('_')[0]))
				{
					if (group.Count(n => n.Value.Value.Contains("coast-abutment-", StringComparison.Ordinal)) != 1 ||
						!group.Any(n => RubberduckRockCoastRenderer.AtMapEdge(map,
							new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value)))
						throw new InvalidDataException("A coast stops before the map edge or has internal endpoints.");
					var banks = group.Where(n => n.Value.Value.Contains("coast-native-", StringComparison.Ordinal))
						.Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value).ToHashSet();
					ushort Restored(CPos c) => map.Tiles[c].Type switch { 14010 => 1050, 14011 => 1020, 14012 => 1000, var tile => tile };
					var footprint = RubberduckRockCoastPlan.Footprint(map, banks, Array.Empty<CPos>(), Restored, clipMapEdge: true);
					if (footprint.Keys.Any(c => !map.Contains(c))) throw new InvalidDataException("Footprint escaped the playable map.");
					void Refused(Action action)
					{
						try { action(); } catch (InvalidOperationException) { return; }
						throw new InvalidDataException("Edge clipping weakened an in-map coast protection.");
					}
					Refused(() => RubberduckRockCoastPlan.Footprint(map, banks, new[] { banks.First() }, Restored, clipMapEdge: true));
					var deepFoot = footprint.Keys.First(c => map.Tiles[c].Type == 14010 &&
						!PlateauTopology.Directions.Any(d => banks.Contains(c + d)));
					Refused(() => RubberduckRockCoastPlan.Footprint(map, banks, Array.Empty<CPos>(),
						c => c == deepFoot ? (ushort)1000 : Restored(c), clipMapEdge: true));
					if (group.Any(n => (n.Value.Value.EndsWith("coast-abutment-2", StringComparison.Ordinal) ||
						n.Value.Value.EndsWith("coast-abutment-3", StringComparison.Ordinal))))
						throw new InvalidDataException("A rear bank retained a tall end tower.");
				}
				if (faces.Count != expected) throw new InvalidDataException("Coast face count differs from physical bank topology.");
				Console.WriteLine($"CONTINUOUS COAST PASS seed={seed} faces={expected} runs={actors.Select(n => n.Key.Split('_')[0]).Distinct().Count()}");
			}
		}
	}
}
