using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Pathfinder;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckRangeSearchTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-range-search-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Check bounded forward range search against exhaustive weighted graphs; exhaustion must fall back.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var steps = (from x in Enumerable.Range(-1, 3) from y in Enumerable.Range(-1, 3)
				where x != 0 || y != 0 select new CVec(x, y)).ToArray();
			var all = (from x in Enumerable.Range(-10, 21) from y in Enumerable.Range(-10, 21) select new CPos(x, y)).ToArray();
			var random = new Random(43);
			var checks = 0;
			for (var fixture = 0; fixture < 200; fixture++)
			{
				var passable = all.Where(_ => random.Next(5) != 0).ToHashSet();
				var goals = all.Where(c => Math.Abs(c.X) <= 4 && Math.Abs(c.Y) <= 4 && passable.Contains(c)).ToHashSet();
				var costs = all.ToDictionary(c => c, _ => random.Next(1, 4));
				IEnumerable<GraphConnection> Edges(CPos cell)
				{
					foreach (var step in steps)
					{
						var next = cell + step;
						if (!passable.Contains(next) || step.X != 0 && step.Y != 0 &&
							(!passable.Contains(cell + new CVec(step.X, 0)) || !passable.Contains(cell + new CVec(0, step.Y)))) continue;
						yield return new GraphConnection(next, (step.X == 0 || step.Y == 0 ? 10 : 14) * costs[next]);
					}
				}
				int Estimate(CPos cell)
				{
					var x = Math.Max(0, Math.Abs(cell.X) - 4); var y = Math.Max(0, Math.Abs(cell.Y) - 4);
					return (10 * Math.Max(x, y) + 4 * Math.Min(x, y)) * 125 / 100;
				}
				foreach (var source in new[] { new CPos(-9, -9), new CPos(9, -9), new CPos(-9, 9), new CPos(9, 9), CPos.Zero })
				{
					// Source cells may be inaccessible, but outgoing edges remain valid.
					var distances = new Dictionary<CPos, int> { [source] = 0 };
					var pending = new SortedSet<(int Cost, int X, int Y)> { (0, source.X, source.Y) };
					var reference = -1;
					while (pending.Count != 0)
					{
						var next = pending.Min; pending.Remove(next); var cell = new CPos(next.X, next.Y);
						if (distances[cell] != next.Cost) continue;
						if (goals.Contains(cell)) { reference = next.Cost; break; }
						foreach (var edge in Edges(cell))
						{
							var cost = next.Cost + edge.Cost;
							if (distances.TryGetValue(edge.Destination, out var old) && old <= cost) continue;
							distances[edge.Destination] = cost;
							pending.Add((cost, edge.Destination.X, edge.Destination.Y));
						}
					}
					if (!SafeRangePathSearch.TrySearch(source, goals, Edges, Estimate, 2048, out var path) ||
						(reference < 0) != (path.Count == 0)) throw new InvalidOperationException("Range search changed reachability.");
					if (path.Count != 0)
					{
						if (path[path.Count - 1] != source || !goals.Contains(path[0])) throw new InvalidOperationException("Range path endpoints/order changed.");
						var total = 0;
						for (var i = path.Count - 1; i > 0; i--) total += Edges(path[i]).Single(e => e.Destination == path[i - 1]).Cost;
						if (total > reference * 125 / 100) throw new InvalidOperationException("Range search exceeded its heuristic bound.");
					}
					if (!goals.Contains(source) && SafeRangePathSearch.TrySearch(source, goals, Edges, Estimate, 0, out _))
						throw new InvalidOperationException("Search budget exhaustion was mistaken for a completed search.");
					checks++;
				}
			}
			var a = CPos.Zero; var b = new CPos(0, 0, 1); var c = new CPos(1, 0, 1);
			IEnumerable<GraphConnection> Layers(CPos from) => from == a ? new[] { new GraphConnection(b, 0) } : from == b ?
				new[] { new GraphConnection(a, 0), new GraphConnection(c, 10) } : Array.Empty<GraphConnection>();
			if (!SafeRangePathSearch.TrySearch(a, new HashSet<CPos> { c }, Layers, _ => 0, 20, out var layered) || layered.Count != 3)
				throw new InvalidOperationException("Range search lost zero-cost layer transitions.");
			Console.WriteLine($"PASS: {checks} weighted range-search comparisons, 125% cost bound, budget fallback and zero-cost layer transitions.");
		}
	}
}
