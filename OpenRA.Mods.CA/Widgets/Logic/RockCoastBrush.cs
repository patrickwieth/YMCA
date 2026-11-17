using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	sealed class RockCoastBrush : IEditorBrush
	{
		readonly EditorViewportControllerWidget controller;
		readonly WorldRenderer wr;
		readonly Action<string> status;
		HashSet<CPos> preview = new();
		readonly HashSet<CPos> stroke = new();
		CPos last;
		bool painting;
		bool oversized;
		public RockCoastBrush(EditorViewportControllerWidget controller, WorldRenderer wr, Action<string> status)
		{
			this.controller = controller; this.wr = wr; this.status = status;
			status("Click: coast stamp. Shift-drag: draw/extend/join banks. Ctrl-click: remove whole coast. Alt-click: trim one bank cell. Right-click cancels.");
		}
		internal static HashSet<CPos> Banks(Map map, CPos cell)
		{
			bool Water(CPos c) => map.Contains(c) && (map.Tiles[c].Type == 1050 || map.Tiles[c].Type == RubberduckRockCoastPlan.BlockedWater);
			var water = Enumerable.Range(0, 4).Where(d => Water(cell + PlateauTopology.Directions[d])).ToArray();
			if (water.Length == 2 && (water[0] + 2) % 4 != water[1])
				// Leave clearance for the end piles before another nearby coast turn.
				return Enumerable.Range(0, 5).SelectMany(i => new[] { cell - PlateauTopology.Directions[water[0]] * i, cell - PlateauTopology.Directions[water[1]] * i }).ToHashSet();
			if (water.Length == 0)
			{
				var bays = Enumerable.Range(0, 4).Where(d => Water(cell + PlateauTopology.Directions[d] + PlateauTopology.Directions[(d + 1) % 4])).ToArray();
				if (bays.Length == 1)
					return Enumerable.Range(1, 7).SelectMany(i => new[] { cell + PlateauTopology.Directions[bays[0]] * i, cell + PlateauTopology.Directions[(bays[0] + 1) % 4] * i }).ToHashSet();
			}
			if (water.Length != 1) throw new InvalidOperationException("Select a straight shore or an unambiguous coast corner.");
			var tangent = PlateauTopology.Directions[(water[0] + 1) % 4];
			return Enumerable.Range(-3, 7).Select(i => cell + tangent * i).ToHashSet();
		}
		internal IEditorAction CreateAction(CPos cell)
		{
			return CreateAction(Banks(wr.World.Map, cell));
		}

		internal IEditorAction CreateAction(IEnumerable<CPos> cells)
		{
			var map = wr.World.Map; var layer = wr.World.WorldActor.Trait<EditorActorLayer>();
			var plan = RubberduckCoastExtensionPlan.Create(map, layer, cells);
			return new PlateauTerrainEditAction(map, plan.Patch, PlateauActorEdit.Replace(layer, plan.BeforeActors, plan.AfterActors), "Draw/extend/join rock coast and rebuild ends");
		}
		internal IEditorAction RemoveAction(CPos cell)
		{
			var layer = wr.World.WorldActor.Trait<EditorActorLayer>();
			var plan = RubberduckCoastRemovalPlan.Create(wr.World.Map, layer, cell);
			return new PlateauTerrainEditAction(wr.World.Map, plan.Patch,
				PlateauActorEdit.Replace(layer, plan.Actors, Array.Empty<MiniYamlNode>()), "Remove complete rock coast and restore shore");
		}

		internal IEditorAction TrimAction(CPos cell)
		{
			var map = wr.World.Map; var layer = wr.World.WorldActor.Trait<EditorActorLayer>();
			if (!map.Contains(cell) || (map.Tiles[cell].Type != RubberduckRockCoastPlan.BlockedSand && map.Tiles[cell].Type != RubberduckRockCoastPlan.BlockedGrass))
				throw new InvalidOperationException("Alt-click a blocked land bank, not the water footprint.");
			var recovered = RubberduckCoastRemovalPlan.Create(map, layer, cell);
			var banks = recovered.Patch.Keys.Where(c => c != cell && map.Tiles[c].Type != RubberduckRockCoastPlan.BlockedWater).ToArray();
			var patch = recovered.Patch.ToDictionary(p => p.Key, p => p.Value);
			var actors = new List<MiniYamlNode>();
			if (banks.Length != 0)
			{
				ushort Restored(CPos c) => recovered.Patch.TryGetValue(c, out var tile) ? tile.Type : map.Tiles[c].Type;
				var remaining = RubberduckRockCoastPlan.Footprint(map, banks, Array.Empty<CPos>(), Restored);
				// Trimming may only open terrain; it must never acquire another footprint.
				if (remaining.Keys.Any(c => !patch.ContainsKey(c))) throw new InvalidOperationException("Trimming would expand the rock footprint.");
				foreach (var p in remaining) patch[p.Key] = p.Value;
				var serial = 0;
				while (layer.Save().Any(n => n.Key.StartsWith($"EditorRockCoast{serial}_", StringComparison.Ordinal))) serial++;
				actors = RubberduckRockCoastRenderer.Actors(map, banks, $"EditorRockCoast{serial}_", Restored);
			}
			return new PlateauTerrainEditAction(map, patch, PlateauActorEdit.Replace(layer, recovered.Actors, actors), "Trim rock coast and rebuild ends");
		}

		void Extend(CPos cell)
		{
			if (oversized) return;
			var steps = Math.Max(Math.Abs(cell.X - last.X), Math.Abs(cell.Y - last.Y));
			if (steps > 512) { oversized = true; return; }
			for (var i = 0; i <= steps; i++)
			{
				var c = steps == 0 ? cell : new CPos(last.X + (cell.X - last.X) * i / steps, last.Y + (cell.Y - last.Y) * i / steps);
				stroke.Add(c);
				if (stroke.Count > 512) { oversized = true; break; }
			}
			last = cell;
			status($"Draft coast: {stroke.Count} bank cells. Release to check geometry, resources and access.");
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up)
			{ controller.YieldMouseFocus(mi); controller.ClearBrush(); return true; }
			if (mi.Event == MouseInputEvent.Move && painting)
			{ Extend(wr.Viewport.ViewToWorld(mi.Location)); return true; }
			if (mi.Button != MouseButton.Left) return false;
			if (mi.Event == MouseInputEvent.Down && mi.Modifiers.HasModifier(Modifiers.Shift) && !mi.Modifiers.HasModifier(Modifiers.Ctrl) && !mi.Modifiers.HasModifier(Modifiers.Alt))
			{
				if (!controller.TakeMouseFocus(mi)) return false;
				painting = true; oversized = false; stroke.Clear();
				last = wr.Viewport.ViewToWorld(mi.Location); Extend(last);
				return true;
			}
			if (mi.Event == MouseInputEvent.Up && painting)
			{
				Extend(wr.Viewport.ViewToWorld(mi.Location));
				painting = false; controller.YieldMouseFocus(mi);
				try
				{
					if (oversized) throw new InvalidOperationException("Coast stroke too large; use at most 512 bank cells.");
					wr.World.WorldActor.Trait<EditorActionManager>().Add(CreateAction(stroke));
					status("Freehand coast added; terrain and artwork share one Undo step.");
				}
				catch (ArgumentException e) { status(e.Message); }
				catch (InvalidOperationException e) { status(e.Message); }
				finally { stroke.Clear(); preview.Clear(); }
				return true;
			}
			if (mi.Event != MouseInputEvent.Down) return true;
			try
			{
				var remove = mi.Modifiers.HasModifier(Modifiers.Ctrl);
				var trim = mi.Modifiers.HasModifier(Modifiers.Alt);
				var cell = wr.Viewport.ViewToWorld(mi.Location);
				wr.World.WorldActor.Trait<EditorActionManager>().Add(remove ? RemoveAction(cell) : trim ? TrimAction(cell) : CreateAction(cell));
				status(remove ? "Connected rock coast removed; shore and artwork share one Undo step." : trim ? "Bank trimmed; remaining rock ends rebuilt (one Undo step)." : "Flat rock coast added; terrain and artwork share one Undo step.");
			}
			catch (ArgumentException e) { status(e.Message); }
			catch (InvalidOperationException e) { status(e.Message); }
			return true;
		}
		public void TickRender(WorldRenderer renderer, Actor self)
		{
			if (painting) { preview = new HashSet<CPos>(stroke); return; }
			try { preview = Banks(wr.World.Map, wr.Viewport.ViewToWorld(Viewport.LastMousePos)); }
			catch (InvalidOperationException) { preview.Clear(); }
		}
		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer renderer)
		{
			foreach (var c in preview.Where(renderer.World.Map.Contains))
				foreach (var d in PlateauTopology.Directions)
				{
					if (preview.Contains(c + d)) continue;
					var center = renderer.World.Map.CenterOfCell(c);
					var normal = renderer.World.Map.CenterOfCell(c + d) - center;
					var tangent = renderer.World.Map.CenterOfCell(c + new CVec(-d.Y, d.X)) - center;
					yield return new LineAnnotationRenderable(center + (normal - tangent) / 2, center + (normal + tangent) / 2, 2, Color.Yellow);
				}
		}
		public IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer renderer) { yield break; }
		public void Tick() { }
		public void Dispose()
		{
			painting = false; stroke.Clear(); preview.Clear();
			if (Ui.MouseFocusWidget == controller) controller.YieldMouseFocus(default);
		}
	}
}
