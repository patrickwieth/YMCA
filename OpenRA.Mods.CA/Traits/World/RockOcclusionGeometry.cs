using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	// Rendering only: never change resource density, terrain, visibility or simulation state.
	static class RockOcclusionGeometry
	{
		static readonly ConditionalWeakTable<Sheet, byte[]> SheetPixels = new();
		public static bool[] HigherGround(WorldRenderer wr, WPos position, Rectangle bounds)
		{
			var map = wr.World.Map;
			var hidden = new bool[bounds.Width * bounds.Height];
			var cell = map.CellContaining(position);
			if (!map.Contains(cell)) return hidden;
			var elevation = position.Z / 724.0;
			var reach = map.Grid.MaximumTerrainHeight + 3;
			var lateral = Math.Max(2, bounds.Width / 64 + 2);
			var cornerX = new[] { 0, 64, 0, -64 };
			var cornerY = new[] { -32, 0, 32, 0 };
			for (var dx = -lateral; dx <= reach; dx++)
				for (var dy = Math.Max(-lateral, dx-lateral); dy <= Math.Min(reach, dx+lateral); dy++)
				{
					var other = cell + new CVec(dx, dy);
					if (dx + dy <= 0 || !map.Contains(other) || map.Ramp[other] > 4 ||
						map.Height[other] + (map.Ramp[other] == 0 ? 0 : 1) <= Math.Max(elevation,map.Height[cell])) continue;
					var surface = new PlateauSurface(map.Height[other], map.Ramp[other]);
					// Project each surface from its own zero-height world center. A ramp's
					// CenterOfCell includes its interpolated height; using the unit cell's
					// projected center shifted every other roof by that ramp offset.
					var center = map.CenterOfCell(other);
					var origin = wr.ScreenPosition(new WPos(center.X,center.Y,0));
					var poly = new (double X, double Y, double Depth)[4];
					for (var c = 0; c < 4; c++)
					{
						var depth = (center.Y-position.Y)*32.0/724 + cornerY[c];
						poly[c] = (origin.X + cornerX[c], origin.Y + cornerY[c] - 32 * surface.CornerHeight(c), depth);
					}
					FillRoof(hidden, bounds, poly, 0);
				}
			return hidden;
		}

		static void FillRoof(bool[] mask, Rectangle bounds, (double X, double Y, double Depth)[] poly, double minimumDepth)
		{
			var left = Math.Max(bounds.Left, (int)Math.Floor(poly.Min(p => p.X)));
			var right = Math.Min(bounds.Right, (int)Math.Ceiling(poly.Max(p => p.X)));
			for (var x = left; x < right; x++)
			{
				var near = double.PositiveInfinity; var far = double.NegativeInfinity;
				var nearDepth = 0.0; var farDepth = 0.0;
				for (var i = 0; i < 4; i++)
				{
					var a = poly[i]; var b = poly[(i+1)%4]; var px = x + .5;
					if (a.X == b.X || px < Math.Min(a.X,b.X) || px > Math.Max(a.X,b.X)) continue;
					var t = (px-a.X)/(b.X-a.X); var y = a.Y+t*(b.Y-a.Y); var depth = a.Depth+t*(b.Depth-a.Depth);
					if (y < near) { near=y; nearDepth=depth; }
					if (y > far) { far=y; farDepth=depth; }
				}
				if (far <= near) continue;
				for (var y = Math.Max(bounds.Top,(int)Math.Floor(near)); y < Math.Min(bounds.Bottom,(int)Math.Ceiling(far)); y++)
					if (y+.5 >= near && y+.5 < far && nearDepth+(y+.5-near)/(far-near)*(farDepth-nearDepth) > minimumDepth+.001)
						mask[(y-bounds.Top)*bounds.Width+x-bounds.Left]=true;
			}
		}

		public static bool[] RockPixels(WorldRenderer wr, CliffSliceRenderable rock, Rectangle bounds)
		{
			var mask = new bool[bounds.Width*bounds.Height];
			var origin = wr.Screen3DPxPosition(rock.Pos);
			foreach (var s in rock.Slices)
			{
				if (s.Channel != TextureChannel.RGBA) continue;
				var x0=(int)(origin.X-(int)(s.Size.X/2)+s.Offset.X);
				var y0=(int)(origin.Y-(int)(s.Size.Y/2)+s.Offset.Y);
				var overlap=Rectangle.Intersect(bounds,new Rectangle(x0,y0,s.Bounds.Width,s.Bounds.Height));
				if (overlap.Width <= 0 || overlap.Height <= 0) continue;
				var bytes=SheetPixels.GetValue(s.Sheet, sheet => sheet.GetData());
				for (var y=overlap.Top;y<overlap.Bottom;y++)
					for (var x=overlap.Left;x<overlap.Right;x++)
						if (bytes[((s.Bounds.Top+y-y0)*s.Sheet.Size.Width+s.Bounds.Left+x-x0)*4+3] >= 240)
							mask[(y-bounds.Top)*bounds.Width+x-bounds.Left]=true;
			}
			return mask;
		}

		public static Rectangle[] Regions(bool[] mask, Rectangle bounds, bool occluded)
		{
			var excluded = mask.Select(value => value != occluded).ToArray();
			return RubberduckMaterialLayer.ExclusionRectangles(excluded,bounds.Width,bounds.Height)
				.Select(r => new Rectangle(r.X+bounds.X,r.Y+bounds.Y,r.Width,r.Height)).ToArray();
		}
	}

	// Screen-space pixel clips use the world framebuffer coordinate system, not UI zoom coordinates.
	sealed class RockClippedRenderable : IRenderable, IFinalizedRenderable, IModifyableRenderable, IPalettedRenderable
	{
		readonly IRenderable source;
		readonly Rectangle[] regions;
		readonly Func<bool> visibility;
		internal bool IsSilhouette => visibility != null;
		IFinalizedRenderable prepared;
		public WPos Pos { get; }
		public int ZOffset { get; }
		public bool IsDecoration => source.IsDecoration;
		public float Alpha => ((IModifyableRenderable)source).Alpha;
		public float3 Tint => ((IModifyableRenderable)source).Tint;
		public TintModifiers TintModifiers => ((IModifyableRenderable)source).TintModifiers;
		public PaletteReference Palette => ((IPalettedRenderable)source).Palette;
		public IModifyableRenderable WithAlpha(float value) => new RockClippedRenderable(((IModifyableRenderable)source).WithAlpha(value),regions,Pos,ZOffset,visibility);
		public IModifyableRenderable WithTint(in float3 tint, TintModifiers modifiers) => new RockClippedRenderable(((IModifyableRenderable)source).WithTint(tint,modifiers),regions,Pos,ZOffset,visibility);
		public IPalettedRenderable WithPalette(PaletteReference palette) => new RockClippedRenderable(((IPalettedRenderable)source).WithPalette(palette),regions,Pos,ZOffset,visibility);
		public RockClippedRenderable(IRenderable source, Rectangle[] regions, WPos? position = null, int? zOffset = null, Func<bool> visibility = null)
		{ this.source=source; this.regions=regions; this.visibility=visibility; Pos=position ?? source.Pos; ZOffset=zOffset ?? source.ZOffset; }
		public IRenderable WithZOffset(int offset) => new RockClippedRenderable(source,regions,Pos,offset,visibility);
		public IRenderable OffsetBy(in WVec offset) => new RockClippedRenderable(source.OffsetBy(offset),regions,Pos+offset,ZOffset,visibility);
		public IRenderable AsDecoration() => new RockClippedRenderable(source.AsDecoration(),regions,Pos,ZOffset,visibility);
		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { prepared=source.PrepareRender(wr); return this; }
		IEnumerable<Rectangle> Scissors(WorldRenderer wr)
		{
			var origin=wr.Viewport.TopLeft;
			var factor=Game.Renderer.WorldDownscaleFactor;
			if (factor == 1) return regions.Select(r => new Rectangle(r.X-origin.X,r.Y-origin.Y,r.Width,r.Height));
			// Sample at framebuffer pixel centers, then union the clips. Adjacent world
			// strips must never blend the same downscaled pixel multiple times.
			int Edge(int value) => (int)Math.Ceiling((value-factor*.5)/factor);
			var scaled=regions.Select(r => Rectangle.FromLTRB(Edge(r.Left-origin.X),Edge(r.Top-origin.Y),Edge(r.Right-origin.X),Edge(r.Bottom-origin.Y)))
				.Where(r => r.Width>0 && r.Height>0).ToArray();
			if (scaled.Length==0) return Array.Empty<Rectangle>();
			var bounds=scaled.Aggregate(Rectangle.Union);
			var excluded=Enumerable.Repeat(true,bounds.Width*bounds.Height).ToArray();
			foreach(var r in scaled)
				for(var y=r.Top;y<r.Bottom;y++) for(var x=r.Left;x<r.Right;x++) excluded[(y-bounds.Top)*bounds.Width+x-bounds.Left]=false;
			return RubberduckMaterialLayer.ExclusionRectangles(excluded,bounds.Width,bounds.Height)
				.Select(r => new Rectangle((r.X+bounds.X)*factor,(r.Y+bounds.Y)*factor,r.Width*factor,r.Height*factor));
		}
		public void Render(WorldRenderer wr)
		{
			if (visibility != null && !visibility()) return;
			foreach (var r in Scissors(wr))
			{
				Game.Renderer.EnableScissor(r);
				try { prepared.Render(wr); }
				finally { Game.Renderer.DisableScissor(); }
			}
		}
		public Rectangle ScreenBounds(WorldRenderer wr) => prepared?.ScreenBounds(wr) ?? Rectangle.Empty;
		public void RenderDebugGeometry(WorldRenderer wr) { }
	}
}
