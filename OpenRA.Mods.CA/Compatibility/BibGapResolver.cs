using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Compatibility
{
	// Presentation only. Generated cells are never fed back into the source set.
	public static class BibGapResolver
	{
		public readonly struct Cell
		{
			public readonly int X, Y;
			public readonly uint Actor;
			public readonly string Owner;
			public Cell(int x, int y, uint actor, string owner) { X = x; Y = y; Actor = actor; Owner = owner; }
		}

		public readonly struct Gap
		{
			public readonly int X, Y;
			public readonly uint First, Second;
			public Gap(int x, int y, uint first, uint second) { X = x; Y = y; First = first; Second = second; }
		}

		public static IEnumerable<Gap> Resolve(IEnumerable<Cell> sources, ISet<(int X, int Y)> blocked)
		{
			var cells = sources.GroupBy(c => (c.X, c.Y)).ToDictionary(g => g.Key, g => g.OrderBy(c => c.Actor).ToArray());
			var candidates = new Dictionary<(int X, int Y), (Cell First, Cell Second)>();
			var contested = new HashSet<(int, int)>();
			foreach (var entry in cells.OrderBy(e => e.Key.X).ThenBy(e => e.Key.Y))
			{
				// Overlapping enemy foundations must not become a bridge anchor.
				var first = entry.Value[0];
				if (entry.Value.Any(c => c.Owner != first.Owner))
					continue;

				foreach (var (dx, dy) in new[] { (1, 0), (0, 1), (1, 1), (1, -1) })
				{
					var gap = (first.X + dx, first.Y + dy);
					if (cells.ContainsKey(gap) || blocked.Contains(gap) ||
						!cells.TryGetValue((first.X + 2 * dx, first.Y + 2 * dy), out var other) ||
						other.Any(c => c.Owner != first.Owner))
						continue;

					var second = other.FirstOrDefault(c => c.Actor != first.Actor);
					if (second.Owner == null)
						continue;

					if (candidates.TryGetValue(gap, out var existing))
					{
						if (existing.First.Owner != first.Owner)
							contested.Add(gap);
					}
					else
						candidates.Add(gap, (first, second));
				}
			}

			foreach (var candidate in candidates)
				if (!contested.Contains(candidate.Key))
					yield return new Gap(candidate.Key.X, candidate.Key.Y, candidate.Value.First.Actor, candidate.Value.Second.Actor);
		}
	}
}
