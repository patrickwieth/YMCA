using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Native high-cliff source connections, not terrain height or collision declarations.
	// Coordinates refer to complete 128x256 source rectangles. Keeping both roof and
	// foot sockets is essential: matching only the roof leaves gaps at convex corners.
	static class RubberduckNativeCliffComposition
	{
		public static Dictionary<string, List<(int Slot, CVec Offset)>> CreateSamples(int length)
		{
			if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));
			var convex = new List<(int, CVec)>();
			var concave = new List<(int, CVec)>();
			var southwest = new List<(int, CVec)> { (30, new CVec(-1, 0)) };
			var southeast = new List<(int, CVec)> { (26, new CVec(0, -1)) };
			for (var i = 0; i < length; i++)
			{
				convex.Add((i % 3, new CVec(-i, 0)));
				convex.Add((5 + i % 3, new CVec(1, -1 - i)));
				concave.Add((i % 3, new CVec(i + 1, 0)));
				concave.Add((5 + i % 3, new CVec(0, i + 1)));
				southwest.Add((i % 3, new CVec(i, 0)));
				southeast.Add((5 + i % 3, new CVec(0, i)));
			}
			convex.Add((24, new CVec(1, 0)));
			// Replace the two overlapping first faces; do not layer a wedge over them.
			concave.Add((16, CVec.Zero));
			southwest.Add((24, new CVec(length, 0)));
			southeast.Add((24, new CVec(0, length)));
			var result = new Dictionary<string, List<(int, CVec)>>
			{
				{ "convex-south", convex }, { "concave-north", concave },
				{ "open-southwest", southwest }, { "open-southeast", southeast },
			};
			foreach (var assembly in result.Values) ValidateChain(assembly);
			return result;
		}

		static (int RoofX, int RoofY, int FootX, int FootY)[] Sockets(int slot)
		{
			if (slot >= 0 && slot <= 2) return new[] { (64, 32, 0, 224), (128, 64, 64, 256) };
			if (slot >= 5 && slot <= 7) return new[] { (64, 32, 128, 224), (0, 64, 64, 256) };
			switch (slot)
			{
				case 16: return new[] { (0, 64, 64, 256), (128, 64, 64, 256) };
				case 24: return new[] { (64, 32, 0, 224), (64, 32, 128, 224) };
				case 26: return new[] { (0, 64, 64, 256) };
				case 30: return new[] { (128, 64, 64, 256) };
				default: throw new ArgumentOutOfRangeException(nameof(slot), "Uncalibrated source piece.");
			}
		}

		public static void ValidateChain(IReadOnlyList<(int Slot, CVec Offset)> assembly)
		{
			if (assembly.Count < 2) throw new ArgumentException("Expected a connected wall assembly.", nameof(assembly));
			var sockets = assembly.Select(piece => Sockets(piece.Slot).Select(socket =>
			{
				var x = 64 * (piece.Offset.X - piece.Offset.Y);
				var y = 32 * (piece.Offset.X + piece.Offset.Y);
				return (socket.RoofX + x, socket.RoofY + y, socket.FootX + x, socket.FootY + y);
			}).ToArray()).ToArray();
			var adjacency = Enumerable.Range(0, assembly.Count).Select(_ => new List<int>()).ToArray();
			for (var i = 0; i < assembly.Count; i++)
				for (var j = i + 1; j < assembly.Count; j++)
					if (sockets[i].Intersect(sockets[j]).Any())
					{
						adjacency[i].Add(j);
						adjacency[j].Add(i);
					}
			var visited = new HashSet<int>();
			var pending = new Stack<int>();
			pending.Push(0);
			while (pending.Count > 0)
			{
				var i = pending.Pop();
				if (!visited.Add(i)) continue;
				foreach (var j in adjacency[i]) pending.Push(j);
			}
			if (visited.Count != assembly.Count || adjacency.Count(a => a.Count == 1) != 2 || adjacency.Any(a => a.Count < 1 || a.Count > 2))
				throw new ArgumentException("Native cliff chain has an unmatched roof/foot socket or a branch.", nameof(assembly));
		}
	}
}
