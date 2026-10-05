using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckGeneratedPlateauTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-generated-plateau-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2;

		[Desc("OUTPUT [players=N] [seed=N] [mode=tactical|operational|strategic] [fog=true] [army-followers=true] [shaded=true]",   "Export a production Mountain Valleys map and a separate copy with real plateau movement probes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var options = DebugMapGeneratorCommand.ParseOptions(args.Where((a, i) => i < 2 ||
				!a.StartsWith("fog=", StringComparison.OrdinalIgnoreCase) && !a.StartsWith("army-followers=", StringComparison.OrdinalIgnoreCase) &&
				!a.StartsWith("shaded=", StringComparison.OrdinalIgnoreCase)).ToArray());
			if (options.Preset != MapGenerationPreset.MountainValleys) throw new ArgumentException("This probe requires Mountain Valleys.");
			var plan = MapPlanGenerator.Generate(options);
			if (plan.Plateaus.Count == 0) throw new InvalidDataException("This plan contains no plateaus to test.");
			var output = Path.GetFullPath(args[1]);
			var path = new RubberduckMapExporter(utility.ModData).Export(plan, options, output);
			new MapPlanDebugRenderer().SaveAll(plan, output);
			using var stream = File.OpenRead(path);
			using var original = new ZipFileLoader.ReadOnlyZipFile(stream, path);
			using var map = new Map(utility.ModData, original);
			var shadeOption = args.Skip(2).SingleOrDefault(a => a.StartsWith("shaded=", StringComparison.OrdinalIgnoreCase));
			if (shadeOption != null && bool.Parse(shadeOption.Substring("shaded=".Length))) map.Tileset = "RUBBERDUCK-TEMPERATE-SHADED";
			if (map.RuleDefinitions.Nodes.Any()) throw new InvalidDataException("Production map unexpectedly has local rules.");
			// Launch argument overrides are not reliable through every lobby path.
			// Pin the diagnostic player in the map, without changing the production package.
			var testPlayers = new MapPlayers(map.PlayerDefinitions);
			testPlayers.Players["Multi0"].Faction = "blackh";
			testPlayers.Players["Multi0"].LockFaction = true;
			testPlayers.Players["Multi0"].Spawn = 1;
			testPlayers.Players["Multi0"].LockSpawn = true;
			map.PlayerDefinitions = testPlayers.ToMiniYaml();
			var padding = map.Grid.MaximumTerrainHeight;
			CPos Cell(PlanPoint p) => new MPos(p.X + 1, p.Y + padding + 1).ToCPos(map);
			var followOption = args.Skip(2).SingleOrDefault(a => a.StartsWith("army-followers=", StringComparison.OrdinalIgnoreCase));
			var observe = followOption != null && bool.Parse(followOption.Substring("army-followers=".Length));
			if (observe && options.Mode != GeneratedMapMode.Operational) throw new ArgumentException("Army follower observation requires Operational mode.");
			var camera = Cell(plan.Spawns[0]) + new CVec(3, -3);
			var fogOption = args.Skip(2).SingleOrDefault(a => a.StartsWith("fog=", StringComparison.OrdinalIgnoreCase));
			var fog = fogOption != null && bool.Parse(fogOption.Substring(4));
			var resultDirectory = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes");
			Directory.CreateDirectory(resultDirectory);
			var resultName = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(output)));
			var resultPath = Path.Combine(resultDirectory, resultName + ".log");
			File.WriteAllText(resultPath, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {camera}\n\t\tResultPath: {resultPath}\n\t\tValidateHomeBase: true\n\t\tMinimumZoomScale: 0.4\n\t\tZoomSteps: -10\n\t\tScreenshotTicks: {(observe ? 1550 : 900)}\n\t\tSelectPlateauProbes: {options.Mode == GeneratedMapMode.Tactical}\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: {fog}\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: {!fog}\n\t\tExploredMapCheckboxLocked: true\n");
			var actors = map.ActorDefinitions.ToList();
			var report = new StringBuilder("actor\tstart\ttarget\tramp\tcliff-top\tcliff-bottom\n");
			for (var index = 0; index < plan.Plateaus.Count; index++)
			{
				var plateau = plan.Plateaus[index];
				var target = Cell(plateau.Landing);
				var homeOwner = plan.HomeOwnerAt(plateau.Landing.X, plateau.Landing.Y);
				if (homeOwner >= 0)
				{
					var spawn = Cell(plan.Spawns.First(p => plan.HomeOwnerAt(p.X, p.Y) == homeOwner));
					// Walk into the base, not just onto the first high ramp landing.
					target = new CPos((target.X + spawn.X) / 2, (target.Y + spawn.Y) / 2);
				}
				var candidates = plan.PlateauSurfaces.Where(p => !p.Value.Blocked && p.Value.Ramp == 0)
					.Select(p => Cell(new PlanPoint(p.Key.U, p.Key.V)))
					.OrderBy(p => (p - target).LengthSquared);
				var top = CPos.Zero;
				var bottom = CPos.Zero;
				var found = false;
				foreach (var candidate in candidates)
				{
					foreach (var direction in PlateauTopology.Directions)
					{
						var neighbor = candidate + direction;
						if (map.Height[neighbor] != 0 || (map.Tiles[neighbor].Type != 1000 && map.Tiles[neighbor].Type != 3992)) continue;
						var approach = neighbor + direction;
						if (map.Tiles[neighbor].Type == 3992 && (!map.Contains(approach) || map.Height[approach] != 0 || map.Tiles[approach].Type != 1000)) continue;
						top = candidate; bottom = neighbor; found = true; break;
					}
					if (found) break;
				}
				if (!found) throw new InvalidDataException("No steep edge or reserved foot available for negative movement test.");
				for (var kind = 0; kind < 2; kind++)
				{
					var cliffTop = top;
					var cliffBottom = bottom;
					var blockedFoot = map.Tiles[bottom].Type == 3992;
					if (kind == 0)
						foreach (var face in plan.NativeCliffFaces.OrderBy(f =>
							(Cell(new PlanPoint(f.Point.U, f.Point.V)) - target).LengthSquared))
						{
							var cell = Cell(new PlanPoint(face.Point.U, face.Point.V));
							var foot = cell + PlateauTopology.Directions[face.Direction];
							var low = foot + PlateauTopology.Directions[face.Direction];
							if (map.Tiles[cell].Type != 1000 || map.Tiles[foot].Type != 3992 || map.Height[foot] != 0 ||
								map.Tiles[low].Type != 1000 || map.Height[low] != 0) continue;
							cliffTop = cell; cliffBottom = foot; blockedFoot = true; break;
						}
					var lateral = PlateauTopology.Directions[plateau.Ramp % 4] * (kind == 0 ? -1 : 1);
					var start = Cell(plateau.Approach) + lateral;
					var end = target + lateral;
					var name = $"calibration.generatedplateau{index}-{kind}";
					// Fixed-target terrain probes must not compete with automatic army orders.
					// The separate observer run retains the real ArmyFollower controller.
					var controllers = observe ? "" : "\n\t-ArmyFollower:\n\t-Autobattler:";
					rules.AppendLine($"{name}:\n\tInherits: {(kind == 0 ? "Light_Infantry" : "Heavy_Tank")}{controllers}\n\tRenderSprites:\n\t\tImage: {(kind == 0 ? "light_infantry" : "heavytank")}\n\tPlateauMovementProbe:\n\t\tTarget: {end}\n\t\tRamp: {plateau.Ramp}\n\t\tCliffTop: {cliffTop}\n\t\tCliffBottom: {cliffBottom}\n\t\tBlockedCliffFoot: {blockedFoot}\n\t\tObserveArmyFollower: {observe}\n\t\tResultPath: {resultPath}");
					var actor = new ActorReference(name) { new LocationInit(start), new OwnerInit("Multi0") };
					actors.Add(new MiniYamlNode("PlateauProbe" + actors.Count, actor.Save()));
					report.AppendLine($"{name}\t{start}\t{end}\t{plateau.Ramp}\t{cliffTop}\t{cliffBottom}");
				}
			}
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "generated-plateau-probes"));
			map.ActorDefinitions = actors;
			map.Title = "Plateau runtime test - " + map.Title;
			var testPath = Path.Combine(output, "rubberduck-generated-plateau-test.oramap");
			if (File.Exists(testPath)) File.Delete(testPath);
			using (var package = ZipFileLoader.Create(testPath)) map.Save(package);
			using var testStream = File.OpenRead(testPath);
			using var testPackage = new ZipFileLoader.ReadOnlyZipFile(testStream, testPath);
			using var reloaded = new Map(utility.ModData, testPackage);
			if (reloaded.InvalidCustomRules) throw new InvalidDataException("Generated probe rules failed.", reloaded.InvalidCustomRulesException);
			foreach (var cell in map.AllCells)
				if (reloaded.Height[cell] != map.Height[cell] || reloaded.Ramp[cell] != map.Ramp[cell] || reloaded.Tiles[cell].Type != map.Tiles[cell].Type)
					throw new InvalidDataException("Diagnostic copy changed terrain.");
			File.WriteAllText(Path.Combine(output, "movement-probes.tsv"), report.ToString());
			Console.WriteLine($"Production terrain preserved; {plan.Plateaus.Count * 2} {(observe ? "ArmyFollower observers" : "round-trip probes")}: {testPath}");
		}
	}
}
