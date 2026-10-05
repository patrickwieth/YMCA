using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.UtilityCommands;
using OpenRA.Mods.CA.Widgets.Logic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public class PlateauRampMoveProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new PlateauRampMoveProbe(this);
	}
	public class PlateauRampMoveProbe : IPostWorldLoaded, ITickRender
	{
		readonly PlateauRampMoveProbeInfo info;
		int frames;
		string before;
		string after;
		public PlateauRampMoveProbe(PlateauRampMoveProbeInfo info) { this.info = info; }
		public void PostWorldLoaded(World w, WorldRenderer wr)
		{ if (w.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(w.Map.Uid)); }
		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 210) return;
			var map = self.World.Map; var layer = self.Trait<EditorActorLayer>(); var history = self.Trait<EditorActionManager>();
			var source = RubberduckRampMoveTestCommand.Source(map); var target = RubberduckRampMoveTestCommand.Target(map);
			void Check(string expected, string label)
			{
				if (PlateauEditorProbe.Snapshot(map, layer) != expected) throw new InvalidOperationException("Ramp transaction mismatch: " + label);
				PlateauMovementProbe.WriteResult(info.ResultPath, "RAMP MOVE PASS " + label);
			}
			if (frames == 20)
			{
				wr.Viewport.UnlockMinimumZoom(0.1f); wr.Viewport.AdjustZoom((float)Math.Log(0.4f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(RubberduckRampMoveTestCommand.Center(map) + new CVec(9, -6)));
				Game.TakeScreenshot();
			}
			if (frames == 30)
			{
				before = PlateauEditorProbe.Snapshot(map, layer);
				Ui.Root.Get<ButtonWidget>("PLATEAU_RAMP").OnClick();
				var controller = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR");
				var brush = controller.CurrentBrush as PlateauRampBrush ?? throw new InvalidOperationException("Move-ramp button failed.");
				void Click(CPos c) => brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left,
					wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(c))), int2.Zero, Modifiers.None, 1));
				Click(source - new CVec(2, 0));
				Check(before, "selection is non-mutating");
				brush.HandleMouseInput(new MouseInput(MouseInputEvent.Up, MouseButton.Right, int2.Zero, int2.Zero, Modifiers.None, 1));
				Check(before, "cancel selection");
				Ui.Root.Get<ButtonWidget>("PLATEAU_RAMP").OnClick();
				brush = controller.CurrentBrush as PlateauRampBrush ?? throw new InvalidOperationException("Move-ramp reactivation failed.");
				Click(source - new CVec(2, 0));
				map.Height[source] = 1;
				var staleRefused = false;
				try { brush.CreateAction(target); } catch (InvalidOperationException) { staleRefused = true; }
				map.Height[source] = 0;
				if (!staleRefused) throw new InvalidOperationException("Changed ramp selection was accepted.");
				Check(before, "source is revalidated");
				map.Resources[target] = new ResourceTile(1, 5);
				var refused = false;
				try { brush.CreateAction(target); } catch (InvalidOperationException) { refused = true; }
				map.Resources[target] = default;
				if (!refused) throw new InvalidOperationException("Destination resource was not protected.");
				Check(before, "destination resource protection");
				history.Add(brush.CreateAction(source + new CVec(0, 1)));
				if (map.Ramp[source + new CVec(0, 1)] != 3 || map.Tiles[source + new CVec(0, -2)].Type != 1000)
					throw new InvalidOperationException("Overlapping shift did not restore the released retaining strip.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "RAMP MOVE PASS overlapping one-cell shift");
				history.Undo(); Check(before, "undo overlapping shift");
				Click(target); controller.ClearBrush();
				if (map.Ramp[source] != 0 || map.Height[source] != 4 || map.Ramp[target] != 2 || map.Height[target] != 0)
					throw new InvalidOperationException("Two-click relocation failed.");
				after = PlateauEditorProbe.Snapshot(map, layer); Check(after, "move and rotate with cliff replan");
			}
			if (frames == 60) { history.Undo(); Check(before, "undo original ramp and actors"); }
			if (frames == 90) { history.Redo(); Check(after, "redo moved ramp and actors"); }
			if (frames == 120) { history.Undo(); Check(before, "second undo"); }
			if (frames == 150) { history.Redo(); Check(after, "second redo"); }
			if (frames == 160)
			{
				history.Undo(); Check(before, "reset before independent add/close");
				var controller = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR");
				Ui.Root.Get<ButtonWidget>("PLATEAU_RAMP_ADD").OnClick();
				var brush = controller.CurrentBrush as PlateauRampBrush ?? throw new InvalidOperationException("Add-ramp button failed.");
				void Click(CPos c) => brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left,
					wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(c))), int2.Zero, Modifiers.None, 1));
				map.Resources[target] = new ResourceTile(1, 5);
				var refused = false;
				try { brush.CreateAction(target); } catch (InvalidOperationException) { refused = true; }
				map.Resources[target] = default;
				if (!refused) throw new InvalidOperationException("Add-ramp resource protection failed.");
				Check(before, "add ramp protects resources");
				Click(target);
				if (map.Ramp[target] != 2 || map.AllCells.Count(c => map.Ramp[c] != 0) != 60)
					throw new InvalidOperationException("Additional ramp did not preserve original accesses.");
				var added = PlateauEditorProbe.Snapshot(map, layer); Check(added, "mouse adds fifth ramp");
				history.Undo(); Check(before, "undo add"); history.Redo(); Check(added, "redo add");
				Ui.Root.Get<ButtonWidget>("PLATEAU_RAMP_CLOSE").OnClick();
				brush = controller.CurrentBrush as PlateauRampBrush ?? throw new InvalidOperationException("Close-ramp button failed.");
				map.Resources[source] = new ResourceTile(1, 5); refused = false;
				try { brush.CreateAction(source); } catch (InvalidOperationException) { refused = true; }
				map.Resources[source] = default;
				if (!refused) throw new InvalidOperationException("Close-ramp resource protection failed.");
				Check(added, "close ramp protects resources");
				Click(source - new CVec(2, 0));
				if (map.Ramp[source] != 0 || map.Height[source] != 4 || map.AllCells.Count(c => map.Ramp[c] != 0) != 48)
					throw new InvalidOperationException("Mouse ramp closure failed.");
				after = PlateauEditorProbe.Snapshot(map, layer); Check(after, "mouse closes single ramp");
				history.Undo(); Check(added, "undo close"); history.Redo(); Check(after, "redo close");
				for (var d = 1; d < 4; d++)
					history.Add(brush.CreateAction(RubberduckRampMoveTestCommand.Center(map) + MapGeneration.PlateauTopology.Directions[d] * 12));
				var last = PlateauEditorProbe.Snapshot(map, layer); refused = false;
				try { brush.CreateAction(target); } catch (InvalidOperationException) { refused = true; }
				if (!refused) throw new InvalidOperationException("Closing last ramp was accepted in editor.");
				Check(last, "last accessible ramp rejected without mutation");
				for (var d = 1; d < 4; d++) history.Undo();
				Check(after, "restore retained accesses"); controller.ClearBrush();
			}
			if (frames == 180)
			{
				PlateauMovementProbe.WriteResult(info.ResultPath, "RAMP MOVE saving fixture");
				map.ActorDefinitions = layer.Save();
				var path = Path.ChangeExtension(Path.GetFullPath(info.ResultPath), ".oramap");
				using (var package = ZipFileLoader.Create(path)) map.Save(package);
				using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
				using var reload = new Map(Game.ModData, zip);
				if (reload.InvalidCustomRules) throw new InvalidOperationException("Ramp edit rules failed reload.", reload.InvalidCustomRulesException);
				foreach (var c in map.AllCells)
					if (map.Height[c] != reload.Height[c] || map.Ramp[c] != reload.Ramp[c] || !map.Tiles[c].Equals(reload.Tiles[c]) || !map.Resources[c].Equals(reload.Resources[c]))
						throw new InvalidOperationException("Ramp edit terrain changed during save/reload.");
				if (map.ActorDefinitions.OrderBy(n => n.Key).WriteToString() != reload.ActorDefinitions.OrderBy(n => n.Key).WriteToString())
					throw new InvalidOperationException("Ramp edit actors changed during save/reload.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "RAMP MOVE PASS save/reload");
			}
			if (frames == 210) Game.TakeScreenshot();
		}
	}
}
