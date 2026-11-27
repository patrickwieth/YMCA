using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public class AlliedBuildingIconReviewProbeInfo : TraitInfo
	{
		public readonly string[] Expected = { "fact", "fact.nukular", "powr", "apwr", "tent", "proc", "weap", "dome", "fix", "afld.allies", "hpad", "syrd", "atek", "orep", "pdox", "weat", "pris", "gap", "pbox", "hbox", "agun", "grand_cannon", "gun", "silo", "oilp", "oil_platform", "swiss_bank", "facf", "weaf", "domf", "syrf" };
		public readonly string[] AllowedLegacy = Array.Empty<string>();
		public override object Create(ActorInitializer init) => new AlliedBuildingIconReviewProbe(this);
	}

	// Added only by the isolated icon-gallery fixture, never by production World rules.
	public class AlliedBuildingIconReviewProbe : ITick
	{
		readonly AlliedBuildingIconReviewProbeInfo info;
		public AlliedBuildingIconReviewProbe(AlliedBuildingIconReviewProbeInfo info) { this.info = info; }
		int ticks;
		int group;
		bool finished;
		bool waitingForScreenshots;
		int screenshots;
		Action nextPage;
		readonly HashSet<string> seen = new();
		readonly string[] groups = { "Building", "Defense" };

		void ITick.Tick(Actor self)
		{
			if (finished || Environment.GetEnvironmentVariable("YMCA_ALLIED_ICON_REVIEW") != "1") return;
			if (waitingForScreenshots)
			{
				var paths = Directory.GetFiles(Path.Combine(Platform.SupportDir, "Screenshots"), "*.png", SearchOption.AllDirectories);
				if (paths.Length < screenshots) return;
				foreach (var path in paths)
				{
					try
					{
						using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
						if (stream.Length < 12) return;
						stream.Seek(-12, SeekOrigin.End);
						var end = new byte[12];
						if (stream.Read(end, 0, 12) != 12 || !end.SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 })) return;
					}
					catch (IOException) { return; }
				}

				finished = true;
				File.WriteAllText(Path.Combine(Platform.SupportDir, "allied-icon-review.json"), JsonConvert.SerializeObject(new { Passed = true, ActualUIScale = Game.Renderer.WindowScale, Screenshots = screenshots, Actors = seen.OrderBy(s => s).ToArray() }, Formatting.Indented));
				return;
			}

			ticks++;
			if (ticks % 40 == 10 && nextPage != null)
			{
				var action = nextPage;
				nextPage = null;
				action();
				return;
			}

			var palette = Ui.Root.Get<ProductionPaletteWidget>("PRODUCTION_PALETTE");
			void SelectQueue()
			{
				palette.IconRowOffset = 0;
				palette.CurrentQueue = self.World.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>().Single(q => q.Info.Type == groups[group]);
			}
			if (ticks == 40) SelectQueue();
			if (ticks < 80 || ticks % 40 != 0) return;
			for (var i = 0; i < palette.DisplayedIconCount; i++)
			{
				var point = palette.RenderOrigin + new int2(i % palette.Columns * 97 + 48, i / palette.Columns * 73 + 36);
				palette.HandleMouseInput(new MouseInput(MouseInputEvent.Move, MouseButton.None, point, int2.Zero, Modifiers.None, 1));
				var icon = palette.TooltipIcon;
				if (icon == null || (self.World.Map.Rules.Actors[icon.Name].HasTraitInfo<BuildingInfo>() && !info.AllowedLegacy.Contains(icon.Name) && (icon.Sprite.Size.X != 192 || icon.Sprite.Size.Y != 144 || icon.ButtonLabelFont != "OsShp")))
					throw new InvalidDataException("Wrong native Allied cameo: " + icon?.Name);
				seen.Add(icon.Name);
			}

			Game.TakeScreenshot();
			screenshots++;
			if (palette.CanScrollDown)
			{
				// Defer scrolling until the screenshot has been rendered.
				nextPage = palette.ScrollDown;
				return;
			}

			if (++group < groups.Length)
			{
				nextPage = SelectQueue;
				return;
			}

			var missing = info.Expected.Except(seen).ToArray();
			if (missing.Length != 0) throw new InvalidDataException("Missing Allied gallery icons: " + string.Join(",", missing));
			waitingForScreenshots = true;
		}
	}
}
