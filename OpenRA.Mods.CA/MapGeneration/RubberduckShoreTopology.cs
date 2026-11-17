using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Directions follow PlateauTopology: southeast, southwest, northwest, northeast.
	// A diagonal only cuts a concave corner when both adjacent edges remain land.
	internal static class RubberduckShoreTopology
	{
		// Old square-neighborhood autotiling baked sand into otherwise Clear grass
		// cells. Resolve their material identity without changing collision or IDs.
		internal static bool IsLegacyGrassSand(ushort tile) => tile >= 1120 && tile <= 1139 || tile >= 1304 && tile <= 1307;

		internal static bool IsLegacyGrassDirt(ushort tile) => tile >= 1100 && tile <= 1119 || tile >= 1200 && tile <= 1219 ||
			tile >= 1300 && tile <= 1303 || tile >= 1320 && tile <= 1323;

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
			// Exactly two adjacent sea edges can use one authored diagonal shoreline.
			// Never intersect opposing half-cell wedges on spits or tiny islands.
			// Even frames retain a majority-opaque center in all ten material blocks;
			// several odd sand variants erase the physical cell center.
			var diagonal = (topology & 15) switch { 3 => 46, 6 => 44, 12 => 40, 9 => 42, _ => -1 };
			if (diagonal >= 0) frames.Add(diagonal);
			for (var d = 0; d < 4; d++)
			{
				if (diagonal < 0 && (topology & (1 << d)) != 0) frames.Add(Edges[d] + variant);
				if ((topology & (16 << d)) != 0) frames.Add(Concave[d] + variant % 2);
			}
			return frames.ToArray();
		}

		internal static bool[] GroundBlendExclusion(byte[] shore, byte[] fullDiamond)
		{
			if (shore.Length != 128 * 64 * 4 || fullDiamond.Length != shore.Length)
				throw new ArgumentException("Expected matching 128x64 RGBA shore and ground frames.");
			var exclusion = new bool[128 * 64];
			for (var p = 0; p < exclusion.Length; p++)
			{
				var alpha = shore[p * 4 + 3];
				// Suppress only the almost transparent tail that a ground overlay
				// would turn into an opaque fringe. Keep the broad authored blend
				// and uncut land/land diamond antialiasing intact.
				exclusion[p] = alpha == 0 || alpha <= 16 && alpha < fullDiamond[p * 4 + 3];
			}
			return exclusion;
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
