using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class BuildingUiFollowupProbeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new BuildingUiFollowupProbe();
	}

	// Disposable native fixture only: normal creations, no SkipMakeAnimsInit.
	public sealed class BuildingUiFollowupProbe : ITick, IPostWorldLoaded
	{
		WorldRenderer wr;
		int ticks;
		readonly List<Actor> yards = new();
		CPos previewCell;
		bool showPreview;
		bool finished;
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer renderer) { wr = renderer; }
		void ITick.Tick(Actor self)
		{
			if (Environment.GetEnvironmentVariable("YMCA_BUILDING_UI_REVIEW") != "1") return;
			ticks++;
			if (ticks == 30)
			{
				foreach (var name in new[] { "syrd", "spen", "spen.nod", "syrd.gdi", "chsyrd" })
				{
					var old = self.World.Actors.Single(a => a.Info.Name == name);
					if (old.Info.HasTraitInfo<WithMakeAnimationInfo>()) throw new InvalidDataException(name + " still has a make animation.");
					var cell = old.Location;
					old.Dispose();
					self.World.AddFrameEndTask(w => yards.Add(w.CreateActor(name, new TypeDictionary { new OwnerInit(w.LocalPlayer), new LocationInit(cell) })));
				}
			}

			if (ticks >= 60 && ticks <= 220 && (ticks - 60) % 40 == 0)
			{
				var yard = yards[(ticks - 60) / 40];
				wr.Viewport.UnlockMinimumZoom(.1f);
				wr.Viewport.AdjustZoom((float)Math.Log(1f / wr.Viewport.Zoom));
				wr.Viewport.Center(yard.CenterPosition);
				if (!yard.IsInWorld || yard.IsDead || yard.TraitsImplementing<WithSpriteBody>().Any(b => b.DefaultAnimation.CurrentSequence.Name == "construction" || b.DefaultAnimation.CurrentSequence.Name == "make"))
					throw new InvalidDataException(yard.Info.Name + " did not enter its ordinary body state.");
			}
			if (ticks >= 70 && ticks <= 230 && (ticks - 70) % 40 == 0) Game.TakeScreenshot();
			if (ticks == 250)
			{
				var target = self.World.Actors.First(a => a.Info.Name == "chcommand");
				previewCell = target.Location + new CVec(1, 1);
				var queue = self.World.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>().Single(q => q.Info.Type == "Building");
				Sync.RunUnsynced(self.World, () => self.World.OrderGenerator = new PlaceBuildingOrderGenerator(queue, "chcommand.radar", wr, true));
				wr.Viewport.Center(target.CenterPosition);
				showPreview = true;
			}
			if (showPreview) Viewport.LastMousePos = wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(self.World.Map.CenterOfCell(previewCell)));
			if (ticks == 270) Game.TakeScreenshot();
			if (ticks >= 310 && !finished)
			{
				var files = Directory.GetFiles(Path.Combine(Platform.SupportDir, "Screenshots"), "*.png", SearchOption.AllDirectories);
				if (files.Length < 6 || files.Any(p => !File.ReadAllBytes(p).TakeLast(12).SequenceEqual(new byte[] { 0,0,0,0,73,69,78,68,174,66,96,130 }))) return;
				finished = true;
				File.WriteAllText(Path.Combine(Platform.SupportDir, "building-ui-followup.json"), JsonConvert.SerializeObject(new { Passed = true, NavalActors = yards.Select(a => a.Info.Name).ToArray(), RadarPreview = "PlaceBuildingOrderGenerator", PreviewFile = "bits/misc/overlays/china-radar-preview.png" }, Formatting.Indented));
			}
		}

	}
}
