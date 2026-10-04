using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Directions follow PlateauTopology: southeast, southwest, northwest, northeast.
	// A diagonal only cuts a concave corner when both adjacent edges remain land.
	internal static class RubberduckShoreTopology
	{
		static readonly int[] Edges = { 20, 28, 24, 16 };
		static readonly int[] Concave = { 32, 34, 38, 36 };

		public static int Normalize(int edges, int diagonals)
		{
			if ((edges & ~15) != 0 || (diagonals & ~15) != 0)
				throw new ArgumentOutOfRangeException(nameof(edges));
			for (var d = 0; d < 4; d++)
				if ((edges & ((1 << d) | (1 << ((d + 1) % 4)))) != 0)
					diagonals &= ~(1 << d);
			return edges | diagonals << 4;
		}

		public static int[] SourceFrames(int topology, int variant)
		{
			if (topology != Normalize(topology & 15, topology >> 4) || variant < 0 || variant > 3)
				throw new ArgumentOutOfRangeException(nameof(topology));
			var frames = new List<int>();
			for (var d = 0; d < 4; d++)
			{
				if ((topology & (1 << d)) != 0) frames.Add(Edges[d] + variant);
				// Frames 40..47 are half-diamond land wedges, not full-cell corners.
				// Intersecting opposing wedges makes a walkable island almost invisible.
				// Full-cell convex corners therefore use the two authored edge cuts.
				if ((topology & (16 << d)) != 0) frames.Add(Concave[d] + variant % 2);
			}
			return frames.ToArray();
		}

		// Intersection retains an actual source RGBA sample at every output pixel.
		// It is derived composition, not an assertion that the artist supplied all 47 cases.
		public static byte[] Compose(byte[][] frames, int topology, int variant)
		{
			if (frames.Length != 48) throw new InvalidDataException("Expected 48 authored ground frames.");
			var donors = SourceFrames(topology, variant);
			var result = (byte[])frames[donors.Length == 0 ? variant : donors[0]].Clone();
			foreach (var donor in donors)
				for (var p = 0; p < result.Length; p += 4)
					if (frames[donor][p + 3] < result[p + 3]) Array.Copy(frames[donor], p, result, p, 4);
			return result;
		}
	}
}
