using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	// All clipped pieces belong to one stationary face and share its depth/tint.
	// Keep the exact SpriteRenderable pixel placement, but evaluate lighting and
	// sort the actor once rather than once for every one-pixel clipping strip.
	sealed class CliffSliceRenderable : IPalettedRenderable, IModifyableRenderable, IFinalizedRenderable
	{
		readonly Sprite[] slices;
		public WPos Pos { get; }
		public int ZOffset { get; }
		public bool IsDecoration => true;
		public PaletteReference Palette { get; }
		public float Alpha { get; }
		public float3 Tint { get; }
		public TintModifiers TintModifiers { get; }

		public CliffSliceRenderable(Sprite[] slices, WPos position, int zOffset = 0, PaletteReference palette = null,
			float alpha = 1, float3? tint = null, TintModifiers modifiers = TintModifiers.None)
		{
			this.slices = slices; Pos = position; ZOffset = zOffset;
			Palette = palette; Alpha = alpha; Tint = tint ?? float3.Ones; TintModifiers = modifiers;
		}
		public IRenderable WithZOffset(int value) => new CliffSliceRenderable(slices, Pos, value, Palette, Alpha, Tint, TintModifiers);
		public IRenderable OffsetBy(in WVec offset) => new CliffSliceRenderable(slices, Pos + offset, ZOffset, Palette, Alpha, Tint, TintModifiers);
		public IRenderable AsDecoration() => this;
		public IPalettedRenderable WithPalette(PaletteReference value) => new CliffSliceRenderable(slices, Pos, ZOffset, value, Alpha, Tint, TintModifiers);
		public IModifyableRenderable WithAlpha(float value) => new CliffSliceRenderable(slices, Pos, ZOffset, Palette, value, Tint, TintModifiers);
		public IModifyableRenderable WithTint(in float3 value, TintModifiers modifiers) => new CliffSliceRenderable(slices, Pos, ZOffset, Palette, Alpha, value, modifiers);
		public IFinalizedRenderable PrepareRender(WorldRenderer wr) => this;

		static float3 Position(float3 origin, Sprite sprite)
		{
			var half = .5f * sprite.Size;
			return origin - new float3((int)half.X, (int)half.Y, half.Z);
		}
		public void Render(WorldRenderer wr)
		{
			var tint = Alpha * Tint;
			if (wr.TerrainLighting != null && (TintModifiers & TintModifiers.IgnoreWorldTint) == 0)
				tint *= wr.TerrainLighting.TintAt(Pos);
			var alpha = (TintModifiers & TintModifiers.ReplaceColor) == 0 ? Alpha : -Alpha;
			var origin = wr.Screen3DPxPosition(Pos);
			foreach (var sprite in slices)
			{
				var palette = sprite.Channel == TextureChannel.RGBA && !(Palette?.HasColorShift ?? false) ? null : Palette;
				Game.Renderer.WorldSpriteRenderer.DrawSprite(sprite, palette, Position(origin, sprite), 1, tint, alpha, 0);
			}
		}
		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var origin = wr.Screen3DPxPosition(Pos);
			foreach (var sprite in slices)
			{
				var corner = Position(origin, sprite) + sprite.Offset;
				Game.Renderer.RgbaColorRenderer.DrawRect(wr.Viewport.WorldToViewPx(corner), wr.Viewport.WorldToViewPx(corner + sprite.Size), 1, Color.Red);
			}
		}
		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var origin = wr.Screen3DPxPosition(Pos);
			var bounds = Rectangle.Empty;
			foreach (var sprite in slices)
			{
				var next = Util.BoundingRectangle(Position(origin, sprite) + sprite.Offset, sprite.Size, 0);
				bounds = bounds.IsEmpty ? next : Rectangle.Union(bounds, next);
			}
			return bounds;
		}
	}
}
