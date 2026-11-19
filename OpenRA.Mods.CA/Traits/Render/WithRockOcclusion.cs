using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Clip units behind raised ground and show translucent white silhouettes through rock pixels only.")]
	public sealed class WithRockOcclusionInfo : TraitInfo
	{
		public readonly float Alpha = .35f;
		public readonly bool Enabled = true;
		public override object Create(ActorInitializer init) => new WithRockOcclusion(this);
	}
	public sealed class WithRockOcclusion : IRenderModifier
	{
		readonly WithRockOcclusionInfo info;
		WPos previousPosition;
		Rectangle previousBounds;
		long previousRevision = -1;
		bool[] cachedGround;
		public int SilhouetteRegions { get; private set; }
		public bool GroundClipped { get; private set; }
		public WithRockOcclusion(WithRockOcclusionInfo info) { this.info=info; }
		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds) => bounds;
		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> renderables)
		{
			SilhouetteRegions=0; GroundClipped=false;
			var sources=renderables.ToArray();
			if (!info.Enabled) return sources;
			if (sources.Length == 0 || self.IsDead || self.Disposed || wr.World.Type == WorldType.Editor) return sources;
			// Explicit viewer/visibility checks supplement the actor's normal render modifiers.
			// Even detected/own cloaked units do not get a brighter outline here.
			bool Visible() => self.IsInWorld && !self.IsDead && !self.Disposed && wr.World.RenderPlayer != null && self.CanBeViewedByPlayer(wr.World.RenderPlayer) &&
				!self.TraitsImplementing<Cloak>().Any(c => c.Cloaked);
			var reveal=Visible();
			var bounds=Rectangle.Empty;
			foreach (var renderer in self.TraitsImplementing<IRender>())
				foreach (var r in renderer.ScreenBounds(self,wr)) bounds=bounds.IsEmpty ? r : Rectangle.Union(bounds,r);
			if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.Width > 768 || bounds.Height > 768) return sources;
			var revision=TerrainRenderRevision.Get(wr.World.Map);
			if (cachedGround==null || previousPosition!=self.CenterPosition || previousBounds!=bounds || previousRevision!=revision)
			{
				cachedGround=RockOcclusionGeometry.HigherGround(wr,self.CenterPosition,bounds);
				previousPosition=self.CenterPosition; previousBounds=bounds; previousRevision=revision;
			}
			var ground=cachedGround;
			var groundHidden=ground.Any(v => v);
			GroundClipped=groundHidden;
			var rocks=new List<(CliffSliceRenderable Rock, bool[] Mask)>();
			if (reveal)
				foreach (var actor in wr.World.ScreenMap.RenderableActorsInBox(new int2(bounds.Left,bounds.Top),new int2(bounds.Right,bounds.Bottom)))
				{
					if (!actor.Info.HasTraitInfo<NativeCliffBodyInfo>() && !actor.Info.HasTraitInfo<PlateauFaceBodyInfo>() && !actor.Info.HasTraitInfo<RockCoastBodyInfo>()) continue;
					foreach (var rock in actor.Render(wr).OfType<CliffSliceRenderable>())
					{
						if (WorldRenderer.RenderableZPositionComparisonKey(rock) <= self.CenterPosition.Y+self.CenterPosition.Z) continue;
						var mask=RockOcclusionGeometry.RockPixels(wr,rock,bounds);
						if (mask.Any(v => v)) rocks.Add((rock,mask));
					}
				}
			if (!groundHidden && rocks.Count == 0) return sources;
			var result=new List<IRenderable>();

			foreach (var source in sources)
			{
				if (source is not SpriteRenderable && source is not ModelRenderable) { result.Add(source); continue; }
				// One union and one final draw: a silhouette attached to an earlier face
				// is otherwise painted over by the next face of the same hill. The
				// normal and white parts must use complementary masks of that union.
				var hidden=(bool[])ground.Clone();
				var depth=WorldRenderer.RenderableZPositionComparisonKey(source);
				foreach (var entry in rocks)
				{
					var rockDepth=WorldRenderer.RenderableZPositionComparisonKey(entry.Rock);
					if (rockDepth <= WorldRenderer.RenderableZPositionComparisonKey(source)) continue;
					for (var i=0;i<hidden.Length;i++) hidden[i] |= entry.Mask[i];
					depth=Math.Max(depth,rockDepth+1);
				}
				if (!hidden.Any(v=>v)) { result.Add(source); continue; }
				var visibleRegions=RockOcclusionGeometry.Regions(hidden,bounds,false);
				var hiddenRegions=RockOcclusionGeometry.Regions(hidden,bounds,true);
				result.Add(new RockClippedRenderable(source,visibleRegions));
				if (!reveal || source.IsDecoration || source is not IModifyableRenderable modifiable || modifiable.Alpha <= 0) continue;
				var white=modifiable.WithAlpha(Math.Min(info.Alpha,modifiable.Alpha)).WithTint(float3.Ones,TintModifiers.ReplaceColor|TintModifiers.IgnoreWorldTint);
				// ModelRenderable supplies tint directly; its replacement color must be premultiplied.
				if (white is ModelRenderable model) white=((ModelRenderable)model.WithTint(model.Alpha*float3.Ones,TintModifiers.ReplaceColor|TintModifiers.IgnoreWorldTint)).WithoutShadow();
				result.Add(new RockClippedRenderable(white,hiddenRegions,source.Pos,depth-source.Pos.Y-source.Pos.Z,Visible));
				SilhouetteRegions += hiddenRegions.Length;
			}
			return result;
		}
	}
}
