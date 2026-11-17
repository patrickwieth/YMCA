using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Uses the vehicle hover motion for building sprites while leaving decorative foundations fixed.")]
	public class HoversAboveFoundationInfo : HoversInfo
	{
		public override object Create(ActorInitializer init) => new HoversAboveFoundation(this);
	}

	public class HoversAboveFoundation : Hovers, IRenderModifier
	{
		readonly WVec maximumVisualOffset;

		public HoversAboveFoundation(HoversAboveFoundationInfo info)
			: base(info)
		{
			maximumVisualOffset = new WVec(0, 0, info.InitialHeight.Length - info.BobDistance.Length);
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> renderables)
		{
			// WithIdleOverlay bibs explicitly mark themselves as decorations.
			// Body and attached non-decoration effects share the same visual motion.
			return renderables.Select(r => r.IsDecoration ? r : r.OffsetBy(WorldVisualOffset));
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			var offset = wr.ScreenPxOffset(maximumVisualOffset);
			// Cover the full bob cycle even when stationary actors are not re-indexed.
			// Bounds have no decoration flags: retain the stationary foundation too.
			return bounds.Select(b => Rectangle.FromLTRB(
				System.Math.Min(b.Left, b.Left + offset.X), System.Math.Min(b.Top, b.Top + offset.Y),
				System.Math.Max(b.Right, b.Right + offset.X), System.Math.Max(b.Bottom, b.Bottom + offset.Y)));
		}
	}
}
