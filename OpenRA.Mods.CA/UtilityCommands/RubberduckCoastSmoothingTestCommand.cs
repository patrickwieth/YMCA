using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckCoastSmoothingTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-coast-smoothing-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT", "Check coast corner reduction, area, protected cells, component identity, symmetry and determinism.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var results = new List<string>();
			void Require(bool ok, string message)
			{ if (!ok) throw new InvalidDataException(message); results.Add("PASS " + message); }
			MapPlan Fixture(bool integerCenter = false)
			{
				var p = new MapPlan(64, 64);
				for (var y = 0; y < 64; y++)
					for (var x = 0; x < 64; x++) p.SetTerrain(x, y,
						Math.Pow(x - (integerCenter ? 31 : 31.5), 2) + Math.Pow(y - (integerCenter ? 31 : 31.5), 2) < 324 ? PlannedTerrain.Land : PlannedTerrain.Water);
				p.AddFeature(14, 31, PlannedFeature.LaunchShore);
				p.AddFeature(49, 32, PlannedFeature.Resource);
				return p;
			}
			Dictionary<CPos, PlannedTerrain> Snapshot(MapPlan p) => Enumerable.Range(0, p.Width * p.Height)
				.ToDictionary(i => MountainPlateauPlanner.Cell(i % p.Width, i / p.Width), i => p.TerrainAt(i % p.Width, i / p.Width));
			Dictionary<CPos, int> Labels(Dictionary<CPos, PlannedTerrain> t)
			{
				var labels = new Dictionary<CPos, int>(); var serial = 0;
				foreach (var start in t.Keys)
				{
					if (labels.ContainsKey(start)) continue;
					var queue = new Queue<CPos>(); queue.Enqueue(start); var id = serial++;
					while (queue.Count != 0)
					{
						var c = queue.Dequeue(); if (labels.ContainsKey(c)) continue; labels.Add(c, id);
						foreach (var d in PlateauTopology.Directions)
							if (t.TryGetValue(c + d, out var value) && value == t[start] && !labels.ContainsKey(c + d)) queue.Enqueue(c + d);
					}
				}
				return labels;
			}
			var plan = Fixture(); var before = Snapshot(plan); var score = RubberduckCoastSmoother.CornerScore(plan);
			var swaps = RubberduckCoastSmoother.Apply(plan); var after = Snapshot(plan);
			Require(swaps > 0 && RubberduckCoastSmoother.CornerScore(plan) < score, "fewer corners on a rasterized symmetric coast");
			Require(before.Count(p => p.Value == PlannedTerrain.Water) == after.Count(p => p.Value == PlannedTerrain.Water), "exact water/land area preserved");
			var oldLabels = Labels(before); var newLabels = Labels(after);
			var retained = before.Keys.Where(c => before[c] == after[c]).ToArray();
			Require(oldLabels.Values.Distinct().Count() == newLabels.Values.Distinct().Count() &&
				retained.GroupBy(c => oldLabels[c]).All(g => g.Select(c => newLabels[c]).Distinct().Count() == 1) &&
				retained.GroupBy(c => newLabels[c]).All(g => g.Select(c => oldLabels[c]).Distinct().Count() == 1), "land and water component identities preserved");
			Require(before.Keys.All(c =>
			{
				var p = MountainPlateauPlanner.Point(c); var mirror = MountainPlateauPlanner.Cell(63 - p.U, 63 - p.V);
				return (before[c] != after[c]) == (before[mirror] != after[mirror]);
			}), "changes occur in mirrored pairs");
			Require(new[] { MountainPlateauPlanner.Cell(14, 31), MountainPlateauPlanner.Cell(49, 32) }.All(c =>
				Enumerable.Range(-2, 5).All(x => Enumerable.Range(-2, 5).All(y => before[c + new CVec(x, y)] == after[c + new CVec(x, y)]))), "landing/resource protection plus clearance retained");
			var repeat = Fixture(); RubberduckCoastSmoother.Apply(repeat);
			Require(Snapshot(repeat).All(p => after[p.Key] == p.Value), "deterministic result");
			var frozen = Fixture();
			for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++) frozen.AddFeature(x, y, PlannedFeature.BuildClearance);
			Require(RubberduckCoastSmoother.Apply(frozen) == 0 && Snapshot(frozen).All(p => before[p.Key] == p.Value), "fully protected geometry remains unchanged");
			var integerPlan = Fixture(true); var integerBefore = Snapshot(integerPlan);
			var integerSwaps = RubberduckCoastSmoother.Apply(integerPlan, true); var integerAfter = Snapshot(integerPlan);
			var integerOldLabels = Labels(integerBefore); var integerNewLabels = Labels(integerAfter);
			Require(integerSwaps > 0 && integerBefore.Count(p => p.Value == PlannedTerrain.Water) == integerAfter.Count(p => p.Value == PlannedTerrain.Water) &&
				integerBefore.Keys.Where(c => integerBefore[c] != integerAfter[c]).All(c =>
				{
					var p = MountainPlateauPlanner.Point(c); var mirror = MountainPlateauPlanner.Cell(62 - p.U, 62 - p.V);
					return integerBefore[mirror] != integerAfter[mirror];
				}) && integerOldLabels.Values.Distinct().Count() == integerNewLabels.Values.Distinct().Count() &&
				integerBefore.Keys.Where(c => integerBefore[c] == integerAfter[c]).GroupBy(c => integerOldLabels[c])
					.All(g => g.Select(c => integerNewLabels[c]).Distinct().Count() == 1), "integer-centered island symmetry and components retained");
			var four = Fixture(); var fourBefore = Snapshot(four);
			var fourSwaps = RubberduckCoastSmoother.Apply(four, false, 4); var fourAfter = Snapshot(four);
			Require(fourSwaps > 0 && fourSwaps % 4 == 0 && fourBefore.Keys.Where(c => fourBefore[c] != fourAfter[c]).All(c =>
			{
				var p = MountainPlateauPlanner.Point(c); var rotated = MountainPlateauPlanner.Cell(63 - p.V, p.U);
				return fourBefore[rotated] != fourAfter[rotated];
			}), "fourfold layouts keep complete rotation groups");
			var odd = Fixture();
			Require(RubberduckCoastSmoother.Apply(odd, false, 3) == 0 && Snapshot(odd).All(p => before[p.Key] == p.Value), "unsupported rotational layouts are untouched");
			Directory.CreateDirectory(args[1]); File.WriteAllLines(Path.Combine(args[1], "results.txt"), results);
			Console.WriteLine(string.Join(Environment.NewLine, results));
			Console.WriteLine($"{swaps} swaps; corner score {score} -> {RubberduckCoastSmoother.CornerScore(plan)}");
		}
	}
}
