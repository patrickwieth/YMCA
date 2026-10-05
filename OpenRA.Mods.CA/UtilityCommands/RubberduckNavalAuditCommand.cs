using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	// Conservative terrain-only audit. Actor traffic and real travel times require client probes.
	sealed class RubberduckNavalAuditCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-naval-audit";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		internal static readonly CVec[] Steps = (from x in Enumerable.Range(-1, 3) from y in Enumerable.Range(-1, 3)
			where x != 0 || y != 0 select new CVec(x, y)).ToArray();

		[Desc("MAP_DIRECTORY OUTPUT", "Audit exported naval routes, shore access and 1/3/5-cell clearance without changing maps.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			SelfTest();
			Directory.CreateDirectory(args[2]);
			using var report = new StreamWriter(Path.Combine(args[2], "naval-routes.tsv"));
			report.WriteLine("map\twidth\tfrom\tto\tland-access-from\tland-access-to\twater-cost\tstatus");
			using var coastReport = new StreamWriter(Path.Combine(args[2], "naval-body-access.tsv"));
			coastReport.WriteLine("map\tplayer\tbody\twater-cells\tnearest-reachable-shore-cost");
			var count = 0;
			foreach (var file in Directory.GetFiles(args[1], "*.oramap", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
			{
				using var stream = File.OpenRead(file);
				using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, file);
				using var map = new Map(utility.ModData, zip);
				var locomotors = map.Rules.Actors["world"].TraitInfos<LocomotorInfo>().ToArray();
				Dictionary<CPos, int> Terrain(string name)
				{
					var speeds = locomotors.Single(l => l.Name == name).TerrainSpeeds;
					return map.AllCells.Where(map.Contains).Where(c => speeds.TryGetValue(map.GetTerrainInfo(c).Type, out var t) && t.Speed > 0)
						.ToDictionary(c => c, c => speeds[map.GetTerrainInfo(c).Type].Speed);
				}
				var water = Terrain("naval");
				var land = Terrain("foot");
				var bodies = Bodies(water);
				var bodyForCell = bodies.SelectMany((body, index) => body.Select(c => (Cell: c, Body: index))).ToDictionary(p => p.Cell, p => p.Body);
				var spawns = map.ActorDefinitions.Where(n => n.Value.Value == "mpspawn")
					.Select(n => new ActorReference(n.Value.Value, n.Value.ToDictionary()).Get<LocationInit>().Value).ToArray();
				var ports = new List<CPos[]>();
				var access = new List<long>();
				foreach (var spawn in spawns)
				{
					var walk = Distances(land, new[] { spawn });
					var shores = walk.Where(p => !water.ContainsKey(p.Key) && Steps.Any(s => water.ContainsKey(p.Key + s))).ToArray();
					// Report every reachable water body, not only the nearest pond. This prevents
					// treating a poor nearest-port choice as proof that a player cannot reach the sea.
					var reachableBodies = shores.SelectMany(p => Steps.Select(s => p.Key + s).Where(bodyForCell.ContainsKey)
						.Select(c => (Body: bodyForCell[c], Cost: p.Value))).GroupBy(p => p.Body).ToDictionary(g => g.Key, g => g.Min(p => p.Cost));
					for (var body = 0; body < bodies.Count; body++)
						coastReport.WriteLine(string.Join("\t", Path.GetRelativePath(args[1], file), ports.Count + 1, body + 1, bodies[body].Count,
							reachableBodies.TryGetValue(body, out var distance) ? distance : -1));
					var nearest = shores.Length == 0 ? -1 : shores.Min(p => p.Value);
					access.Add(nearest);
					ports.Add(shores.Where(p => p.Value <= nearest + 6000).SelectMany(p => Steps.Select(s => p.Key + s))
						.Where(water.ContainsKey).Distinct().ToArray());
				}
				foreach (var radius in new[] { 0, 1, 2 })
				{
					var wide = Erode(water, radius);
					// Ports lie on the bank. Enter the wide network through at most six cells of water;
					// do not require a bank cell itself to have water on its land-facing side.
					var launches = ports.Select(p => Distances(water, p, 6000).Keys.Where(wide.ContainsKey).ToArray()).ToArray();
					for (var a = 0; a < spawns.Length; a++)
					{
						var routes = Distances(wide, launches[a]);
						for (var b = a + 1; b < spawns.Length; b++)
						{
							var costs = launches[b].Where(routes.ContainsKey).Select(c => routes[c]).ToArray();
							var status = water.Count == 0 ? "NO_NAVAL_TERRAIN" : ports[a].Length == 0 || ports[b].Length == 0 ? "NO_SHORE_ACCESS" :
								launches[a].Length == 0 || launches[b].Length == 0 ? "NO_WIDE_LAUNCH" : costs.Length == 0 ? "DISCONNECTED" : "CONNECTED";
							report.WriteLine(string.Join("\t", Path.GetRelativePath(args[1], file), 2 * radius + 1, a + 1, b + 1,
								access[a], access[b], costs.Length == 0 ? "-1" : costs.Min().ToString(CultureInfo.InvariantCulture), status));
						}
					}
				}
				count++;
				Console.WriteLine($"AUDIT: {Path.GetFileName(file)}, {water.Count} naval cells, {spawns.Length} starts.");
			}
			if (count == 0) throw new InvalidDataException("No maps to audit.");
			File.WriteAllText(Path.Combine(args[2], "scope.txt"), "Terrain-only, conservative no-corner-cut 8-neighbor paths. Costs: 1000 cardinal/1414 diagonal at speed 100; actual locomotor terrain-speed ratios. Width is square CPos clearance, not a ship footprint. Bank access uses foot terrain costs, excludes actors, height-step restrictions and resource speed overrides. Multiple near-best ports avoid arbitrary tie selection. Six-cell launch access is excluded from route cost; costs are not end-to-end travel times. Nearest-port disconnections are findings, not automatic failures: enclosed basins may be intentional. naval-body-access.tsv separately measures access to EVERY water body; consult it before concluding the sea is inaccessible. No fairness certification or terrain mutation.\n");
			Console.WriteLine($"PASS: graph regressions; audited {count} maps. Review findings in naval-routes.tsv.");
		}

		internal static List<HashSet<CPos>> Bodies(Dictionary<CPos, int> cells)
		{
			var remaining = cells.Keys.ToHashSet();
			var result = new List<HashSet<CPos>>();
			foreach (var start in cells.Keys.OrderBy(c => c.X).ThenBy(c => c.Y))
			{
				if (!remaining.Remove(start)) continue;
				var body = new HashSet<CPos> { start }; var pending = new Queue<CPos>(); pending.Enqueue(start);
				while (pending.Count != 0)
				{
					var cell = pending.Dequeue();
					foreach (var step in Steps)
					{
						if (step.X != 0 && step.Y != 0 && (!cells.ContainsKey(cell + new CVec(step.X, 0)) || !cells.ContainsKey(cell + new CVec(0, step.Y)))) continue;
						var next = cell + step;
						if (!remaining.Remove(next)) continue;
						body.Add(next); pending.Enqueue(next);
					}
				}
				result.Add(body);
			}
			return result;
		}

		static Dictionary<CPos, int> Erode(Dictionary<CPos, int> cells, int radius) => cells.Where(p =>
			Enumerable.Range(-radius, 2 * radius + 1).All(x => Enumerable.Range(-radius, 2 * radius + 1)
				.All(y => cells.ContainsKey(p.Key + new CVec(x, y))))).ToDictionary(p => p.Key, p => p.Value);

		internal static Dictionary<CPos, long> Distances(Dictionary<CPos, int> cells, IEnumerable<CPos> starts, long limit = long.MaxValue)
		{
			var distances = new Dictionary<CPos, long>();
			var pending = new SortedSet<(long Cost, int X, int Y)>();
			foreach (var start in starts.Where(cells.ContainsKey).Distinct())
			{
				distances[start] = 0;
				pending.Add((0, start.X, start.Y));
			}
			while (pending.Count > 0)
			{
				var next = pending.Min; pending.Remove(next);
				var cell = new CPos(next.X, next.Y);
				if (distances[cell] != next.Cost) continue;
				foreach (var step in Steps)
				{
					var target = cell + step;
					if (!cells.TryGetValue(target, out var speed) || step.X != 0 && step.Y != 0 &&
						(!cells.ContainsKey(cell + new CVec(step.X, 0)) || !cells.ContainsKey(cell + new CVec(0, step.Y)))) continue;
					// Average reciprocal speeds makes the edge cost symmetric when crossing terrain types.
					var cost = next.Cost + (step.X == 0 || step.Y == 0 ? 1000L : 1414L) * 50 * (speed + cells[cell]) / speed / cells[cell];
					if (cost > limit || distances.TryGetValue(target, out var old) && old <= cost) continue;
					distances[target] = cost;
					pending.Add((cost, target.X, target.Y));
				}
			}
			return distances;
		}

		static void SelfTest()
		{
			var cells = (from x in Enumerable.Range(-5, 11) from y in Enumerable.Range(-3, 7)
				where x != 0 || y == 0 select new CPos(x, y)).ToDictionary(c => c, _ => 100);
			var start = new CPos(-3, 0); var end = new CPos(3, 0);
			if (Distances(cells, new[] { start })[end] != 6000 || Distances(Erode(cells, 1), new[] { start }).ContainsKey(end))
				throw new InvalidDataException("Naval neck/clearance regression.");
			var slow = cells.ToDictionary(p => p.Key, _ => 50);
			if (Distances(slow, new[] { start })[end] != 12000) throw new InvalidDataException("Naval speed regression.");
			var rotated = cells.ToDictionary(p => new CPos(-p.Key.Y, p.Key.X), p => p.Value);
			if (Distances(rotated, new[] { new CPos(0, -3) })[new CPos(0, 3)] != 6000)
				throw new InvalidDataException("Naval rotation regression.");
			cells[CPos.Zero] = 70;
			if (Distances(cells, new[] { start })[end] != Distances(cells, new[] { end })[start])
				throw new InvalidDataException("Naval mixed-speed reversal regression.");
			var corners = new Dictionary<CPos, int> { [CPos.Zero] = 100, [new CPos(1, 1)] = 100 };
			if (Distances(corners, new[] { CPos.Zero }).Count != 1 || Bodies(corners).Count != 2 || Bodies(cells).Count != 1 || Bodies(Erode(cells, 1)).Count != 2) throw new InvalidDataException("Naval diagonal leak.");
		}
	}
}
