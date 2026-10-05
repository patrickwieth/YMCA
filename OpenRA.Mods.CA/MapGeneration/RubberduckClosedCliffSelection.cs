using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// All-or-nothing component selection. Planning and rendering use the same sockets.
	sealed class RubberduckClosedCliffSelection
	{
		public readonly HashSet<CPos> Cells = new();
		public readonly List<(CPos Cell, int Slot)> Pieces = new();

		public RubberduckClosedCliffSelection(Map map, IReadOnlyDictionary<CPos, PlateauSurface> surfaces)
		{
			var remaining = surfaces.Where(p => Elevated(p.Value)).Select(p => p.Key).ToHashSet();
			while (remaining.Count != 0)
			{
				var component = new HashSet<CPos>(); var pending = new Stack<CPos>(); pending.Push(remaining.First());
				while (pending.Count != 0)
				{
					var c = pending.Pop(); if (!remaining.Remove(c)) continue;
					component.Add(c);
					foreach (var d in PlateauTopology.Directions) if (remaining.Contains(c + d)) pending.Push(c + d);
				}
				if (!TryPieces(surfaces, component, FlatLow, out var pieces) ||
					pieces.Any(p => !map.Rules.Actors.ContainsKey("terrain.rubberduck.closedcliff" + p.Slot))) continue;
				Cells.UnionWith(component); Pieces.AddRange(pieces);
			}

			bool FlatLow(CPos cell, bool blocked)
			{
				if (!map.Contains(cell) || map.Resources[cell].Type != 0) return false;
				if (surfaces.TryGetValue(cell, out var value)) return value.Height == 0 && value.Ramp == 0 && value.Blocked == blocked;
				return map.Height[cell] == 0 && map.Ramp[cell] == 0 && (blocked ? map.Tiles[cell].Type == 3992 : map.Tiles[cell].Type == 1000);
			}
		}

		public static bool Elevated(PlateauSurface surface) => surface.Height == 4 && surface.Ramp == 0 || surface.Ramp != 0;

		public static bool TryPieces(IReadOnlyDictionary<CPos, PlateauSurface> surfaces, HashSet<CPos> component,
			Func<CPos, bool, bool> flatLow, out List<(CPos Cell, int Slot)> pieces, Action<string> rejected = null)
		{
			pieces = new List<(CPos Cell, int Slot)>();
			bool Reject(string reason) { rejected?.Invoke(reason); return false; }
			if (component.Count == 0) return false;
			var origin = component.OrderBy(c => c.X).ThenBy(c => c.Y).First();
			List<(int Slot, CVec Offset)> contour;
			try { contour = RubberduckClosedCliffContour.Create(component.Select(c => c - origin)); }
			catch (InvalidDataException e) { return Reject(e.Message); }
			foreach (var piece in contour)
			{
				var anchor = origin + piece.Offset;
				var slot = piece.Slot;
				var direction = slot <= 2 ? 1 : slot >= 5 && slot <= 7 ? 0 : slot >= 8 && slot <= 10 ? 3 : slot >= 12 && slot <= 14 ? 2 : -1;
				if (direction >= 0)
				{
					var owner = anchor - PlateauTopology.Directions[direction];
					var surface = surfaces[owner]; var edge = PlateauTopology.Edge(direction);
					if (surface.Ramp != 0)
					{
						// Only the flat zero-height downhill edge can be an open mouth.
						if (surface.CornerHeight(edge.A) != 0 || surface.CornerHeight(edge.B) != 0 || !flatLow(anchor, false)) return Reject($"Unsupported ramp edge at {anchor}.");
						continue;
					}
					// Keep variants stable when a component's local origin moves.
					slot = (direction == 0 ? 5 : direction == 1 ? 0 : direction == 2 ? 12 : 8) + (int)(Math.Abs((long)owner.X + owner.Y) % 3);
					if (direction < 2)
					{
						var side = owner + PlateauTopology.Directions[1 - direction];
						if (surfaces.TryGetValue(side, out var ramp) && ramp.Ramp != 0 && ramp.Height == 0)
							slot = direction == 0 ? 105 : 100;
					}
				}
				else
				{
					// Validate the cells owning the corner's boundary, not every nearby
					// roof cell. A diagonal interior ramp does not own these wall sockets.
					var mask = slot == 16 ? 3 : slot == 18 ? 9 : slot == 20 ? 12 : slot == 22 ? 6 : 0;
					bool FlatRoof(CPos c) => surfaces.TryGetValue(c, out var s) && s.Height == 4 && s.Ramp == 0;
					if (mask != 0)
					{
						var inward = anchor;
						for (var d = 0; d < 4; d++)
							if ((mask & (1 << d)) != 0)
							{
								if (!FlatRoof(anchor - PlateauTopology.Directions[d])) return Reject($"Mixed corner at {anchor}.");
								inward -= PlateauTopology.Directions[d];
							}
						var vertex = mask == 3 ? 2 : mask == 9 ? 1 : mask == 12 ? 0 : 3;
						if (surfaces.TryGetValue(inward, out var diagonal) && diagonal.Ramp != 0 && diagonal.CornerHeight(vertex) != 4)
							return Reject($"Ramp does not meet the corner vertex at {anchor}.");
					}
					else
					{
						var d = slot == 24 ? 0 : slot == 30 ? 1 : slot == 28 ? 2 : 3;
						if (!FlatRoof(anchor - PlateauTopology.Directions[d] - PlateauTopology.Directions[(d + 1) % 4])) return Reject($"Mixed corner at {anchor}.");
					}
				}
				if (!flatLow(anchor, true)) return Reject($"Unreserved foot at {anchor}.");
				pieces.Add((anchor, slot));
			}
			return true;
		}
	}
}
