using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	// One sorting item per authored deck sheet. Cropped UV rectangles share the
	// original canvas origin, including fractional scale and horizontal reflection.
	sealed class BridgeDeckRenderable : IPalettedRenderable, IModifyableRenderable, IFinalizedRenderable
	{
		readonly Sprite[] slices;
		internal string ClipSignature => string.Join(";", slices.Select(s => $"{s.Bounds}/{s.Offset}"));
		readonly float scale;
		readonly bool mirrored;
		public WPos Pos { get; }
		public int ZOffset { get; }
		public bool IsDecoration { get; }
		public PaletteReference Palette { get; }
		public float Alpha { get; }
		public float3 Tint { get; }
		public TintModifiers TintModifiers { get; }
		public BridgeDeckRenderable(Sprite source, IEnumerable<Rectangle> visible, WPos pos, PaletteReference palette, float scale, bool mirrored)
			: this(visible.Select(r => new Sprite(source.Sheet,
				Rectangle.FromLTRB(source.Bounds.Left + r.X + (mirrored ? r.Width : 0), source.Bounds.Top + r.Y,
					source.Bounds.Left + r.X + (mirrored ? 0 : r.Width), source.Bounds.Top + r.Bottom), 0,
				new float3(mirrored ? -r.X : r.X, r.Y, 0), source.Channel, source.BlendMode)).ToArray(),
				pos, -2172, palette, scale, mirrored, 1, float3.Ones, TintModifiers.None) { }
		BridgeDeckRenderable(Sprite[] slices, WPos pos, int z, PaletteReference palette, float scale, bool mirrored, float alpha, float3 tint, TintModifiers modifiers, bool decoration = false)
		{
			this.slices = slices; Pos = pos; ZOffset = z; Palette = palette; this.scale = scale; this.mirrored = mirrored;
			Alpha = alpha; Tint = tint; TintModifiers = modifiers; IsDecoration = decoration;
		}
		public IRenderable WithZOffset(int z) => new BridgeDeckRenderable(slices, Pos, z, Palette, scale, mirrored, Alpha, Tint, TintModifiers, IsDecoration);
		public IRenderable OffsetBy(in WVec v) => new BridgeDeckRenderable(slices, Pos + v, ZOffset, Palette, scale, mirrored, Alpha, Tint, TintModifiers, IsDecoration);
		public IRenderable AsDecoration() => new BridgeDeckRenderable(slices, Pos, ZOffset, Palette, scale, mirrored, Alpha, Tint, TintModifiers, true);
		public IPalettedRenderable WithPalette(PaletteReference p) => new BridgeDeckRenderable(slices, Pos, ZOffset, p, scale, mirrored, Alpha, Tint, TintModifiers, IsDecoration);
		public IModifyableRenderable WithAlpha(float a) => new BridgeDeckRenderable(slices, Pos, ZOffset, Palette, scale, mirrored, a, Tint, TintModifiers, IsDecoration);
		public IModifyableRenderable WithTint(in float3 t, TintModifiers m) => new BridgeDeckRenderable(slices, Pos, ZOffset, Palette, scale, mirrored, Alpha, t, m, IsDecoration);
		public IFinalizedRenderable PrepareRender(WorldRenderer wr) => this;
		public void Render(WorldRenderer wr)
		{
			var tint = Alpha * Tint;
			if (wr.TerrainLighting != null && (TintModifiers & TintModifiers.IgnoreWorldTint) == 0) tint *= wr.TerrainLighting.TintAt(Pos);
			var origin = wr.Screen3DPxPosition(Pos) - new float3((mirrored ? -1 : 1) * (int)(128 * scale), (int)(128 * scale), 0);
			foreach (var sprite in slices)
				Game.Renderer.WorldSpriteRenderer.DrawSprite(sprite, Palette, origin, scale, tint,
					(TintModifiers & TintModifiers.ReplaceColor) == 0 ? Alpha : -Alpha, 0);
		}
		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var p = wr.ScreenPxPosition(Pos); var r = (int)System.Math.Ceiling(128 * scale) + 2;
			return new Rectangle(p.X - r, p.Y - r, r * 2, r * 2);
		}
		public void RenderDebugGeometry(WorldRenderer wr) { }
	}
}
