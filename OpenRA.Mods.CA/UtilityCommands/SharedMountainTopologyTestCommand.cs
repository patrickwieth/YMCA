using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class SharedMountainTopologyTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--shared-mountain-topology-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT", "Check shared mountain contours, deterministic exports and atomic protected-terrain rejection.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var cases = 0;
			foreach (var preset in new[] { "hill-fortress", "battle-nexus" })
				foreach (var mode in preset == "hill-fortress" ? new[] { "tactical", "operational", "strategic" } : new[] { "tactical" })
					foreach (var players in new[] { 2, 4, 8, 16 })
						foreach (var seed in new[] { 42, 120999725 })
						{
							var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", $"preset={preset}", $"mode={mode}", $"players={players}", $"seed={seed}", "size=auto" });
							var plan = MapPlanGenerator.Generate(options); var repeat = MapPlanGenerator.Generate(options);
							if (!plan.RequireClosedPlateaus || !plan.Spawns.SequenceEqual(repeat.Spawns) || !plan.PlateauSurfaces.SequenceEqual(repeat.PlateauSurfaces))
								throw new InvalidDataException("Missing or nondeterministic closed contours.");
							for (var y = 0; y < plan.Height; y++)
								for (var x = 0; x < plan.Width; x++)
								{
									if (plan.TerrainAt(x, y) != repeat.TerrainAt(x, y) || plan.FeaturesAt(x, y) != repeat.FeaturesAt(x, y)) throw new InvalidDataException("Terrain/economy changed on repeat.");
									if (plan.TerrainAt(x, y) == PlannedTerrain.Mountain && !plan.PlateauSurfaces.ContainsKey(new MPos(x, y))) throw new InvalidDataException("Unplanned mountain cell.");
								}
							if (plan.PlateauSurfaces.Any(p => p.Value.Height > 4 || p.Value.Ramp == 0 && p.Value.Height != 0 && p.Value.Height != 4 ||
								plan.TerrainAt(p.Key.U, p.Key.V) != (p.Value.Blocked ? PlannedTerrain.Mountain : PlannedTerrain.Land) ||
								p.Value.Blocked && plan.HasFeature(p.Key.U, p.Key.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.Spawn | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator) ||
								preset == "battle-nexus" && !p.Value.Blocked))
								throw new InvalidDataException("Roof/foot geometry overlaps economy or loses collision.");
							if (preset == "hill-fortress")
							{
								if (plan.Plateaus.Count != 2 || plan.Spawns.Any(p => !HillFortressPlateauPlanner.FlatHome(plan, p.X, p.Y)))
									throw new InvalidDataException("Hill Fortress lost its two raised team bases.");
								var reachable = MountainPlateauPlanner.Reachable(plan, plan.Spawns[0]);
								for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
								{
									if (plan.HasFeature(x, y, PlannedFeature.BuildClearance | PlannedFeature.Resource | PlannedFeature.ResourceGenerator) && !HillFortressPlateauPlanner.FlatHome(plan, x, y))
										throw new InvalidDataException("A home clearance or economy cell is not on the raised roof.");
									if (plan.HasFeature(x, y, PlannedFeature.ContestedCenter) && (plan.PlateauSurfaces.ContainsKey(new MPos(x, y)) || !reachable[x + y * plan.Width]))
										throw new InvalidDataException("Battle valley is raised or inaccessible.");
								}
							}
							var path = new RubberduckMapExporter(utility.ModData).Export(plan, options, Path.Combine(args[1], $"{preset}-{mode}-{players}-{seed}"));
							using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var map = new Map(utility.ModData, zip);
							if (preset == "hill-fortress")
							{
								if (map.Bounds.Width != options.Width || map.Bounds.Height != options.Height ||
									map.AllCells.Any(c => map.Resources[c].Type != 0 && !map.Contains(c)) ||
									plan.PlateauSurfaces.Count(p => p.Value.Ramp != 0) != 40)
									throw new InvalidDataException("Hill backing changed playable size, stranded economy or narrowed the five-wide ramps.");
								foreach (var node in map.ActorDefinitions.Where(n => !n.Value.Value.StartsWith("terrain.", StringComparison.Ordinal)))
									if (!map.Contains(new ActorReference(node.Value.Value, node.Value.ToDictionary()).Get<LocationInit>().Value))
										throw new InvalidDataException("Gameplay actor placed in non-playable backing terrain.");
								var inset = plan.PlayableInset;
								var edges = new[]
								{
									Enumerable.Range(inset, options.Width).Select(x => new PlanPoint(x, inset + 4)),
									Enumerable.Range(inset, options.Width).Select(x => new PlanPoint(x, plan.Height - inset + 3)),
									Enumerable.Range(inset + 4, options.Height).Select(y => new PlanPoint(inset, y)),
									Enumerable.Range(inset + 4, options.Height).Select(y => new PlanPoint(plan.Width - inset - 1, y)),
								};
								foreach (var edge in edges)
									if (edge.Count(p => HillFortressPlateauPlanner.FlatHome(plan, p.X, p.Y)) <= edge.Count() / 2)
										throw new InvalidDataException("Raised homes no longer continue across a playable map edge.");
							}
							if (!map.ActorDefinitions.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.closedcliff", StringComparison.Ordinal)) ||
								map.ActorDefinitions.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.mountainwall", StringComparison.Ordinal)))
								throw new InvalidDataException("Export silently fell back to legacy walls.");
							foreach (var node in map.ActorDefinitions.Where(n => n.Value.Value.StartsWith("terrain.rubberduck.plateauwall", StringComparison.Ordinal)))
							{
								// Walkable homes use the established sloped ramp retaining faces;
								// never accept a full-height projected contour as a fallback.
								var index = int.Parse(node.Value.Value.Substring("terrain.rubberduck.plateauwall".Length));
								var cell = new ActorReference(node.Value.Value, node.Value.ToDictionary()).Get<LocationInit>().Value;
								if (preset != "hill-fortress" || index % 25 == 24 || map.Ramp[cell + PlateauTopology.Directions[index / 25]] == 0)
									throw new InvalidDataException("Projected wall is not a sloped ramp retaining face.");
							}
							Console.WriteLine($"SHARED MOUNTAIN PASS {preset}/{mode}/{players}/{seed} attempt={plan.GenerationAttempt}"); cases++;
						}
			var blocked = new MapPlan(48, 48);
			for (var x = 16; x < 26; x++) for (var y = 18; y < 30; y++)
			{
				blocked.SetTerrain(x, y, PlannedTerrain.Mountain); blocked.AddFeature(x, y, PlannedFeature.Resource);
			}
			if (RubberduckMountainTopology.Prepare(blocked) || blocked.PlateauSurfaces.Count != 0) throw new InvalidDataException("Protected layout was not rejected atomically.");
			for (var x = 16; x < 26; x++) for (var y = 18; y < 30; y++)
				if (blocked.TerrainAt(x, y) != PlannedTerrain.Mountain || !blocked.HasFeature(x, y, PlannedFeature.Resource)) throw new InvalidDataException("Rejected proposal mutated input.");
			Console.WriteLine($"SHARED MOUNTAIN COMPLETE {cases} twice-generated physical exports; protected-input negative control passed.");
		}
	}
}
