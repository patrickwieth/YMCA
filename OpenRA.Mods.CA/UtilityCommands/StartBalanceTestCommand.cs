using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class StartBalanceTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--start-balance-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1 || args.Length == 2 && args[1] == "options";
		[Desc("Validate owned starting budgets, oil ownership, native access, option controls and deterministic generation.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var cases = 0;
			if (args.Length == 1)
			foreach (var preset in new[] { "plains", "mountain-valleys" })
			foreach (var players in Enumerable.Range(2, 11).Append(16))
			{
				Check(preset, players, GeneratedMapMode.Strategic, ResourceFieldLayout.Patches, TechBuildingDensity.Sparse, false, 42);
				cases++;
			}
			foreach (var preset in new[] { "plains", "mountain-valleys" })
			foreach (var mode in new[] { GeneratedMapMode.Tactical, GeneratedMapMode.Operational })
			foreach (var resource in new[] { ResourceFieldLayout.None, ResourceFieldLayout.Glitter, ResourceFieldLayout.Patches })
			{
				Check(preset, 4, mode, resource, resource == ResourceFieldLayout.None ? TechBuildingDensity.None : TechBuildingDensity.Dense, true, 43);
				cases++;
			}
			Console.WriteLine($"START BALANCE COMPLETE: {cases} complete layouts.");
		}

		static void Check(string preset, int players, GeneratedMapMode mode, ResourceFieldLayout resource, TechBuildingDensity density, bool finite, int seed)
		{
			var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", $"preset={preset}", $"players={players}", $"mode={mode}",
				"size=auto", $"seed={seed}", $"resources={resource}", $"tech={density}", $"finite={finite}" });
			if (options.Mode != mode || options.Resources != resource || options.TechBuildings != density || options.FiniteResources != finite)
				throw new InvalidDataException("Preset defaults overrode an explicit economy option.");
			var plan = MapPlanGenerator.Generate(options);
			// Guard against economy intruding into the existing construction core;
			// compact high-player-count plateau geometry is intentionally retained.
			if (StartBalanceMetrics.Report(plan).Any(r => JObject.FromObject(r).Value<int>("BuildableCellsWithin12") < 160))
				throw new InvalidDataException("Starting economy squeezed the free construction core.");
			var distances = plan.Spawns.Select(s => StartBalanceMetrics.Distances(plan, s)).ToArray();
			var fields = plan.StartingFields.Where(f => !f.Expansion).ToArray();
			if (resource == ResourceFieldLayout.Patches)
			{
				if (fields.Length != players || fields.Select(f => f.Cells.Length).Distinct().Count() != 1 || fields[0].Cells.Length < 81)
					throw new InvalidDataException("Starting budgets are missing or unequal.");
				var edgeDistances = new List<int>();
				foreach (var field in fields)
				{
					var owner = plan.Spawns.IndexOf(field.Start); var edge = int.MaxValue;
					foreach (var p in field.Cells)
					{
						var i = p.X + p.Y * plan.Width; var own = distances[owner][i]; edge = Math.Min(edge, own);
						if (own < 11 || distances.Where((_, n) => n != owner).Any(d => d[i] >= 0 && d[i] < own + 3) ||
							!plan.HasFeature(p.X, p.Y, PlannedFeature.Resource) || plan.PlateauSurfaces.ContainsKey(new MPos(p.X, p.Y)))
							throw new InvalidDataException("Starting field was lost, moved onto a slope or assigned nearer to another start.");
					}
					edgeDistances.Add(edge);
				}
				if (edgeDistances.Max() > 32 || edgeDistances.Max() - edgeDistances.Min() > 4)
					throw new InvalidDataException("Starting field access is too far or uneven.");
			}
			else if (fields.Length != 0) throw new InvalidDataException("Fields were added despite another resource mode.");

			var ownedOil = new int[players]; var nearest = Enumerable.Repeat(int.MaxValue, players).ToArray(); var occupied = new HashSet<CPos>();
			var generators = 0;
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
			{
				if (plan.HasFeature(x, y, PlannedFeature.ResourceGenerator)) generators++;
				if (!plan.HasFeature(x, y, PlannedFeature.TechBuilding)) continue;
				var i = x + y * plan.Width;
				var owners = Enumerable.Range(0, players).Where(n => distances[n][i] >= 0).OrderBy(n => distances[n][i]).ToArray();
				if (owners.Length != players || preset != "plains" && distances[owners[1]][i] < distances[owners[0]][i] + 3)
					throw new InvalidDataException("Oil has no unique native-path owner.");
				var owner = owners[0]; ownedOil[owner]++; nearest[owner] = Math.Min(nearest[owner], distances[owner][i]);
				var cell = MountainPlateauPlanner.Cell(x, y);
				for (var dy = 0; dy < 2; dy++) for (var dx = 0; dx < 2; dx++)
				{
					var c = cell + new CVec(dx, dy); var p = MountainPlateauPlanner.Point(c);
					if (!occupied.Add(c) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land || plan.PlateauSurfaces.ContainsKey(p) ||
						plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.BuildClearance) || plan.StrategicReservations.Contains(p))
						throw new InvalidDataException("Oil footprint is obstructed.");
				}
			}
			var expected = density == TechBuildingDensity.None ? 0 : density == TechBuildingDensity.Sparse ? 1 : 2;
			if (preset == "plains") PlainsOilClusters.Validate(plan, density);
			if (preset != "plains" && (ownedOil.Any(count => count != expected) || expected > 0 && nearest.Max() - nearest.Min() > 2))
				throw new InvalidDataException("Oil counts or access differ between starts.");
			if (finite && generators != 0) throw new InvalidDataException("Finite mode gained generators.");
			if (resource == ResourceFieldLayout.Patches && !finite && generators < players) throw new InvalidDataException("Missing starting regeneration.");
			if (players == 4 && resource == ResourceFieldLayout.Patches)
			{
				var copy = plan.Clone();
				if (copy.OilClusters.Count != plan.OilClusters.Count || copy.OilClusters.Where((c, i) => !c.Towers.SequenceEqual(plan.OilClusters[i].Towers)).Any())
					throw new InvalidDataException("Clone lost oil cluster metadata.");
				if (copy.StartingFields.Count != plan.StartingFields.Count) throw new InvalidDataException("Clone lost starting-field guards.");
				var removed = copy.StartingFields[0].Cells[0];
				copy.RemoveFeature(removed.X, removed.Y, PlannedFeature.Resource);
				var rejected = false;
				try { BalancedStartingEconomy.ValidateFields(copy); }
				catch (InvalidOperationException) { rejected = true; }
				if (!rejected || !plan.HasFeature(removed.X, removed.Y, PlannedFeature.Resource))
					throw new InvalidDataException("Lost starting resources were accepted or clone edits affected the original.");
				var repeat = MapPlanGenerator.Generate(options);
				if (!plan.Spawns.SequenceEqual(repeat.Spawns)) throw new InvalidDataException("Start order is nondeterministic.");
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
					if (plan.TerrainAt(x, y) != repeat.TerrainAt(x, y) || plan.FeaturesAt(x, y) != repeat.FeaturesAt(x, y))
						throw new InvalidDataException("Terrain or economy is nondeterministic.");
			}
			Console.WriteLine($"START BALANCE PASS {preset} {players}P {mode} {resource} {density} finite={finite} seed={seed}");
		}
	}
}
