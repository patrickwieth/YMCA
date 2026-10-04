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

		internal static IEnumerable<(CPos Cell, int Direction)> EndCaps(Map map, IEnumerable<CPos> cells, Func<CPos, ushort> terrainType)
		{
			var faces = cells.Distinct().SelectMany(c => Enumerable.Range(0, 4).Where(d => map.Contains(c + PlateauTopology.Directions[d]) &&
				terrainType(c + PlateauTopology.Directions[d]) == 1050).Select(d => new Face(c, d))).ToArray();
			return faces.SelectMany(f => new[] { (Vertex: f.Start, Face: f), (Vertex: f.End, Face: f) })
				.GroupBy(p => p.Vertex).Where(g => g.Count() == 1).Select(g =>
				{
					var f = g.First().Face;
					return (CapAnchor(f, g.Key, false), f.Direction);
				});
		}

		public static List<MiniYamlNode> Actors(Map map, IEnumerable<CPos> cells, string prefix, Func<CPos, ushort> terrainType = null, bool legacyCaps = false)
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
			var result = new List<MiniYamlNode>(); var replaced = new HashSet<Face>();
			var joints = new List<(CPos Cell, string Image)>();
			foreach (var pair in vertices)
			{
				var touching = pair.Value;
				if (touching.Count > 2) throw new InvalidOperationException("Rock coast branches at a shared vertex; leave a shore gap.");
				if (touching.Count == 1)
				{
					var f = touching[0]; joints.Add((CapAnchor(f, pair.Key, legacyCaps), "coast-abutment-" + f.Direction)); continue;
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
					replaced.Add(a); replaced.Add(b);
				}
				joints.Add((anchor, "coast-piece-" + slot));
			}
			void Add(CPos c, string image) => result.Add(new MiniYamlNode(prefix + result.Count,
				new ActorReference("terrain.rubberduck." + image) { new LocationInit(c), new OwnerInit("Neutral") }.Save()));
			foreach (var face in faces.Where(f => !replaced.Contains(f)))
			{
				var variant = (int)(unchecked((uint)(face.Cell.X * 73856093 ^ face.Cell.Y * 19349663)) % 3);
				Add(face.Cell, RubberduckRockCoastArt.Name(face.Direction, variant, 0));
			}
			foreach (var joint in joints.Distinct()) Add(joint.Cell, joint.Image);
			return result;
		}
	}
}
