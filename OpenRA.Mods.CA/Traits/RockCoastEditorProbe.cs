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
	public class RockCoastEditorProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly bool AllCorners = false;
		public override object Create(ActorInitializer init) => new RockCoastEditorProbe(this);
	}
	public class RockCoastEditorProbe : IPostWorldLoaded, ITickRender
	{
		readonly RockCoastEditorProbeInfo info;
		int frames;
		string before, after;
		public RockCoastEditorProbe(RockCoastEditorProbeInfo info) { this.info = info; }
		public void PostWorldLoaded(World w, WorldRenderer wr)
		{ if (w.Type != WorldType.Editor) Game.RunAfterTick(() => Game.LoadEditor(w.Map.Uid)); }
		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.World.Type != WorldType.Editor || ++frames > 180) return;
			var map = self.World.Map; var center = new MPos(48, 64).ToCPos(map); var corner = center + (info.AllCorners ? new CVec(12, 5) : new CVec(12, 12));
			var layer = self.Trait<EditorActorLayer>(); var history = self.Trait<EditorActionManager>();
			void Check(string expected, string message)
			{
				if (PlateauEditorProbe.Snapshot(map, layer) != expected) throw new InvalidOperationException("Coast editor mismatch: " + message);
				PlateauMovementProbe.WriteResult(info.ResultPath, "COAST EDITOR PASS " + message);
			}
			if (frames == 20)
			{
				wr.Viewport.UnlockMinimumZoom(0.1f); wr.Viewport.AdjustZoom((float)Math.Log(0.4f / wr.Viewport.Zoom));
				wr.Viewport.Center(map.CenterOfCell(corner)); before = PlateauEditorProbe.Snapshot(map, layer);
			}
			if (frames == 30)
			{
				RubberduckMaterialTransitions.ValidateMasks();
				RubberduckMaterialTransitions.ValidateVariantMasks();
				var phases = new System.Collections.Generic.HashSet<int>();
				for (var x = -12; x <= 12; x++)
					for (var y = -12; y <= 12; y++)
					{
						var cell = new CPos(x, y);
						var phase = RubberduckMaterialTransitions.WaterPhase(cell);
						phases.Add(phase);
						if (phase < 0 || phase >= 36 || phase != RubberduckMaterialTransitions.WaterPhase(cell + new CVec(6, 0)) ||
							phase != RubberduckMaterialTransitions.WaterPhase(cell + new CVec(0, 6)))
							throw new InvalidOperationException("Native water phase is discontinuous at negative coordinates or period boundaries.");
					}
				if (phases.Count != 36) throw new InvalidOperationException("Native water does not use all 36 spatial phases.");
				var materials = self.Trait<RubberduckMaterialTransitions>();
				if (materials.SecondaryWaterLoaded) throw new InvalidOperationException("Unused secondary water was loaded eagerly.");
				var blendCell = map.AllCells.First(c => map.Contains(c) && materials.TransitionKey(c) != 0);
				var key = materials.TransitionKey(blendCell); var tile = map.Tiles[blendCell];
				map.Tiles[blendCell] = new TerrainTile(1050, 0);
				if (materials.TransitionKey(blendCell) != 0) throw new InvalidOperationException("Material overlay did not clear on terrain edit.");
				map.Tiles[blendCell] = tile;
				if (materials.TransitionKey(blendCell) != key) throw new InvalidOperationException("Material overlay did not recover on terrain restore.");
				Check(before, "material masks and live terrain invalidation");
				var sampleCells = new[] { center }.Concat(MapGeneration.PlateauTopology.Directions.Select(d => center + d)).ToArray();
				var sampleTiles = sampleCells.Select(c => map.Tiles[c]).ToArray();
				var sampleHeights = sampleCells.Select(c => map.Height[c]).ToArray();
				try
				{
					foreach (var c in sampleCells) map.Height[c] = 0;
					var variants = new ushort[] { 1050, 1000, 1010, 1020, 1030, 1040 };
					var shifts = new[] { 0, 4, 12, 8, 16 };
					for (var destination = 0; destination < variants.Length; destination++)
						for (var source = 0; source < variants.Length; source++)
							for (var direction = 0; direction < 4; direction++)
							{
								foreach (var c in sampleCells) map.Tiles[c] = new TerrainTile(variants[destination], 0);
								map.Tiles[center + MapGeneration.PlateauTopology.Directions[direction]] = new TerrainTile(variants[source], 0);
								var expected = source < destination ? 1 << (direction + shifts[source]) : 0;
								if (materials.TransitionKey(center) != expected) throw new InvalidOperationException("Authored material variant used the wrong source or direction.");
							}
					foreach (var land in new ushort[] { 1000, 1010, 1020, 1030, 1040 })
						for (var direction = 0; direction < 4; direction++)
						{
							foreach (var c in sampleCells) map.Tiles[c] = new TerrainTile(land, 0);
							map.Tiles[center + MapGeneration.PlateauTopology.Directions[direction]] = new TerrainTile(1060, 0);
							var expected = 1 << (20 + direction);
							if (!materials.SecondaryWaterLoaded || materials.TransitionKey(center) != expected)
								throw new InvalidOperationException("Secondary water did not load lazily or used the wrong shore mask.");
							map.Tiles[center + MapGeneration.PlateauTopology.Directions[(direction + 1) % 4]] = new TerrainTile(1050, 0);
							if (materials.TransitionKey(center) != (expected | 1 << ((direction + 1) % 4)))
								throw new InvalidOperationException("Mixed water shores lost a water family.");
							map.Height[center] = 1;
							if (materials.TransitionKey(center) != 0) throw new InvalidOperationException("Secondary water crossed a height edge.");
							map.Height[center] = 0;
							foreach (var excluded in new ushort[] { 1060, 3992, 14001, 14010, 14011, 14012 })
							{
								map.Tiles[center] = new TerrainTile(excluded, 0);
								if ((materials.TransitionKey(center) & 0xF0000F) != 0)
									throw new InvalidOperationException("Water shore overlay concealed a blocked bank or water cell.");
							}
						}
					foreach (var legacy in new ushort[] { 1070, 1080 })
						foreach (var land in new ushort[] { 1000, 1010, 1020, 1030, 1040 })
							for (var direction = 0; direction < 4; direction++)
							{
								foreach (var c in sampleCells) map.Tiles[c] = new TerrainTile(land, 0);
								var donor = center + MapGeneration.PlateauTopology.Directions[direction];
								map.Tiles[donor] = new TerrainTile((ushort)(legacy - 20), 0);
								var expected = materials.TransitionKey(center);
								var cached = materials.CachedWaterSpriteCount;
								for (byte frame = 0; frame < 16; frame++)
								{
									map.Tiles[donor] = new TerrainTile(legacy, frame);
									if (materials.TransitionKey(center) != expected || materials.CachedWaterSpriteCount != cached)
										throw new InvalidOperationException("Legacy water lost its shore or allocated frame-dependent phase sprites.");
								}
								map.Height[center] = 1;
								if (materials.TransitionKey(center) != 0) throw new InvalidOperationException("Legacy water shore crossed a height edge.");
								map.Height[center] = 0;
								foreach (var excluded in new ushort[] { 1050, 1060, 1070, 1080, 3992, 14001, 14010, 14011, 14012 })
								{
									map.Tiles[center] = new TerrainTile(excluded, 0);
									if ((materials.TransitionKey(center) & 0xF0000F) != 0) throw new InvalidOperationException("Legacy shore painted over protected terrain.");
								}
							}
					for (var rotation = 0; rotation < 4; rotation++)
					{
						var types = new ushort[] { 1000, 1020, 1050, 1030 };
						for (var d = 0; d < 4; d++) map.Tiles[center + MapGeneration.PlateauTopology.Directions[(d + rotation) % 4]] = new TerrainTile(types[d], 0);
						map.Tiles[center] = new TerrainTile(1030, 0);
						var expected = (1 << rotation << 4) | (1 << ((rotation + 1) % 4) << 8) | (1 << ((rotation + 2) % 4));
						if (materials.TransitionKey(center) != expected) throw new InvalidOperationException("Dirt junction lost a directional material mask.");
						map.Height[center] = 1;
						if (materials.TransitionKey(center) != 0) throw new InvalidOperationException("Material overlay crossed a height edge.");
						map.Height[center] = 0;
						foreach (var excluded in new ushort[] { 1050, 3992, 14001, 14010 })
						{
							map.Tiles[center] = new TerrainTile(excluded, 0);
							if (materials.TransitionKey(center) != 0) throw new InvalidOperationException("Material overlay concealed water, cliff or ramp terrain.");
						}
					}
				}
				finally
				{
					for (var i = 0; i < sampleCells.Length; i++) { map.Tiles[sampleCells[i]] = sampleTiles[i]; map.Height[sampleCells[i]] = sampleHeights[i]; }
				}
				Check(before, "dirt junctions, all rotations, height and collision exclusions");
				Check(before, "144 authored-variant pair/direction cases and 2401 mask combinations");
				Check(before, "secondary water lazy loading, mixed shores, rotations and protected banks");
				Check(before, "640 legacy-water frame/base/direction cases, cache reuse and collision exclusions");
				var apronCells = Enumerable.Range(-3, 9).SelectMany(x => Enumerable.Range(-3, 9).Select(y => center + new CVec(x, y))).ToArray();
				var apronTiles = apronCells.Select(c => map.Tiles[c]).ToArray();
				var apronHeights = apronCells.Select(c => map.Height[c]).ToArray();
				try
				{
					foreach (var c in apronCells) { map.Height[c] = 0; map.Tiles[c] = new TerrainTile(1000, 0); }
					for (var direction = 0; direction < 4; direction++)
					{
						var donor = center + MapGeneration.PlateauTopology.Directions[direction];
						var raised = center + MapGeneration.PlateauTopology.Directions[direction] * 2;
						map.Tiles[donor] = new TerrainTile(3992, 0);
						if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Isolated blocked dirt acquired a cliff-foot fringe.");
						map.Tiles[raised] = new TerrainTile(3992, 0); map.Height[raised] = 4;
						if (materials.ApronTransitionKey(center) != 1 << direction || materials.ApronTransitionKey(donor) != 0)
							throw new InvalidOperationException("Cliff-foot fringe lost its direction or painted blocked terrain.");
						foreach (var excluded in new ushort[] { 1020, 1030, 1040, 1050, 1060, 3992, 14001, 14010, 14011, 14012 })
						{
							map.Tiles[center] = new TerrainTile(excluded, 0);
							if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Cliff-foot fringe painted an excluded receiver.");
						}
						map.Tiles[center] = new TerrainTile(1010, 0); map.Height[center] = 1;
						if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Cliff-foot fringe painted elevated grass.");
						map.Height[center] = 0;
						if (materials.ApronTransitionKey(center) != 1 << direction) throw new InvalidOperationException("Grass B fringe did not recover.");
						var foreground = center + new CVec(2, 2);
						map.Height[foreground] = 4; materials.RefreshAprons();
						if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Ground fringe would paint over a nearer roof.");
						for (var ramp = 1; ramp <= 4; ramp++)
						{
							map.Height[foreground] = 3;
							map.Tiles[foreground] = new TerrainTile((ushort)(14000 + ramp), 0);
							materials.RefreshAprons();
							if (!materials.ApronOccluded(center) || materials.ApronTransitionKey(center) == 0)
								throw new InvalidOperationException("Ramp occlusion failed to retain only the visible part of the ground fringe.");
						}
						map.Tiles[foreground] = new TerrainTile(1000, 0);
						map.Height[foreground] = 0; materials.RefreshAprons();
						var distant = center + new CVec(4, 4);
						map.Height[distant] = 8; materials.RefreshAprons();
						if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Distant high roof failed to occlude a fringe outside the local edit ring.");
						map.Height[distant] = 0; materials.RefreshAprons();
						if (materials.ApronTransitionKey(center) != 1 << direction) throw new InvalidOperationException("Hidden fringe did not recover after lowering the nearer roof.");
						map.Height[raised] = 0;
						if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Second-ring height invalidation left a stale apron fringe.");
						map.Tiles[donor] = new TerrainTile(1000, 0); map.Tiles[raised] = new TerrainTile(1000, 0);
					}
					map.Tiles[center] = new TerrainTile(1000, 0);
					for (var direction = 0; direction < 4; direction++)
						foreach (var turn in new[] { 1, 3 })
						{
							var inward = MapGeneration.PlateauTopology.Directions[direction];
							var lateral = MapGeneration.PlateauTopology.Directions[(direction + turn) % 4];
							var donor = center + inward;
							var flankA = donor + inward;
							var flankB = donor + lateral;
							var raised = donor + inward + lateral;
							map.Tiles[donor] = new TerrainTile(3992, 0);
							map.Height[raised] = 4;
							if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Unbacked diagonal dirt acquired a fringe.");
							map.Tiles[flankA] = new TerrainTile(3992, 0);
							if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Single-flank diagonal dirt acquired a fringe.");
							map.Tiles[flankB] = new TerrainTile(3992, 0);
							if (materials.ApronTransitionKey(center) != 1 << direction)
								throw new InvalidOperationException("Closed convex cliff foot lost its outward soil fringe.");
							foreach (var flank in new[] { flankA, flankB })
								foreach (var excluded in new ushort[] { 1000, 1050, 14001, 14010 })
								{
									map.Tiles[flank] = new TerrainTile(excluded, 0);
									if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Invalid convex-foot flank retained its fringe.");
									map.Tiles[flank] = new TerrainTile(3992, 0);
								}
							map.Tiles[raised] = new TerrainTile(14001, 0);
							if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Diagonal ramp qualified as a solid convex roof.");
							map.Tiles[raised] = new TerrainTile(1000, 0);
							map.Height[raised] = 0;
							if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Third-ring lowering left a stale convex fringe.");
							var patch = new System.Collections.Generic.Dictionary<CPos, MapGeneration.PlateauSurface>
								{ [raised] = new MapGeneration.PlateauSurface(4) };
							var action = new MapGeneration.PlateauTerrainEditAction(map, patch, _ => { }, "Convex fringe invalidation probe");
							for (var repeat = 0; repeat < 2; repeat++)
							{
								action.Do();
								if (materials.ApronTransitionKey(center) != 1 << direction) throw new InvalidOperationException("Third-ring Do/Redo failed to create a convex fringe.");
								action.Undo();
								if (materials.ApronTransitionKey(center) != 0) throw new InvalidOperationException("Third-ring Undo left a convex fringe.");
							}
							var failure = new MapGeneration.PlateauTerrainEditAction(map, patch, _ => throw new InvalidOperationException("Convex fringe rollback probe"), "Failure probe");
							var failed = false;
							try { failure.Do(); } catch (InvalidOperationException) { failed = true; }
							if (!failed || map.Height[raised] != 0 || materials.ApronTransitionKey(center) != 0)
								throw new InvalidOperationException("Convex fringe failed to roll back with terrain.");
							foreach (var cell in new[] { donor, flankA, flankB }) map.Tiles[cell] = new TerrainTile(1000, 0);
						}
					foreach (var waterTile in new ushort[] { 1050, 1060, 1070, 1080 })
					{
						map.Tiles[center] = new TerrainTile(waterTile, 0);
						var secondary = waterTile == 1060 || waterTile == 1080;
						var equalHeight = center + new CVec(2, 2);
						map.Height[center] = 4; map.Height[equalHeight] = 4;
						if (RubberduckMaterialLayer.VisibleRectangles(map, center) != null || materials.WaterOccluded(center, secondary))
							throw new InvalidOperationException("Equal-height terrain incorrectly clipped a water surface.");
						map.Height[center] = 0; map.Height[equalHeight] = 0;
						foreach (var height in new byte[] { 4, 8 })
						{
							var foreground = center + new CVec(height / 2, height / 2);
							map.Height[foreground] = height;
							var rectangles = RubberduckMaterialLayer.VisibleRectangles(map, center);
							if (rectangles == null || rectangles.Length != 0 || !materials.WaterOccluded(center, secondary))
								throw new InvalidOperationException("Native water painted a fully occluding foreground roof.");
							map.Height[foreground] = 0;
							if (RubberduckMaterialLayer.VisibleRectangles(map, center) != null || materials.WaterOccluded(center, secondary))
								throw new InvalidOperationException("Water clipping failed to recover after lowering a distant roof.");
						}
						var rampCell = center + new CVec(2, 2);
						for (var ramp = 1; ramp <= 4; ramp++)
						{
							map.Height[rampCell] = 3;
							map.Tiles[rampCell] = new TerrainTile((ushort)(14000 + ramp), 0);
							var rectangles = RubberduckMaterialLayer.VisibleRectangles(map, center);
							if (rectangles == null || !materials.WaterOccluded(center, secondary))
								throw new InvalidOperationException("Foreground ramp failed to clip water.");
							var visiblePixels = 0;
							for (var y = 0; y < 64; y++)
								for (var x = 0; x < 128; x++)
								{
									var count = rectangles.Count(r => x >= r.X && x < r.Right && y >= r.Y && y < r.Bottom);
									if (count > 1) throw new InvalidOperationException("Clipped material slices overlap.");
									if (Math.Abs(x + 0.5 - 64) / 64 + Math.Abs(y + 0.5 - 32) / 32 <= 1) visiblePixels += count;
								}
							if (visiblePixels == 0 || visiblePixels >= 4096)
								throw new InvalidOperationException("Partial ramp clipping hid the whole water cell or none of it.");
						}
						map.Height[rampCell] = 0; map.Tiles[rampCell] = new TerrainTile(1000, 0);
						if (materials.WaterOccluded(center, secondary)) throw new InvalidOperationException("Stale ramp clipping after terrain restoration.");
					}
				}
				finally
				{
					for (var i = 0; i < apronCells.Length; i++) { map.Tiles[apronCells[i]] = apronTiles[i]; map.Height[apronCells[i]] = apronHeights[i]; }
					materials.RefreshAprons();
				}
				Check(before, "cliff-foot soil fringe rotations, exclusions, second-ring edits and foreground roof occlusion");
				Check(before, "convex cliff-foot fringes: eight orientations, closed flanks, ramp exclusions, third-ring Do/Undo/Redo and rollback");
				Check(before, "four water families: distant roof clipping, partial ramps in all directions, disjoint slices and terrain restoration");
				PlateauMovementProbe.WriteResult(info.ResultPath, materials.ValidateApronOcclusion());
				Check(before, "three-band occlusion matches square reference without terrain mutations");
				PlateauMovementProbe.WriteResult(info.ResultPath, "MATERIAL CACHE " + materials.ProfileCache(true));
				Check(before, "bounded worst-case material cache leaves map unchanged");
				var failing = new MapGeneration.PlateauTerrainEditAction(map,
					new System.Collections.Generic.Dictionary<CPos, TerrainTile> { [corner] = new TerrainTile(MapGeneration.RubberduckRockCoastPlan.BlockedSand, 0) },
					_ => throw new InvalidOperationException("Injected coast actor failure"), "Failure probe");
				var rolledBack = false;
				try { failing.Do(); } catch (InvalidOperationException) { rolledBack = true; }
				if (!rolledBack) throw new InvalidOperationException("Missing coast rollback failure."); Check(before, "tile transaction rollback");
				Ui.Root.Get<ButtonWidget>("ROCK_COAST").OnClick();
				var brush = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR").CurrentBrush as RockCoastBrush ?? throw new InvalidOperationException("Coast button failed.");
				map.Resources[corner] = new ResourceTile(1, 5); var refused = false;
				try { brush.CreateAction(corner); } catch (InvalidOperationException) { refused = true; }
				map.Resources[corner] = default;
				if (!refused) throw new InvalidOperationException("Coast ignored resource protection."); Check(before, "resource protection");
				brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left,
					wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(corner))), int2.Zero, Modifiers.None, 1));
				if (map.Tiles[corner].Type != MapGeneration.RubberduckRockCoastPlan.BlockedSand || map.AllCells.Any(c => map.Height[c] != 0))
					throw new InvalidOperationException("Coast click did not create a flat corner.");
				after = PlateauEditorProbe.Snapshot(map, layer); Check(after, "mouse paints corner and native pieces");
			}
			if (frames == 50) { history.Undo(); Check(before, "exact undo"); }
			if (frames == 70) { history.Redo(); Check(after, "exact redo"); }
			if (frames == 90) { history.Undo(); Check(before, "second undo"); history.Redo(); Check(after, "second redo"); }
			if (frames == 110)
			{
				var controller = Ui.Root.Get<EditorViewportControllerWidget>("MAP_EDITOR");
				var brush = controller.CurrentBrush as RockCoastBrush ?? throw new InvalidOperationException("Coast brush lost.");
				if (info.AllCorners)
				{
					history.Undo(); Check(before, "reset corner matrix");
					foreach (var test in new[] { (new CVec(12, 5), 24), (new CVec(-5, 12), 30), (new CVec(-12, -5), 28), (new CVec(5, -12), 26),
						(new CVec(5, 5), 25), (new CVec(-5, 5), 31), (new CVec(-5, -5), 29), (new CVec(5, -5), 27) })
					{
						var c = center + test.Item1; wr.Viewport.Center(map.CenterOfCell(c));
						brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left,
							wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(c))), int2.Zero, Modifiers.None, 1));
						if (!layer.Save().Any(n => n.Value.Value == "terrain.rubberduck.coast-piece-" + test.Item2)) throw new InvalidOperationException("Corner mouse composition failed: " + test.Item2);
						PlateauMovementProbe.WriteResult(info.ResultPath, "COAST EDITOR PASS corner " + test.Item2);
						var cornerSnapshot = PlateauEditorProbe.Snapshot(map, layer);
						var bank = test.Item2 % 2 == 0 ? c : c + MapGeneration.PlateauTopology.Directions.First(d => map.Contains(c + d) && (map.Tiles[c + d].Type == MapGeneration.RubberduckRockCoastPlan.BlockedSand || map.Tiles[c + d].Type == MapGeneration.RubberduckRockCoastPlan.BlockedGrass));
						if (test.Item2 == 31 || test.Item2 == 27)
						{
							var refused = false;
							try { brush.TrimAction(bank); } catch (InvalidOperationException) { refused = true; }
							if (!refused) throw new InvalidOperationException("Tight inner trim unexpectedly accepted: " + test.Item2);
							Check(cornerSnapshot, "inner trim protects end-piece clearance " + test.Item2);
						}
						else
						{
							history.Add(brush.TrimAction(bank));
							if (map.Tiles[bank].Type != 1000 && map.Tiles[bank].Type != 1020) throw new InvalidOperationException("Corner trim did not open its bank.");
							var trimmedCorner = PlateauEditorProbe.Snapshot(map, layer);
							Check(trimmedCorner, "corner trim " + test.Item2);
							if (test.Item2 % 2 == 0)
							{
								// The removed convex tip is an isolated cardinal land component.
								// Rejoining must not erase it (or any actor that could occupy it).
								var rejected = false;
								try { brush.CreateAction(new[] { bank }); } catch (InvalidOperationException) { rejected = true; }
								if (!rejected) throw new InvalidOperationException("Joining erased an isolated land component.");
								Check(trimmedCorner, "isolated corner rejoin rejected " + test.Item2);
							}
							else
							{
								history.Add(brush.CreateAction(new[] { bank }));
								if (!layer.Save().Any(n => n.Value.Value == "terrain.rubberduck.coast-piece-" + test.Item2)) throw new InvalidOperationException("Joining did not restore corner artwork.");
								PlateauMovementProbe.WriteResult(info.ResultPath, "COAST EDITOR PASS corner rejoin " + test.Item2);
								history.Undo(); Check(trimmedCorner, "corner rejoin undo " + test.Item2);
							}
							history.Undo(); Check(cornerSnapshot, "corner trim undo " + test.Item2);
						}
						history.Add(brush.RemoveAction(bank)); Check(before, "corner removal " + test.Item2);
						history.Undo(); Check(cornerSnapshot, "corner removal undo " + test.Item2);
						history.Undo(); Check(before, "corner undo " + test.Item2);
					}
					var start = center + new CVec(12, -3); var end = center + new CVec(12, 3);
					var legacyBanks = Enumerable.Range(-3, 7).Select(y => center + new CVec(12, y)).ToArray();
					var legacyPatch = new System.Collections.Generic.Dictionary<CPos, TerrainTile>();
					foreach (var bank in legacyBanks)
					{
						legacyPatch[bank] = new TerrainTile(14011, 0);
						for (var depth = 1; depth <= 2; depth++) legacyPatch[bank + new CVec(depth, 0)] = new TerrainTile(14010, 0);
					}
					var legacyActors = MapGeneration.RubberduckRockCoastRenderer.Actors(map, legacyBanks, "LegacyCoast", null, true);
					history.Add(new MapGeneration.PlateauTerrainEditAction(map, legacyPatch,
						MapGeneration.PlateauActorEdit.Replace(layer, Array.Empty<MiniYamlNode>(), legacyActors), "Legacy coast fixture"));
					var legacySnapshot = PlateauEditorProbe.Snapshot(map, layer);
					history.Add(brush.RemoveAction(start)); Check(before, "legacy coast footprint and artwork recovered together");
					history.Undo(); Check(legacySnapshot, "legacy removal exact undo"); history.Undo(); Check(before, "legacy fixture undo");
					wr.Viewport.Center(map.CenterOfCell(start));
					void DragInput(MouseInputEvent kind, CPos c) => brush.HandleMouseInput(new MouseInput(kind, MouseButton.Left,
						wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(c))), int2.Zero, Modifiers.Shift, 1));
					DragInput(MouseInputEvent.Down, start);
					DragInput(MouseInputEvent.Move, end);
					Check(before, "freehand draft does not mutate terrain");
					DragInput(MouseInputEvent.Up, end);
					if (Enumerable.Range(-3, 7).Any(y => map.Tiles[center + new CVec(12, y)].Type != MapGeneration.RubberduckRockCoastPlan.BlockedSand))
						throw new InvalidOperationException("Interpolated freehand coast missing bank cells.");
					var drawn = PlateauEditorProbe.Snapshot(map, layer);
					Check(drawn, "freehand interpolated mouse stroke");
					history.Undo(); Check(before, "freehand atomic undo");
					history.Redo(); Check(drawn, "freehand exact redo");
					history.Undo();
					map.Resources[end] = new ResourceTile(1, 5);
					var protectedSnapshot = PlateauEditorProbe.Snapshot(map, layer);
					DragInput(MouseInputEvent.Down, start); DragInput(MouseInputEvent.Up, end);
					Check(protectedSnapshot, "freehand rejects entire resource-crossing stroke");
					map.Resources[end] = default;
					history.Redo(); Check(drawn, "restore freehand stroke for save/reload");
					map.Resources[start] = new ResourceTile(1, 5);
					var refusedRemoval = false;
					try { brush.RemoveAction(start); } catch (InvalidOperationException) { refusedRemoval = true; }
					map.Resources[start] = default;
					if (!refusedRemoval) throw new InvalidOperationException("Removal ignored coast resources.");
					Check(drawn, "removal resource protection");
					var decoration = layer.Save().First(n => n.Value.Value.StartsWith("terrain.rubberduck.coast-", StringComparison.Ordinal));
					var missing = MapGeneration.PlateauActorEdit.Replace(layer, new[] { decoration }, Array.Empty<MiniYamlNode>());
					missing(true); refusedRemoval = false;
					try { brush.RemoveAction(start); } catch (InvalidOperationException) { refusedRemoval = true; }
					finally { missing(false); }
					if (!refusedRemoval) throw new InvalidOperationException("Removal accepted missing artwork.");
					Check(drawn, "removal rejects incomplete actor graph without mutation");
					brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left,
						wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(start))), int2.Zero, Modifiers.Ctrl, 1));
					Check(before, "Ctrl-click restores shore and removes artwork");
					history.Undo(); Check(drawn, "removal exact undo");
					history.Redo(); Check(before, "removal exact redo");
					history.Undo(); Check(drawn, "restore removed coast for save/reload");
					var middle = center + new CVec(12, 0);
					brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left,
						wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(middle))), int2.Zero, Modifiers.Alt, 1));
					if (map.Tiles[middle].Type != 1020) throw new InvalidOperationException("Alt-click did not open the bank.");
					var trimmed = PlateauEditorProbe.Snapshot(map, layer); Check(trimmed, "Alt-click trims freehand bank");
					history.Undo(); Check(drawn, "trim exact undo");
					history.Redo(); Check(trimmed, "trim exact redo and save layout");
					DragInput(MouseInputEvent.Down, middle); DragInput(MouseInputEvent.Up, middle);
					if (map.Tiles[middle].Type != MapGeneration.RubberduckRockCoastPlan.BlockedSand ||
						layer.Save().Count(n => n.Value.Value.StartsWith("terrain.rubberduck.coast-", StringComparison.Ordinal)) != 9)
						throw new InvalidOperationException("Joining failed to replace internal end caps.");
					var joined = PlateauEditorProbe.Snapshot(map, layer); Check(joined, "mouse joins split bank without internal caps");
					history.Undo(); Check(trimmed, "join exact undo");
					history.Redo(); Check(joined, "join exact redo");
					var extension = center + new CVec(12, 4);
					map.Resources[extension] = new ResourceTile(1, 5); var guarded = PlateauEditorProbe.Snapshot(map, layer);
					DragInput(MouseInputEvent.Down, extension); DragInput(MouseInputEvent.Up, extension);
					Check(guarded, "extension rejects resources without altering existing coast"); map.Resources[extension] = default;
					DragInput(MouseInputEvent.Down, end); DragInput(MouseInputEvent.Up, extension);
					if (map.Tiles[extension].Type != MapGeneration.RubberduckRockCoastPlan.BlockedSand) throw new InvalidOperationException("Overlapping extension failed.");
					var extended = PlateauEditorProbe.Snapshot(map, layer); Check(extended, "overlapping mouse stroke extends existing bank");
					history.Undo(); Check(joined, "extension exact undo");
					history.Redo(); Check(extended, "extension exact redo and save layout");
				}
				else foreach (var c in new[] { center + new CVec(-12, -6), center + new CVec(6, -12) })
				{
					wr.Viewport.Center(map.CenterOfCell(c));
					brush.HandleMouseInput(new MouseInput(MouseInputEvent.Down, MouseButton.Left,
						wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(map.CenterOfCell(c))), int2.Zero, Modifiers.None, 1));
					if (map.Tiles[c].Type != MapGeneration.RubberduckRockCoastPlan.BlockedSand) throw new InvalidOperationException("Rear coast mouse stamp failed.");
				}
				controller.ClearBrush(); wr.Viewport.Center(map.CenterOfCell(corner));
				after = PlateauEditorProbe.Snapshot(map, layer); Check(after, info.AllCorners ? "final freehand layout" : "rear coast stamps");
			}
			if (frames == 150)
			{
				PlateauMovementProbe.WriteResult(info.ResultPath, "COAST EDITOR saving");
				map.ActorDefinitions = layer.Save(); var path = Path.ChangeExtension(Path.GetFullPath(info.ResultPath), ".oramap");
				using (var package = ZipFileLoader.Create(path)) map.Save(package);
				using var stream = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, path); using var reload = new Map(Game.ModData, zip);
				if (reload.InvalidCustomRules) throw new InvalidOperationException("Saved coast rules invalid.", reload.InvalidCustomRulesException);
				foreach (var c in map.AllCells)
					if (map.Height[c] != reload.Height[c] || !map.Tiles[c].Equals(reload.Tiles[c]) || !map.Resources[c].Equals(reload.Resources[c])) throw new InvalidOperationException("Coast save changed terrain.");
				if (map.ActorDefinitions.OrderBy(n => n.Key).WriteToString() != reload.ActorDefinitions.OrderBy(n => n.Key).WriteToString()) throw new InvalidOperationException("Coast save changed actors.");
				PlateauMovementProbe.WriteResult(info.ResultPath, "COAST EDITOR PASS save/reload");
			}
			if (frames == 180) Game.TakeScreenshot();
		}
	}
}
