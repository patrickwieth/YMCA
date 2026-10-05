using System;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Shared height-four fitting and runtime/editor depth model for source-grid actors.
	// Front-facing lean adds 64px to the column drop; rear-facing lean subtracts it.
	static class RubberduckClosedCliffGeometry
	{
		public static double SourceY(int slot, double x, double y)
		{
			var column = Column(slot, x, y);
			var drop = Math.Clamp((y - column.Roof) / column.Drop, 0, 1);
			// Existing tall-family fitting. Side contacts remain a visual acceptance
			// gate; rejected pinning/short-family experiments are not selected here.
			return y + 32 * drop;
		}

		public static void ValidateSideSeams()
		{
			foreach (var slot in new[] { 18, 22 })
				for (var i = 0; i < 128; i++)
				{
					var x = i + 0.5; var y = slot == 18 ? 64 + x : 192 - x;
					var a = Column(slot, x, y - 0.01); var b = Column(slot, x, y + 0.01);
					var va = (y - a.Roof) / a.Drop; var vb = (y - b.Roof) / b.Drop;
					if (Math.Abs(va - vb) > 0.000001 || Math.Abs(a.Roof + va * (a.Drop + 32) - b.Roof - vb * (b.Drop + 32)) > 0.000001)
						throw new InvalidOperationException("Source-grid side fit is discontinuous.");
					var top = Column(slot, x);
					if (Math.Abs(SourceY(slot, x, top.Roof) - top.Roof) > 0.000001)
						throw new InvalidOperationException("Height-four fitting moved a roof socket.");
					for (var row = 1; row < 256; row++)
						if (SourceY(slot, x, row + 0.5) < SourceY(slot, x, row - 0.5))
							throw new InvalidOperationException("Side fitting folded its source rows.");
				}
		}

		public static (double Roof, double Drop) Column(int slot, double x, double y = double.NegativeInfinity)
		{
			// Side inward corners contain two planes. Their fitted seam is the ray
			// from the side roof vertex; both inverse fits agree exactly on it.
			if (slot == 18 && y > 64 + x) return (64 + x / 2, 64);
			if (slot == 22 && y > 192 - x) return (128 - x / 2, 64);
			if (slot >= 0 && slot <= 2 || slot == 22 || slot == 30 || slot == 24 && x < 64 || slot == 16 && x >= 64)
				return (x / 2, 192);
			if (slot >= 5 && slot <= 7 || slot == 18 || slot == 26 || slot == 24 || slot == 16)
				return (64 - x / 2, 192);
			if (slot >= 8 && slot <= 10 || slot == 20 && x < 64 || slot == 28 && x >= 64)
				return (64 + x / 2, 64);
			if (slot >= 12 && slot <= 14 || slot == 20 || slot == 28)
				return (128 - x / 2, 64);
			throw new ArgumentOutOfRangeException(nameof(slot));
		}
	}
}
