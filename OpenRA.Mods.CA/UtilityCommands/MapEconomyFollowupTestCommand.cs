using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class MapEconomyFollowupTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--test-map-economy-followup";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT.json", "Check additional Plains/Two Sides fields, paths and separation across player counts.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rows = new List<object>();
			foreach (var preset in new[] { "plains", "two-sides" })
			foreach (var players in new[] { 2, 4, 8, 12, 16 })
			foreach (var seed in new[] { 1, 42, 43 })
			{
				var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "mode=tactical", $"preset={preset}", $"players={players}", $"seed={seed}", "size=auto" });
				var plan = MapPlanGenerator.Generate(options);
				var resources = new HashSet<CPos>();
				for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
					if (plan.HasFeature(x, y, PlannedFeature.Resource)) resources.Add(MountainPlateauPlanner.Cell(x, y));
				var count = resources.Count; var components = new List<int>();
				while (resources.Count > 0)
				{
					var pending = new Stack<CPos>(); pending.Push(resources.First()); var size = 0;
					while (pending.Count > 0)
					{
						var c = pending.Pop(); if (!resources.Remove(c)) continue; size++;
						for (var dy = -1; dy <= 1; dy++) for (var dx = -1; dx <= 1; dx++)
							if ((dx != 0 || dy != 0) && resources.Contains(c + new CVec(dx, dy))) pending.Push(c + new CVec(dx, dy));
					}
					components.Add(size);
				}
				if (preset == "plains")
				{
					foreach (var start in plan.Spawns)
						if (plan.StartingFields.Count(f => f.Start.Equals(start) && !f.Contested) != 3)
							throw new InvalidDataException("Plains must have one start and two owned expansion fields per player.");
					if (plan.StartingFields.Count(f => f.Contested) != 2 * Math.Max(2, players / 2))
						throw new InvalidDataException("Missing contested field access guards.");
					BalancedStartingEconomy.ValidateFields(plan);
				}
				if (preset == "two-sides" && components.Any(size => size != 135))
					throw new InvalidDataException("Every Two Sides expansion must match the full starting patch budget.");
				var expected = players * 3 + (preset == "plains" ? Math.Max(2, players / 2) : 0);
				if (components.Count != expected) throw new InvalidDataException($"Merged or missing fields: {preset}/{players}/{seed}: {components.Count}, expected {expected}; sizes={string.Join(",", components.OrderBy(x => x))}");
				var split = (players + 1) / 2;
				var distance = StartBalanceMetrics.Distances(plan, plan.Spawns[0]); var target = plan.Spawns[split];
				var a = MountainPlateauPlanner.Cell(plan.Spawns[0].X, plan.Spawns[0].Y); var b = MountainPlateauPlanner.Cell(target.X, target.Y);
				var air = Math.Sqrt((b - a).LengthSquared); var steps = distance[target.X + target.Y * plan.Width];
				if (preset == "two-sides" && (air < 50 || steps <= air * 1.2)) throw new InvalidDataException("Two Sides lost separation or its river detour.");
				rows.Add(new { Preset = preset, Players = players, Seed = seed, plan.Width, plan.Height, Fields = components.Count, Sizes = components.OrderBy(x => x).ToArray(), ResourceCells = count, OpponentAirCells = air, CardinalGroundSteps = steps });
				File.WriteAllText(args[1], JsonConvert.SerializeObject(rows, Formatting.Indented));
				Console.WriteLine($"PASS {preset} {players}P seed={seed}: {components.Count} fields, {count} cells, air={air:F1}, path={steps}");
			}
		}
	}
}
