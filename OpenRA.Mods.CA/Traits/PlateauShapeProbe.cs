using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.CA.UtilityCommands;
using OpenRA.Mods.CA.Widgets.Logic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public class PlateauShapeProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new PlateauShapeProbe(this);
	}
	public class PlateauShapeProbe : IPostWorldLoaded, ITickRender
	{
		readonly PlateauShapeProbeInfo info;
		int frames;
		string before;
		string joined;
		string shaped;
		public PlateauShapeProbe(PlateauShapeProbeInfo info) { this.info = info; }
		public void PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
		}
		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 300) return;
			var map = self.World.Map;
			var layer = self.Trait<EditorActorLayer>();
			var history = self.Trait<EditorActionManager>();
			var controller = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			var first = RubberduckPlateauShapeTestCommand.First(map);
			void Check(string expected, string label)
			{
				if (PlateauEditorProbe.Snapshot(map, layer) != expected) throw new InvalidOperationException("Shape transaction mismatch: " + label);
				PlateauMovementProbe.WriteResult(info.ResultPath, "SHAPE PASS " + label);
			}
			MouseInput Mouse(CPos c, MouseInputEvent ev) => new(ev, MouseButton.Left,
				wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(c))), int2.Zero, Modifiers.None, 1);
			if (frames == 20)
			{
				wr.Viewport.UnlockMinimumZoom(0.1f); wr.Viewport.AdjustZoom((float)Math.Log(0.35f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(first + new CVec(12, 0)));
				Game.TakeScreenshot();
			}
			if (frames == 30)
			{
				before = PlateauEditorProbe.Snapshot(map, layer);
				Ui.Root.Get<ButtonWidget>("PLATEAU_GROW").OnClick();
				var canceled = controller.CurrentBrush as PlateauShapeBrush ?? throw new InvalidOperationException("Grow button did not activate brush.");
				canceled.HandleMouseInput(Mouse(first + new CVec(5, 6), MouseInputEvent.Down));
				canceled.HandleMouseInput(Mouse(first + new CVec(19, 6), MouseInputEvent.Move));
				canceled.HandleMouseInput(new MouseInput(MouseInputEvent.Up, MouseButton.Right, int2.Zero, int2.Zero, Modifiers.None, 1));
				Check(before, "canceled stroke leaves no changes");
				Ui.Root.Get<ButtonWidget>("PLATEAU_GROW").OnClick();
				var brush = controller.CurrentBrush as PlateauShapeBrush ?? throw new InvalidOperationException("Grow button did not activate brush.");
				var plan = brush.Plan(RubberduckPlateauShapeTestCommand.Bridge(map));
				if (plan.NativePieces == 0) throw new InvalidOperationException("Join has no native pieces.");
				var failure = false;
				try
				{
					var broken = plan.AfterActors.Append(layer.Save().First(n => n.Key == "Spawn0")).ToArray();
					new PlateauTerrainEditAction(map, plan.Patch, PlateauActorEdit.Replace(layer, plan.BeforeActors, broken), "Injected replacement failure").Do();
				}
				catch (InvalidOperationException) { failure = true; }
				if (!failure) throw new InvalidOperationException("Cliff replacement failure injection did not fail.");
				Check(before, "failed cliff replacement rolls back all changes");
				brush.HandleMouseInput(Mouse(first + new CVec(5, 6), MouseInputEvent.Down));
				brush.HandleMouseInput(Mouse(first + new CVec(19, 6), MouseInputEvent.Move));
				Check(before, "drag is non-mutating until release");
				var guarded = first + new CVec(12, 6);
				map.Resources[guarded] = new ResourceTile(1, 5);
				brush.HandleMouseInput(Mouse(first + new CVec(19, 6), MouseInputEvent.Up));
				map.Resources[guarded] = default;
				Check(before, "release revalidates newly protected cells");
				brush.HandleMouseInput(Mouse(first + new CVec(5, 6), MouseInputEvent.Down));
				brush.HandleMouseInput(Mouse(first + new CVec(19, 6), MouseInputEvent.Move));
				brush.HandleMouseInput(Mouse(first + new CVec(19, 6), MouseInputEvent.Up));
				controller.ClearBrush();
				if (RubberduckPlateauShapeTestCommand.Bridge(map).Any(c => map.Height[c] != 4 || map.Ramp[c] != 0))
					throw new InvalidOperationException("Mouse stroke did not join both plateaus.");
				joined = PlateauEditorProbe.Snapshot(map, layer);
				if (joined == before) throw new InvalidOperationException("Join did not change map.");
				Check(joined, "join with native cliff replan");
			}
			if (frames == 60) { history.Undo(); Check(before, "undo entire stroke"); }
			if (frames == 90) { history.Redo(); Check(joined, "redo entire stroke"); }
			if (frames == 120)
			{
				Ui.Root.Get<ButtonWidget>("PLATEAU_TRIM").OnClick();
				var brush = controller.CurrentBrush as PlateauShapeBrush ?? throw new InvalidOperationException("Trim button did not activate brush.");
				var refused = false;
				try { brush.CreateAction(new[] { first }); }
				catch (InvalidOperationException) { refused = true; }
				if (!refused) throw new InvalidOperationException("Resource-bearing trim accepted.");
				Check(joined, "resource protection on occupied roof");
				brush.HandleMouseInput(Mouse(first + new CVec(-6, -6), MouseInputEvent.Down));
				brush.HandleMouseInput(Mouse(first + new CVec(-6, -6), MouseInputEvent.Up));
				controller.ClearBrush();
				if (RubberduckPlateauShapeTestCommand.Trim(map).Any(c => map.Height[c] != 0)) throw new InvalidOperationException("Trim did not lower its roof corner.");
				shaped = PlateauEditorProbe.Snapshot(map, layer);
				Check(shaped, "trim and native corner replan");
			}
			if (frames == 150) { history.Undo(); Check(joined, "undo trim"); }
			if (frames == 180) { history.Undo(); Check(before, "undo join after trim"); }
			if (frames == 210) { history.Redo(); Check(joined, "redo join after trim"); }
			if (frames == 240) { history.Redo(); Check(shaped, "redo trim"); }
			if (frames == 270)
			{
				PlateauMovementProbe.WriteResult(info.ResultPath, "SHAPE saving edited fixture");
				map.ActorDefinitions = layer.Save();
				var path = Path.ChangeExtension(Path.GetFullPath(info.ResultPath), ".oramap");
				using (var package = ZipFileLoader.Create(path)) map.Save(package);
				using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
				using var reload = new Map(Game.ModData, zip);
				if (reload.InvalidCustomRules) throw new InvalidOperationException("Shaped map rules failed reload.", reload.InvalidCustomRulesException);
				foreach (var c in map.AllCells)
					if (map.Height[c] != reload.Height[c] || map.Ramp[c] != reload.Ramp[c] || !map.Tiles[c].Equals(reload.Tiles[c]) || !map.Resources[c].Equals(reload.Resources[c]))
						throw new InvalidOperationException("Shaped terrain changed on save/reload.");
				if (map.ActorDefinitions.OrderBy(n => n.Key).WriteToString() != reload.ActorDefinitions.OrderBy(n => n.Key).WriteToString())
					throw new InvalidOperationException("Shaped actor definitions changed on save/reload.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "SHAPE PASS save/reload");
			}
			if (frames == 300) Game.TakeScreenshot();
		}
	}
}
