using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class OccludedResourceRendererInfo : ResourceRendererInfo
	{
		public readonly bool ClipToRaisedTerrain = true;
		public override object Create(ActorInitializer init) => new OccludedResourceRenderer(init.Self,this);
	}
	// Keep the standard fog-memory, density, variant, tooltip and radar behavior.
	// Only the final projection is clipped against nearer raised ground.
	public sealed class OccludedResourceRenderer : ResourceRenderer
	{
		readonly Dictionary<CPos, IFinalizedRenderable[]> rendered = new();
		WorldRenderer renderer;
		long revision;
		public int ClippedCells { get; private set; }
		readonly HashSet<CPos> clipped = new();
		readonly HashSet<CPos> populated = new();
		public OccludedResourceRenderer(Actor self, ResourceRendererInfo info) : base(self,info) { }
		protected override void WorldLoaded(World w, WorldRenderer wr)
		{ renderer=wr; revision=TerrainRenderRevision.Get(w.Map); base.WorldLoaded(w,wr); }
		public override void UpdateRenderedSprite(CPos cell, RendererCellContents content)
		{
			rendered.Remove(cell); clipped.Remove(cell); populated.Remove(cell); ClippedCells=clipped.Count;
			if (renderer == null) return;
			if (content.Type == null || content.Density <= 0) { base.UpdateRenderedSprite(cell,content); return; }
			populated.Add(cell);
			var sequence=content.Sequence;
			var frame=int2.Lerp(0,sequence.Length-1,content.Density,ResourceLayer.GetMaxDensity(content.Type));
			var pos=World.Map.CenterOfCell(cell)-new WVec(0,0,World.Map.Grid.Ramps[World.Map.Ramp[cell]].CenterHeightOffset);
			var result=new List<IFinalizedRenderable>();
			foreach (var sprite in new[] { sequence.GetShadow(frame,WAngle.Zero),sequence.GetSprite(frame) })
			{
				if (sprite == null) continue;
				var source=new SpriteRenderable(sprite,pos,WVec.Zero,0,content.Palette,sequence.Scale,sequence.GetAlpha(frame),
					float3.Ones,sequence.IgnoreWorldTint ? TintModifiers.IgnoreWorldTint : TintModifiers.None,false);
				var bounds=source.ScreenBounds(renderer);
				if (bounds.Width <= 0 || bounds.Height <= 0) continue;
				var mask=((OccludedResourceRendererInfo)Info).ClipToRaisedTerrain ? RockOcclusionGeometry.HigherGround(renderer,pos,bounds) : new bool[bounds.Width*bounds.Height];
				if (mask.Any(v => v))
				{
					clipped.Add(cell);
					result.Add(new RockClippedRenderable(source,RockOcclusionGeometry.Regions(mask,bounds,false)).PrepareRender(renderer));
				}
				else result.Add(source);
			}
			if (clipped.Contains(cell))
			{
				base.UpdateRenderedSprite(cell,RendererCellContents.Empty);
				rendered[cell]=result.ToArray();
			}
			else base.UpdateRenderedSprite(cell,content);
			ClippedCells=clipped.Count;
		}
		protected override void Render(WorldRenderer wr)
		{
			var next=TerrainRenderRevision.Get(World.Map);
			if (revision != next)
			{
				revision=next;
				foreach (var cell in populated.ToArray()) UpdateRenderedSprite(cell,RenderContents[cell]);
			}
			base.Render(wr);
			var view=wr.Viewport.Rectangle;
			foreach (var sprites in rendered.Values)
				foreach (var sprite in sprites)
					if (sprite.ScreenBounds(wr).IntersectsWith(view)) sprite.Render(wr);
		}
		protected override void Disposing(Actor self) { rendered.Clear(); clipped.Clear(); populated.Clear(); base.Disposing(self); }
	}
}
