using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Source-space calibration shapes. The contour implementation is shared with
	// arbitrary components; these fixtures retain their original placement order.
	static class RubberduckOriginalClosedReference
	{
		internal static CVec[] Roof(int notch) => Enumerable.Range(0, 5)
			.SelectMany(x => Enumerable.Range(-4, 5).Select(y => new CVec(x, y)))
			.Where(c => !(notch == 0 && c.X >= 3 && c.Y >= -1 || notch == 1 && c.X < 2 && c.Y >= -1 ||
				notch == 2 && c.X < 2 && c.Y < -2 || notch == 3 && c.X >= 3 && c.Y < -2)).ToArray();

		internal static List<(int Slot, CVec Offset)> Walls(int notch)
		{
			var roof = Roof(notch);
			var result = RubberduckClosedCliffContour.Create(roof);
			var index = result.FindIndex(p => Rear(p.Slot) && p.Slot < 16);
			var displaced = result.ToList();
			displaced[index] = (displaced[index].Slot, displaced[index].Offset + new CVec(1, 0));
			var corner = result.First(p => p.Slot >= 24);
			var wrongCorner = result.Select(p => p == corner ? (24 + (p.Slot - 24 + 2) % 8, p.Offset) : p).ToList();
			foreach (var invalid in new[] { displaced, wrongCorner, result.Where(p => p != corner).ToList(), result.Where((p, i) => i != index).ToList() })
			{
				var rejected = false;
				try { RubberduckClosedCliffContour.Validate(invalid, roof); }
				catch (InvalidDataException) { rejected = true; }
				if (!rejected) throw new InvalidDataException("Closed reference accepted a broken closure.");
			}
			return result;
		}

		internal static bool Rear(int slot) => slot >= 8 && slot <= 14 || slot == 20 || slot == 28;
	}
}
