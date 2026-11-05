using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class CliffVisualEditorProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly CPos Center = CPos.Zero;
		public override object Create(ActorInitializer init) => new CliffVisualEditorProbe(this);
	}

	// Opt-in diagnostic only. Exercise the real editor history and the same slice
	// builder used by game actors and CliffEditorPreview, without replacing actors.
	public class CliffVisualEditorProbe : IPostWorldLoaded, ITickRender
	{
		readonly CliffVisualEditorProbeInfo info;
		int frames;
		CPos cell;
		string before, changed, slicesBefore, slicesChanged;
		public CliffVisualEditorProbe(CliffVisualEditorProbeInfo info) { this.info = info; }
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
		}
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 100) return;
			var map = self.World.Map; var actors = self.Trait<EditorActorLayer>(); var history = self.Trait<EditorActionManager>();
			var bodyInfo = map.Rules.Actors["terrain.rubberduck.mountainwall0"].TraitInfo<PlateauFaceBodyInfo>();
			string Slices() => string.Join(";", new PlateauFaceBody(bodyInfo).BuildSlices(map, cell, "terrain.rubberduck.mountainwall0").Select(s => $"{s.Bounds}/{s.Offset}"));
			void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); PlateauMovementProbe.WriteResult(info.ResultPath, "CLIFF EDITOR PASS " + message); }
			if (frames == 20)
			{
				cell = map.AllCells.Where(map.Contains).Where(c => map.Height[c] == 4 && map.Ramp[c] == 0 &&
					map.Contains(c + new CVec(1, 0)) && map.Height[c + new CVec(1, 0)] == 0 &&
					map.Contains(c + new CVec(0, 1)) && map.Height[c + new CVec(0, 1)] == 0)
					.OrderBy(c => Math.Abs(c.X - info.Center.X) + Math.Abs(c.Y - info.Center.Y)).First();
				wr.Viewport.UnlockMinimumZoom(.1f); wr.Viewport.AdjustZoom((float)Math.Log(.45f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(info.Center));
				before = PlateauEditorProbe.Snapshot(map, actors); slicesBefore = Slices();
				Check(RubberduckCliffVisuals.Corner(map, cell, 0, 1) == 0, "convex corner selected from terrain");
			}
			if (frames == 30)
			{
				history.Add(new PlateauTerrainEditAction(map, new Dictionary<CPos, PlateauSurface> { [cell + new CVec(0, 1)] = new PlateauSurface(4, 0, true) }, _ => { }, "Cliff visual neighborhood probe"));
				changed = PlateauEditorProbe.Snapshot(map, actors); slicesChanged = Slices();
				Check(slicesChanged != slicesBefore, "neighbor edit refreshes corner slices");
			}
			if (frames == 50 || frames == 90)
			{
				history.Undo();
				Check(before == PlateauEditorProbe.Snapshot(map, actors) && slicesBefore == Slices(), "Undo restores exact terrain/resources/actors and visual slices");
			}
			if (frames == 70)
			{
				history.Redo();
				Check(changed == PlateauEditorProbe.Snapshot(map, actors) && slicesChanged == Slices(), "Redo restores exact data and visual slices");
			}
			if (frames == 100)
			{
				history.Add(new PlateauTerrainEditAction(map, new Dictionary<CPos, PlateauSurface>
				{
					[cell + new CVec(0, 1)] = new PlateauSurface(4, 0, true),
					[cell + new CVec(1, 1)] = new PlateauSurface(4, 0, true),
				}, _ => { }, "Concave cliff neighborhood probe"));
				Check(RubberduckCliffVisuals.Corner(map, cell, 0, 1) == 1 && Slices() != slicesBefore, "concave corner selected instead of convex overlay");
				history.Undo();
				Check(before == PlateauEditorProbe.Snapshot(map, actors) && slicesBefore == Slices(), "concave edit Undo restores original data and slices");
				map.ActorDefinitions = actors.Save(); var path = Path.ChangeExtension(info.ResultPath, ".oramap");
				PlateauEditorProbe.SaveTerrainCopy(map, path);
				using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var reload = new Map(Game.ModData, zip);
				Check(map.AllCells.All(c => map.Tiles[c].Equals(reload.Tiles[c]) && map.Height[c] == reload.Height[c] && map.Resources[c].Equals(reload.Resources[c])), "save/reload preserves terrain/heights/resources");
				Check(map.ActorDefinitions.Select(n => n.Key + n.Value.Value + n.Value.Nodes.WriteToString()).SequenceEqual(reload.ActorDefinitions.Select(n => n.Key + n.Value.Value + n.Value.Nodes.WriteToString())), "save/reload preserves actor definitions");
				Game.TakeScreenshot();
			}
		}
	}
}
