using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Isometric selection decorations for unit-like actors that use Interactable bounds.")]
	public class IsometricSelectionDecorationsForUnitsInfo : SelectionDecorationsBaseInfo, Requires<InteractableInfo>
	{
		[Desc("Height of the top edge above the base selection diamond.")]
		public readonly int Height = 10;

		[Desc("If true, the extra (non-health) selection bars are only shown while this actor is selected.")]
		public readonly bool ExtraBarsRequireSelection = false;

		public override object Create(ActorInitializer init) { return new IsometricSelectionDecorationsForUnits(init.Self, this); }
	}

	public class IsometricSelectionDecorationsForUnits : SelectionDecorationsBase, IRender
	{
		readonly Interactable interactable;
		readonly IsometricSelectionDecorationsForUnitsInfo info;

		public IsometricSelectionDecorationsForUnits(Actor self, IsometricSelectionDecorationsForUnitsInfo info)
			: base(info)
		{
			interactable = self.Trait<Interactable>();
			this.info = info;
		}

		Polygon DecorationBounds(Actor self, WorldRenderer wr)
		{
			var rect = interactable.DecorationBounds(self, wr);
			var top = new int2(rect.Left + rect.Width / 2, rect.Top);
			var left = new int2(rect.Left, rect.Top + rect.Height / 2);
			var bottom = new int2(rect.Left + rect.Width / 2, rect.Bottom);
			var right = new int2(rect.Right, rect.Top + rect.Height / 2);
			var h = new int2(0, info.Height);

			return new Polygon(new[] { top - h, left - h, left, bottom, right, right - h });
		}

		int2 GetDecorationPosition(Actor self, WorldRenderer wr, string pos)
		{
			var bounds = DecorationBounds(self, wr);
			switch (pos)
			{
				case "TopLeft": return bounds.Vertices[1];
				case "TopRight": return bounds.Vertices[5];
				case "BottomLeft": return bounds.Vertices[2];
				case "BottomRight": return bounds.Vertices[4];
				case "Top": return new int2((bounds.Vertices[1].X + bounds.Vertices[5].X) / 2, bounds.Vertices[1].Y);
				default: return bounds.BoundingRect.TopLeft + new int2(bounds.BoundingRect.Size.Width / 2, bounds.BoundingRect.Size.Height / 2);
			}
		}

		static int2 GetDecorationMargin(string pos, int2 margin)
		{
			switch (pos)
			{
				case "TopLeft": return margin;
				case "TopRight": return new int2(-margin.X, margin.Y);
				case "BottomLeft": return new int2(margin.X, -margin.Y);
				case "BottomRight": return -margin;
				case "Top": return new int2(0, margin.Y);
				default: return int2.Zero;
			}
		}

		protected override int2 GetDecorationOrigin(Actor self, WorldRenderer wr, string pos, int2 margin)
		{
			return wr.Viewport.WorldToViewPx(GetDecorationPosition(self, wr, pos)) + GetDecorationMargin(pos, margin);
		}

		protected override IEnumerable<IRenderable> RenderSelectionBox(Actor self, WorldRenderer wr, Color color)
		{
			var bounds = DecorationBounds(self, wr);
			yield return new IsometricSelectionBoxAnnotationRenderable(self, bounds, color);
		}

		protected override IEnumerable<IRenderable> RenderSelectionBars(Actor self, WorldRenderer wr, bool displayHealth, bool displayExtra)
		{
			if (info.ExtraBarsRequireSelection && !self.World.Selection.Contains(self))
				displayExtra = false;

			if (!displayHealth && !displayExtra)
				yield break;

			var bounds = DecorationBounds(self, wr);
			yield return new IsometricSelectionBarsAnnotationRenderable(self, bounds, displayHealth, displayExtra);
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			yield break;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			yield return DecorationBounds(self, wr).BoundingRect;
		}
	}
}
