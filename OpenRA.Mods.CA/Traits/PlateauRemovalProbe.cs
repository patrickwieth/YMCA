using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.CA.Widgets.Logic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public class PlateauRemovalProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly CPos Cell;
		public override object Create(ActorInitializer init) => new PlateauRemovalProbe(this);
	}

	// Diagnostic only: real editor, real actor layer, real history manager, production terrain.
	public class PlateauRemovalProbe : IPostWorldLoaded, ITickRender
	{
		readonly PlateauRemovalProbeInfo info;
		int frames;
		string before;
		string lowered;
		PlateauRemovalPlan plan;
		public PlateauRemovalProbe(PlateauRemovalProbeInfo info) { this.info = info; }
		public void PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
		}
		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 210) return;
			var map = self.World.Map;
			var layer = self.Trait<EditorActorLayer>();
			var manager = self.Trait<EditorActionManager>();
			var controller = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			void Check(string expected, string operation)
			{
				if (PlateauEditorProbe.Snapshot(map, layer) != expected) throw new InvalidOperationException("Removal transaction mismatch: " + operation);
				PlateauMovementProbe.WriteResult(info.ResultPath, "REMOVAL PASS " + operation);
			}
			if (frames == 20)
			{
				plan = PlateauRemovalPlan.Create(map, layer.Save(), info.Cell);
				var center = new CPos((int)plan.Patch.Keys.Average(c => c.X), (int)plan.Patch.Keys.Average(c => c.Y));
				wr.Viewport.UnlockMinimumZoom(0.1f);
				wr.Viewport.AdjustZoom((float)Math.Log(0.4f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(center));
				Ui.Root.Get<ButtonWidget>("PLATEAU_LOWER").OnClick();
				Viewport.LastMousePos = wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(info.Cell)));
				controller.CurrentBrush.TickRender(wr, self);
				Game.TakeScreenshot();
			}
			if (frames == 30)
			{
				before = PlateauEditorProbe.Snapshot(map, layer);
				plan = PlateauRemovalPlan.Create(map, layer.Save(), info.Cell);
				Ui.Root.Get<ButtonWidget>("PLATEAU_LOWER").OnClick();
				var brush = controller.CurrentBrush as PlateauRemovalBrush ?? throw new InvalidOperationException("Lower button did not activate brush.");
				// A resource added after hover must invalidate the action, not just its preview.
				map.Resources[info.Cell] = new ResourceTile(1, 5);
				var refused = false;
				try { brush.CreateAction(info.Cell); }
				catch (InvalidOperationException) { refused = true; }
				map.Resources[info.Cell] = default;
				if (!refused) throw new InvalidOperationException("Resource protection failed in actual editor.");
				Check(before, "resource protection");
				var actor = layer.Add("RemovalProtectedActor", new ActorReference("truk") { new LocationInit(info.Cell), new OwnerInit("Neutral") });
				refused = false;
				try { brush.CreateAction(info.Cell); }
				catch (InvalidOperationException) { refused = true; }
				layer.Remove(actor);
				if (!refused) throw new InvalidOperationException("Actor protection failed in actual editor.");
				Check(before, "actor protection");
				var anchor = plan.Clearance.OrderBy(c => c.X).ThenBy(c => c.Y).First() + new CVec(-1, 0);
				var building = layer.Add("RemovalFootprintActor", new ActorReference("proc") { new LocationInit(anchor), new OwnerInit("Neutral") });
				if (!building.Footprint.Keys.Any(c => plan.Clearance.Contains(c))) throw new InvalidOperationException("Footprint fixture does not overlap clearance.");
				PlateauRemovalPlan.Create(map, layer.Save(), info.Cell); // Anchor is outside; only the actual footprint overlaps.
				refused = false;
				try { brush.CreateAction(info.Cell); }
				catch (InvalidOperationException) { refused = true; }
				layer.Remove(building);
				if (!refused) throw new InvalidOperationException("Overlapping building footprint was accepted.");
				Check(before, "outside-anchor building footprint protection");
				var broken = plan.Decorations.Concat(new[] { new MiniYamlNode("MissingRemovalTestActor", new MiniYaml("terrain.rubberduck.cliff17")) }).ToArray();
				var failed = false;
				try { new PlateauTerrainEditAction(map, plan.Patch, PlateauActorEdit.Patch(layer, broken, true), "Injected removal failure").Do(); }
				catch (InvalidOperationException) { failed = true; }
				if (!failed) throw new InvalidOperationException("Actor failure injection did not fail.");
				Check(before, "partial actor removal rolls back terrain and actors");
				var pixel = wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(info.Cell)));
				brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left, pixel, int2.Zero, Modifiers.None, 1));
				controller.ClearBrush();
				foreach (var cell in plan.Patch.Keys)
					if (map.Height[cell] != 0 || map.Ramp[cell] != 0 || map.Tiles[cell].Type != 1000)
						throw new InvalidOperationException("Removal left raised or blocked terrain.");
				if (plan.Decorations.Any(n => layer[n.Key] != null)) throw new InvalidOperationException("Removal left cliff actors.");
				lowered = PlateauEditorProbe.Snapshot(map, layer);
				if (lowered == before) throw new InvalidOperationException("Removal made no change.");
				Check(lowered, "lower roof, ramps, feet and native actors");
			}
			if (frames == 60) { manager.Undo(); Check(before, "undo restores complete original map"); }
			if (frames == 90) { manager.Redo(); Check(lowered, "redo matches lowered map"); }
			if (frames == 120) { manager.Undo(); Check(before, "second undo exact"); }
			if (frames == 150) { manager.Redo(); Check(lowered, "second redo exact"); }
			if (frames == 180)
			{
				PlateauMovementProbe.WriteResult(info.ResultPath, "REMOVAL saving lowered fixture");
				map.ActorDefinitions = layer.Save();
				var path = Path.ChangeExtension(Path.GetFullPath(info.ResultPath), ".oramap");
				using (var package = ZipFileLoader.Create(path)) map.Save(package);
				using var stream = File.OpenRead(path);
				using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
				using var reloaded = new Map(Game.ModData, zip);
				if (reloaded.InvalidCustomRules) throw new InvalidOperationException("Lowered map rule reload failed.", reloaded.InvalidCustomRulesException);
				foreach (var c in map.AllCells)
					if (reloaded.Height[c] != map.Height[c] || reloaded.Ramp[c] != map.Ramp[c] || !reloaded.Tiles[c].Equals(map.Tiles[c]) || !reloaded.Resources[c].Equals(map.Resources[c]))
						throw new InvalidOperationException("Removal save changed terrain.");
				if (reloaded.ActorDefinitions.OrderBy(n => n.Key).WriteToString() != layer.Save().OrderBy(n => n.Key).WriteToString())
					throw new InvalidOperationException("Removal save changed actors.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "REMOVAL PASS save/reload lowered map");
			}
			if (frames == 210)
			{
				wr.Viewport.UnlockMinimumZoom(0.1f);
				wr.Viewport.AdjustZoom((float)Math.Log(0.4f / wr.Viewport.Zoom));
				var center = new CPos((int)plan.Patch.Keys.Average(c => c.X), (int)plan.Patch.Keys.Average(c => c.Y));
				wr.Viewport.Center(map.CenterOfCell(center));
				Game.TakeScreenshot();
			}
		}
	}
}
