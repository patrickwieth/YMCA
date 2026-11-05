using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class ArabiaLayoutTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--arabia-layout-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT", "Validate Plains (formerly Arabia), legacy preset aliases, closed rocks and wide native routes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var cases = 0;
			foreach (var players in new[] { 2, 3, 4, 8, 12, 16 })
				foreach (var seed in new[] { 42, 811863718 })
				{
					var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "preset=plains", "mode=strategic", $"players={players}", $"seed={seed}", "size=auto" });
					var legacy = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "preset=arabia", "mode=strategic", $"players={players}", $"seed={seed}", "size=auto" });
					if (options.Preset != legacy.Preset || options.Preset.DisplayName() != "Plains") throw new InvalidDataException("Plains/Arabia alias changed the preset identity.");
					var plan = MapPlanGenerator.Generate(options); var repeat = MapPlanGenerator.Generate(legacy);
					if (!plan.RequireClosedPlateaus || !ArabiaMountainPlanner.HasWideRoutes(plan)) throw new InvalidDataException("Arabia lost its wide native routes.");
					if (!plan.Spawns.SequenceEqual(repeat.Spawns) || !plan.PlateauSurfaces.SequenceEqual(repeat.PlateauSurfaces)) throw new InvalidDataException("Arabia geometry is nondeterministic.");
					for (var y = 0; y < plan.Height; y++)
						for (var x = 0; x < plan.Width; x++)
							if (plan.TerrainAt(x, y) != repeat.TerrainAt(x, y) || plan.FeaturesAt(x, y) != repeat.FeaturesAt(x, y))
								throw new InvalidDataException("Arabia terrain/economy is nondeterministic.");
					var surfaces = plan.PlateauSurfaces.ToDictionary(p => MountainPlateauPlanner.Cell(p.Key.U, p.Key.V), p => p.Value);
					var remaining = surfaces.Keys.ToHashSet(); var components = new List<HashSet<CPos>>();
					while (remaining.Count != 0)
					{
						var cells = new HashSet<CPos>(); var queue = new Queue<CPos>(); queue.Enqueue(remaining.First());
						while (queue.Count != 0)
						{
							var c = queue.Dequeue(); if (!remaining.Remove(c)) continue; cells.Add(c);
							foreach (var d in PlateauTopology.Directions) queue.Enqueue(c + d);
						}
						var roof = cells.Where(c => surfaces[c].Height == 4).ToArray();
						if (roof.Length == 0 || roof.Length != (roof.Max(c => c.X) - roof.Min(c => c.X) + 1) * (roof.Max(c => c.Y) - roof.Min(c => c.Y) + 1))
							throw new InvalidDataException("A hooked, bent or joined rock component was created.");
						if (cells.Any(c => !surfaces[c].Blocked)) throw new InvalidDataException("Obstacle roof or foot became traversable.");
						foreach (var other in components)
							if (cells.Any(c => other.Any(n => Math.Max(Math.Abs(c.X - n.X), Math.Abs(c.Y - n.Y)) <= ArabiaMountainPlanner.Clearance)))
								throw new InvalidDataException("Rock-foot clearance is too narrow.");
						components.Add(cells);
					}
					if (components.Count < 2) throw new InvalidDataException("Arabia lost its rock groups.");
					var path = new RubberduckMapExporter(utility.ModData).Export(plan, options, Path.Combine(args[1], $"{players}-{seed}"));
					using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var map = new Map(utility.ModData, zip);
					if (map.Title != $"{options.Mode} Plains {players}P {seed}" || !Path.GetFileName(path).Contains("plains-players-", StringComparison.Ordinal))
						throw new InvalidDataException("Plains display title or export filename retained the old name.");
					if (!map.ActorDefinitions.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.closedcliff", StringComparison.Ordinal)) ||
						map.ActorDefinitions.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.mountainwall", StringComparison.Ordinal) || n.Value.Value.StartsWith("terrain.rubberduck.plateauwall", StringComparison.Ordinal)))
						throw new InvalidDataException("Closed Arabia contours fell back to mismatched wall pieces.");
					Console.WriteLine($"ARABIA PASS players={players} seed={seed} rocks={components.Count}; six-cell gaps, five-cell routes, closed export and determinism.");
					cases++;
				}
			Console.WriteLine($"ARABIA COMPLETE {cases} twice-generated layouts and physical exports.");
		}
	}
}
