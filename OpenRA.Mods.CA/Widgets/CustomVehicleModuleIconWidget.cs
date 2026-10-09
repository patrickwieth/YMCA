using System;
using OpenRA.Graphics;
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

		public static bool DrawIcon(string module, Rectangle bounds)
		{
			if (module == null || bounds.Width <= 0 || bounds.Height <= 0) return false;
			var collection = module is "engine-basic" or "engine-improved" or "generator-basic"
				? "modular-component-icons" : "modular-module-icons";
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
