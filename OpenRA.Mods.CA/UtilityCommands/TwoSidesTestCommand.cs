using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class TwoSidesTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--two-sides-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("", "Check native bank separation, preserved Strategic team order, unmodified coast/economy and deterministic objectives.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			foreach (var tileset in new[] { "RUBBERDUCK-TEMPERATE", "RUBBERDUCK-TEMPERATE-SHADED" })
			{
				var terrain = (DefaultTerrain)utility.ModData.DefaultTerrainInfo[tileset];
				foreach (var tile in Enumerable.Range(1120, 20).Concat(Enumerable.Range(1304, 4)))
					foreach (var variant in Enumerable.Range(0, terrain.Templates[(ushort)tile].TilesCount))
						if (terrain.GetTerrainIndex(new TerrainTile((ushort)tile, (byte)variant)) != terrain.GetTerrainIndex(new TerrainTile(1000, 0)))
							throw new InvalidDataException("Legacy grass/sand normalization would change terrain type.");
			}
			var count = 0;
			foreach (var players in Enumerable.Range(2, 11).Append(16))
				foreach (var seed in new[] { 42, 43, 1775378146 })
				{
					var options = DebugMapGeneratorCommand.ParseOptions(new[] { "test", ".", "mode=strategic", "preset=two-sides", "size=auto", $"players={players}", $"seed={seed}" });
					var plan = MapPlanGenerator.Generate(options);
					var before = new TwoSidesGenerator().Generate(options, plan.GenerationAttempt);
					var repeat = MapPlanGenerator.Generate(options);
					if (!before.Spawns.SequenceEqual(plan.Spawns) || !plan.Spawns.SequenceEqual(repeat.Spawns) ||
						!plan.StrategicCheckpoints.SequenceEqual(repeat.StrategicCheckpoints))
						throw new InvalidDataException("Two Sides bank order changed or objectives are nondeterministic.");
					var firstBank = DryBank(plan, plan.Spawns[0]);
					var split = (players + 1) / 2;
					var a = MountainPlateauPlanner.Cell(plan.Spawns[0].X, plan.Spawns[0].Y);
					var b = MountainPlateauPlanner.Cell(plan.Spawns[split].X, plan.Spawns[split].Y);
					var separation = b - a;
					if ((separation.X == 0) == (separation.Y == 0) ||
						plan.Spawns[0].X == plan.Spawns[split].X || plan.Spawns[0].Y == plan.Spawns[split].Y)
						throw new InvalidDataException("Default opponents are not separated along a native diagonal across the river.");
					for (var i = 0; i < players; i++)
						if (firstBank.Contains(plan.Spawns[i]) != (i < split))
							throw new InvalidDataException($"Two Sides team crosses native river bank: {players}/{seed}/{i}.");
					for (var y = 0; y < plan.Height; y++)
						for (var x = 0; x < plan.Width; x++)
							if (before.TerrainAt(x, y) != plan.TerrainAt(x, y) || before.FeaturesAt(x, y) != plan.FeaturesAt(x, y) ||
								plan.TerrainAt(x, y) != repeat.TerrainAt(x, y) || plan.FeaturesAt(x, y) != repeat.FeaturesAt(x, y))
								throw new InvalidDataException("Strategic objectives changed existing terrain or economy.");
					if (plan.StrategicCheckpoints.Count % 2 == 1 &&
						!plan.StrategicCheckpoints[plan.StrategicCheckpoints.Count / 2].Point.Equals(StrategicCheckpointPlanner.TwoSidesObjectiveCenter(plan)))
						throw new InvalidDataException("Central checkpoint is not centered on the sand causeway.");
					if (plan.StrategicCheckpoints.Count % 2 == 1)
					{
						var point = plan.StrategicCheckpoints[plan.StrategicCheckpoints.Count / 2].Point;
						var origin = MountainPlateauPlanner.Cell(point.X, point.Y);
						for (var y = 0; y < 3; y++)
							for (var x = 0; x < 2; x++)
							{
								var cell = MountainPlateauPlanner.Point(origin + new CVec(x, y));
								if (plan.TerrainAt(cell.U, cell.V) != PlannedTerrain.ShallowWater || !plan.HasFeature(cell.U, cell.V, PlannedFeature.Bottleneck))
									throw new InvalidDataException("Central objective footprint or approach leaves the sand crossing.");
							}
					}
					foreach (var cp in plan.StrategicCheckpoints)
						if (!StrategicCheckpointPlanner.FitsUnmodifiedCheckpointArea(plan, cp.Point))
							throw new InvalidDataException("Checkpoint does not fit its original native footprint and approach.");
					Console.WriteLine($"TWO SIDES PASS players={players} seed={seed} native banks, unchanged coast/economy, deterministic checkpoints.");
					count++;
				}
			foreach (var pair in new[] { (3, 46), (6, 44), (12, 40), (9, 42) })
				for (var variant = 0; variant < 4; variant++)
					if (!RubberduckShoreTopology.SourceFrames(pair.Item1, variant).SequenceEqual(new[] { pair.Item2 }))
						throw new InvalidDataException("Authored diagonal shore direction was lost.");
			for (var edges = 0; edges < 16; edges++)
				if (edges != 3 && edges != 6 && edges != 12 && edges != 9 &&
					RubberduckShoreTopology.SourceFrames(edges, 0).Any(f => f >= 40))
					throw new InvalidDataException("Half-cell wedges erase a narrow spit or isolated land cell.");
			Console.WriteLine($"TWO SIDES COMPLETE cases={count}; 16 diagonal direction/variant checks and narrow-land controls.");
		}

		static HashSet<PlanPoint> DryBank(MapPlan plan, PlanPoint start)
		{
			var visited = new HashSet<PlanPoint>();
			var pending = new Queue<PlanPoint>(); pending.Enqueue(start);
			while (pending.Count != 0)
			{
				var p = pending.Dequeue();
				if (!plan.Contains(p.X, p.Y) || plan.TerrainAt(p.X, p.Y) != PlannedTerrain.Land || !visited.Add(p)) continue;
				foreach (var d in PlateauTopology.Directions)
				{
					var n = MountainPlateauPlanner.Point(MountainPlateauPlanner.Cell(p.X, p.Y) + d);
					pending.Enqueue(new PlanPoint(n.U, n.V));
				}
			}
			return visited;
		}
	}
}
