using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Primitives;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class BuildingOcclusionProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly bool Baseline = false;
		public readonly bool SlopeCases = false;
		public override object Create(ActorInitializer init) => new BuildingOcclusionProbe(this);
	}

	public sealed class BuildingOcclusionProbe : ITickRender
	{
		readonly BuildingOcclusionProbeInfo info;
		int frames;
		Actor moving;
		CPos rearPosition;
		int clearedAt;
		readonly HashSet<int> voxelMasks = new();
		public BuildingOcclusionProbe(BuildingOcclusionProbeInfo info)
		{
			if (Environment.GetEnvironmentVariable("YMCA_ROCK_OCCLUSION_PROBE") != "1") throw new InvalidOperationException("Local diagnostic opt-in required.");
			this.info = info;
		}
		void Log(string text) => File.AppendAllText(info.ResultPath, text + "\n");
		bool White(Actor actor, WorldRenderer wr) => actor.Render(wr).OfType<RockClippedRenderable>().Any(r => r.IsSilhouette);
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			frames++;
			if (frames == 1) wr.Viewport.AdjustZoom((float)Math.Log(1f / wr.Viewport.Zoom));
			var world = self.World;
			wr.Viewport.Center(world.Map.CenterOfCell(new CPos(110, -25)));
			var tower = world.Actors.FirstOrDefault(a => a.Info.Name == "atwr");
			if (tower != null && frames < 210)
			{
				var angle = frames < 110 ? 0 : frames < 150 ? 256 : frames < 190 ? 512 : 768;
				world.AddFrameEndTask(_ => tower.Trait<Turreted>().LocalOrientation = WRot.FromYaw(new WAngle(angle)));
			}
			if (tower != null && (frames == 100 || frames == 130 || frames == 170 || frames == 200))
			{
				var center = wr.ScreenPosition(tower.CenterPosition);
				var area = new Rectangle((int)center.X - 256, (int)center.Y - 256, 512, 512);
				var mask = new bool[area.Width * area.Height];
				foreach (var model in tower.Render(wr).OfType<ModelRenderable>())
				{
					var part = ModelOcclusionGeometry.Pixels(wr, tower, model, area);
					for (var i = 0; i < mask.Length; i++) mask[i] |= part[i];
				}
				var hash = 17;
				for (var i = 0; i < mask.Length; i++) if (mask[i]) hash = unchecked(hash * 31 + i);
				voxelMasks.Add(hash);
				Log("VOXEL_MASK pixels=" + mask.Count(p => p) + " hash=" + hash);
				if (!mask.Any(p => p)) throw new InvalidOperationException("No voxel geometry exercised.");
				if (frames == 130 || frames == 170) Game.TakeScreenshot();
			}
			var units = world.Actors.Where(a => a.TraitOrDefault<Mobile>() != null && !a.Info.HasTraitInfo<BuildingInfo>()).ToArray();
			if (frames == 100 || frames == 200) Game.TakeScreenshot();
			if (frames == 210)
			{
				foreach (var a in units) Log(a.Info.Name + " at=" + a.Location + " visible=" + a.CanBeViewedByPlayer(world.RenderPlayer) + " white=" + White(a, wr));
				var front = units.Single(a => a.Location == new CPos(115, -20));
				moving = units.Single(a => (a.Info.Name == "mtnk" || a.Info.Name == "chnmcv") && a != front);
				rearPosition = moving.Location;
				if (!info.Baseline)
				{
					if (tower != null && voxelMasks.Count < 2) throw new InvalidOperationException("Rotating voxel mask did not update.");
					foreach (var a in units.Where(a => a.Owner == world.LocalPlayer && (a == moving || (tower == null && (a.Location == new CPos(109, -25) || a.Location == new CPos(110, -26))))))
						if (!White(a, wr)) throw new InvalidOperationException("Rear unit not outlined: " + a.Info.Name);
					if (White(front, wr)) throw new InvalidOperationException("Front unit outlined.");
					if (units.Single(a => a.Info.Name == "ltnk").CanBeViewedByPlayer(world.RenderPlayer))
						throw new InvalidOperationException("Fog case not exercised.");
					var cloak = units.Single(a => a.Info.Name == "stnk");
					if (!cloak.TraitsImplementing<Cloak>().Any(c => c.Cloaked)) throw new InvalidOperationException("Cloak not exercised.");
					foreach (var a in units.Where(a => a.Owner != world.LocalPlayer))
						if (White(a, wr)) throw new InvalidOperationException("Hidden enemy outlined.");
				}
				world.AddFrameEndTask(_ => moving.QueueActivity(false, moving.Trait<Mobile>().MoveTo(new CPos(116, -20))));
			}
			if (moving != null && moving.Location == new CPos(116, -20) && clearedAt == 0)
			{
				if (White(moving, wr)) throw new InvalidOperationException("Stale moving silhouette.");
				clearedAt = frames;
				Game.TakeScreenshot();
				// Exercise cloak in the exact previously occluded, now vacant position.
				var cloak = units.Single(a => a.Info.Name == "stnk");
				world.AddFrameEndTask(_ => cloak.Trait<Mobile>().SetPosition(cloak, rearPosition));
			}
			if (clearedAt > 0 && frames == clearedAt + 5)
			{
				var cloak = units.Single(a => a.Info.Name == "stnk");
				if (!cloak.TraitsImplementing<Cloak>().Any(c => c.Cloaked) || White(cloak, wr)) throw new InvalidOperationException("Occluded cloak leaked.");
				wr.Viewport.AdjustZoom((float)Math.Log(.5f / wr.Viewport.Zoom));
			}
			if (clearedAt > 0 && frames == clearedAt + 15)
			{
				Game.TakeScreenshot();
				world.AddFrameEndTask(_ => world.Actors.Single(a => a.Info.Name is "proc" or "proc.td" or "chcommand" or "atwr").Dispose());
			}
			if (clearedAt > 0 && frames == clearedAt + 20)
			{
				if (units.Any(a => White(a, wr))) throw new InvalidOperationException("Stale removed-building silhouette.");
				Log("PASS: rear sprite/voxel/infantry, front, cloak/fog, movement and building removal " + (info.Baseline ? "baseline" : "fixed"));
				Game.TakeScreenshot();
				moving = null;
			}
			if (frames == 2400 && moving != null) throw new InvalidOperationException("Movement timed out.");
		}
	}
}
