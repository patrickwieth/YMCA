using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	// Stretch the central strip, not the sidebar's tab rail or outside borders.
	public class SidebarImageWidget : ImageWidget
	{
		public readonly int FixedLeft = 8;
		public readonly int FixedRight = 8;
		public readonly bool PreserveAspect = false;
		Sprite source;
		Sprite[] strips;

		public SidebarImageWidget() { }

		protected SidebarImageWidget(SidebarImageWidget other) : base(other)
		{
			FixedLeft = other.FixedLeft;
			FixedRight = other.FixedRight;
			PreserveAspect = other.PreserveAspect;
		}

		public override Widget Clone() => new SidebarImageWidget(this);

		public override void Draw()
		{
			var sprite = GetSprite();
			if (PreserveAspect)
			{
				WidgetUtils.FillRectWithColor(RenderBounds, Color.Black);
				var scale = Math.Min(Bounds.Width / sprite.Size.X, Bounds.Height / sprite.Size.Y);
				var size = scale * new float2(sprite.Size.X, sprite.Size.Y);
				WidgetUtils.DrawSprite(sprite, RenderOrigin + (new float2(Bounds.Width, Bounds.Height) - size) / 2, size);
				return;
			}

			if (source != sprite)
			{
				source = sprite;
				var b = sprite.Bounds;
				strips = new[]
				{
					new Sprite(sprite.Sheet, new Rectangle(b.X, b.Y, FixedLeft, b.Height), sprite.Channel),
					new Sprite(sprite.Sheet, new Rectangle(b.X + FixedLeft, b.Y, b.Width - FixedLeft - FixedRight, b.Height), sprite.Channel),
					new Sprite(sprite.Sheet, new Rectangle(b.Right - FixedRight, b.Y, FixedRight, b.Height), sprite.Channel)
				};
			}

			var middle = Bounds.Width - FixedLeft - FixedRight;
			WidgetUtils.DrawSprite(strips[0], RenderOrigin, new float2(FixedLeft, Bounds.Height));
			WidgetUtils.DrawSprite(strips[1], RenderOrigin + new int2(FixedLeft, 0), new float2(middle, Bounds.Height));
			WidgetUtils.DrawSprite(strips[2], RenderOrigin + new int2(Bounds.Width - FixedRight, 0), new float2(FixedRight, Bounds.Height));
		}
	}
}
