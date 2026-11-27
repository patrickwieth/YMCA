using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	// Used only in the opt-in source export fixture. Native preview includes voxel
	// turrets, sprite offsets and the resolved authoritative building textures.
	public class BuildingCameoSourceWidget : Widget
	{
		readonly WorldRenderer wr;
		readonly IActorPreview[] previews;
		readonly float scale;
		readonly float2 focus;
		IFinalizedRenderable[] renderables;

		public BuildingCameoSourceWidget(WorldRenderer wr, ActorInfo actor, TypeDictionary init, Rectangle bounds, float crop, float fx, float fy)
		{
			this.wr = wr;
			Bounds = new WidgetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
			var initializer = new ActorPreviewInitializer(actor, wr, init);
			previews = actor.TraitInfos<IRenderActorPreviewInfo>().SelectMany(p => p.RenderPreview(initializer))
				.Where(p => !(p is SpriteActorPreview s) || !s.Animation.Name.Contains("bib", StringComparison.OrdinalIgnoreCase)).ToArray();
			var b = previews.SelectMany(p => p.ScreenBounds(wr, WPos.Zero)).Union();
			if (b.Width == 0 || b.Height == 0) throw new InvalidOperationException("Empty building preview: " + actor.Name);
			// Export a complete native source first; crop the actual opaque artwork later.
			scale = Math.Min(bounds.Width / (b.Width * 1.35f), bounds.Height / (b.Height * 1.35f));
			focus = new float2(b.Left + b.Width * .5f, b.Top + b.Height * .5f);
		}

		public override void PrepareRenderables()
		{
			var origin = RenderOrigin + new int2(Bounds.Width / 2, Bounds.Height / 2) - (scale * focus).ToInt2();
			renderables = previews.SelectMany(p =>
			{
				var first = p.RenderUI(wr, origin, scale).ToArray();
				// Animation.RenderUI scales world-space turret offsets by sequence.Scale;
				// undo that extra factor here to preserve the live actor's registration.
				if (p is SpriteActorPreview sprite && first.Length > 0)
				{
					var correction = scale * (1 - sprite.Animation.CurrentSequence.Scale) * wr.ScreenVectorComponents(first[0].Pos - WPos.Zero).XY;
					return p.RenderUI(wr, origin + correction.ToInt2(), scale);
				}

				return first;
			})
				.OrderBy(WorldRenderer.RenderableZPositionComparisonKey).Select(r => r.PrepareRender(wr)).ToArray();
		}

		public override void Draw()
		{
			Game.Renderer.EnableScissor(RenderBounds);
			WidgetUtils.FillRectWithColor(RenderBounds, Color.FromArgb(43,67,85));
			Game.Renderer.EnableAntialiasingFilter();
			foreach (var r in renderables) r.Render(wr);
			Game.Renderer.DisableAntialiasingFilter();
			Game.Renderer.DisableScissor();
		}
	}
}
