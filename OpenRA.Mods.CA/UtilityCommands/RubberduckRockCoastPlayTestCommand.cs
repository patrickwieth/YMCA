using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckRockCoastPlayTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-rock-coast-playtest";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 5 &&
			(args[3] == "cliff-review" || args[3] == "cliff-review-shaded") &&
			new[] { "16", "24", "26", "30" }.Contains(args[4]) || args.Length == 3 || args.Length == 4 && (args[3] == "generated-fog" || args[3] == "generated-fog-shaded" || args[3] == "ground-routes-fog" || args[3] == "ground-routes" || args[3] == "generated" || args[3] == "trimmed" || args[3] == "joined" || args[3] == "overview" || args[3] == "overview-shaded" || args[3] == "detail-review" || args[3] == "detail-review-shaded" || args[3] == "mountain-review" || args[3] == "mountain-review-shaded" || args[3] == "shore-review" || args[3] == "shore-review-shaded");
		[Desc("MAP OUTPUT [generated|generated-fog|generated-fog-shaded|trimmed|joined|ground-routes|ground-routes-fog|overview|overview-shaded|detail-review|detail-review-shaded|mountain-review|mountain-review-shaded|shore-review|shore-review-shaded]", "Test saved coast geometry or make visual reviews. Also: MAP OUTPUT cliff-review[-shaded] SLOT (16/24/26/30). Never regenerate terrain.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData; var output = Path.GetFullPath(args[2]); Directory.CreateDirectory(output);
			using var stream = File.OpenRead(args[1]); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]); using var map = new Map(utility.ModData, zip);
			if (args.Length == 5 && args[3].StartsWith("cliff-review", StringComparison.Ordinal))
			{
				ExportReview(utility, map, output, args[3], int.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture));
				return;
			}
			if (args.Length == 4 && args[3].StartsWith("ground-routes", StringComparison.Ordinal)) { ExportGroundRoutes(utility, map, output, args[3] == "ground-routes-fog"); return; }
			if (args.Length == 4 && (args[3].StartsWith("overview", StringComparison.Ordinal) || args[3].StartsWith("detail-review", StringComparison.Ordinal) || args[3].StartsWith("mountain-review", StringComparison.Ordinal) || args[3].StartsWith("shore-review", StringComparison.Ordinal)))
			{
				ExportReview(utility, map, output, args[3]);
				return;
			}
			var fog = args.Length == 4 && args[3].StartsWith("generated-fog", StringComparison.Ordinal);
			if (args.Length == 4 && args[3] == "generated-fog-shaded") map.Tileset = "RUBBERDUCK-TEMPERATE-SHADED";
			var center = new MPos(48, 64).ToCPos(map); var actors = map.ActorDefinitions.ToList();
			var routes = args.Length == 4 ? GeneratedRoutes(map, args[3] == "trimmed", args[3] == "joined" ? center + new CVec(12, 0) : (CPos?)null) : Enumerable.Range(0, 4).Select(d =>
			{
				var outward = PlateauTopology.Directions[d]; var lateral = PlateauTopology.Directions[(d + 1) % 4];
				var rock = center + outward * 12 + lateral * (d == 0 ? 9 : d == 1 ? -9 : 6);
				return (Rock: rock, Boarder: rock - outward * 2, Beach: center + outward * 13, Land: center + outward * 9, Start: rock + outward * 3);
			}).ToArray();
			var view = args.Length == 4 ? routes[0].Rock : center + new CVec(12, 12);
			var result = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(result)); File.WriteAllText(result, ""); File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {view}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: 0.4\n\t\tScreenshotTicks: 1800\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: {(fog ? "true" : "false")}\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: {(fog ? "false" : "true")}\n\t\tExploredMapCheckboxLocked: true\n");
			foreach (var kind in new[] { "infantry", "tank" })
				rules.AppendLine($"calibration.coast{kind}:\n\tInherits: {(kind == "infantry" ? "Light_Infantry" : "Heavy_Tank")}\n\t-ArmyFollower:\n\t-Autobattler:\n\tRenderSprites:\n\t\tImage: {(kind == "infantry" ? "light_infantry" : "heavytank")}");
			for (var d = 0; d < routes.Length; d++)
			{
				var route = routes[d]; var rock = route.Rock;
				if ((map.Tiles[rock].Type != RubberduckRockCoastPlan.BlockedSand && map.Tiles[rock].Type != RubberduckRockCoastPlan.BlockedGrass) || map.Height[rock] != 0) throw new InvalidDataException("Missing flat coast bank.");
				var name = "calibration.coastboat" + d;
				rules.AppendLine($"{name}:\n\tInherits: LST\n\tRenderSprites:\n\t\tImage: lst\n\tCargo:\n\t\tInitialUnits: calibration.coastinfantry, calibration.coasttank\n\tRockCoastProbe:\n\t\tRock: {rock}\n\t\tBoarder: {route.Boarder}\n\t\tBeach: {route.Beach}\n\t\tLand: {route.Land}\n\t\tResultPath: {result}");
				actors.Add(new MiniYamlNode("CoastPlayBoat" + d, new ActorReference(name) { new LocationInit(route.Start), new OwnerInit("Multi0") }.Save()));
			}
			map.ActorDefinitions = actors; map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "coast-playtest"));
			map.Title = "Actual editor coast save cargo test";
			var path = Path.Combine(output, "rubberduck-rock-coast-playtest.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var checkStream = File.OpenRead(path); using var checkZip = new ZipFileLoader.ReadOnlyZipFile(checkStream, path); using var reload = new Map(utility.ModData, checkZip);
			if (reload.InvalidCustomRules) throw new InvalidDataException("Coast playtest rules invalid.", reload.InvalidCustomRulesException);
			foreach (var c in map.AllCells)
				if (map.Height[c] != reload.Height[c] || !map.Tiles[c].Equals(reload.Tiles[c]) || !map.Resources[c].Equals(reload.Resources[c])) throw new InvalidDataException("Playtest changed editor terrain.");
			File.WriteAllText(Path.Combine(output, "routes.txt"), string.Join("\n", routes.Select(r => r.ToString())));
			Console.WriteLine(path);
		}

		static void ExportGroundRoutes(Utility utility, Map map, string output, bool fog)
		{
			var spawns = map.ActorDefinitions.Where(n => n.Value.Value == "mpspawn")
				.Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value).Take(4).ToArray();
			if (spawns.Length < 2) throw new InvalidDataException("Ground route fixture needs two player spawns.");
			var barrier = map.AllCells.First(c => map.Contains(c) && map.Tiles[c].Type == 3992 && map.Height[c] == 4);
			CPos Endpoint(CPos spawn, int kind, bool target)
			{
				for (var radius = 4 + kind * 2; radius <= 12; radius++)
				{
					var c = spawn + (target ? new CVec(0, radius) : new CVec(radius, 0));
					if (map.Contains(c) && map.Height[c] == 0 && map.Ramp[c] == 0 && map.Resources[c].Type == 0 &&
						(map.Tiles[c].Type == 1000 || map.Tiles[c].Type == 1020 || map.Tiles[c].Type == 1030)) return c;
				}
				throw new InvalidDataException("No resource-free ground probe endpoint near spawn.");
			}
			var result = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(result)); File.WriteAllText(result, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {spawns[0] + new CVec(3, -3)}\n\t\tMinimumZoomScale: 0.1\n\t\tZoomScale: 0.35\n\t\tScreenshotTicks: 1200\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: {(fog ? "true" : "false")}\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: {(fog ? "false" : "true")}\n\t\tExploredMapCheckboxLocked: true\n");
			var actors = map.ActorDefinitions.ToList();
			for (var route = 0; route < spawns.Length; route++)
				for (var kind = 0; kind < 2; kind++)
				{
					var start = Endpoint(spawns[route], kind, false);
					var target = Endpoint(spawns[(route + 1) % spawns.Length], kind, true);
					var name = $"calibration.groundroute{route}-{kind}";
					rules.AppendLine($"{name}:\n\tInherits: {(kind == 0 ? "Light_Infantry" : "Heavy_Tank")}\n\t-ArmyFollower:\n\t-Autobattler:\n\tRenderSprites:\n\t\tImage: {(kind == 0 ? "light_infantry" : "heavytank")}\n\tPlateauMovementProbe:\n\t\tGroundRoute: true\n\t\tTarget: {target}\n\t\tCliffTop: {barrier}\n\t\tResultPath: {result}");
					actors.Add(new MiniYamlNode("GroundRoute" + actors.Count, new ActorReference(name) { new LocationInit(start), new OwnerInit("Multi0") }.Save()));
				}
			map.ActorDefinitions = actors;
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "ground-route-review"));
			var path = Path.Combine(output, "rubberduck-ground-routes.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var stream = File.OpenRead(path);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
			using var reload = new Map(utility.ModData, zip);
			if (reload.InvalidCustomRules) throw new InvalidDataException("Ground route rules failed.", reload.InvalidCustomRulesException);
			foreach (var c in map.AllCells)
				if (map.Height[c] != reload.Height[c] || !map.Tiles[c].Equals(reload.Tiles[c]) || !map.Resources[c].Equals(reload.Resources[c]))
					throw new InvalidDataException("Ground route fixture changed terrain.");
			Console.WriteLine(path);
		}

		static void ExportReview(Utility utility, Map map, string output, string mode, int? cliffSlot = null)
		{
			if (!map.Tileset.StartsWith("RUBBERDUCK-", StringComparison.Ordinal) ||
				map.RuleDefinitions != null && (!map.RuleDefinitions.Nodes.IsDefaultOrEmpty || !string.IsNullOrEmpty(map.RuleDefinitions.Value)) ||
				map.SequenceDefinitions != null && (!map.SequenceDefinitions.Nodes.IsDefaultOrEmpty || !string.IsNullOrEmpty(map.SequenceDefinitions.Value)))
				throw new InvalidDataException("Visual review requires a Rubberduck production map using shared rules/art.");
			if (mode.EndsWith("-shaded", StringComparison.Ordinal)) map.Tileset = "RUBBERDUCK-TEMPERATE-SHADED";
			var mountain = mode.StartsWith("mountain-review", StringComparison.Ordinal);
			var shore = mode.StartsWith("shore-review", StringComparison.Ordinal);
			var detail = shore || mountain || cliffSlot.HasValue || mode.StartsWith("detail-review", StringComparison.Ordinal);
			var center = new MPos((map.Bounds.Left + map.Bounds.Right) / 2, (map.Bounds.Top + map.Bounds.Bottom) / 2).ToCPos(map);
			if (cliffSlot.HasValue)
			{
				var candidates = map.ActorDefinitions.Where(n => n.Value.Value == "terrain.rubberduck.nativecliff" + cliffSlot.Value)
					.Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value).ToArray();
				if (candidates.Length == 0) throw new InvalidDataException("No native cliff connector of the requested kind in this production map.");
				var middle = center;
				center = candidates.OrderBy(c => (c - middle).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y).First();
				File.WriteAllText(Path.Combine(output, "cliff-location.txt"), $"Slot {cliffSlot.Value}, anchor {center}; shared production art, no geometry changes.\n");
				WriteCliffApronAudit(map, output);
				center += new CVec(3, -3);
			}
			else if (mountain)
			{
				var middle = center;
				var candidates = map.ActorDefinitions.Where(n => n.Value.Value == "terrain.rubberduck.mountainwall0" || n.Value.Value == "terrain.rubberduck.mountainwall1")
					.Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value).ToArray();
				if (candidates.Length == 0) throw new InvalidDataException("No projected mountain walls in this map.");
				center = candidates.OrderBy(c => (c - middle).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y).First() + new CVec(3, -3);
			}
			else if (shore)
			{
				var middle = center;
				bool Water(CPos c) => map.Contains(c) && map.Height[c] == 0 && map.Ramp[c] == 0 &&
					(map.Tiles[c].Type == 1050 || map.Tiles[c].Type == 1060 || map.Tiles[c].Type == 1070 || map.Tiles[c].Type == 1080);
				var candidates = map.AllCells.Where(c => map.Contains(c) && map.Height[c] == 0 && map.Ramp[c] == 0 &&
					new ushort[] { 1000, 1010, 1020, 1030, 1040 }.Contains(map.Tiles[c].Type) && PlateauTopology.Directions.Any(d => Water(c + d))).ToArray();
				if (candidates.Length == 0) throw new InvalidDataException("No ordinary source-shore receiver in this production map.");
				center = candidates.OrderBy(c => (c - middle).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y).First();
				File.WriteAllText(Path.Combine(output, "shore-location.txt"), $"Receiver {center}; {candidates.Length} cardinal shore receivers; no terrain or collision changes.\n");
			}
			else if (detail)
			{
				var spawn = map.ActorDefinitions.First(n => n.Value.Value == "mpspawn");
				center = new ActorReference(spawn.Value.Value, spawn.Value.ToDictionary()).Get<LocationInit>().Value + new CVec(3, -3);
			}
			if (!detail) center += new CVec(map.MapSize.X / 6, -map.MapSize.X / 6);
			var occlusionRules = "";
			if (cliffSlot.HasValue)
			{
				var resultDirectory = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes");
				Directory.CreateDirectory(resultDirectory);
				var resultName = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(output)));
				var resultPath = Path.Combine(resultDirectory, resultName + ".log");
				File.WriteAllText(resultPath, "");
				File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), resultPath);
				occlusionRules = $"\t\tValidateMaterialOcclusion: true\n\t\tReportCliffGeometry: true\n\t\tResultPath: {resultPath}\n";
			}
			var zoom = (cliffSlot.HasValue || mountain || shore ? 0.65 : detail ? 0.35 : 6.0 / map.MapSize.X).ToString(System.Globalization.CultureInfo.InvariantCulture);
			var shoreRules = shore ? "\tRubberduckMaterialTransitions:\n\t\tAuthoredShores: true\n" : "";
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($"World:\n{shoreRules}\tTerrainCalibrationView:\n\t\tCenter: {center}\n\t\tMinimumZoomScale: 0.025\n\t\tZoomScale: {zoom}\n\t\tScreenshotTicks: 75\n{occlusionRules}Player:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n", "terrain-visual-review"));
			var players = new MapPlayers(map.PlayerDefinitions);
			foreach (var player in players.Players.Where(p => p.Key.StartsWith("Multi", StringComparison.Ordinal)).Select(p => p.Value))
			{
				player.Faction = "blackh";
				player.LockFaction = true;
			}
			players.Players["Multi0"].Spawn = 1;
			players.Players["Multi0"].LockSpawn = true;
			map.PlayerDefinitions = players.ToMiniYaml();
			var path = Path.Combine(output, "rubberduck-terrain-review.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var stream = File.OpenRead(path);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
			using var reload = new Map(utility.ModData, zip);
			if (reload.InvalidCustomRules) throw new InvalidDataException("Visual review rules failed.", reload.InvalidCustomRulesException);
			foreach (var cell in map.AllCells)
				if (map.Height[cell] != reload.Height[cell] || !map.Tiles[cell].Equals(reload.Tiles[cell]) || !map.Resources[cell].Equals(reload.Resources[cell]))
					throw new InvalidDataException("Visual review changed terrain.");
			File.WriteAllText(Path.Combine(output, "review-scope.txt"), "Visual-only review: no movement assertions. Terrain preserved; camera/fog/faction pinned; tileset " + map.Tileset + ".\n");
			Console.WriteLine(path);
		}

		static void WriteCliffApronAudit(Map map, string output)
		{
			var actors = map.ActorDefinitions.Where(n => n.Value.Value.StartsWith("terrain.rubberduck.nativecliff", StringComparison.Ordinal) ||
				n.Value.Value.StartsWith("terrain.rubberduck.plateauwall", StringComparison.Ordinal))
				.Select(n => (Type: n.Value.Value, Cell: new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value))
				.GroupBy(a => a.Cell).ToDictionary(g => g.Key, g => g.Select(a => a.Type).OrderBy(a => a, StringComparer.Ordinal).ToArray());
			var rows = new StringBuilder("cell\tkind\tvisible-grass-receivers\tnearby-actors-not-coverage\n");
			var edges = 0; var corners = 0; var unclassified = 0; var addedEdges = 0;
			foreach (var cell in map.AllCells.Where(map.Contains).Where(c => map.Tiles[c].Type == 3992 && map.Height[c] == 0).OrderBy(c => c.X).ThenBy(c => c.Y))
			{
				var direct = PlateauTopology.Directions.Any(d => map.Contains(cell + d) && map.Height[cell + d] > 0);
				var eligible = RubberduckMaterialTransitions.IsApronDonor(map, cell);
				var kind = direct ? "edge" : eligible ? "closed-convex" : "unclassified";
				if (direct) edges++; else if (eligible) corners++; else unclassified++;
				var receivers = PlateauTopology.Directions.Select(d => cell + d).Where(c => map.Contains(c) && map.Height[c] == 0 && map.Ramp[c] == 0 &&
					(map.Tiles[c].Type == 1000 || map.Tiles[c].Type == 1010)).Where(c =>
				{
					var visible = RubberduckMaterialLayer.VisibleRectangles(map, c);
					return visible == null || visible.Length > 0;
				}).ToArray();
				if (!direct && eligible) addedEdges += receivers.Length;
				var nearby = new System.Collections.Generic.List<string>();
				for (var dx = -2; dx <= 2; dx++)
					for (var dy = -2; dy <= 2; dy++)
					{
						var other = cell + new CVec(dx, dy);
						if (actors.TryGetValue(other, out var types)) nearby.AddRange(types.Select(t => $"{t}@{other}"));
					}
				rows.AppendLine($"{cell}\t{kind}\t{string.Join(";", receivers)}\t{string.Join(";", nearby)}");
			}
			var junctions = new StringBuilder("cell\tdirection\tcardinal-x\tcardinal-y\tdiagonal-end\n");
			string Surface(CPos c) => map.Contains(c) ? $"{c}:tile={map.Tiles[c].Type},height={map.Height[c]},ramp={map.Ramp[c]}" : $"{c}:outside";
			foreach (var pair in actors.OrderBy(p => p.Key.X).ThenBy(p => p.Key.Y))
				for (var direction = 0; direction < 2; direction++)
				{
					if (!pair.Value.Contains("terrain.rubberduck.plateauwall" + (direction * 25 + 24))) continue;
					var foot = pair.Key + PlateauTopology.Directions[direction];
					if (!map.Contains(foot) || map.Height[foot] != 0 || map.Tiles[foot].Type != 3992) continue;
					junctions.AppendLine($"{pair.Key}\t{direction}\t{Surface(pair.Key + new CVec(1, 0))}\t{Surface(pair.Key + new CVec(0, 1))}\t{Surface(pair.Key + new CVec(1, 1))}");
				}
			File.WriteAllText(Path.Combine(output, "cliff-junctions.tsv"), junctions.ToString());
			File.WriteAllText(Path.Combine(output, "cliff-apron-audit.tsv"), rows.ToString());
			File.WriteAllText(Path.Combine(output, "cliff-apron-audit.txt"),
				$"Low blocked dirt: {edges} cardinal-edge, {corners} closed-convex, {unclassified} unclassified cells.\n" +
				$"Previously missing convex fringe: {addedEdges} geometrically visible receiver edges (not unique cells).\n" +
				"Read-only topology census. Nearby actors are context, not pixel coverage; roofs clip receivers but native actors may cover them further.\n" +
				"Unclassified does not mean unused or safe to unblock. All blocked tiles and their artwork remain unchanged.\n");
		}

		static (CPos Rock, CPos Boarder, CPos Beach, CPos Land, CPos Start)[] GeneratedRoutes(Map map, bool trimmed, CPos? requiredRock)
		{
			bool Ground(CPos c) => map.Contains(c) && map.Height[c] == 0 && (map.Tiles[c].Type == 1000 || map.Tiles[c].Type == 1020);
			bool Water(CPos c) => map.Contains(c) && map.Tiles[c].Type == 1050;
			foreach (var rock in map.AllCells.Where(map.Contains).Where(c => (!requiredRock.HasValue || c == requiredRock.Value) && map.Height[c] == 0 && (map.Tiles[c].Type == RubberduckRockCoastPlan.BlockedGrass || map.Tiles[c].Type == RubberduckRockCoastPlan.BlockedSand)).OrderBy(c => Math.Abs(c.ToMPos(map).U - map.MapSize.X / 2) + Math.Abs(c.ToMPos(map).V - map.MapSize.Y / 2)))
				foreach (var outward in PlateauTopology.Directions)
				{
					var start = rock + outward * 3; var boarder = rock - outward * 2;
					if (!Water(start) || !Ground(boarder) || !map.Contains(rock + outward) || map.Tiles[rock + outward].Type != RubberduckRockCoastPlan.BlockedWater) continue;
					if (Enumerable.Range(-1, 3).Any(x => Enumerable.Range(-1, 3).Any(y => Ground(start + new CVec(x, y))))) continue;
					var water = new System.Collections.Generic.HashSet<CPos>(); var queue = new System.Collections.Generic.Queue<CPos>(); queue.Enqueue(start);
					while (queue.Count != 0)
					{
						var c = queue.Dequeue(); if (!water.Add(c)) continue;
						foreach (var d in PlateauTopology.Directions) if (Water(c + d) && !water.Contains(c + d)) queue.Enqueue(c + d);
					}
					foreach (var beach in water.OrderBy(c => Math.Abs(c.X - start.X) + Math.Abs(c.Y - start.Y)))
						foreach (var d in PlateauTopology.Directions)
						{
							var land = beach + d * (trimmed ? 5 : 3);
							var tangent = new CVec(-d.Y, d.X);
							bool RockBank(CPos c) => map.Contains(c) && (map.Tiles[c].Type == RubberduckRockCoastPlan.BlockedGrass || map.Tiles[c].Type == RubberduckRockCoastPlan.BlockedSand);
							if (trimmed && (!RockBank(beach + d + tangent) || !RockBank(beach + d - tangent))) continue;
							if (Ground(beach + d) && Ground(beach + d * 2) && Ground(land) && Ground(land + new CVec(2, -2)))
								return new[] { (rock, boarder, beach, land, start) };
						}
				}
			throw new InvalidDataException("No safe generated coast/landing pair found for runtime probe.");
		}
	}
}
