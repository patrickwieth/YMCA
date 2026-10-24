using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class CustomMapTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--custom-map-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2;
		[Desc("OUTPUT [players=N] [seed=N]", "Exercise every custom topology combination and compare canonical routes with their existing generators.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var cases = new List<CustomMapSettings>();
			foreach (var subtype in Enum.GetValues<StrongholdSubtype>())
				foreach (var connected in new[] { false, true }) foreach (var battle in new[] { false, true })
					cases.Add(new CustomMapSettings { Strongholds = subtype, ConnectedStartAreas = connected, BattleInTheCenter = battle });
			foreach (var subtype in Enum.GetValues<PlainsSubtype>()) cases.Add(new CustomMapSettings { Topology = CustomTopology.OpenPlains, Plains = subtype });
			foreach (var subtype in Enum.GetValues<IslandSubtype>()) foreach (var connections in new[] { false, true })
				cases.Add(new CustomMapSettings { Topology = CustomTopology.Islands, Islands = subtype, Connections = connections });
			cases.AddRange(cases.Where(c => c.HasCenter).Select(c => { var copy = c.Copy(); copy.CenterBuildable = !c.CenterBuildable; return copy; }).ToArray());
			var failures = 0;
			for (var i = 0; i < cases.Count; i++)
			{
				var selected = args.Skip(2).FirstOrDefault(a => a.StartsWith("case="));
				if (selected != null && i != int.Parse(selected.Substring(5))) continue;
				var settings = cases[i];
				try
				{
					var parse = new[] { "test", args[1], "preset=" + settings.BasePreset, args.Skip(2).FirstOrDefault(a => a.StartsWith("players=")) ?? "players=4", args.Skip(2).FirstOrDefault(a => a.StartsWith("seed=")) ?? "seed=42" }.Concat(args.Skip(2).Where(a => !a.StartsWith("players=") && !a.StartsWith("seed=") && !a.StartsWith("case=") && !a.StartsWith("plan-only="))).ToArray();
					var options = DebugMapGeneratorCommand.ParseOptions(parse); options.Custom = settings;
					var resourceArgument = args.Skip(2).FirstOrDefault(a => a.StartsWith("resources="));
					if (resourceArgument != null)
					{
						options.Resources = Enum.Parse<ResourceFieldLayout>(resourceArgument.Substring(10), true);
						if (options.Resources == ResourceFieldLayout.None) options.FiniteResources = true;
					}
					var finiteArgument = args.Skip(2).FirstOrDefault(a => a.StartsWith("finite="));
					var techArgument = args.Skip(2).FirstOrDefault(a => a.StartsWith("tech="));
					if (finiteArgument != null) options.FiniteResources = bool.Parse(finiteArgument.Substring(7));
					if (techArgument != null) options.TechBuildings = Enum.Parse<TechBuildingDensity>(techArgument.Substring(5), true);
					if (options.Resources == ResourceFieldLayout.None) options.FiniteResources = true;
					settings.ApplyAutomaticSize(options);
					var plan = MapPlanGenerator.Generate(options);
					if (settings.BasePreset == MapGenerationPreset.BattleNexus && !settings.River)
						CheckNexusBacking(plan, options);
					if (settings.Topology == CustomTopology.OpenPlains && settings.BasePreset == MapGenerationPreset.Arabia &&
						options.Resources == ResourceFieldLayout.Patches)
						CheckStartingFields(plan);
					if (settings.Topology == CustomTopology.Islands && ((plan.Bridges.Count > 0) != settings.Connections)) throw new InvalidDataException("Connections flag does not match bridge output.");
					if (settings.Topology == CustomTopology.OpenPlains && settings.Plains == PlainsSubtype.Flat &&
						Enumerable.Range(0, plan.Width * plan.Height).Any(n => plan.TerrainAt(n % plan.Width, n / plan.Width) != PlannedTerrain.Land)) throw new InvalidDataException("Flat plains contain terrain barriers.");
					if (settings.Topology == CustomTopology.Strongholds && !settings.River && settings.ConnectedStartAreas && plan.Plateaus.Count != (settings.BattleInTheCenter ? 2 : 4)) throw new InvalidDataException("Wrong number of team ramps.");
					var canonical = settings.Topology == CustomTopology.Strongholds ?
						(settings.River ? settings.ConnectedStartAreas && settings.BattleInTheCenter : !settings.ConnectedStartAreas || settings.BattleInTheCenter) :
						settings.Topology == CustomTopology.OpenPlains ? settings.Plains != PlainsSubtype.Flat :
						settings.Islands == IslandSubtype.BigIslands && settings.Connections || settings.Islands == IslandSubtype.MixedSizes && !settings.Connections;
					if (canonical && i < 17 && resourceArgument == null && finiteArgument == null && techArgument == null)
					{
						var baseline = MapPlanGenerator.Generate(DebugMapGeneratorCommand.ParseOptions(parse));
						if (!plan.Spawns.SequenceEqual(baseline.Spawns) || plan.Width != baseline.Width || plan.Height != baseline.Height || plan.Bridges.Count != baseline.Bridges.Count) throw new InvalidDataException("Canonical layout diverged.");
						const PlannedFeature economy = PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding;
						for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
							if (plan.TerrainAt(x, y) != baseline.TerrainAt(x, y) || (plan.FeaturesAt(x, y) & economy) != (baseline.FeaturesAt(x, y) & economy)) throw new InvalidDataException("Canonical terrain/economy diverged.");
					}
					if (settings.BasePreset == MapGenerationPreset.MountainValleys && plan.PlateauSurfaces.Keys.Any(p =>
						plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding)))
						throw new InvalidDataException("Mountain Valleys placed economy on a plateau or ramp.");
					var resourceCount = Enumerable.Range(0, plan.Width * plan.Height).Count(n => plan.HasFeature(n % plan.Width, n / plan.Width, PlannedFeature.Resource | PlannedFeature.RichResource));
					if (options.Resources == ResourceFieldLayout.None ? resourceCount != 0 : resourceCount == 0)
						throw new InvalidDataException("Resource selection does not match generated fields.");
					if (settings.Topology == CustomTopology.Islands && resourceArgument != null && options.Resources != ResourceFieldLayout.None)
					{
						var defaults = DebugMapGeneratorCommand.ParseOptions(parse); defaults.Custom = settings;
						var original = MapPlanGenerator.Generate(defaults);
						var originalCount = Enumerable.Range(0, original.Width * original.Height).Count(n => original.HasFeature(n % original.Width, n / original.Width, PlannedFeature.Resource | PlannedFeature.RichResource));
						if (settings.Islands == IslandSubtype.MixedSizes && options.Resources == ResourceFieldLayout.Patches && resourceCount >= originalCount ||
							settings.Islands != IslandSubtype.MixedSizes && options.Resources == ResourceFieldLayout.Glitter && resourceCount <= originalCount)
							throw new InvalidDataException("Glitter/Patches did not change the island resource distribution.");
					}
					Console.WriteLine($"RESOURCE PASS case={i} players={options.Players} seed={options.Seed} layout={options.Resources} finite={options.FiniteResources} tech={options.TechBuildings} cells={resourceCount}");
					if (args.Contains("plan-only=true")) continue;
					var directory = Path.Combine(args[1], i.ToString("D2"));
					var path = new RubberduckMapExporter(utility.ModData).Export(plan, options, directory);
					using var stream = File.OpenRead(path); using var package = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var map = new Map(utility.ModData, package);
					if (settings.BasePreset == MapGenerationPreset.BattleNexus && !settings.River &&
						(map.Bounds.Width != options.Width || map.Bounds.Height != options.Height))
						throw new InvalidDataException("Nexus backing changed the playable map dimensions.");
					if (settings.BasePreset == MapGenerationPreset.MountainValleys && map.ActorDefinitions.Any(a =>
						a.Value.Value.StartsWith("terrain.rubberduck.mountainwall", StringComparison.Ordinal)))
						throw new InvalidDataException("Mountain Valleys exported a legacy mountain wall.");
					if (settings.Topology == CustomTopology.Islands && (map.ActorDefinitions.Any(a =>
						a.Value.Value.StartsWith("terrain.rubberduck.coast", StringComparison.Ordinal)) ||
						map.AllCells.Any(c => map.Tiles[c].Type >= 14010 && map.Tiles[c].Type <= 14012)))
						throw new InvalidDataException("Island map exported rock-coast art or invisible coast blockers.");
					if (settings.Topology == CustomTopology.Islands)
					{
						var occupied = new HashSet<CPos>();
						foreach (var oil in map.ActorDefinitions.Where(a => a.Value.Value == "oilb"))
						{
							var cell = new ActorReference(oil.Value.Value, oil.Value.ToDictionary()).Get<LocationInit>().Value;
							for (var dy = -1; dy <= 2; dy++) for (var dx = -1; dx <= 2; dx++)
							{
								var c = cell + new CVec(dx, dy);
								if (!map.Contains(c) || !new[] { "Clear", "Road", "Dirt" }.Contains(map.GetTerrainInfo(c).Type))
									throw new InvalidDataException("Exported oil derrick lacks a complete dry shore margin.");
								if (dx >= 0 && dx < 2 && dy >= 0 && dy < 2 && !occupied.Add(c))
									throw new InvalidDataException("Oil derrick foundations overlap.");
							}
						}
					}
					for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
						if ((plan.TerrainAt(x, y) == PlannedTerrain.Land || plan.TerrainAt(x, y) == PlannedTerrain.ShallowWater) &&
							plan.HasFeature(x, y, PlannedFeature.ContestedCenter) && !plan.HasFeature(x, y, PlannedFeature.DestructibleBridge) &&
							!(plan.Bridges.Count > 0 && plan.HasFeature(x, y, PlannedFeature.Bottleneck)))
						{
							var cell = new MPos(x + 1, y + map.Grid.MaximumTerrainHeight + 1).ToCPos(map);
							var expected = settings.HasCenter && !settings.CenterBuildable ? "Dirt" : "Clear";
							if (map.GetTerrainInfo(cell).Type != expected) throw new InvalidDataException("Center buildability policy not exported.");
						}
					Console.WriteLine($"CUSTOM PASS {i:D2} {settings.Topology}/{settings.BasePreset} connected={settings.ConnectedStartAreas} battle={settings.BattleInTheCenter} bridges={plan.Bridges.Count} canonical={canonical}");
				}
				catch (Exception e) { failures++; Console.WriteLine($"CUSTOM FAIL {i:D2}: {e}"); }
			}
			if (failures != 0) throw new InvalidOperationException($"{failures} custom combinations failed.");
		}

		static void CheckNexusBacking(MapPlan plan, MapGenerationOptions options)
		{
			if (plan.PlayableInset != 16 || plan.Width != options.Width + 32 || plan.Height != options.Height + 32)
				throw new InvalidDataException("Nexus has no off-map contour backing.");
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				if (!plan.ContainsPlayable(x, y) && plan.HasFeature(x, y,
					PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding | PlannedFeature.Spawn))
					throw new InvalidDataException("Nexus placed gameplay economy outside the playable bounds.");
			var roofs = plan.PlateauSurfaces.Where(p => p.Value.Height == 4)
				.Select(p => MountainPlateauPlanner.Cell(p.Key.U, p.Key.V)).ToHashSet();
			if (roofs.Count == 0) throw new InvalidDataException("Nexus has no planned rock roofs.");
			while (roofs.Count != 0)
			{
				var pending = new Queue<CPos>(); pending.Enqueue(roofs.First());
				var visible = false; var extended = false;
				while (pending.Count != 0)
				{
					var cell = pending.Dequeue(); if (!roofs.Remove(cell)) continue;
					var p = MountainPlateauPlanner.Point(cell);
					visible |= plan.ContainsPlayable(p.U, p.V);
					extended |= p.U < 8 || p.V < 8 || p.U >= plan.Width - 8 || p.V >= plan.Height - 8;
					foreach (var direction in new[] { new CVec(1, 0), new CVec(-1, 0), new CVec(0, 1), new CVec(0, -1) })
						if (roofs.Contains(cell + direction)) pending.Enqueue(cell + direction);
				}
				if (visible && !extended) throw new InvalidDataException("A Nexus wall ends without extending well beyond the playable edge.");
			}
			Console.WriteLine("NEXUS BACKING PASS: closed walls extend beyond playable bounds; no off-map economy.");
		}

		static void CheckStartingFields(MapPlan plan)
		{
			var resources = Enumerable.Range(0, plan.Width * plan.Height)
				.Where(n => plan.HasFeature(n % plan.Width, n / plan.Width, PlannedFeature.Resource)).ToHashSet();
			double Distance(int cell, PlanPoint start) => Math.Sqrt(
				Math.Pow(cell % plan.Width - start.X, 2) + Math.Pow(cell / plan.Width - start.Y, 2));
			double NativeDistance(int cell, PlanPoint start) => Math.Sqrt(
				2 * Math.Pow(cell % plan.Width - start.X, 2) + 0.5 * Math.Pow(cell / plan.Width - start.Y, 2));
			foreach (var start in plan.Spawns)
			{
				var nearest = resources.OrderBy(n => Distance(n, start)).First();
				if (Distance(nearest, start) <= 10 || Distance(nearest, start) > 14)
					throw new InvalidDataException("Starting patch is not immediately outside the clear build area.");
				var reached = MountainValleysGenerator.FindReachable(plan, start);
				var field = new HashSet<int> { nearest };
				var pending = new Queue<int>(); pending.Enqueue(nearest);
				while (pending.Count != 0)
				{
					var cell = pending.Dequeue();
					if (!reached[cell] || plan.Spawns.Any(other =>
						(other.X != start.X || other.Y != start.Y) && NativeDistance(cell, other) < NativeDistance(cell, start) + 3 - 0.000001))
						throw new InvalidDataException("Starting patch is unreachable or ambiguously assigned to another start.");
					for (var dy = -1; dy <= 1; dy++) for (var dx = -1; dx <= 1; dx++)
					{
						var x = cell % plan.Width + dx; var y = cell / plan.Width + dy;
						var next = x + y * plan.Width;
						if (plan.Contains(x, y) && resources.Contains(next) && field.Add(next)) pending.Enqueue(next);
					}
				}
				if (field.Count < 81 || field.Count > 149)
					throw new InvalidDataException("Starting patch was truncated or merged into another deposit.");
			}
			Console.WriteLine($"START PATCH PASS players={plan.Spawns.Count}: close, reachable, distinct and uniquely assigned fields.");
		}
	}
}
