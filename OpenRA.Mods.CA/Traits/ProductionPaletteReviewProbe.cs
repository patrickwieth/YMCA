using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public class ProductionPaletteReviewProbeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new ProductionPaletteReviewProbe();
	}

	// Isolated UI diagnostic only; never attached to production World rules.
	public class ProductionPaletteReviewProbe : ITick
	{
		int frames;
		int remaining;
		bool scrolled;
		bool cancelled;
		int2 first;

		static MouseInput Mouse(MouseInputEvent ev, int2 pos, MouseButton button = MouseButton.None, int scroll = 0)
			=> new MouseInput(ev, button, pos, new int2(0, scroll), Modifiers.None, 1);

		static void Require(bool ok, string message)
		{
			if (!ok) throw new InvalidOperationException("Production icon review: " + message);
		}

		void ITick.Tick(Actor self)
		{
			if (Environment.GetEnvironmentVariable("YMCA_ICON_REVIEW") != "1") return;
			frames++;
			if (frames == 1 || frames == 40 || frames == 120 || frames == 240)
				File.WriteAllText(Path.Combine(Platform.SupportDir, "production-icon-review-progress.txt"), frames.ToString());
			var palette = Ui.Root.Get<ProductionPaletteWidget>("PRODUCTION_PALETTE");
			if (frames == 40)
			{
				Require(palette.IconSize == new int2(96, 72) && palette.FitIconsToCell, "cell configuration");
				Require(palette.CurrentQueue != null, "no actual production queue");
				for (var i = 0; i < palette.DisplayedIconCount; i++)
				{
					var top = palette.RenderOrigin + new int2(i % palette.Columns * 97, i / palette.Columns * 73);
					palette.HandleMouseInput(Mouse(MouseInputEvent.Move, top + new int2(1, 1)));
					var icon = palette.TooltipIcon;
					Require(icon != null, "top-left hit box");
					palette.HandleMouseInput(Mouse(MouseInputEvent.Move, top + new int2(95, 71)));
					Require(palette.TooltipIcon == icon, "bottom-right hit box");
					Require(top.Y + 72 <= Game.Renderer.Resolution.Height - 8, "row exceeds screen");
					if (icon.Name == "powr" || icon.Name == "apwr")
						Require(icon.Sprite.Size.X == 192 && icon.Sprite.Size.Y == 144, "resolved HD icon");
				}

				first = palette.RenderOrigin + new int2(48, 36);
				if (palette.CanScrollDown)
				{
					var old = palette.IconRowOffset;
					palette.HandleMouseInput(Mouse(MouseInputEvent.Scroll, first, scroll: -1));
					Require(palette.IconRowOffset == old + 1, "scroll down");
					palette.HandleMouseInput(Mouse(MouseInputEvent.Scroll, first, scroll: 1));
					Require(palette.IconRowOffset == old, "scroll up");
					scrolled = true;
				}
			}
			else if (frames == 60)
			{
				palette.HandleMouseInput(Mouse(MouseInputEvent.Move, first));
				Require(palette.TooltipIcon.Name == "powr", "wrong first production item");
				Require(palette.HandleMouseInput(Mouse(MouseInputEvent.Down, first, MouseButton.Left)), "production click");
			}
			else if (frames == 120)
			{
				var item = palette.CurrentQueue.AllQueued().FirstOrDefault(q => q.Item == "powr");
				Require(item != null && item.RemainingTime > 0 && item.RemainingTime < item.TotalTime,
					"actual progress: " + (item == null ? "no queued powr" : $"remaining={item.RemainingTime}, total={item.TotalTime}, paused={item.Paused}"));
				cancelled = palette.CurrentQueue.Info.DisallowPaused || item.TotalCost == item.RemainingCost || !item.Started;
				remaining = item.TotalTime;
				palette.HandleMouseInput(Mouse(MouseInputEvent.Down, first, MouseButton.Right));
			}
			else if (frames == 160)
			{
				var item = palette.CurrentQueue.AllQueued().FirstOrDefault(q => q.Item == "powr");
				if (cancelled) Require(item == null, "right-click cancel");
				else
				{
					Require(item != null && item.Paused, "right-click pause");
					remaining = item.RemainingTime;
				}
				palette.HandleMouseInput(Mouse(MouseInputEvent.Down, first, MouseButton.Left));
			}
			else if (frames == 240)
			{
				var item = palette.CurrentQueue.AllQueued().First(q => q.Item == "powr");
				Require(!item.Paused && item.RemainingTime < remaining, "left-click resume/restart");
				File.WriteAllText(Path.Combine(Platform.SupportDir, "production-icon-review.json"),
					JsonConvert.SerializeObject(new { Passed = true, IconSize = new[] { 96, 72 },
						SourceSize = new[] { 192, 144 }, ActualUIScale = Game.Renderer.WindowScale,
						Hitboxes = true, ProductionClicksAndProgress = true, RightClickCancels = cancelled, Scrolled = scrolled }));
				Game.TakeScreenshot();
			}
		}
	}
}
