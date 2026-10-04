using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class LegacyFootEditorProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly CPos Cell = new CPos(87, -22);
		public readonly CPos Center = new CPos(87, -23);
		public override object Create(ActorInitializer init) => new LegacyFootEditorProbe(this);
	}
	public class LegacyFootEditorProbe : IPostWorldLoaded, ITickRender
	{
		readonly LegacyFootEditorProbeInfo info;
		int frames;
		string before;
		readonly CPos cell;
		public LegacyFootEditorProbe(LegacyFootEditorProbeInfo info) { this.info = info; cell = info.Cell; }
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
		}
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 150) return;
			var map = self.World.Map;
			var history = self.Trait<EditorActionManager>();
			var layer = self.Trait<EditorActorLayer>();
			var materials = self.Trait<RubberduckMaterialTransitions>();
			void Check(bool covered, string label)
			{
				if (materials.LegacyAbutmentAt(cell) != covered) throw new InvalidOperationException("Legacy abutment invalidation: " + label);
				PlateauMovementProbe.WriteResult(info.ResultPath, "LEGACY EDITOR PASS " + label);
			}
			if (frames == 20)
			{
				wr.Viewport.UnlockMinimumZoom(.1f); wr.Viewport.AdjustZoom((float)Math.Log(.65f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(info.Center));
				before = PlateauEditorProbe.Snapshot(map, layer); Check(true, "original saved-map foot");
			}
			if (frames == 30)
			{
				// Controlled cache test through the same atomic terrain/history transaction.
				// This temporary change is undone before saving; no production map is edited.
				history.Add(new PlateauTerrainEditAction(map, new Dictionary<CPos, PlateauSurface> { [cell] = new PlateauSurface(0, 0, false) }, _ => { }, "Diagnostic foot invalidation"));
				Check(false, "terrain change removes unsupported decoration");
			}
			if (frames == 60)
			{
				history.Undo(); Check(true, "undo restores decoration");
				if (PlateauEditorProbe.Snapshot(map, layer) != before) throw new InvalidOperationException("Legacy undo changed saved terrain or actors.");
			}
			if (frames == 90) { history.Redo(); Check(false, "redo invalidates decoration"); }
			if (frames == 120)
			{
				history.Undo(); Check(true, "restored before save");
				if (PlateauEditorProbe.Snapshot(map, layer) != before) throw new InvalidOperationException("Legacy restoration was not exact.");
				map.ActorDefinitions = layer.Save();
				var path = Path.ChangeExtension(info.ResultPath, ".oramap");
				PlateauEditorProbe.SaveTerrainCopy(map, path);
				using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
				using var reload = new Map(Game.ModData, zip);
				foreach (var c in map.AllCells)
					if (!map.Tiles[c].Equals(reload.Tiles[c]) || map.Height[c] != reload.Height[c] || !map.Resources[c].Equals(reload.Resources[c]))
						throw new InvalidOperationException("Legacy save/reload changed terrain.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "LEGACY EDITOR PASS exact save/reload; no decoration actors inserted");
			}
			if (frames == 150) Game.TakeScreenshot();
		}
	}
}
