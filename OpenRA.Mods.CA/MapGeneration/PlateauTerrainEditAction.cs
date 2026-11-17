using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	// All cell values are captured once. Redo replays the same values and never
	// rerolls variants or accumulates a second undo snapshot.
	sealed class PlateauTerrainEditAction : IEditorAction
	{
		readonly Map map;
		readonly Dictionary<CPos, (TerrainTile Tile, byte Height)> before;
		readonly Dictionary<CPos, (TerrainTile Tile, byte Height)> after;
		readonly Action<bool> updateActors;
		bool applied;
		public string Text { get; }

		public PlateauTerrainEditAction(Map map, IReadOnlyDictionary<CPos, PlateauSurface> patch,
			Action<bool> updateActors, string text)
		{
			this.map = map;
			this.updateActors = updateActors;
			Text = text;
			if (patch.Count == 0 || patch.Any(p => !map.Contains(p.Key) || p.Value.Height > map.Grid.MaximumTerrainHeight))
				throw new ArgumentException("Plateau edit lies outside editable terrain.", nameof(patch));
			before = patch.Keys.ToDictionary(c => c, c => (map.Tiles[c], map.Height[c]));
			after = patch.ToDictionary(p => p.Key, p => (new TerrainTile(p.Value.Blocked ? (ushort)3992 :
				p.Value.Ramp == 0 ? (ushort)1000 : (ushort)(14000 + p.Value.Ramp), 0), p.Value.Height));
		}
		// Flat coast edits retain exact underlying heights and use collision tiles
		// whose images can remain sand/water instead of the plateau's grass tile.
		public PlateauTerrainEditAction(Map map, IReadOnlyDictionary<CPos, TerrainTile> patch,
			Action<bool> updateActors, string text)
		{
			this.map = map; this.updateActors = updateActors; Text = text;
			if (patch.Count == 0 || patch.Any(p => !map.Contains(p.Key))) throw new ArgumentException("Terrain edit is outside the map.", nameof(patch));
			before = patch.Keys.ToDictionary(c => c, c => (map.Tiles[c], map.Height[c]));
			after = patch.ToDictionary(p => p.Key, p => (p.Value, map.Height[p.Key]));
		}
		public void Execute() => Do();
		public void Do()
		{
			if (applied) throw new InvalidOperationException("Plateau action is already applied.");
			Apply(after, before, true);
			applied = true;
		}
		public void Undo()
		{
			if (!applied) throw new InvalidOperationException("Plateau action is not applied.");
			Apply(before, after, false);
			applied = false;
		}
		void Set(Dictionary<CPos, (TerrainTile Tile, byte Height)> state)
		{
			foreach (var pair in state)
			{
				map.Tiles[pair.Key] = pair.Value.Tile;
				map.Height[pair.Key] = pair.Value.Height;
			}
		}
		void Apply(Dictionary<CPos, (TerrainTile Tile, byte Height)> state,
			Dictionary<CPos, (TerrainTile Tile, byte Height)> rollback, bool forward)
		{
			try
			{
				Set(state);
				// The actor patch callback must roll back its own partial operations if it fails.
				updateActors(forward);
			}
			catch
			{
				Set(rollback);
				throw;
			}
		}
	}
}
