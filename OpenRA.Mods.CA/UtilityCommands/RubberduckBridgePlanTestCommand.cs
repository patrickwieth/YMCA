using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckBridgePlanTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-bridge-plan-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("", "Validate deterministic whole-pair bridge planning, symmetry, dry land ends and protected economies.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var cases = 0;
			foreach (var mode in new[] { GeneratedMapMode.Tactical, GeneratedMapMode.Operational })
				foreach (var players in new[] { 2, 4, 8, 12, 16 })
					foreach (var seed in new[] { 42, 43, 101, 202 })
					{
						var size = 64 + players * 8;
						var options = new MapGenerationOptions { Preset = MapGenerationPreset.Continents, Players = players,
							Width = size, Height = size, Seed = seed, Mode = mode, Resources = ResourceFieldLayout.Patches, TechBuildings = TechBuildingDensity.Sparse };
						var a = MapPlanGenerator.Generate(options); var b = MapPlanGenerator.Generate(options);
						if (a.Bridges.Count != 2 || b.Bridges.Count != 2 || a.GenerationAttempt != b.GenerationAttempt)
							throw new InvalidDataException("Bridge pair retry failed.");
						var center = PlannedRubberduckBridge.Cell((size - 1) / 2, (size - 1) / 2);
						if (!a.Bridges[0].Cells().Select(c => new CPos(2 * center.X - c.X, 2 * center.Y - c.Y)).ToHashSet().SetEquals(a.Bridges[1].Cells()))
							throw new InvalidDataException("Bridge pair is not a complete half-turn orbit.");
						for (var i = 0; i < 2; i++)
						{
							if (!a.Bridges[i].Cells().SequenceEqual(b.Bridges[i].Cells())) throw new InvalidDataException("Bridge retry is nondeterministic.");
							if (a.Bridges[i].LandDepth != 2) throw new InvalidDataException("Generated bridge does not sit inland.");
							foreach (var approach in a.Bridges[i].Approaches())
							{
								var side = a.Bridges[i].AlongY ? new CVec(1, 0) : new CVec(0, 1);
								for (var lateral = -1; lateral <= 1; lateral++)
								{
									var p = PlannedRubberduckBridge.Point(approach + side * lateral);
									if (a.TerrainAt(p.U, p.V) != PlannedTerrain.Land || a.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.BuildClearance | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator))
										throw new InvalidDataException("Bridge approach has no dry protected landing pad.");
								}
							}
							var cells = a.Bridges[i].Cells().ToArray();
							for (var j = 0; j < cells.Length; j++)
							{
								var p = PlannedRubberduckBridge.Point(cells[j]); var depth = a.Bridges[i].LandDepth;
								var expected = j < depth || j >= cells.Length - depth ? PlannedTerrain.Land : PlannedTerrain.ShallowWater;
								if (a.TerrainAt(p.U, p.V) != expected || !a.HasFeature(p.U, p.V, PlannedFeature.DestructibleBridge) ||
									a.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.BuildClearance | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator))
									throw new InvalidDataException("Bridge footprint lost water/land topology or overlaps protected economy.");
							}
						}
						for (var y = 0; y < size; y++)
							for (var x = 0; x < size; x++)
								if (a.TerrainAt(x, y) != b.TerrainAt(x, y) || a.FeaturesAt(x, y) != b.FeaturesAt(x, y)) throw new InvalidDataException("Terrain/economy changed across identical retries.");
						var clone = a.Clone(); clone.Bridges.Clear(); if (a.Bridges.Count != 2) throw new InvalidDataException("Bridge plan clone aliases its mutable list.");
						Console.WriteLine($"BRIDGE PLAN PASS {mode} players={players} seed={seed} attempt={a.GenerationAttempt} length={a.Bridges[0].Length}"); cases++;
					}
			Console.WriteLine($"PASS: {cases} twice-generated symmetric bridge plans.");
			foreach (var teams in new[] { 3, 4 })
				foreach (var seed in new[] { 42, 202 })
				{
					var size = 64 + teams * 16;
					var options = new MapGenerationOptions { Preset = MapGenerationPreset.Continents, Players = teams * 2, Teams = teams,
						Width = size, Height = size, Seed = seed, Resources = ResourceFieldLayout.Patches, TechBuildings = TechBuildingDensity.Sparse };
					var a = MapPlanGenerator.Generate(options); var b = MapPlanGenerator.Generate(options);
					if (a.Bridges.Count != teams || b.Bridges.Count != teams || a.GenerationAttempt != b.GenerationAttempt)
						throw new InvalidDataException("Compact continent ring did not generate deterministically.");
					for (var i = 0; i < teams; i++)
					{
						var bridge = a.Bridges[i];
						if (bridge.LandDepth != 2 || bridge.ApproachLength != 1 || !bridge.Cells().SequenceEqual(b.Bridges[i].Cells()))
							throw new InvalidDataException("Compact landing lost its inland anchor or determinism.");
						foreach (var c in bridge.Approaches())
						{
							var p = PlannedRubberduckBridge.Point(c);
							if (a.TerrainAt(p.U, p.V) != PlannedTerrain.Land || a.HasFeature(p.U, p.V,
								PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.BuildClearance | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator))
								throw new InvalidDataException("Compact approach overlaps water or protected economy.");
						}
					}
					Console.WriteLine($"BRIDGE RING PASS teams={teams} seed={seed} attempt={a.GenerationAttempt}");
				}
		}
	}
}
