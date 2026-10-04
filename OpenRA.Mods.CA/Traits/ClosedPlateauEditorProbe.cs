using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class ClosedPlateauEditorProbeInfo : TraitInfo
	{
		public readonly CPos Center = CPos.Zero;
		public readonly int Notch = -1;
		public readonly bool RearRamp = false;
		public readonly int RampDirection = 0;
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new ClosedPlateauEditorProbe(this);
	}
	public class ClosedPlateauEditorProbe : IPostWorldLoaded, ITickRender
	{
		readonly ClosedPlateauEditorProbeInfo info;
		int frames;
		string before, placed;
		RubberduckClosedPlateauPlan plan;
		public ClosedPlateauEditorProbe(ClosedPlateauEditorProbeInfo info) { this.info = info; }
		public void PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(world.Map.Uid));
		}
		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 180) return;
			var map = self.World.Map; var layer = self.Trait<EditorActorLayer>(); var manager = self.Trait<EditorActionManager>();
			void Check(string expected, string name)
			{
				if (PlateauEditorProbe.Snapshot(map, layer) != expected) throw new InvalidOperationException("Closed editor mismatch: " + name);
				PlateauMovementProbe.WriteResult(info.ResultPath, "CLOSED EDITOR PASS " + name);
			}
			if (frames == 30)
			{
				before = PlateauEditorProbe.Snapshot(map, layer);
				plan = new RubberduckClosedPlateauPlan(map, info.Center, info.Notch, Array.Empty<CPos>(), info.RearRamp, info.RampDirection, layer.Save());
				foreach (var protectedCell in plan.Patch.Keys)
				{
					var rejected = false;
					try { new RubberduckClosedPlateauPlan(map, info.Center, info.Notch, new[] { protectedCell }, info.RearRamp, info.RampDirection, layer.Save()); }
					catch (InvalidOperationException) { rejected = true; }
					if (!rejected) throw new InvalidOperationException("Closed plan overwrites protected roof/foot.");
				}
				foreach (var resourceCell in plan.Patch.Keys)
				{
					var savedResource = map.Resources[resourceCell]; var resourceRejected = false;
					try
					{
						map.Resources[resourceCell] = new ResourceTile(1, 5);
						new RubberduckClosedPlateauPlan(map, info.Center, info.Notch, Array.Empty<CPos>(), info.RearRamp, info.RampDirection, layer.Save());
					}
					catch (InvalidOperationException) { resourceRejected = true; }
					finally { map.Resources[resourceCell] = savedResource; }
					if (!resourceRejected) throw new InvalidOperationException("Closed plan overwrites resources.");
				}
				Check(before, "protected roof, foot, ramp/landing and resource rejection");
				var building = map.Rules.Actors["soviet_refinery"].TraitInfo<BuildingInfo>();
				var offset = building.Footprint.Keys.First(c => c != CVec.Zero);
				var anchor = plan.Patch.Keys.First(c => !plan.Patch.ContainsKey(c - offset)) - offset;
				var liveBuilding = layer.Add("ClosedFootprintProbe", new ActorReference("soviet_refinery") { new LocationInit(anchor), new OwnerInit("Neutral") });
				var footprintRejected = false;
				try { new RubberduckClosedPlateauPlan(map, info.Center, info.Notch, Array.Empty<CPos>(), info.RearRamp, info.RampDirection, layer.Save()); }
				catch (InvalidOperationException) { footprintRejected = true; }
				finally { layer.Remove(liveBuilding); }
				if (!footprintRejected) throw new InvalidOperationException("Closed plan overwrites a live building whose anchor lies outside the patch.");
				Check(before, "live building footprint beyond anchor protection");
				manager.Add(new PlateauTerrainEditAction(map, plan.Patch, PlateauActorEdit.Patch(layer, plan.Actors, false), "Closed height-four plateau"));
				placed = PlateauEditorProbe.Snapshot(map, layer);
				if (placed == before || plan.Roof.Any(c => map.Height[c] != 4) || plan.Feet.Any(c => map.Height[c] != 0 || map.Tiles[c].Type != 3992))
					throw new InvalidOperationException("Closed editor stamp did not apply its topology.");
				if (plan.Ramps.Any(c => map.Ramp[c] != plan.Patch[c].Ramp || map.Height[c] != plan.Patch[c].Height) ||
					plan.Landings.Any(c => map.Ramp[c] != 0 || map.Height[c] != 0 || map.Tiles[c].Type != 1000))
					throw new InvalidOperationException("Closed editor stamp damaged a ramp lane or landing.");
				var preview = layer[plan.Actors.First(n => n.Value.Value == "calibration.closedcliff0" || n.Value.Value == "calibration.closedcliff100").Key];
				int Area() => preview.Render().Sum(r => { var b = r.PrepareRender(wr).ScreenBounds(wr); return b.Width * b.Height; });
				var originalArea = Area(); var neighbor = preview.Location + new CVec(0, 1); var height = map.Height[neighbor];
				map.Height[neighbor] = 4;
				if (originalArea == 0 || Area() >= originalArea) throw new InvalidOperationException("Source-grid editor clipping did not refresh.");
				map.Height[neighbor] = height;
				if (Area() != originalArea) throw new InvalidOperationException("Source-grid preview failed to restore.");
				Check(placed, "stamp and live clipping refresh");
			}
			if (frames == 60) { manager.Undo(); Check(before, "exact undo"); }
			if (frames == 90) { manager.Redo(); Check(placed, "exact redo"); }
			if (frames == 120)
			{
				var invalidActors = new[] { plan.Actors[0], plan.Actors[0] };
				var patch = plan.Roof.ToDictionary(c => c, _ => new PlateauSurface(0));
				var action = new PlateauTerrainEditAction(map, patch, PlateauActorEdit.Replace(layer, plan.Actors, invalidActors), "Injected duplicate actor");
				var rejected = false;
				try { action.Do(); } catch (InvalidOperationException) { rejected = true; }
				if (!rejected) throw new InvalidOperationException("Injected actor failure was accepted.");
				Check(placed, "partial actor failure rolls back terrain and actors");
			}
			if (frames == 180)
			{
				PlateauMovementProbe.WriteResult(info.ResultPath, "CLOSED EDITOR saving");
				map.ActorDefinitions = layer.Save(); var path = Path.ChangeExtension(info.ResultPath, ".oramap");
				// Map.Save copies embedded assets and replaces Map.Package. Snapshot
				// bytes before saving, rather than reading the disposed output package.
				var art = map.Package.Contents.Where(f => f.StartsWith("height4-", StringComparison.Ordinal) && f.EndsWith(".png", StringComparison.Ordinal))
					.ToDictionary(f => f, f => { using var input = map.Package.GetStream(f); using var bytes = new MemoryStream(); input.CopyTo(bytes); return bytes.ToArray(); });
				PlateauEditorProbe.SaveTerrainCopy(map, path);
				using var saved = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(saved, path); using var reload = new Map(Game.ModData, zip);
				if (reload.InvalidCustomRules) throw new InvalidOperationException("Closed editor rule reload failed.", reload.InvalidCustomRulesException);
				foreach (var c in map.AllCells)
					if (map.Height[c] != reload.Height[c] || !map.Tiles[c].Equals(reload.Tiles[c]) || !map.Resources[c].Equals(reload.Resources[c])) throw new InvalidOperationException("Closed editor terrain changed on reload.");
				if (map.ActorDefinitions.OrderBy(n => n.Key).WriteToString() != reload.ActorDefinitions.OrderBy(n => n.Key).WriteToString()) throw new InvalidOperationException("Closed editor actors changed on reload.");
				foreach (var file in art)
				{
					using var restored = reload.Package.GetStream(file.Key); using var bytes = new MemoryStream(); restored.CopyTo(bytes);
					if (!file.Value.SequenceEqual(bytes.ToArray())) throw new InvalidOperationException("Saved source art changed.");
				}
				Check(placed, "save/reload terrain, actors and map-local sprites");
				wr.Viewport.UnlockMinimumZoom(0.1f);
				wr.Viewport.AdjustZoom((float)Math.Log(0.65f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(info.Center + new CVec(5, -2)));
				Game.TakeScreenshot();
			}
		}
	}
}
