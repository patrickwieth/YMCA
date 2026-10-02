using System;

namespace OpenRA.Mods.CA.Modular
{
	// UI-only, independent of simulation ticks and gameplay facing/movement locks.
	public static class CustomPreviewMath
	{
		public const int RotationPeriodMs = 24000;
		public static int Facing(long elapsedMs) => (384 + (int)(Math.Max(0, elapsedMs) % RotationPeriodMs * 1024 / RotationPeriodMs)) % 1024;

		public static float Fit(double width, double height, double availableWidth, double availableHeight)
		{
			if (width <= 0 || height <= 0 || availableWidth <= 0 || availableHeight <= 0 ||
				double.IsNaN(width + height + availableWidth + availableHeight) || double.IsInfinity(width + height + availableWidth + availableHeight))
				return 1;
			return (float)Math.Min(2, Math.Min(availableWidth / width, availableHeight / height));
		}
	}
}
