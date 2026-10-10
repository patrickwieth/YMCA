using System;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	// RGBA chrome sprites: independent of world palettes and actor spawning.
	public sealed class CustomVehicleModuleIconWidget : Widget
	{
		public Func<string> GetModule = () => null;
		public override bool EventBoundsContains(int2 location) => false;
		public override void Draw() => DrawIcon(GetModule(), RenderBounds);

		public static string ForPart(string module, string part) => (module, part) switch
		{
			("engine", "diesel") => "engine-basic",
			("engine", "diesel-large") => "engine-improved",
			("generator", "baseline-generator") => "generator-basic",
			_ => module
		};

		public static bool DrawCannon(CustomVehicleCannonArt art, Rectangle bounds, bool rotated)
		{
			if (art?.Sprite == null || bounds.Width <= 0 || bounds.Height <= 0) return false;
			var sprite = ChromeProvider.TryGetImage("modular-cannon-icons", art.Sprite + (rotated ? "-rotated" : ""));
			if (sprite == null) return false;
			for (var i = 0; i < art.Barrels; i++)
			{
				var lane = rotated
					? new Rectangle(bounds.X + i * bounds.Width / art.Barrels, bounds.Y, bounds.Width / art.Barrels, bounds.Height)
					: new Rectangle(bounds.X, bounds.Y + i * bounds.Height / art.Barrels, bounds.Width, bounds.Height / art.Barrels);
				var scale = Math.Min(lane.Width / sprite.Size.X, lane.Height / sprite.Size.Y);
				var size = new float2(Math.Max(1, (int)(sprite.Size.X * scale)), Math.Max(1, (int)(sprite.Size.Y * scale)));
				// The muzzle stays at the forward end; longer breeches extend inward.
				var position = rotated ? new float2(lane.X + (lane.Width - size.X) / 2, lane.Bottom - size.Y)
					: new float2(lane.Right - size.X, lane.Y + (lane.Height - size.Y) / 2);
				WidgetUtils.DrawSprite(sprite, position, size);
			}
			return true;
		}

		public static bool DrawIcon(string module, Rectangle bounds)
		{
			if (module == null || bounds.Width <= 0 || bounds.Height <= 0) return false;
			var collection = module is "engine-basic" or "engine-improved" or "generator-basic"
				? "modular-component-icons" : "modular-module-icons";
			if (module.StartsWith("cannon-", StringComparison.Ordinal)) collection = "modular-cannon-icons";
			if (collection == "modular-component-icons" && Math.Max(bounds.Width, bounds.Height) > 64)
				collection = "modular-component-icons-large";
			var sprite = ChromeProvider.TryGetImage(collection, module);
			if (sprite == null) return false;
			var scale = Math.Min(bounds.Width / sprite.Size.X, bounds.Height / sprite.Size.Y);
			var size = new float2(Math.Max(1, (int)(sprite.Size.X * scale)), Math.Max(1, (int)(sprite.Size.Y * scale)));
			var position = new float2(bounds.X + (int)((bounds.Width - size.X) / 2), bounds.Y + (int)((bounds.Height - size.Y) / 2));
			WidgetUtils.DrawSprite(sprite, position, size);
			return true;
		}
	}
}
