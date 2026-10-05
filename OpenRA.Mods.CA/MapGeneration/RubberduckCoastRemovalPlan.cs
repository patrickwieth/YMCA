using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Recover a complete isolated collision component, never infer ownership from actor IDs.
	sealed class RubberduckCoastRemovalPlan
	{
		public readonly Dictionary<CPos, TerrainTile> Patch;
		public readonly List<MiniYamlNode> Actors;
		RubberduckCoastRemovalPlan(Dictionary<CPos, TerrainTile> patch, List<MiniYamlNode> actors)
		{ Patch = patch; Actors = actors; }

		public static RubberduckCoastRemovalPlan Create(Map map, EditorActorLayer layer, CPos clicked)
		{
			bool Blocked(CPos c) => map.Contains(c) && map.Tiles[c].Type >= 14010 && map.Tiles[c].Type <= 14012;
			if (!map.Rules.TerrainInfo.Id.StartsWith("RUBBERDUCK-", StringComparison.Ordinal) || !Blocked(clicked))
				throw new InvalidOperationException("Select a blocked rock-coast bank or water footprint.");
			var cells = new HashSet<CPos>(); var pending = new Stack<CPos>(); pending.Push(clicked);
			while (pending.Count != 0)
			{
				var c = pending.Pop();
				if (!Blocked(c) || !cells.Add(c)) continue;
				if (cells.Count > 4096) throw new InvalidOperationException("Connected coast is too large for isolated removal.");
				if (map.Height[c] != 0 || map.Ramp[c] != 0 || map.Tiles[c].Index != 0 || map.Resources[c].Type != 0)
					throw new InvalidOperationException("Modified coast terrain or resources are protected.");
				for (var x = -1; x <= 1; x++)
					for (var y = -1; y <= 1; y++) pending.Push(c + new CVec(x, y));
			}
			var patch = cells.ToDictionary(c => c, c => new TerrainTile((ushort)(map.Tiles[c].Type == 14010 ? 1050 : map.Tiles[c].Type == 14011 ? 1020 : 1000), 0));
			ushort Restored(CPos c) => patch.TryGetValue(c, out var t) ? t.Type : map.Tiles[c].Type;
			var banks = cells.Where(c => map.Tiles[c].Type != 14010).ToArray();
			var expected = new HashSet<CPos>(banks);
			foreach (var c in banks)
			{
				var water = PlateauTopology.Directions.Where(d => map.Contains(c + d) && Restored(c + d) == 1050).ToArray();
				if (water.Length == 0 || water.Length > 2 || (water.Length == 2 && water[0] + water[1] == CVec.Zero))
					throw new InvalidOperationException("Coast bank geometry has changed.");
				foreach (var d in water)
					for (var depth = 1; depth <= 2; depth++) expected.Add(c + d * depth);
				if (water.Length == 2)
					for (var x = 1; x <= 2; x++)
						for (var y = 1; y <= 2; y++) expected.Add(c + water[0] * x + water[1] * y);
			}
			var legacyFootprint = banks.Length != 0 && expected.SetEquals(cells);
			var modernFootprint = false;
			try { modernFootprint = cells.SetEquals(RubberduckRockCoastPlan.Footprint(map, banks, Array.Empty<CPos>(), Restored).Keys); }
			catch (InvalidOperationException) { /* An old saved coast may not have room for corrected end sockets. */ }
			if (!legacyFootprint && !modernFootprint)
				throw new InvalidOperationException("Incomplete or shared coast footprint; removal refused.");

			var saved = layer.Save().ToList(); var actors = new List<MiniYamlNode>();
			// Match complete actor graphs; legacy recovery does not silently upgrade artwork.
			string Signature(MiniYamlNode n) => new[] { new MiniYamlNode("Actor", n.Value) }.WriteToString();
			var bySignature = saved.GroupBy(Signature).ToDictionary(g => g.Key, g => g.ToArray());
			var wanted = RubberduckRockCoastRenderer.Actors(map, banks, "Recovered", Restored);
			if (!modernFootprint || wanted.Any(n => !bySignature.TryGetValue(Signature(n), out var m) || m.Length != 1))
			{
				if (!legacyFootprint) throw new InvalidOperationException("Missing, duplicated or customized coast artwork; removal refused.");
				wanted = RubberduckRockCoastRenderer.Actors(map, banks, "Recovered", Restored, true);
			}
			foreach (var n in wanted)
			{
				var signature = Signature(n);
				if (!bySignature.TryGetValue(signature, out var matches) || matches.Length != 1)
					throw new InvalidOperationException("Missing, duplicated or customized coast artwork; removal refused.");
				actors.Add(matches[0]); saved.Remove(matches[0]); bySignature.Remove(signature);
			}
			var clearance = cells.SelectMany(c => new[] { c }.Concat(PlateauTopology.Directions.Select(d => c + d))).ToHashSet();
			foreach (var n in saved)
			{
				var preview = layer[n.Key];
				if (preview.Footprint.Keys.Any(cells.Contains) || cells.Contains(preview.Location) ||
					(n.Value.Value.StartsWith("terrain.rubberduck.coast-", StringComparison.Ordinal) && clearance.Contains(preview.Location)))
					throw new InvalidOperationException("Another actor or neighboring coast occupies this component.");
			}
			return new RubberduckCoastRemovalPlan(patch, actors);
		}
	}
}
