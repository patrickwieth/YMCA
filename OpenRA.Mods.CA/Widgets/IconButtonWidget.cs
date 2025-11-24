using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public class IconButtonWidget : ButtonWidget
	{
		public string IconImageCollection = "";
		public string IconImageName = "";
		public bool UseStatefulIcon = true;
		public int IconPadding = 2;
		public int TextPadding = 2;

		public Func<string> GetIconImageCollection;
		public Func<string> GetIconImageName;
		public Func<Sprite> GetIconSprite;

		readonly CachedTransform<(string Collection, string Image), Sprite> iconCache =
			new(args => ChromeProvider.GetImage(args.Collection, args.Image));

		readonly CachedTransform<(string Collection, string Image, bool Disabled, bool Pressed, bool Hover, bool Highlighted), Sprite> statefulIconCache =
			new(args =>
			{
				var collection = args.Highlighted ? args.Collection + "-highlighted" : args.Collection;
				var imageName = WidgetUtils.GetStatefulImageName(args.Image, args.Disabled, args.Pressed, args.Hover);
				return ChromeProvider.TryGetImage(collection, imageName) ?? ChromeProvider.GetImage(collection, args.Image);
			});

		[ObjectCreator.UseCtor]
		public IconButtonWidget(ModData modData)
			: base(modData)
		{
			GetIconImageCollection = () => IconImageCollection;
			GetIconImageName = () => IconImageName;
			GetIconSprite = DefaultGetIconSprite;
		}

		protected IconButtonWidget(IconButtonWidget other)
			: base(other)
		{
			IconImageCollection = other.IconImageCollection;
			IconImageName = other.IconImageName;
			UseStatefulIcon = other.UseStatefulIcon;
			IconPadding = other.IconPadding;
			TextPadding = other.TextPadding;

			GetIconImageCollection = other.GetIconImageCollection;
			GetIconImageName = other.GetIconImageName;
			GetIconSprite = other.GetIconSprite ?? DefaultGetIconSprite;
		}

		public override Widget Clone()
		{
			return new IconButtonWidget(this);
		}

		Sprite DefaultGetIconSprite()
		{
			var collection = GetIconImageCollection?.Invoke();
			var image = GetIconImageName?.Invoke();
			if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(image))
				return null;

			if (UseStatefulIcon)
				return statefulIconCache.Update((collection, image, IsDisabled(), Depressed, IsHovered(), IsHighlighted()));

			return iconCache.Update((collection, image));
		}

		bool IsHovered()
		{
			return Ui.MouseOverWidget == this || Children.FirstOrDefault(c => c == Ui.MouseOverWidget) != null;
		}

		public override void Draw()
		{
			var rb = RenderBounds;
			var disabled = IsDisabled();
			var hover = IsHovered();
			var highlighted = IsHighlighted();
			var font = Game.Renderer.Fonts[Font];

			var rawText = GetText();
			var text = string.IsNullOrEmpty(rawText) ? string.Empty : WidgetUtils.TruncateText(rawText, rb.Width - LeftMargin - RightMargin, font);
			var textSize = string.IsNullOrEmpty(text) ? int2.Zero : font.Measure(text);

			var stateOffset = Depressed ? new int2(VisualHeight, VisualHeight) : int2.Zero;

			DrawBackground(rb, disabled, Depressed, hover, highlighted);

			var iconAreaHeight = rb.Height - (textSize.Y > 0 ? textSize.Y + TextPadding : 0);
			if (iconAreaHeight > 0)
			{
				var icon = GetIconSprite();
				if (icon != null)
				{
					var availableWidth = Math.Max(0, rb.Width - 2 * IconPadding);
					var availableHeight = Math.Max(0, iconAreaHeight - IconPadding * 2);
					var scale = Math.Min(availableWidth / (float)icon.Size.X, availableHeight / (float)icon.Size.Y);
					var scaledSize = new float2(icon.Size.X * scale, icon.Size.Y * scale);
					var iconTopLeft = new float2(
						rb.X + (availableWidth - scaledSize.X) / 2f + IconPadding,
						rb.Y + (iconAreaHeight - scaledSize.Y) / 2f + IconPadding);

					WidgetUtils.DrawSprite(icon, iconTopLeft + stateOffset, scaledSize);
				}
			}

			if (!string.IsNullOrEmpty(text))
			{
				var textPosition = new int2(rb.X + (rb.Width - textSize.X) / 2, rb.Bottom - textSize.Y - TextPadding) + stateOffset;
				var color = disabled ? GetColorDisabled() : GetColor();

				if (Contrast)
					font.DrawTextWithContrast(text, textPosition, color, GetContrastColorDark(), GetContrastColorLight(), ContrastRadius);
				else if (Shadow)
					font.DrawTextWithShadow(text, textPosition, color, GetContrastColorDark(), GetContrastColorLight(), 1);
				else
					font.DrawText(text, textPosition, color);
			}
		}
	}
}
