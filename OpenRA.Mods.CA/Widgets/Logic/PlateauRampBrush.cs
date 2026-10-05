using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	enum PlateauRampOperation { Move, Add, Close }

	sealed class PlateauRampBrush : IEditorBrush
	{
		readonly EditorViewportControllerWidget controller;
		readonly WorldRenderer wr;
		readonly Action<string> status;
		readonly PlateauRampOperation operation;
		PlateauRampMovePlan selected;
		public PlateauRampBrush(EditorViewportControllerWidget controller, WorldRenderer wr, Action<string> status,
			PlateauRampOperation operation = PlateauRampOperation.Move)
		{
			this.controller = controller; this.wr = wr; this.status = status; this.operation = operation;
			status(operation == PlateauRampOperation.Add ? "Click a free straight roof edge to add a ramp. Right-click cancels." :
				operation == PlateauRampOperation.Close ? "Click a ramp to close it; the last accessible route is protected. Right-click cancels." :
				"Select an existing ramp, then click the new straight roof edge. Right-click cancels.");
		}
		internal void Select(CPos cell) => selected = PlateauRampMovePlan.Find(wr.World.Map, cell);
		internal IEditorAction CreateAction(CPos target)
		{
			if (operation == PlateauRampOperation.Move && selected == null) throw new InvalidOperationException("Select a ramp first.");
			var layer = wr.World.WorldActor.Trait<EditorActorLayer>();
			var protection = PlateauShapeBrush.Protection(layer);
			var plan = operation == PlateauRampOperation.Add ? PlateauRampMovePlan.Add(wr.World.Map, layer.Save(), target, protection) :
				operation == PlateauRampOperation.Close ? PlateauRampMovePlan.Find(wr.World.Map, target).Close(wr.World.Map, layer.Save(), protection) :
				selected.Move(wr.World.Map, layer.Save(), target, protection);
			return new PlateauTerrainEditAction(wr.World.Map, plan.Patch,
				PlateauActorEdit.Replace(layer, plan.BeforeActors, plan.AfterActors), operation + " ramp and rebuild cliffs");
		}
		public bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up) { controller.ClearBrush(); return true; }
			if (mi.Button != MouseButton.Left) return false;
			if (mi.Event != MouseInputEvent.Down) return true;
			try
			{
				var cell = wr.Viewport.ViewToWorld(mi.Location);
				if (operation == PlateauRampOperation.Move && selected == null) { Select(cell); status("Ramp selected. Click a free straight roof edge; orientation is inferred from the edge."); }
				else
				{
					wr.World.WorldActor.Trait<EditorActionManager>().Add(CreateAction(cell));
					selected = null;
					status(operation + " ramp completed with cliff rebuild (one Undo step).");
				}
			}
			catch (ArgumentException e) { status(e.Message); }
			catch (InvalidOperationException e) { status(e.Message); }
			catch (InvalidDataException e) { status(e.Message); }
			return true;
		}
		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer renderer)
		{
			if (selected == null) yield break;
			foreach (var c in selected.Original.Keys)
				foreach (var d in PlateauTopology.Directions)
				{
					if (selected.Original.ContainsKey(c + d)) continue;
					var center = renderer.World.Map.CenterOfCell(c);
					var normal = renderer.World.Map.CenterOfCell(c + d) - center;
					var tangent = renderer.World.Map.CenterOfCell(c + new CVec(-d.Y, d.X)) - center;
					normal = new WVec(normal.X, normal.Y, 0); tangent = new WVec(tangent.X, tangent.Y, 0);
					yield return new LineAnnotationRenderable(center + (normal - tangent) / 2, center + (normal + tangent) / 2, 2, Color.Yellow);
				}
		}
		public IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer renderer) { yield break; }
		public void TickRender(WorldRenderer renderer, Actor self) { }
		public void Tick() { }
		public void Dispose() { selected = null; }
	}
}
