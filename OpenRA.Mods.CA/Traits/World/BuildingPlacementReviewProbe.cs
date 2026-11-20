using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class BuildingPlacementReviewProbeInfo : TraitInfo
	{
		public readonly string ActorType = "tpwr";
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new BuildingPlacementReviewProbe(this);
	}

	public sealed class BuildingPlacementReviewProbe : ITickRender
	{
		readonly BuildingPlacementReviewProbeInfo info;
		int frames;

		public BuildingPlacementReviewProbe(BuildingPlacementReviewProbeInfo info)
		{
			if (Environment.GetEnvironmentVariable("YMCA_BUILDING_REVIEW_PROBE") != "1")
				throw new InvalidOperationException("Diagnostic opt-in required.");

			this.info = info;
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			frames++;
			var world = self.World;
			var actor = world.Actors.Single(a => a.Info.Name == info.ActorType);
			wr.Viewport.UnlockMinimumZoom(.1f);
			wr.Viewport.AdjustZoom((float)Math.Log((frames < 150 ? 1f : .5f) / wr.Viewport.Zoom));
			wr.Viewport.Center(actor.CenterPosition);
			world.Selection.Combine(world, new[] { actor }, false, false);
			if (frames == 1)
			{
				Ui.Root.Get("INTERACTION_CONTROLLER").IsVisible = () => false;
				Game.Settings.Game.SelectionBoxMinimumZoom = 0;
				var building = actor.Info.TraitInfo<BuildingInfo>();
				File.AppendAllText(info.ResultPath,
					$"{actor.Info.Name} at {actor.Location}: dimensions={building.Dimensions}, center={building.LocalCenterOffset}\n");
			}

			if (frames == 100 || frames == 200)
				Game.TakeScreenshot();

			if (frames == 230)
				File.AppendAllText(info.ResultPath, "PASS: real building display actor loaded and native detail/context screenshots captured.\n");
		}
	}
}
