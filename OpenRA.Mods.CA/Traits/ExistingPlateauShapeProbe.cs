using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Widgets.Logic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits
{
	public class ExistingPlateauShapeProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly CPos Cell;
		public override object Create(ActorInitializer init) => new ExistingPlateauShapeProbe(this);
	}
	public class ExistingPlateauShapeProbe : IPostWorldLoaded, ITickRender
	{
		readonly ExistingPlateauShapeProbeInfo info;
		int frames;
		string before;
		string after;
		public ExistingPlateauShapeProbe(ExistingPlateauShapeProbeInfo info) { this.info = info; }
		public void PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
		}
		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 180) return;
			var map = self.World.Map; var layer = self.Trait<EditorActorLayer>(); var history = self.Trait<EditorActionManager>();
			void Check(string snapshot, string label)
			{
				if (PlateauEditorProbe.Snapshot(map, layer) != snapshot) throw new InvalidOperationException("Existing plateau edit mismatch: " + label);
				PlateauMovementProbe.WriteResult(info.ResultPath, "EXISTING SHAPE PASS " + label);
			}
			if (frames == 20)
			{
				wr.Viewport.UnlockMinimumZoom(0.1f); wr.Viewport.AdjustZoom((float)Math.Log(0.5f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(info.Cell));
			}
			if (frames == 30)
			{
				before = PlateauEditorProbe.Snapshot(map, layer);
				Ui.Root.Get<ButtonWidget>("PLATEAU_GROW").OnClick();
				var controller = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR");
				var brush = controller.CurrentBrush as PlateauShapeBrush ?? throw new InvalidOperationException("Grow button did not activate.");
				var plan = brush.Plan(new[] { info.Cell });
				if (plan.NativePieces == 0) throw new InvalidOperationException("Native generated shape lost its native composition.");
				var center = wr.ScreenPxPosition(map.CenterOfCell(info.Cell));
				var pixel = wr.Viewport.WorldToViewPx(center);
				if (wr.Viewport.ViewToWorld(pixel) != info.Cell)
				{
					// A projected center may be covered even when part of the ground
					// diamond is selectable. Use the real picker, never bypass the brush.
					var tileSize = map.Grid.TileSize;
					var hits = Enumerable.Range(-tileSize.Height / 2, tileSize.Height + 1)
						.SelectMany(y => Enumerable.Range(-tileSize.Width / 2, tileSize.Width + 1).Select(x => new int2(x, y)))
						.Where(d => Math.Abs(d.X) * 2.0 / tileSize.Width + Math.Abs(d.Y) * 2.0 / tileSize.Height < 0.9)
						.OrderBy(d => d.X * d.X + d.Y * d.Y)
						.Select(d => wr.Viewport.WorldToViewPx(center + d)).Where(p => wr.Viewport.ViewToWorld(p) == info.Cell).Take(1).ToArray();
					if (hits.Length == 0) throw new InvalidOperationException($"Existing shape fixture cell {info.Cell} has no selectable ground pixel.");
					pixel = hits[0];
				}
				brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left, pixel, int2.Zero, Modifiers.Shift, 1));
				Check(before, "single-cell draft is non-mutating");
				brush.HandleMouseInput(new MouseInput(MouseInputEvent.Up, MouseButton.Left, pixel, int2.Zero, Modifiers.Shift, 1));
				controller.ClearBrush();
				if (map.Height[info.Cell] != 4 || map.Ramp[info.Cell] != 0 || map.Tiles[info.Cell].Type != 1000)
					throw new InvalidOperationException("Existing plateau did not extend to its painted cell.");
				after = PlateauEditorProbe.Snapshot(map, layer); Check(after, "extend generated native plateau");
			}
			if (frames == 60) { history.Undo(); Check(before, "undo original generated terrain/actors exactly"); }
			if (frames == 90) { history.Redo(); Check(after, "redo generated extension exactly"); }
			if (frames == 120)
			{
				PlateauMovementProbe.WriteResult(info.ResultPath, "EXISTING SHAPE saving fixture");
				map.ActorDefinitions = layer.Save();
				var path = Path.ChangeExtension(Path.GetFullPath(info.ResultPath), ".oramap");
				PlateauEditorProbe.SaveTerrainCopy(map, path);
				using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path);
				using var reload = new Map(Game.ModData, zip);
				if (reload.InvalidCustomRules) throw new InvalidOperationException("Generated edit rules failed reload.", reload.InvalidCustomRulesException);
				foreach (var c in map.AllCells)
					if (map.Height[c] != reload.Height[c] || !map.Tiles[c].Equals(reload.Tiles[c]) || !map.Resources[c].Equals(reload.Resources[c]))
						throw new InvalidOperationException("Generated edit changed during reload.");
				if (map.ActorDefinitions.OrderBy(n => n.Key).WriteToString() != reload.ActorDefinitions.OrderBy(n => n.Key).WriteToString())
					throw new InvalidOperationException("Generated edit actors changed during reload.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "EXISTING SHAPE PASS save/reload");
			}
			if (frames == 180) Game.TakeScreenshot();
		}
	}
}
