using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	sealed class PlateauRemovalBrush : IEditorBrush
	{
		readonly EditorViewportControllerWidget controller;
		readonly WorldRenderer wr;
		readonly Action<string> status;
		PlateauRemovalPlan preview;
		CPos mouseCell;
		long revision = -1;

		public PlateauRemovalBrush(EditorViewportControllerWidget controller, WorldRenderer wr, Action<string> status)
		{
			this.controller = controller;
			this.wr = wr;
			this.status = status;
			status("Hover an empty plateau roof to inspect; click to lower it, right-click cancels.");
		}

		PlateauRemovalPlan Plan(CPos cell)
		{
			var layer = wr.World.WorldActor.Trait<EditorActorLayer>();
			var plan = PlateauRemovalPlan.Create(wr.World.Map, layer.Save(), cell);
			var owned = plan.Decorations.Select(n => n.Key).ToHashSet(StringComparer.Ordinal);
			// An actor anchored outside the area can still have a footprint inside it.
			if (plan.Clearance.Any(c => layer.PreviewsAtCell(c).Any(a => !owned.Contains(a.ID))))
				throw new InvalidOperationException("An actor footprint occupies the plateau clearance.");
			return plan;
		}

		internal IEditorAction CreateAction(CPos cell)
		{
			// Always revalidate on click: hover state must never authorize a later edit.
			var plan = Plan(cell);
			var layer = wr.World.WorldActor.Trait<EditorActorLayer>();
			return new PlateauTerrainEditAction(wr.World.Map, plan.Patch, PlateauActorEdit.Patch(layer, plan.Decorations, true),
				"Lower empty plateau, ramps and cliff feet");
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up) { controller.ClearBrush(); return true; }
			if (mi.Button != MouseButton.Left) return false;
			if (mi.Event != MouseInputEvent.Down) return true;
			try
			{
				wr.World.WorldActor.Trait<EditorActionManager>().Add(CreateAction(wr.Viewport.ViewToWorld(mi.Location)));
				preview = null;
				status("Plateau lowered; Undo restores terrain, ramps and all cliff pieces.");
			}
			catch (ArgumentException e) { status(e.Message); }
			catch (InvalidOperationException e) { status(e.Message); }
			return true;
		}

		public void TickRender(WorldRenderer renderer, Actor self)
		{
			var mouse = renderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			var currentRevision = TerrainRenderRevision.Get(renderer.World.Map);
			if (revision == currentRevision && mouse == mouseCell) return;
			mouseCell = mouse;
			revision = currentRevision;
			try
			{
				preview = Plan(mouse);
				status($"Lower {preview.Patch.Count} cells and remove {preview.Decorations.Count} cliff pieces (one Undo step).");
			}
			catch (InvalidOperationException e) { preview = null; status(e.Message); }
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer renderer)
		{
			if (preview == null) yield break;
			foreach (var c in preview.Patch.Keys)
				foreach (var d in PlateauTopology.Directions)
					if (!preview.Patch.ContainsKey(c + d))
					{
						var center = renderer.World.Map.CenterOfCell(c);
						// Cell axes are diagonal in isometric world coordinates. Derive the
						// edges from the grid instead of treating CPos offsets as WVec axes.
						var outward = renderer.World.Map.CenterOfCell(c + d) - center;
						var lateral = renderer.World.Map.CenterOfCell(c + new CVec(-d.Y, d.X)) - center;
						outward = new WVec(outward.X, outward.Y, 0);
						lateral = new WVec(lateral.X, lateral.Y, 0);
						var a = center + (outward - lateral) / 2;
						var b = center + (outward + lateral) / 2;
						yield return new LineAnnotationRenderable(a, b, 2, Color.Red);
					}
		}
		public IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer renderer) { yield break; }
		public void Tick() { }
		public void Dispose() { }
	}
}
