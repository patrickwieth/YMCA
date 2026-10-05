using System;
using System.IO;
using System.Linq;
using System.Text;
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
	public class PlateauEditorProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new PlateauEditorProbe(this);
	}
	// Only installed through diagnostic map rules. Uses the actual brush/action manager.
	public class PlateauEditorProbe : IPostWorldLoaded, ITickRender
	{
		// Diagnostic terrain snapshots must not replace the live map's package:
		// disposing that output would break subsequent lazy sound/sprite loads.
		internal static void SaveTerrainCopy(Map map, string path)
		{
			using var copy = new Map(Game.ModData, map.Package);
			foreach (var c in map.AllCells)
			{
				copy.Tiles[c] = map.Tiles[c]; copy.Height[c] = map.Height[c]; copy.Resources[c] = map.Resources[c];
			}
			copy.ActorDefinitions = map.ActorDefinitions;
			using (var package = ZipFileLoader.Create(path)) copy.Save(package);
			if (!map.Package.Contains("map.yaml")) throw new InvalidOperationException("Saving a terrain snapshot invalidated the live package.");
		}

		readonly PlateauEditorProbeInfo info;
		int frames;
		string before;
		string placed;
		CPos center;
		public PlateauEditorProbe(PlateauEditorProbeInfo info) { this.info = info; }
		public void PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
			else center = new MPos(40, 48).ToCPos(world.Map);
		}
		internal static string Snapshot(Map map, EditorActorLayer layer)
		{
			var s = new StringBuilder();
			foreach (var c in map.AllCells)
				s.Append($"{map.Tiles[c].Type},{map.Tiles[c].Index},{map.Height[c]},{map.Ramp[c]},{map.Resources[c].Type},{map.Resources[c].Index};");
			return s.Append(layer.Save().OrderBy(n => n.Key, StringComparer.Ordinal).WriteToString()).ToString();
		}
		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 240) return;
			var map = self.World.Map;
			var layer = self.Trait<EditorActorLayer>();
			var manager = self.Trait<EditorActionManager>();
			var controller = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			void Check(string expected, string operation)
			{
				if (Snapshot(map, layer) != expected) throw new InvalidOperationException("Editor transaction mismatch: " + operation);
				PlateauMovementProbe.WriteResult(info.ResultPath, "EDITOR PLATEAU PASS " + operation);
			}
			if (frames == 10)
			{
				var baseline = Snapshot(map, layer);
				var topology = PlateauTopology.CalibrationPlateau(center);
				var definitions = new System.Collections.Generic.List<MiniYamlNode>();
				RubberduckPlateauRenderer.AddActors(map, topology, definitions);
				definitions = definitions.Select((n, i) => new MiniYamlNode($"EditorPlateau_{center.X}_{center.Y}_{i}", n.Value)).ToList();
				var legacy = new PlateauTerrainEditAction(map, topology,
					PlateauActorEdit.Patch(layer, definitions, false), "Legacy stamp compatibility");
				legacy.Do();
				var oldStamp = Snapshot(map, layer);
				var removal = new PlateauStampBrush(controller, wr, true, _ => { }).CreateAction(center);
				removal.Do();
				if (Snapshot(map, layer) != baseline) throw new InvalidOperationException("Legacy stamp deletion changed its original footprint.");
				removal.Undo();
				if (Snapshot(map, layer) != oldStamp) throw new InvalidOperationException("Legacy stamp deletion undo was not exact.");
				legacy.Undo(); Check(baseline, "legacy stamp removal and exact restoration");
			}
			if (frames == 30)
			{
				before = Snapshot(map, layer);
				Ui.Root.Get<ButtonWidget>("PLATEAU_PLACE").OnClick();
				var brush = controller.CurrentBrush as PlateauStampBrush ?? throw new InvalidOperationException("Plateau button did not activate its brush.");
				manager.Add(brush.CreateAction(center));
				controller.ClearBrush();
				placed = Snapshot(map, layer);
				if (placed == before || map.Height[center] != 4) throw new InvalidOperationException("Plateau stamp did not change terrain.");
				var wall = layer.Save().Select(n => layer[n.Key]).First(p => p.Location == center + new CVec(8, 8) && p.Type == "terrain.rubberduck.closedcliff24");
				int Area() => wall.Render().Sum(r => { var bounds = r.PrepareRender(wr).ScreenBounds(wr); return bounds.Width * bounds.Height; });
				var originalArea = Area();
				if (originalArea == 0) throw new InvalidOperationException("Editor wall preview is invisible.");
				var revision = TerrainRenderRevision.Get(map);
				var neighbor = wall.Location + new CVec(1, 0);
				map.Height[neighbor] = 4;
				if (TerrainRenderRevision.Get(map) == revision || Area() >= originalArea)
					throw new InvalidOperationException("Editor cliff clipping did not refresh after neighbor height changed.");
				map.Height[neighbor] = 0;
				if (Area() != originalArea) throw new InvalidOperationException("Editor wall preview did not recover after restoring neighbor.");
				var native = layer.Add("NativePreviewTest", new ActorReference("terrain.rubberduck.nativecliff24")
				{ new LocationInit(center + new CVec(7, 7)), new OwnerInit("Neutral") });
				int NativeArea() => native.Render().Sum(r => { var bounds = r.PrepareRender(wr).ScreenBounds(wr); return bounds.Width * bounds.Height; });
				var nativeArea = NativeArea();
				var neighbors = new[] { new CVec(1, 0), new CVec(0, 1), new CVec(1, 1) }.Select(d => native.Location + d).ToArray();
				foreach (var c in neighbors) map.Height[c] = 4;
				if (nativeArea == 0 || NativeArea() >= nativeArea) throw new InvalidOperationException("Native editor preview did not refresh its clipping.");
				foreach (var c in neighbors) map.Height[c] = 0;
				if (NativeArea() != nativeArea) throw new InvalidOperationException("Native editor preview did not restore its clipping.");
				layer.Remove(native);
				Check(placed, "place and both wall-preview cache refreshes");
			}
			if (frames == 60) { manager.Undo(); Check(before, "undo placement"); }
			if (frames == 90) { manager.Redo(); Check(placed, "redo placement"); }
			if (frames == 100)
			{
				var rejected = false;
				try { new PlateauStampBrush(controller, wr, false, _ => { }).CreateAction(center); }
				catch (InvalidOperationException) { rejected = true; }
				if (!rejected) throw new InvalidOperationException("Overlapping plateau stamp accepted.");
				map.Resources[center] = new ResourceTile(1, 5);
				rejected = false;
				try { new PlateauStampBrush(controller, wr, true, _ => { }).CreateAction(center); }
				catch (InvalidOperationException) { rejected = true; }
				if (!rejected) throw new InvalidOperationException("Resource-bearing plateau deletion accepted.");
				map.Resources[center] = default;
				var vehicle = layer.Add("ProtectedActorTest", new ActorReference("truk") { new LocationInit(center), new OwnerInit("Neutral") });
				rejected = false;
				try { new PlateauStampBrush(controller, wr, true, _ => { }).CreateAction(center); }
				catch (InvalidOperationException) { rejected = true; }
				if (!rejected) throw new InvalidOperationException("Occupied plateau deletion accepted.");
				layer.Remove(vehicle);
				Check(placed, "overlap, resource and actor protection");
			}
			if (frames == 120)
			{
				Ui.Root.Get<ButtonWidget>("PLATEAU_ERASE").OnClick();
				var brush = controller.CurrentBrush as PlateauStampBrush ?? throw new InvalidOperationException("Erase button did not activate its brush.");
				manager.Add(brush.CreateAction(center));
				controller.ClearBrush();
				Check(before, "erase stamp");
			}
			if (frames == 150) { manager.Undo(); Check(placed, "undo erase"); }
			if (frames == 180) { manager.Redo(); Check(before, "redo erase"); }
			if (frames == 210) { manager.Undo(); Check(placed, "restore for screenshot"); }
			if (frames == 240)
			{
				// WriteResult validates the dedicated hashed temporary path before deriving
				// the adjacent save-test filename. Never accept arbitrary map-supplied paths.
				PlateauMovementProbe.WriteResult(info.ResultPath, "EDITOR PLATEAU saving reload fixture");
				map.ActorDefinitions = layer.Save();
				var path = Path.ChangeExtension(Path.GetFullPath(info.ResultPath), ".oramap");
				SaveTerrainCopy(map, path);
				using var stream = File.OpenRead(path);
				using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
				using var reloaded = new Map(Game.ModData, zip);
				if (reloaded.InvalidCustomRules) throw new InvalidOperationException("Editor save failed rule reload.", reloaded.InvalidCustomRulesException);
				foreach (var c in map.AllCells)
					if (reloaded.Height[c] != map.Height[c] || reloaded.Ramp[c] != map.Ramp[c] || !reloaded.Tiles[c].Equals(map.Tiles[c]) ||
						!reloaded.Resources[c].Equals(map.Resources[c]))
						throw new InvalidOperationException("Editor save changed plateau terrain.");
				if (reloaded.ActorDefinitions.OrderBy(n => n.Key, StringComparer.Ordinal).WriteToString() !=
					layer.Save().OrderBy(n => n.Key, StringComparer.Ordinal).WriteToString())
					throw new InvalidOperationException("Editor save changed actor IDs or definitions.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "EDITOR PLATEAU PASS save/reload");
				wr.Viewport.UnlockMinimumZoom(0.1f);
				wr.Viewport.AdjustZoom((float)Math.Log(0.65f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(center));
				Game.TakeScreenshot();
			}
		}
	}
}
