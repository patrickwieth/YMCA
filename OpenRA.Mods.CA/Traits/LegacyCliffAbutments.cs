using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// Visual support for saved, mixed native/vertical contours. No actor, tile,
	// resource or collision edits; new closed-family components are not selected.
	sealed class LegacyCliffAbutments : IDisposable
	{
		readonly World world;
		readonly EditorActorLayer editor;
		readonly EditorActionManager actions;
		readonly NativeCliffBody body = new NativeCliffBody(new NativeCliffBodyInfo(true));
		readonly CellCoordsRegion allCells;
		readonly List<CliffSliceRenderable> pieces = new();
		readonly HashSet<CPos> selectedCells = new();
		bool hasLegacy;
		long revision = -1;
		bool dirty = true;

		public LegacyCliffAbutments(World world)
		{
			this.world = world;
			editor = world.WorldActor.TraitOrDefault<EditorActorLayer>();
			actions = world.WorldActor.TraitOrDefault<EditorActionManager>();
			var cells = world.Map.AllCells;
			allCells = new CellCoordsRegion(new CPos(cells.Min(c => c.X), cells.Min(c => c.Y)), new CPos(cells.Max(c => c.X), cells.Max(c => c.Y)));
			world.ActorAdded += ActorChanged;
			world.ActorRemoved += ActorChanged;
			world.Map.Resources.CellEntryChanged += ResourceChanged;
			if (actions != null) { actions.ItemAdded += ActionAdded; actions.OnChange += Changed; }
		}
		void Changed() { dirty = true; }
		void ResourceChanged(CPos cell) { if (hasLegacy) dirty = true; }
		void ActionAdded(EditorActionContainer action) { dirty = true; }
		void ActorChanged(Actor actor)
		{
			if (actor.Info.HasTraitInfo<NativeCliffBodyInfo>() || hasLegacy && (actor.Info.HasTraitInfo<PlateauFaceBodyInfo>() || actor.Info.HasTraitInfo<BuildingInfo>())) dirty = true;
		}

		internal static HashSet<CPos> Select(Map map, IEnumerable<(ActorInfo Info, CPos Cell)> actors)
		{
			var entries = actors.ToArray();
			var native = entries.Where(a => a.Info.TraitInfoOrDefault<NativeCliffBodyInfo>() is NativeCliffBodyInfo b && !b.SourceGrid && !b.GroundAbutment)
				.Select(a => a.Cell).ToHashSet();
			var result = new HashSet<CPos>();
			foreach (var actor in entries)
			{
				var face = actor.Info.TraitInfoOrDefault<PlateauFaceBodyInfo>();
				if (face == null || face.Direction > 1 || face.DropA != 4 || face.DropB != 4 || !map.Contains(actor.Cell) ||
					map.Height[actor.Cell] != 4 || map.Ramp[actor.Cell] != 0 || map.Tiles[actor.Cell].Type == 3992) continue;
				// Only a continuing vertical run with an unreserved diagonal socket
				// exposes this broad apron. Ordinary convex ends already have native caps.
				var successor = actor.Cell + PlateauTopology.Directions[1 - face.Direction];
				var diagonal = successor + PlateauTopology.Directions[face.Direction];
				if (!map.Contains(successor) || map.Height[successor] != 4 || map.Ramp[successor] != 0 ||
					!map.Contains(diagonal) || map.Height[diagonal] != 0 || map.Ramp[diagonal] != 0 || map.Tiles[diagonal].Type == 3992) continue;
				if (!Enumerable.Range(-1, 3).Any(x => Enumerable.Range(-1, 3).Any(y => native.Contains(actor.Cell + new CVec(x, y))))) continue;
				var foot = actor.Cell + PlateauTopology.Directions[face.Direction];
				foreach (var cell in new[] { foot, foot - PlateauTopology.Directions[1 - face.Direction] })
					if (map.Contains(cell) && map.Height[cell] == 0 && map.Ramp[cell] == 0 && map.Tiles[cell].Type == 3992 && map.Resources[cell].Type == 0)
						result.Add(cell);
			}
			foreach (var actor in entries.Where(a => !a.Info.Name.StartsWith("terrain.rubberduck.", StringComparison.Ordinal)))
			{
				var building = actor.Info.TraitInfoOrDefault<BuildingInfo>();
				if (building != null) result.ExceptWith(building.Tiles(actor.Cell));
				var occupied = actor.Info.TraitInfoOrDefault<IOccupySpaceInfo>()?.OccupiedCells(actor.Info, actor.Cell, SubCell.Any);
				if (occupied != null) result.ExceptWith(occupied.Keys);
			}
			result.ExceptWith(entries.Where(a => a.Info.TraitInfoOrDefault<NativeCliffBodyInfo>()?.GroundAbutment == true).Select(a => a.Cell));
			return result;
		}

		public void Prepare() => Refresh();
		public bool At(CPos cell) { Refresh(); return selectedCells.Contains(cell); }

		void Refresh()
		{
			var next = TerrainRenderRevision.Get(world.Map);
			if (!dirty && revision == next) return;
			var actors = editor != null
				? editor.PreviewsInCellRegion(allCells).Select(a => (a.Info, a.Location))
				: world.Actors.Where(a => a.OccupiesSpace != null).Select(a => (a.Info, a.Location));
			var entries = actors.ToArray();
			hasLegacy = entries.Any(a => a.Info.TraitInfoOrDefault<NativeCliffBodyInfo>() is NativeCliffBodyInfo native && !native.SourceGrid && !native.GroundAbutment);
			pieces.Clear(); selectedCells.Clear();
			foreach (var cell in Select(world.Map, entries).OrderBy(c => c.X).ThenBy(c => c.Y))
			{
				var slices = body.Build(world.Map, cell, "terrain.rubberduck.coast-abutment-0");
				if (slices.Length != 0)
				{
					selectedCells.Add(cell);
					pieces.Add(new CliffSliceRenderable(slices, world.Map.CenterOfCell(cell), 4 * 724));
				}
				foreach (var atlas in slices.Select(s => s.Sheet).Distinct()) atlas.GetTexture();
			}
			revision = next; dirty = false;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			Refresh();
			var view = Rectangle.FromLTRB(wr.Viewport.TopLeft.X, wr.Viewport.TopLeft.Y, wr.Viewport.BottomRight.X, wr.Viewport.BottomRight.Y);
			foreach (var piece in pieces)
				if (piece.ScreenBounds(wr).IntersectsWith(view)) yield return piece;
		}
		public void Dispose()
		{
			world.ActorAdded -= ActorChanged; world.ActorRemoved -= ActorChanged;
			world.Map.Resources.CellEntryChanged -= ResourceChanged;
			if (actions != null) { actions.ItemAdded -= ActionAdded; actions.OnChange -= Changed; }
			pieces.Clear();
		}
	}
}
