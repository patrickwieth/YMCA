using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Low expansion valleys keep their collision footprint. Use the same wall
	// composition as the plateaus instead of mixing two unrelated rock renderers.
	static class RubberduckMountainRenderer
	{
		public static void Apply(Map map, IEnumerable<CPos> mountainCells, List<MiniYamlNode> actors)
		{
			var surfaces = mountainCells.Distinct().ToDictionary(cell => cell, _ => new PlateauSurface(4, 0, true));
			var first = actors.Count;
			RubberduckPlateauRenderer.Apply(map, surfaces, actors);
			UseMountainFaces(actors, first);
		}

		// Preserve actor IDs, anchors, roof art and exact wall geometry. Only full
		// mountain faces use the projected authored material; ramps keep their vocabulary.
		internal static void UseMountainFaces(List<MiniYamlNode> actors, int first = 0)
		{
			const string prefix = "terrain.rubberduck.plateauwall";
			for (var i = first; i < actors.Count; i++)
			{
				var actor = actors[i];
				if (actor.Value.Value.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(actor.Value.Value.Substring(prefix.Length), out var index) &&
					index >= 0 && index < 100 && index % 25 == 24)
					actors[i] = new MiniYamlNode(actor.Key, new MiniYaml("terrain.rubberduck.mountainwall" + index / 25, actor.Value.Nodes));
			}
		}
	}
}
