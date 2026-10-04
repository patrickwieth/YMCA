using System;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public class AuthoredShoreEditorProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly CPos Cell = CPos.Zero;
		public override object Create(ActorInitializer init) => new AuthoredShoreEditorProbe(this);
	}

	// Uses the ordinary terrain brush and its real editor history, not direct tile writes.
	public class AuthoredShoreEditorProbe : IPostWorldLoaded, ITickRender
	{
		readonly AuthoredShoreEditorProbeInfo info;
		int frames;
		string terrainBefore, shoresBefore, terrainPainted, shoresPainted;
		public AuthoredShoreEditorProbe(AuthoredShoreEditorProbeInfo info) { this.info = info; }
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
		}
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 150) return;
			var map = self.World.Map;
			var history = self.Trait<EditorActionManager>();
			var actors = self.Trait<EditorActorLayer>();
			var materials = self.Trait<RubberduckMaterialTransitions>();
			void Pass(string text) => PlateauMovementProbe.WriteResult(info.ResultPath, "SHORE EDITOR PASS " + text);
			if (frames == 20)
			{
				wr.Viewport.UnlockMinimumZoom(.1f); wr.Viewport.AdjustZoom((float)Math.Log(.65f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(info.Cell));
				terrainBefore = PlateauEditorProbe.Snapshot(map, actors);
				shoresBefore = materials.AuthoredShoreSnapshot;
				if (!materials.AuthoredShoreAt(info.Cell)) throw new InvalidOperationException("Fixture has no native shore at the brush target.");
				Pass("original source-backed shoreline");
			}
			if (frames == 30)
			{
				var controller = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR");
				var brush = new EditorTileBrush(controller, 1050, wr);
				var pixel = wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(info.Cell)));
				if (wr.Viewport.ViewToWorld(pixel) != info.Cell) throw new InvalidOperationException("Shore brush picked a different cell.");
				brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left, pixel, int2.Zero, Modifiers.None, 1));
				brush.HandleMouseInput(new MouseInput(MouseInputEvent.Up, MouseButton.Left, pixel, int2.Zero, Modifiers.None, 1));
				if (map.Tiles[info.Cell].Type != 1050 || materials.AuthoredShoreAt(info.Cell)) throw new InvalidOperationException("Painting water left a stale land shore.");
				terrainPainted = PlateauEditorProbe.Snapshot(map, actors); shoresPainted = materials.AuthoredShoreSnapshot;
				if (shoresPainted == shoresBefore) throw new InvalidOperationException("Shore neighborhood failed to refresh.");
				Pass("ordinary brush refreshes the shoreline neighborhood");
			}
			if (frames == 60 || frames == 120)
			{
				history.Undo();
				if (terrainBefore != PlateauEditorProbe.Snapshot(map, actors) || shoresBefore != materials.AuthoredShoreSnapshot)
					throw new InvalidOperationException("Shore Undo did not restore terrain, resources, actors and source keys exactly.");
				Pass("exact Undo of terrain, resources, actors and shoreline keys");
			}
			if (frames == 90)
			{
				history.Redo();
				if (terrainPainted != PlateauEditorProbe.Snapshot(map, actors) || shoresPainted != materials.AuthoredShoreSnapshot)
					throw new InvalidOperationException("Shore Redo differs from the original brush transaction.");
				Pass("exact Redo");
			}
			if (frames == 120)
			{
				map.ActorDefinitions = actors.Save();
				var path = Path.ChangeExtension(info.ResultPath, ".oramap");
				PlateauEditorProbe.SaveTerrainCopy(map, path);
				using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
				using var reload = new Map(Game.ModData, zip);
				foreach (var c in map.AllCells)
					if (!map.Tiles[c].Equals(reload.Tiles[c]) || map.Height[c] != reload.Height[c] || !map.Resources[c].Equals(reload.Resources[c]))
						throw new InvalidOperationException("Shore save/reload changed terrain or resources.");
				Pass("save/reload preserves terrain and resources");
				PlateauMovementProbe.WriteResult(info.ResultPath, materials.ValidateAuthoredShoreCache());
			}
			if (frames == 150) Game.TakeScreenshot();
		}
	}
}
