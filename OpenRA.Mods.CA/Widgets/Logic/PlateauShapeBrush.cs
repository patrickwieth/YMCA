using System;
using System.Collections.Generic;
using System.IO;
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
	sealed class PlateauShapeBrush : IEditorBrush
	{
		readonly EditorViewportControllerWidget controller;
		readonly WorldRenderer wr;
		readonly bool grow;
		readonly Action<string> status;
		readonly HashSet<CPos> stroke = new();
		CPos last;
		bool painting;
		bool oversized;

		public PlateauShapeBrush(EditorViewportControllerWidget controller, WorldRenderer wr, bool grow, Action<string> status)
		{
			this.controller = controller; this.wr = wr; this.grow = grow; this.status = status;
			status("Drag a 3-wide stroke; Shift: single cell. Release to validate/apply; right-click cancels.");
		}

		internal static HashSet<CPos> Protection(EditorActorLayer layer)
		{
			return layer.Save().Where(n => !PlateauRemovalPlan.Decoration(n.Value.Value)).SelectMany(n =>
			{
				var preview = layer[n.Key];
				return preview.Footprint.Count == 0 ? new[] { preview.Location }.AsEnumerable() : preview.Footprint.Keys;
			}).ToHashSet();
		}

		internal PlateauShapePlan Plan(IEnumerable<CPos> cells)
		{
			var layer = wr.World.WorldActor.Trait<EditorActorLayer>();
			return PlateauShapePlan.Create(wr.World.Map, layer.Save(), cells, grow, Protection(layer));
		}

		internal IEditorAction CreateAction(IEnumerable<CPos> cells)
		{
			var plan = Plan(cells);
			return new PlateauTerrainEditAction(wr.World.Map, plan.Patch,
				PlateauActorEdit.Replace(wr.World.WorldActor.Trait<EditorActorLayer>(), plan.BeforeActors, plan.AfterActors),
				grow ? "Extend/join plateau and rebuild cliffs" : "Trim plateau and rebuild cliffs");
		}

		void Extend(CPos cell, int radius)
		{
			if (oversized) return;
			var steps = Math.Max(Math.Abs(cell.X - last.X), Math.Abs(cell.Y - last.Y));
			if (steps > 2048) { oversized = true; return; }
			for (var step = 0; step <= steps; step++)
			{
				var center = steps == 0 ? cell : new CPos(last.X + (cell.X - last.X) * step / steps, last.Y + (cell.Y - last.Y) * step / steps);
				for (var x = -radius; x <= radius; x++)
					for (var y = -radius; y <= radius; y++) stroke.Add(center + new CVec(x, y));
				if (stroke.Count > 2048) { oversized = true; break; }
			}
			last = cell;
			status($"Draft: {stroke.Count} cells. Release to check access, protection and cliff geometry.");
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up)
			{ controller.YieldMouseFocus(mi); controller.ClearBrush(); return true; }
			if (mi.Event == MouseInputEvent.Move && painting)
			{ Extend(wr.Viewport.ViewToWorld(mi.Location), mi.Modifiers.HasModifier(Modifiers.Shift) ? 0 : 1); return true; }
			if (mi.Button != MouseButton.Left) return false;
			if (mi.Event == MouseInputEvent.Down)
			{
				if (!controller.TakeMouseFocus(mi)) return false;
				painting = true; oversized = false; stroke.Clear();
				last = wr.Viewport.ViewToWorld(mi.Location);
				Extend(last, mi.Modifiers.HasModifier(Modifiers.Shift) ? 0 : 1);
				return true;
			}
			if (mi.Event == MouseInputEvent.Up && painting)
			{
				Extend(wr.Viewport.ViewToWorld(mi.Location), mi.Modifiers.HasModifier(Modifiers.Shift) ? 0 : 1);
				painting = false;
				controller.YieldMouseFocus(mi);
				try
				{
					if (oversized) throw new InvalidOperationException("Stroke too large; use at most 2048 cells.");
					wr.World.WorldActor.Trait<EditorActionManager>().Add(CreateAction(stroke));
					status("Plateau shape and cliff pieces updated together (one Undo step).");
				}
				catch (ArgumentException e) { status(e.Message); }
				catch (InvalidOperationException e) { status(e.Message); }
				catch (InvalidDataException e) { status(e.Message); }
				finally { stroke.Clear(); }
			}
			return true;
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer renderer)
		{
			foreach (var c in stroke)
			{
				if (!renderer.World.Map.Contains(c)) continue;
				var center = renderer.World.Map.CenterOfCell(c);
				foreach (var d in PlateauTopology.Directions)
				{
					if (stroke.Contains(c + d)) continue;
					var normal = renderer.World.Map.CenterOfCell(c + d) - center;
					var tangent = renderer.World.Map.CenterOfCell(c + new CVec(-d.Y, d.X)) - center;
					normal = new WVec(normal.X, normal.Y, 0); tangent = new WVec(tangent.X, tangent.Y, 0);
					yield return new LineAnnotationRenderable(center + (normal - tangent) / 2, center + (normal + tangent) / 2, 2, Color.Yellow);
				}
			}
		}
		public IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer renderer) { yield break; }
		public void TickRender(WorldRenderer renderer, Actor self) { }
		public void Tick() { }
		public void Dispose()
		{
			painting = false; stroke.Clear();
			if (Ui.MouseFocusWidget == controller) controller.YieldMouseFocus(default);
		}
	}
}
