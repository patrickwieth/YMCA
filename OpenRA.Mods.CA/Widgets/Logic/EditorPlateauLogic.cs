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
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class EditorPlateauLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public EditorPlateauLogic(Widget widget, WorldRenderer worldRenderer)
		{
			var controller = widget.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			var place = widget.Get<ButtonWidget>("PLATEAU_PLACE");
			var erase = widget.Get<ButtonWidget>("PLATEAU_ERASE");
			var lower = widget.Get<ButtonWidget>("PLATEAU_LOWER");
			var grow = widget.Get<ButtonWidget>("PLATEAU_GROW");
			var trim = widget.Get<ButtonWidget>("PLATEAU_TRIM");
			var ramp = widget.Get<ButtonWidget>("PLATEAU_RAMP");
			var addRamp = widget.Get<ButtonWidget>("PLATEAU_RAMP_ADD");
			var closeRamp = widget.Get<ButtonWidget>("PLATEAU_RAMP_CLOSE");
			var rockCoast = widget.Get<ButtonWidget>("ROCK_COAST");
			var status = widget.Get<LabelWidget>("PLATEAU_STATUS");
			var supported = worldRenderer.World.Map.Rules.TerrainInfo.Id.StartsWith("RUBBERDUCK-", StringComparison.Ordinal);
			place.IsVisible = erase.IsVisible = lower.IsVisible = grow.IsVisible = trim.IsVisible = ramp.IsVisible = addRamp.IsVisible = closeRamp.IsVisible = rockCoast.IsVisible = status.IsVisible = () => supported;
			var message = "Plateau stamp includes blocked cliff feet; right-click cancels.";
			place.GetText = () => "Plateau + four ramps";
			erase.GetText = () => "Remove plateau stamp";
			lower.GetText = () => "Lower empty plateau";
			grow.GetText = () => "Grow / join plateau";
			trim.GetText = () => "Trim plateau";
			ramp.GetText = () => "Move ramp";
			addRamp.GetText = () => "Add ramp";
			closeRamp.GetText = () => "Close ramp";
			rockCoast.GetText = () => "Rock coast";
			rockCoast.OnClick = () => controller.SetBrush(new RockCoastBrush(controller, worldRenderer, s => message = s));
			addRamp.OnClick = () => controller.SetBrush(new PlateauRampBrush(controller, worldRenderer, s => message = s, PlateauRampOperation.Add));
			closeRamp.OnClick = () => controller.SetBrush(new PlateauRampBrush(controller, worldRenderer, s => message = s, PlateauRampOperation.Close));
			ramp.OnClick = () => controller.SetBrush(new PlateauRampBrush(controller, worldRenderer, s => message = s));
			grow.OnClick = () => controller.SetBrush(new PlateauShapeBrush(controller, worldRenderer, true, s => message = s));
			trim.OnClick = () => controller.SetBrush(new PlateauShapeBrush(controller, worldRenderer, false, s => message = s));
			lower.OnClick = () => controller.SetBrush(new PlateauRemovalBrush(controller, worldRenderer, s => message = s));
			status.GetText = () => message;
			place.OnClick = () => controller.SetBrush(new PlateauStampBrush(controller, worldRenderer, false, s => message = s));
			erase.OnClick = () => controller.SetBrush(new PlateauStampBrush(controller, worldRenderer, true, s => message = s));
		}
	}

	sealed class PlateauStampBrush : IEditorBrush
	{
		readonly EditorViewportControllerWidget controller;
		readonly WorldRenderer wr;
		readonly bool erase;
		readonly Action<string> status;
		CPos cell;
		CPos mouseCell;
		long previewRevision = -1;
		public PlateauStampBrush(EditorViewportControllerWidget controller, WorldRenderer wr, bool erase, Action<string> status)
		{ this.controller = controller; this.wr = wr; this.erase = erase; this.status = status; }
		public bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up) { controller.ClearBrush(); return true; }
			if (mi.Button != MouseButton.Left) return false;
			if (mi.Event != MouseInputEvent.Down) return true;
			try
			{
				var action = CreateAction(wr.Viewport.ViewToWorld(mi.Location));
				wr.World.WorldActor.Trait<EditorActionManager>().Add(action);
				status(erase ? "Plateau removed (one Undo step)." : "Plateau and blocked cliff feet placed (one Undo step).");
			}
			catch (ArgumentException e) { status(e.Message); }
			catch (InvalidOperationException e) { status(e.Message); }
			return true;
		}
		internal IEditorAction CreateAction(CPos clicked)
		{
			var map = wr.World.Map;
			var layer = wr.World.WorldActor.Trait<EditorActorLayer>();
			var center = clicked;
			var saved = layer.Save();
			if (erase && !TryFindCenter(saved, clicked, out center))
				throw new InvalidOperationException("Click a plateau created with this stamp tool.");
			var prefix = $"EditorPlateau_{center.X}_{center.Y}_";
			var topology = PlateauTopology.CalibrationPlateau(center);
			// New stamps reserve the complete native foot ring. Legacy stamps remain
			// removable with their original footprint; never reinterpret saved terrain.
			if (!erase || saved.Any(n => n.Key.StartsWith(prefix, StringComparison.Ordinal) && n.Value.Value.StartsWith("terrain.rubberduck.closedcliff", StringComparison.Ordinal)))
				topology = PlateauTopology.WithCliffFeet(topology);
			for (var dx = -10; dx <= 10; dx++)
				for (var dy = -10; dy <= 10; dy++)
				{
					var c = center + new CVec(dx, dy);
					if (!map.Contains(c)) throw new InvalidOperationException("Leave ten cells of space to the map boundary.");
					var inside = topology.TryGetValue(c, out var s);
					var expectedHeight = erase && inside ? s.Height : (byte)0;
					var expectedTile = erase && inside ? s.Blocked ? 3992 : s.Ramp == 0 ? 1000 : 14000 + s.Ramp : 1000;
					if (map.Height[c] != expectedHeight || map.Tiles[c].Type != expectedTile || map.Resources[c].Type != 0)
						throw new InvalidOperationException("Needs isolated clear ground; changed stamps or resources are protected.");
					if (layer.PreviewsAtCell(c).Any(a => !erase || !a.ID.StartsWith(prefix, StringComparison.Ordinal)))
						throw new InvalidOperationException("An actor or another terrain object occupies the edit area.");
				}
			List<MiniYamlNode> definitions;
			if (erase) definitions = saved.Where(n => n.Key.StartsWith(prefix, StringComparison.Ordinal)).ToList();
			else
			{
				definitions = new List<MiniYamlNode>();
				RubberduckPlateauRenderer.ComposeNative(map, topology, definitions, new HashSet<(CPos, int)>(),
					Array.Empty<(CPos, int)>(), "ClosedCliff", false);
				if (!definitions.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.closedcliff", StringComparison.Ordinal)))
					throw new InvalidOperationException("The complete native cliff vocabulary is unavailable for this stamp.");
				definitions = definitions.Select((n, i) => new MiniYamlNode(prefix + i, n.Value)).ToList();
			}
			var patch = erase ? topology.Keys.ToDictionary(c => c, _ => new PlateauSurface(0)) : topology;
			return new PlateauTerrainEditAction(map, patch, PlateauActorEdit.Patch(layer, definitions, erase), erase ? "Remove plateau and four ramps" : "Place plateau and four ramps");
		}
		public void Tick() { }
		public void Dispose() { }
		static bool TryFindCenter(IEnumerable<MiniYamlNode> saved, CPos clicked, out CPos center)
		{
			center = clicked;
			foreach (var id in saved.Select(n => n.Key).Where(id => id.StartsWith("EditorPlateau_", StringComparison.Ordinal)))
			{
				var parts = id.Split('_');
				if (parts.Length != 4 || !int.TryParse(parts[1], out var x) || !int.TryParse(parts[2], out var y)) continue;
				if (Math.Abs(clicked.X - x) > 7 || Math.Abs(clicked.Y - y) > 7) continue;
				center = new CPos(x, y);
				return true;
			}
			return false;
		}
		public void TickRender(WorldRenderer renderer, Actor self)
		{
			var mouse = renderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			var revision = TerrainRenderRevision.Get(renderer.World.Map);
			if (mouse == mouseCell && revision == previewRevision) return;
			mouseCell = cell = mouse;
			previewRevision = revision;
			if (erase) TryFindCenter(renderer.World.WorldActor.Trait<EditorActorLayer>().Save(), mouse, out cell);
		}
		public IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer renderer) { yield break; }
		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer renderer)
		{
			var corners = new[] { new CVec(-8, -8), new CVec(8, -8), new CVec(8, 8), new CVec(-8, 8) };
			for (var i = 0; i < 4; i++)
				if (renderer.World.Map.Contains(cell + corners[i]) && renderer.World.Map.Contains(cell + corners[(i + 1) % 4]))
					yield return new LineAnnotationRenderable(renderer.World.Map.CenterOfCell(cell + corners[i]),
						renderer.World.Map.CenterOfCell(cell + corners[(i + 1) % 4]), 2, erase ? Color.Red : Color.LimeGreen);
		}
	}
}
