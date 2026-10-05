using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Shared source-grid topology, independent of the five calibration fixtures.
	// Never reserves terrain or silently substitutes unsupported corner geometry.
	static class RubberduckClosedCliffContour
	{
		static readonly CVec[] Directions = PlateauTopology.Directions;
		static readonly int[] OuterSlots = { 24, 30, 28, 26 };
		static readonly int[] StraightSlots = { 5, 0, 12, 8 };

		public static List<(int Slot, CVec Offset)> Create(IEnumerable<CVec> roofCells)
		{
			var roof = roofCells.ToHashSet();
			if (roof.Count == 0) throw new InvalidDataException("A closed cliff contour needs a roof.");
			var reached = new HashSet<CVec>(); var pending = new Stack<CVec>(); pending.Push(roof.First());
			while (pending.Count != 0)
			{
				var c = pending.Pop(); if (!reached.Add(c)) continue;
				foreach (var d in Directions) if (roof.Contains(c + d) && !reached.Contains(c + d)) pending.Push(c + d);
			}
			if (reached.Count != roof.Count) throw new InvalidDataException("Closed contours cannot combine disconnected roofs.");

			var result = new List<(int Slot, CVec Offset)>();
			var faces = new List<(int Direction, CVec Offset, int Variant)>();
			foreach (var cell in roof.OrderBy(c => c.X).ThenBy(c => c.Y))
				for (var d = 0; d < 4; d++)
					if (!roof.Contains(cell + Directions[d]))
					{
						// Use long arithmetic so translations cannot overflow the variant hash.
						faces.Add((d, cell + Directions[d], (int)(Math.Abs((long)cell.X + cell.Y) % 3)));
						if (!roof.Contains(cell + Directions[(d + 1) % 4]))
							result.Add((OuterSlots[d], cell + Directions[d] + Directions[(d + 1) % 4]));
					}
			foreach (var group in faces.GroupBy(f => f.Offset))
			{
				var list = group.ToArray();
				if (list.Length == 1) result.Add((StraightSlots[list[0].Direction] + list[0].Variant, group.Key));
				else if (list.Length == 2)
				{
					var mask = (1 << list[0].Direction) | (1 << list[1].Direction);
					var slot = mask == 3 ? 16 : mask == 9 ? 18 : mask == 12 ? 20 : mask == 6 ? 22 : -1;
					if (slot < 0) throw new InvalidDataException("Opposing cliff faces cannot share one foot cell.");
					result.Add((slot, group.Key));
				}
				else throw new InvalidDataException("Branched cliff contour.");
			}
			Validate(result, roof);
			return result;
		}

		static (long, long, long, long) Edge((long X, long Y) a, (long X, long Y) b) =>
			a.X < b.X || a.X == b.X && a.Y < b.Y ? (a.X, a.Y, b.X, b.Y) : (b.X, b.Y, a.X, a.Y);

		internal static void Validate(IReadOnlyList<(int Slot, CVec Offset)> walls, IEnumerable<CVec> roofCells)
		{
			var roof = roofCells.ToHashSet();
			var expected = new HashSet<(long, long, long, long)>();
			var expectedCorners = new Dictionary<CVec, (int Slot, (long, long) Vertex)>();
			var feet = new HashSet<CVec>();
			foreach (var cell in roof)
			{
				for (var dx = -1; dx <= 1; dx++)
					for (var dy = -1; dy <= 1; dy++)
						if (!roof.Contains(cell + new CVec(dx, dy))) feet.Add(cell + new CVec(dx, dy));
				var x = 64L + 64L * (cell.X - (long)cell.Y); var y = 64L + 32L * (cell.X + (long)cell.Y);
				var corners = new[] { (x + 64, y), (x, y + 32), (x - 64, y), (x, y - 32) };
				for (var d = 0; d < 4; d++)
					if (!roof.Contains(cell + Directions[d]))
					{
						expected.Add(Edge(corners[d], corners[(d + 1) % 4]));
						if (!roof.Contains(cell + Directions[(d + 1) % 4]) &&
							!expectedCorners.TryAdd(cell + Directions[d] + Directions[(d + 1) % 4], (OuterSlots[d], corners[(d + 1) % 4])))
							throw new InvalidDataException("Pinched cliff corners share a foot cell.");
					}
			}
			var actual = new HashSet<(long, long, long, long)>();
			var cornersFound = new Dictionary<CVec, (int Slot, (long, long) Vertex)>();
			var anchors = new HashSet<CVec>();
			foreach (var piece in walls)
			{
				if (!feet.Contains(piece.Offset) || !anchors.Add(piece.Offset)) throw new InvalidDataException("Cliff anchor is not a unique exterior foot cell.");
				var x = 64L * (piece.Offset.X - (long)piece.Offset.Y); var y = 32L * (piece.Offset.X + (long)piece.Offset.Y);
				void Add((long X, long Y) a, (long X, long Y) b)
				{
					if (!actual.Add(Edge((a.X + x, a.Y + y), (b.X + x, b.Y + y)))) throw new InvalidDataException("Duplicate cliff closure edge.");
				}
				if (piece.Slot >= 0 && piece.Slot <= 2) Add((64, 32), (128, 64));
				else if (piece.Slot >= 5 && piece.Slot <= 7) Add((0, 64), (64, 32));
				else if (piece.Slot >= 8 && piece.Slot <= 10) Add((0, 64), (64, 96));
				else if (piece.Slot >= 12 && piece.Slot <= 14) Add((64, 96), (128, 64));
				else if (piece.Slot == 16) { Add((0, 64), (64, 32)); Add((64, 32), (128, 64)); }
				else if (piece.Slot == 18) { Add((64, 32), (0, 64)); Add((0, 64), (64, 96)); }
				else if (piece.Slot == 20) { Add((0, 64), (64, 96)); Add((64, 96), (128, 64)); }
				else if (piece.Slot == 22) { Add((64, 32), (128, 64)); Add((128, 64), (64, 96)); }
				else
				{
					var vertex = piece.Slot == 24 ? (x + 64, y + 32) : piece.Slot == 26 ? (x, y + 64) :
						piece.Slot == 28 ? (x + 64, y + 96) : piece.Slot == 30 ? (x + 128, y + 64) :
						throw new InvalidDataException("Unknown cliff closure slot.");
					cornersFound.Add(piece.Offset, (piece.Slot, vertex));
				}
			}
			if (expected.Count == 0 || !expected.SetEquals(actual)) throw new InvalidDataException("Cliff contour misses roof boundary edges.");
			if (cornersFound.Count != expectedCorners.Count || expectedCorners.Any(p => !cornersFound.TryGetValue(p.Key, out var v) || v != p.Value))
				throw new InvalidDataException("Cliff corner misses its roof vertex.");
			if (!feet.SetEquals(anchors)) throw new InvalidDataException("Cliff contour does not account for every reserved foot cell.");
		}
	}
}
