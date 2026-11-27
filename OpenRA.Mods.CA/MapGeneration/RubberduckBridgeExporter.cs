using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class RubberduckBridgeExporter
	{
		internal static bool OnlyBridgeRules(MiniYaml rules) => rules == null ||
			string.IsNullOrEmpty(rules.Value) && rules.Nodes.All(n => n.Key.StartsWith("terrain.rubberduck.bridge.", StringComparison.Ordinal) &&
				n.Value.Nodes.Any(t => t.Key == "Inherits" && new[] { "^RubberduckBridge", "^RubberduckBridgeRemains", "^RubberduckBridgeRepairHut" }.Contains(t.Value.Value)) &&
				n.Value.Nodes.All(t => new[] { "Inherits", "Building", "RubberduckBridgeBody", "SpawnActorOnDeath@REPAIR", "BridgePlaceholder", "BridgeHut" }.Contains(t.Key)));

		public static void Apply(Map map, MapPlan plan, List<MiniYamlNode> actors, int maximumHeight)
		{
			if (plan.Bridges.Count == 0)
			{
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (plan.HasFeature(x, y, PlannedFeature.DestructibleBridge))
							throw new InvalidOperationException("Bridge flags lost their realizable span metadata.");
				return;
			}
			if ((maximumHeight & 1) != 0) throw new InvalidOperationException("Bridge projection requires even height-border parity.");
			var rules = map.RuleDefinitions?.Nodes.ToList() ?? new List<MiniYamlNode>();
			var types = new HashSet<string>();
			var occupied = new HashSet<CPos>(plan.Bridges.SelectMany(b => b.Cells().Concat(b.Approaches())).Select(c => c + new CVec(maximumHeight / 2, maximumHeight / 2)));
			// Tech flags mark the anchor, not the complete native 2x2 derrick footprint.
			for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
				if (plan.HasFeature(x, y, PlannedFeature.TechBuilding))
					for (var dy = 0; dy < 2; dy++) for (var dx = 0; dx < 2; dx++)
						occupied.Add(PlannedRubberduckBridge.Cell(x, y) + new CVec(maximumHeight / 2 + dx, maximumHeight / 2 + dy));
			foreach (var bridge in plan.Bridges)
			{
				var type = $"terrain.rubberduck.bridge.{(bridge.AlongY ? "y" : "x")}.{bridge.Length}";
				if (types.Add(type))
				{
					var footprint = bridge.AlongY ? string.Join(" ", Enumerable.Repeat("=", bridge.Length)) : new string('=', bridge.Length);
					rules.AddRange(MiniYaml.FromString($"{type}:\n\tInherits: ^RubberduckBridge\n\tBuilding:\n\t\tDimensions: {(bridge.AlongY ? "1," + bridge.Length : bridge.Length + ",1")}\n\t\tFootprint: {footprint}\n\tRubberduckBridgeBody:\n\t\tLength: {bridge.Length}\n\t\tAlongY: {bridge.AlongY}\n", "rubberduck-bridge-export"));
				}
				if (!rules.Any(n => n.Key == type + ".destroyed"))
				{
					var dimensions = bridge.AlongY ? "1," + bridge.Length : bridge.Length + ",1";
					var footprint = bridge.AlongY ? string.Join(" ", Enumerable.Repeat("=", bridge.Length)) : new string('=', bridge.Length);
					var ruleIndex = rules.FindIndex(n => n.Key == type);
					var spawn = MiniYaml.FromString($"SpawnActorOnDeath@REPAIR:\n\tActor: {type}.destroyed\n\tSpawnAfterDefeat: true\n", "rubberduck-bridge-repair");
					rules[ruleIndex] = new MiniYamlNode(type, new MiniYaml(rules[ruleIndex].Value.Value, rules[ruleIndex].Value.Nodes.Concat(spawn)));
					rules.AddRange(MiniYaml.FromString($"{type}.destroyed:\n\tInherits: ^RubberduckBridgeRemains\n\tBuilding:\n\t\tDimensions: {dimensions}\n\t\tFootprint: {footprint}\n\tBridgePlaceholder:\n\t\tReplaceWithActor: {type}\n", "rubberduck-bridge-repair"));
				}
				var location = bridge.Start + new CVec(maximumHeight / 2, maximumHeight / 2);
				var index = 0;
				foreach (var c in bridge.Cells())
				{
					var cell = c + new CVec(maximumHeight / 2, maximumHeight / 2);
					if (!map.Contains(cell) || map.Height[cell] != 0 || map.Ramp[cell] != 0 || map.Resources[cell].Type != 0)
						throw new InvalidOperationException("Bridge export conflicts with protected terrain/economy.");
					var expected = index < bridge.LandDepth || index >= bridge.Length - bridge.LandDepth ? 1000 : 1050;
					if (map.Tiles[cell].Type != expected)
						throw new InvalidOperationException("Bridge lost its dry ends or underlying water before export.");
					if (bridge.LandDepth > 1 && expected == 1000) map.Tiles[cell] = new TerrainTile(1030, 0);
					index++;
				}
				foreach (var approach in bridge.Approaches())
				{
					var cell = approach + new CVec(maximumHeight / 2, maximumHeight / 2);
					var p = PlannedRubberduckBridge.Point(approach);
					if (!map.Contains(cell) || (map.Tiles[cell].Type != 1000 && map.Tiles[cell].Type != 1030) || map.Height[cell] != 0 || map.Resources[cell].Type != 0 ||
						plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.BuildClearance | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator))
						throw new InvalidOperationException("Bridge approach no longer has unoccupied dry ground.");
					// Existing dirt material, with its ordinary movement cost and blends.
					// Only the reserved land cells change; water and blocked terrain do not.
					map.Tiles[cell] = new TerrainTile(1030, 0);
				}
				var actor = new ActorReference(type) { new LocationInit(location), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("RubberduckBridge" + actors.Count, actor.Save()));
				for (var end = 0; end < 2; end++)
				{
					var endpoint = location + bridge.Step * (end == 0 ? 0 : bridge.Length - 1);
					CPos? hut = null;
					for (var radius = 1; radius <= 6 && hut == null; radius++)
					for (var dy = -radius; dy <= radius && hut == null; dy++)
					for (var dx = -radius; dx <= radius && hut == null; dx++)
					{
						if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
						var ox = end == 0 ? dx : -dx; var oy = end == 0 ? dy : -dy;
						var along = bridge.AlongY ? oy : ox; var side = bridge.AlongY ? ox : oy;
						if (side == 0 || (end == 0 ? along > 0 : along < 0)) continue;
						var candidate = endpoint + new CVec(ox, oy);
						var p = PlannedRubberduckBridge.Point(candidate - new CVec(maximumHeight / 2, maximumHeight / 2));
						if (!map.Contains(candidate) || occupied.Contains(candidate) || !plan.Contains(p.U, p.V) ||
							map.Height[candidate] != 0 || map.Resources[candidate].Type != 0 ||
							(map.Tiles[candidate].Type != 1000 && map.Tiles[candidate].Type != 1030) ||
							plan.HasFeature(p.U, p.V, PlannedFeature.BuildClearance | PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator)) continue;
						hut = candidate;
					}
					if (hut == null) throw new InvalidOperationException("No unobstructed bank position for bridge repair hut.");
					occupied.Add(hut.Value);
					var offset = endpoint - hut.Value;
					var hutType = $"terrain.rubberduck.bridge.hut.{offset.X}.{offset.Y}";
					if (types.Add(hutType)) rules.AddRange(MiniYaml.FromString($"{hutType}:\n\tInherits: ^RubberduckBridgeRepairHut\n\tBridgeHut:\n\t\tNeighbourOffsets: {offset.X},{offset.Y}\n", "rubberduck-bridge-hut"));
					var hutActor = new ActorReference(hutType) { new LocationInit(hut.Value), new OwnerInit("Neutral") };
					actors.Add(new MiniYamlNode("RubberduckRepairHut" + actors.Count, hutActor.Save()));
				}
			}
			map.RuleDefinitions = new MiniYaml(map.RuleDefinitions?.Value, rules);
		}
	}
}
