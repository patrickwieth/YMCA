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
				n.Value.Nodes.Any(t => t.Key == "Inherits" && t.Value.Value == "^RubberduckBridge") &&
				n.Value.Nodes.All(t => t.Key == "Inherits" || t.Key == "Building" || t.Key == "RubberduckBridgeBody"));

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
			foreach (var bridge in plan.Bridges)
			{
				var type = $"terrain.rubberduck.bridge.{(bridge.AlongY ? "y" : "x")}.{bridge.Length}";
				if (types.Add(type))
				{
					var footprint = bridge.AlongY ? string.Join(" ", Enumerable.Repeat("=", bridge.Length)) : new string('=', bridge.Length);
					rules.AddRange(MiniYaml.FromString($"{type}:\n\tInherits: ^RubberduckBridge\n\tBuilding:\n\t\tDimensions: {(bridge.AlongY ? "1," + bridge.Length : bridge.Length + ",1")}\n\t\tFootprint: {footprint}\n\tRubberduckBridgeBody:\n\t\tLength: {bridge.Length}\n\t\tAlongY: {bridge.AlongY}\n", "rubberduck-bridge-export"));
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
					if (!map.Contains(cell) || map.Tiles[cell].Type != 1000 || map.Height[cell] != 0 || map.Resources[cell].Type != 0 ||
						plan.HasFeature(p.U, p.V, PlannedFeature.Resource | PlannedFeature.RichResource | PlannedFeature.BuildClearance | PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator))
						throw new InvalidOperationException("Bridge approach no longer has unoccupied dry ground.");
					// Existing dirt material, with its ordinary movement cost and blends.
					// Only the reserved land cells change; water and blocked terrain do not.
					map.Tiles[cell] = new TerrainTile(1030, 0);
				}
				var actor = new ActorReference(type) { new LocationInit(location), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("RubberduckBridge" + actors.Count, actor.Save()));
			}
			map.RuleDefinitions = new MiniYaml(map.RuleDefinitions?.Value, rules);
		}
	}
}
