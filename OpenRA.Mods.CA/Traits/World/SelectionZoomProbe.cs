using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class SelectionZoomProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new SelectionZoomProbe(this);
	}

	public sealed class SelectionZoomProbe : ITickRender
	{
		readonly SelectionZoomProbeInfo info;
		int frames;
		Widget settings;
		public SelectionZoomProbe(SelectionZoomProbeInfo info)
		{
			if (Environment.GetEnvironmentVariable("YMCA_SELECTION_ZOOM_PROBE") != "1") throw new InvalidOperationException("Diagnostic opt-in required.");
			this.info = info;
		}
		static IEnumerable<Widget> Descendants(Widget widget)
		{
			yield return widget;
			foreach (var child in widget.Children)
				foreach (var nested in Descendants(child)) yield return nested;
		}
		void Log(string text) => File.AppendAllText(info.ResultPath,text+"\n");
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			frames++;
			var world = self.World;
			wr.Viewport.Center(world.Map.CenterOfCell(new CPos(110,-25)));
			wr.Viewport.UnlockMinimumZoom(.1f);
			wr.Viewport.AdjustZoom((float)Math.Log((frames < 180 ? .35f : 1f) / wr.Viewport.Zoom));
			if (frames == 1)
			{
				// Ignore desktop clicks on this owned fixture; do not alter any other client.
				Ui.Root.Get("INTERACTION_CONTROLLER").IsVisible = () => false;
				Game.Settings.Game.SelectionBoxMinimumZoom = 1f;
				var units = world.Actors.Where(a=>a.Owner==world.LocalPlayer && a.Info.HasTraitInfo<ISelectableInfo>()).ToArray();
				world.Selection.Combine(world,units,false,false);
				Log("SELECTED "+units.Length);
			}
			if (frames == 100) { Game.TakeScreenshot(); Log("FAR_HIDDEN zoom="+wr.Viewport.Zoom); }
			if (frames == 120) Game.Settings.Game.SelectionBoxMinimumZoom = 0;
			if (frames == 160) { Game.TakeScreenshot(); Log("FAR_SHOWN zoom="+wr.Viewport.Zoom); }
			if (frames == 180)
			{
				Game.Settings.Game.SelectionBoxMinimumZoom = 1f;
				wr.Viewport.AdjustZoom((float)Math.Log(1f / wr.Viewport.Zoom));
			}
			if (frames == 220) { Game.TakeScreenshot(); Log("CLOSE_SHOWN zoom="+wr.Viewport.Zoom); }
			if (frames == 240)
			{
				settings = Ui.OpenWindow("SETTINGS_PANEL", new WidgetArgs
				{
					{ "world", world }, { "worldRenderer", wr }, { "onExit", (Action)(()=>{}) }
				});
				settings.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL").ScrollToItem("SELECTION_BOX_ZOOM_ROW");
				settings.Get<SliderWidget>("SELECTION_BOX_ZOOM_SLIDER").UpdateValue(.75f);
				if (Game.Settings.Game.SelectionBoxMinimumZoom != .75f) throw new InvalidOperationException("Slider did not update live setting.");
				Log("SLIDER "+settings.Get<LabelWidget>("SELECTION_BOX_ZOOM_VALUE").GetText());
			}
			if (frames == 270) Game.TakeScreenshot();
			if (frames == 300)
			{
				Game.Settings.Save();
				var panel = Descendants(settings).Single(w => w.LogicObjects != null && w.LogicObjects.Any(l => l is DisplaySettingsLogic));
				var logic = panel.LogicObjects.OfType<DisplaySettingsLogic>().Single();
				var reset = (Action)typeof(DisplaySettingsLogic).GetMethod("ResetPanel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(logic,new object[] { panel });
				reset();
				if (Game.Settings.Game.SelectionBoxMinimumZoom != 1f || settings.Get<SliderWidget>("SELECTION_BOX_ZOOM_SLIDER").GetValue() != 1f)
					throw new InvalidOperationException("Display reset did not reset slider/setting.");
				Log("RESET_PASS");
				Log("FINAL_SELECTED "+world.Selection.Actors.Count());
				if (world.Selection.Actors.Count() != 8) throw new InvalidOperationException("Selection changed while hiding frames.");
				Log("PASS: selection retained; far/close/always-show captured; real settings slider and reset verified.");
			}
		}
	}
}
