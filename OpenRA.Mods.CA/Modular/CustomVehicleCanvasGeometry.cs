using System;

namespace OpenRA.Mods.CA.Modular
{
	// Display pixels only: never changes inventory capacities or compiled vehicle values.
	public static class CustomVehicleCanvasGeometry
	{
		public const int Cell = 96;
		public const int SketchCell = 28;
		public static int Scale(int value) => (int)Math.Round(value * (double)Cell / SketchCell);
		public static (int X, int Y) TurretOrigin(CustomVehicleSpaceDemo layout) => (layout.Portrait ? 144 : 256, 136);
		public static (int X, int Y) HullOrigin(CustomVehicleSpaceDemo layout) =>
			(layout.Portrait ? 192 : 160, 136 + layout.TurretHeight * Cell + (layout.Portrait ? 120 : 220));
		public static (int Width, int Height) Extent(CustomVehicleSpaceDemo layout)
		{
			var hull = HullOrigin(layout);
			var turret = TurretOrigin(layout);
			var socketEnd = layout.WeaponSocket is { } socket ? socket.X + socket.Width : 0;
			return (Math.Max(hull.X + layout.HullWidth * Cell + 220,
				turret.X + Math.Max(layout.TurretWidth, socketEnd) * Cell + 100), hull.Y + layout.HullHeight * Cell + 180);
		}
		public static (int X, int Y) ClampPan(CustomVehicleSpaceDemo layout, int width, int height, int x, int y)
		{
			var extent = Extent(layout);
			return (Math.Clamp(x, 0, Math.Max(0, extent.Width - width)), Math.Clamp(y, 0, Math.Max(0, extent.Height - height)));
		}
	}
}
