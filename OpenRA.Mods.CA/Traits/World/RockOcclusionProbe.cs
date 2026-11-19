using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class RockOcclusionProbeInfo : TraitInfo
	{
		public readonly string ResultPath="";
		public readonly bool Baseline=false;
		public readonly bool SlopeCases=false;
		public override object Create(ActorInitializer init) => new RockOcclusionProbe(this);
	}
	public sealed class RockOcclusionProbe : ITickRender
	{
		readonly RockOcclusionProbeInfo info;
		int frames, arrived;
		Actor moving;
		public RockOcclusionProbe(RockOcclusionProbeInfo info)
		{
			if (Environment.GetEnvironmentVariable("YMCA_ROCK_OCCLUSION_PROBE") != "1") throw new InvalidOperationException("Local diagnostic opt-in required.");
			this.info=info;
		}
		void Log(string message) => File.AppendAllText(info.ResultPath,message+"\n");
		void ITickRender.TickRender(WorldRenderer wr,Actor self)
		{
			frames++; var w=self.World;
			if (frames==1)
			{
				var bounds=new OpenRA.Primitives.Rectangle(-7,-3,17,13);
				var mask=Enumerable.Range(0,17*13).Select(i => i%5==0 || i/17==6).ToArray();
				foreach(var hidden in new[] { false,true })
				{
					var regions=RockOcclusionGeometry.Regions(mask,bounds,hidden);
					for(var y=0;y<13;y++) for(var x=0;x<17;x++)
						if(regions.Count(r=>r.Contains(x-7,y-3)) != (mask[y*17+x]==hidden ? 1 : 0))
							throw new InvalidOperationException("Mask regions overlap or lose pixels.");
				}
				Log("MASK_PARTITION_PASS");
				Log("UI_SCALE "+Game.Settings.Graphics.UIScale.ToString(System.Globalization.CultureInfo.InvariantCulture));
				if (info.SlopeCases) w.AddFrameEndTask(_ =>
				{
					// Stop midway across a cell, like a moving unit crossing the rim.
					var partial=w.Actors.Single(a=>a.Info.Name=="mtnk" && a.Location==new CPos(101,-34));
					partial.Trait<Mobile>().SetCenterPosition(partial,partial.CenterPosition+new WVec(0,-362,0));
				});
			}
			// Keep the owned visual fixture stable even when the desktop mouse rests
			// on a window edge and would otherwise trigger edge-scrolling.
			wr.Viewport.Center(w.Map.CenterOfCell(new CPos(106,-29)));
			if (frames<20) wr.Viewport.UnlockMinimumZoom(.25f);
			if (frames==100 || frames==200) Game.TakeScreenshot();
			if (info.SlopeCases)
			{
				if (frames==210)
				{
					var units=w.Actors.Where(a=>a.TraitOrDefault<WithRockOcclusion>()!=null).ToArray();
					foreach(var a in units)
					{
						var white=a.Render(wr).OfType<RockClippedRenderable>().Where(r=>r.IsSilhouette).ToArray();
						Log(a.Info.Name+" at="+a.Location+" ramp="+w.Map.Ramp[a.Location]+" white="+white.Length);
						var pixel=wr.Viewport.WorldToViewPx(wr.Screen3DPxPosition(a.CenterPosition));
						Log("PIXEL "+a.Location+" "+pixel.X+" "+pixel.Y+" ramp="+w.Map.Ramp[a.Location]);
						// Empty model bounding-box margins can intersect a rock without
						// any unit pixel doing so. Check ramp body pixels against baseline
						// screenshots, not the number of candidate clip rectangles.
						if (!info.Baseline && a.Location==new CPos(102,-33) && white.Length==0)
							throw new InvalidOperationException("Fully hidden unit has no silhouette.");
					}
					Log("PASS: slope/full-occlusion fixture "+(info.Baseline ? "baseline" : "fixed"));
				}
				return;
			}
			if (frames==210)
			{
				var rr=self.Trait<OccludedResourceRenderer>(); Log("CLIPPED_RESOURCES "+rr.ClippedCells);
				var resources=self.Trait<IResourceLayer>();
				if(w.Map.AllCells.Any(c=>w.Map.Height[c]>0 && resources.GetResource(c).Type!=null)) throw new InvalidOperationException("Fixture unexpectedly has real resources on the plateau.");
				Log("LOWER_RESOURCES_ONLY: no resource cells on the raised roof");
				var units=w.Actors.Where(a=>a.TraitOrDefault<WithRockOcclusion>()!=null).ToArray();
				// TickRender precedes this frame's PrepareRender; refresh diagnostics rather
				// than mistaking a previous visible frame's counter for a cloak-transition leak.
				foreach(var a in units) _=a.Render(wr).ToArray();
				foreach(var a in units) Log(a.Info.Name+" at="+a.Location+" visible="+a.CanBeViewedByPlayer(w.RenderPlayer)+" ground="+a.Trait<WithRockOcclusion>().GroundClipped+" silhouette="+a.Trait<WithRockOcclusion>().SilhouetteRegions);
				if (!info.Baseline)
				{
					if(rr.ClippedCells==0 || !units.Any(a=>a.Trait<WithRockOcclusion>().SilhouetteRegions>0)) throw new InvalidOperationException("Fixture did not exercise occlusion.");
					var stealth=units.Single(a=>a.Info.Name=="stnk");
					if(!stealth.TraitsImplementing<Cloak>().Any(c=>c.Cloaked)) throw new InvalidOperationException("Cloak case not exercised.");
					if(stealth.Owner.IsAlliedWith(w.RenderPlayer)) throw new InvalidOperationException("Cloak case must be hostile.");
					foreach(var a in units.Where(a=>!a.CanBeViewedByPlayer(w.RenderPlayer) || a.TraitsImplementing<Cloak>().Any(c=>c.Cloaked)))
						if(a.Render(wr).OfType<RockClippedRenderable>().Any(r=>r.IsSilhouette)) throw new InvalidOperationException("Visibility leak.");
					var front=units.Single(a=>a.Info.Name=="mtnk" && a.Location==new CPos(116,-19));
					var roof=units.Single(a=>a.Info.Name=="mtnk" && a.Location==new CPos(110,-25));
					if(front.Trait<WithRockOcclusion>().SilhouetteRegions!=0 || roof.Trait<WithRockOcclusion>().SilhouetteRegions!=0) throw new InvalidOperationException("Visible front/roof unit incorrectly outlined.");
				}
				moving=units.First(a=>a.Info.Name=="mtnk" && a.Owner==w.LocalPlayer && a.Location!=new CPos(116,-19) && a.Location!=new CPos(110,-25));
				w.AddFrameEndTask(_=>moving.QueueActivity(false,moving.Trait<Mobile>().MoveTo(new CPos(118,-19))));
			}
			if(frames>240 && moving!=null && moving.CenterPosition==w.Map.CenterOfCell(new CPos(118,-19)))
			{
				if(moving.Render(wr).OfType<RockClippedRenderable>().Any(r=>r.IsSilhouette)) throw new InvalidOperationException("Stale outline after moving into view.");
				Game.TakeScreenshot(); Log("MOVE_CLEAR"); moving=null; arrived=frames;
			}
			if(arrived>0 && frames==arrived+10) wr.Viewport.AdjustZoom((float)Math.Log(1.0f/wr.Viewport.Zoom));
			if(arrived>0 && frames==arrived+25) Game.TakeScreenshot();
			if(arrived>0 && frames==arrived+35)
				w.AddFrameEndTask(_ => { w.RenderPlayer.Shroud.Disabled=true; var layer=self.Trait<IResourceLayer>(); foreach(var cell in w.Map.AllCells) if(layer.GetResource(cell).Type!=null) layer.ClearResources(cell); });
			if(arrived>0 && frames==arrived+80)
			{
				if(self.Trait<OccludedResourceRenderer>().ClippedCells!=0) throw new InvalidOperationException("Stale resource clips after depletion.");
				Game.TakeScreenshot(); Log("PASS: "+(info.Baseline ? "baseline captured" : "resources clipped/depletion cleared; sprites/voxels outlined; front/roof normal; hostile cloak/fog protected; moving outline cleared; two zooms rendered"));
			}
			if(frames==2400 && moving!=null) throw new InvalidOperationException("Movement test timed out.");
		}
	}
}
