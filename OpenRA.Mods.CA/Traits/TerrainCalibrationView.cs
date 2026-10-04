using System.Linq;
using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Centers the camera for isolated terrain calibration maps. Not used by generated gameplay maps.")]
	public class TerrainCalibrationViewInfo : TraitInfo
	{
		public readonly CPos Center = CPos.Zero;
		public readonly int ZoomSteps = -2;
		public readonly float ZoomScale = 0;
		public readonly int ScreenshotTicks = 75;
		public readonly float MinimumZoomScale = 1f;
		public readonly bool SelectPlateauProbes = false;
		public readonly bool ValidateHomeBase = false;
		public readonly bool ValidateMaterialOcclusion = false;
		public readonly bool ValidateMixedWater = false;
		public readonly bool ReportCliffGeometry = false;
		public readonly string ResultPath = "";

		public override object Create(ActorInitializer init) => new TerrainCalibrationView(this);
	}

	public class TerrainCalibrationView : IPostWorldLoaded, ITick
	{
		readonly TerrainCalibrationViewInfo info;
		int screenshotTicks;
		WorldRenderer renderer;
		bool deployed;

		public TerrainCalibrationView(TerrainCalibrationViewInfo info)
		{
			this.info = info;
			screenshotTicks = info.ScreenshotTicks;
		}

		void ITick.Tick(Actor self)
		{
			if (info.ValidateHomeBase && !deployed && self.World.WorldTick >= 10)
			{
				var vehicle = self.World.Actors.FirstOrDefault(a => a.Owner == self.World.LocalPlayer &&
					a.Info.HasTraitInfo<BaseBuildingInfo>() && a.TraitOrDefault<Transforms>() != null);
				if (vehicle == null || self.World.Map.Height[vehicle.Location] != 4 ||
					self.World.Map.Ramp[vehicle.Location] != 0 || vehicle.CenterPosition.Z != 4 * 724)
					throw new InvalidOperationException("Starting MCV is missing or is not on the home plateau: local=" + self.World.LocalPlayer?.InternalName +
						" home=" + self.World.LocalPlayer?.HomeLocation + " vehicle=" + vehicle?.Info.Name + " position=" + vehicle?.CenterPosition);
				PlateauMovementProbe.WriteResult(info.ResultPath, "HOME MCV PASS " + vehicle.Info.Name + " " + vehicle.CenterPosition);
				vehicle.Trait<Transforms>().DeployTransform(false);
				deployed = true;
			}
			if (--screenshotTicks == 0)
			{
				if (info.ValidateMixedWater)
					PlateauMovementProbe.WriteResult(info.ResultPath, self.Trait<RubberduckMaterialTransitions>().ValidateMixedWater());
				if (info.ValidateMaterialOcclusion)
					PlateauMovementProbe.WriteResult(info.ResultPath, self.Trait<RubberduckMaterialTransitions>().ValidateApronOcclusion());
				if (info.ValidateHomeBase)
				{
					var building = self.World.Actors.FirstOrDefault(a => a.Owner == self.World.LocalPlayer &&
						a.Info.HasTraitInfo<BaseBuildingInfo>() && a.TraitOrDefault<Building>() != null);
					if (building == null || building.CenterPosition.Z != 4 * 724)
						throw new InvalidOperationException("Construction yard did not deploy on the elevated home.");
					PlateauMovementProbe.WriteResult(info.ResultPath, "HOME DEPLOY PASS " + building.Info.Name + " " + building.CenterPosition);
				}
				if (info.SelectPlateauProbes)
					self.World.Selection.Combine(self.World, self.World.Actors.Where(a => a.Owner == self.World.LocalPlayer &&
						a.TraitOrDefault<PlateauMovementProbe>() != null), false, false);
				Log.Write("debug", "TERRAIN VIEW local=" + self.World.LocalPlayer?.InternalName + " center=" + info.Center + " units=" +
					string.Join("; ", self.World.Actors.Where(a => a.Owner == self.World.LocalPlayer &&
						(a.TraitOrDefault<OpenRA.Mods.Common.Traits.Mobile>() != null || a.TraitOrDefault<OpenRA.Mods.Common.Traits.Building>() != null) &&
						a.TraitOrDefault<PlateauMovementProbe>() == null)
						.Select(a => a.Info.Name + "@" + a.Location + "/" + a.CenterPosition)));
				renderer.Viewport.Center(self.World.Map.CenterOfCell(info.ValidateHomeBase
					? self.World.LocalPlayer.HomeLocation + new CVec(3, -3) : info.Center));
				var map = self.World.Map;
				if (info.ReportCliffGeometry)
				{
					// Diagnostic projection, not a guessed screenshot-to-cell transform.
					// Run after recentering so annotations refer to the captured viewport.
					var report = new System.Text.StringBuilder();
					report.AppendLine(FormattableString.Invariant($"CLIFF VIEW window-scale={Game.Renderer.WindowScale} resolution={Game.Renderer.Resolution}"));
					int2 FramebufferPoint(int2 worldPoint)
					{
						var view = renderer.Viewport.WorldToViewPx(worldPoint);
						return new int2((int)Math.Round(view.X * Game.Renderer.WindowScale), (int)Math.Round(view.Y * Game.Renderer.WindowScale));
					}
					foreach (var cell in map.AllCells.Where(map.Contains).Where(c => Math.Abs(c.X - info.Center.X) <= 12 && Math.Abs(c.Y - info.Center.Y) <= 12))
					{
						var center = renderer.ScreenPxPosition(map.CenterOfCell(cell));
						var cellPoint = FramebufferPoint(center);
						report.AppendLine($"CLIFF CELL {cell.X},{cell.Y} {map.Tiles[cell].Type} {map.Height[cell]} {map.Ramp[cell]} {cellPoint.X},{cellPoint.Y}");
						if (map.Height[cell] != 0 || map.Tiles[cell].Type != 3992) continue;
						var points = new[] { int2.Zero, new int2(0, -32), new int2(64, 0), new int2(0, 32), new int2(-64, 0) }
							.Select(d => FramebufferPoint(center + d)).Select(p => $"{p.X},{p.Y}");
						report.AppendLine($"CLIFF FOOT {cell.X},{cell.Y} {string.Join(" ", points)}");
					}
					foreach (var actor in self.World.Actors.Where(a => a.Info.HasTraitInfo<NativeCliffBodyInfo>() || a.Info.HasTraitInfo<PlateauFaceBodyInfo>())
						.Where(a => Math.Abs(a.Location.X - info.Center.X) <= 12 && Math.Abs(a.Location.Y - info.Center.Y) <= 12))
					{
						var point = FramebufferPoint(renderer.ScreenPxPosition(actor.CenterPosition));
						report.AppendLine($"CLIFF ACTOR {actor.Info.Name} {actor.Location.X},{actor.Location.Y} {point.X},{point.Y}");
					}
					PlateauMovementProbe.WriteResult(info.ResultPath, report.ToString().TrimEnd());
				}
				var topCell = new MPos(map.Bounds.Left, map.Bounds.Top).ToCPos(map);
				Log.Write("debug", "TERRAIN BOUNDS " + map.Bounds + " top-cell=" + topCell + " top-screen=" +
					renderer.Viewport.WorldToViewPx(renderer.ScreenPxPosition(map.CenterOfCell(topCell))));
				Game.TakeScreenshot();
			}
		}

		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr)
		{
			renderer = wr;
			if (info.MinimumZoomScale > 0 && info.MinimumZoomScale < 1)
				wr.Viewport.UnlockMinimumZoom(info.MinimumZoomScale);
			if (info.ZoomScale > 0)
				wr.Viewport.AdjustZoom((float)Math.Log(info.ZoomScale / wr.Viewport.Zoom));
			else
				wr.Viewport.AdjustZoom(info.ZoomSteps);
			wr.Viewport.Center(world.Map.CenterOfCell(info.Center));
		}
	}
}
