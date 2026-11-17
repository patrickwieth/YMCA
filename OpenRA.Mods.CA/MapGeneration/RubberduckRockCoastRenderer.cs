using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class RubberduckRockCoastRenderer
	{
		readonly struct Face
		{
			public readonly CPos Cell;
			public readonly int Direction;
			public Face(CPos cell, int direction) { Cell = cell; Direction = direction; }
			public CPos Start => Cell + (Direction == 0 ? new CVec(1, 0) : Direction == 1 ? new CVec(0, 1) : CVec.Zero);
			public CPos End => Cell + (Direction < 2 ? new CVec(1, 1) : Direction == 2 ? new CVec(0, 1) : new CVec(1, 0));
		}

		static CPos CapAnchor(Face face, CPos vertex, bool legacy)
		{
			if (legacy) return face.Cell;
			// Front pieces were calibrated at the start vertex; rear pieces at
			// the end vertex. The opposite end is a different geometric socket.
			if (face.Direction < 2) return vertex == face.End ? face.Cell + (face.End - face.Start) : face.Cell;
			return vertex == face.Start ? face.Cell - (face.End - face.Start) : face.Cell;
		}

		internal static bool AtMapEdge(Map map, CPos bank) => PlateauTopology.Directions.Any(d => !map.Contains(bank + d));

		internal static IEnumerable<(CPos Cell, int Direction)> EndCaps(Map map, IEnumerable<CPos> cells, Func<CPos, ushort> terrainType, bool clipMapEdge = false)
		{
			var faces = cells.Distinct().SelectMany(c => Enumerable.Range(0, 4).Where(d => map.Contains(c + PlateauTopology.Directions[d]) &&
				terrainType(c + PlateauTopology.Directions[d]) == 1050).Select(d => new Face(c, d))).ToArray();
			return faces.SelectMany(f => new[] { (Vertex: f.Start, Face: f), (Vertex: f.End, Face: f) })
				.GroupBy(p => p.Vertex).Where(g => g.Count() == 1 && (!clipMapEdge || !AtMapEdge(map, g.First().Face.Cell))).Select(g =>
				{
					var f = g.First().Face;
					return (CapAnchor(f, g.Key, false), f.Direction);
				});
		}

		public static List<MiniYamlNode> Actors(Map map, IEnumerable<CPos> cells, string prefix, Func<CPos, ushort> terrainType = null, bool legacyCaps = false, bool naturalJoints = false, bool clipMapEdge = false, bool lowRearEnds = false)
		{
			terrainType ??= c => map.Tiles[c].Type;
			var faces = cells.Distinct().SelectMany(c => Enumerable.Range(0, 4).Where(d => map.Contains(c + PlateauTopology.Directions[d]) &&
				terrainType(c + PlateauTopology.Directions[d]) == 1050).Select(d => new Face(c, d))).OrderBy(f => f.Cell.X).ThenBy(f => f.Cell.Y).ThenBy(f => f.Direction).ToArray();
			var vertices = new Dictionary<CPos, List<Face>>();
			foreach (var face in faces)
				foreach (var v in new[] { face.Start, face.End })
				{
					if (!vertices.TryGetValue(v, out var list)) vertices.Add(v, list = new List<Face>());
					list.Add(face);
				}
			// Continuous banks use bank-plane projection for the whole component,
			// including corner sockets. Older layouts retain their original family
			// except for the already supported straight rear-run aliases.
			var aligned = new HashSet<Face>();
			var visited = new HashSet<Face>();
			foreach (var root in faces)
			{
				if (!visited.Add(root)) continue;
				var component = new List<Face>();
				var pending = new Stack<Face>(); pending.Push(root);
				while (pending.Count != 0)
				{
					var face = pending.Pop(); component.Add(face);
					foreach (var vertex in new[] { face.Start, face.End })
						foreach (var neighbor in vertices[vertex])
							if (visited.Add(neighbor)) pending.Push(neighbor);
				}
				if (!legacyCaps && (naturalJoints || root.Direction >= 2 && component.All(f => f.Direction == root.Direction)))
					aligned.UnionWith(component.Where(f => naturalJoints || f.Direction >= 2));
			}
			var result = new List<MiniYamlNode>(); var replaced = new HashSet<Face>();
			var joints = new List<(CPos Cell, string Image)>();
			foreach (var pair in vertices)
			{
				var touching = pair.Value;
				if (touching.Count > 2) throw new InvalidOperationException("Rock coast branches at a shared vertex; leave a shore gap.");
				if (touching.Count == 1)
				{
					var f = touching[0];
					// The contour exits the playable map here: no artificial end tower
					// or reserved cap beyond the boundary is needed.
					if (!clipMapEdge || !AtMapEdge(map, f.Cell))
						joints.Add((CapAnchor(f, pair.Key, legacyCaps), "coast-abutment-" + f.Direction +
							(lowRearEnds && f.Direction >= 2 ? ".bank" : "")));
					continue;
				}
				var a = touching[0]; var b = touching[1];
				if (a.Direction == b.Direction) continue;
				if ((a.Direction + 2) % 4 == b.Direction) throw new InvalidOperationException("Rock coast pinches a diagonal passage.");
				var directions = (1 << a.Direction) | (1 << b.Direction);
				var slot = directions == 3 ? 24 : directions == 6 ? 30 : directions == 12 ? 28 : 26;
				var anchor = directions == 3 ? pair.Key - new CVec(1, 1) : directions == 6 ? pair.Key - new CVec(0, 1) : directions == 12 ? pair.Key : pair.Key - new CVec(1, 0);
				if (a.Cell != b.Cell)
				{
					slot++;
					if (!naturalJoints) { replaced.Add(a); replaced.Add(b); }
				}
				joints.Add((anchor, "coast-piece-" + slot + (naturalJoints && (slot != 28 || lowRearEnds) ? ".bank" : "")));
			}
			void Add(CPos c, string image) => result.Add(new MiniYamlNode(prefix + result.Count,
				new ActorReference("terrain.rubberduck." + image) { new LocationInit(c), new OwnerInit("Neutral") }.Save()));
			foreach (var face in faces.Where(f => !replaced.Contains(f)))
			{
				var variant = (int)(unchecked((uint)(face.Cell.X * 73856093 ^ face.Cell.Y * 19349663)) % 3);
				// Rear variants 1/2 are broad flat cap planes, not exposed rock lips.
				if (naturalJoints && face.Direction >= 2) variant = 0;
				Add(face.Cell, RubberduckRockCoastArt.Name(face.Direction, variant, 0) + (aligned.Contains(face) ? ".bank" : ""));
			}
			foreach (var joint in joints.Distinct()) Add(joint.Cell, joint.Image);
			return result;
		}
	}
}
