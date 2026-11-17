using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	// Keep the standard button's input, hover and text behavior over the entire card.
	class SkirmishPresetButtonWidget : ButtonWidget
	{
		readonly Sprite preview;

		public SkirmishPresetButtonWidget(ModData modData, MapGenerationPreset preset) : base(modData)
		{
			preview = ChromeProvider.GetImage("map-preset-" + preset, "preview");
			Align = TextAlign.Left;
			LeftMargin = 104;
		}

		protected SkirmishPresetButtonWidget(SkirmishPresetButtonWidget other) : base(other)
		{
			preview = other.preview;
		}

		public override Widget Clone() => new SkirmishPresetButtonWidget(this);

		public override void Draw()
		{
			base.Draw();
			var offset = 6 + (Depressed ? VisualHeight : 0);
			WidgetUtils.DrawSprite(preview, new float2(RenderBounds.X + offset, RenderBounds.Y + offset), new Size(88, 88));
		}
	}
}
