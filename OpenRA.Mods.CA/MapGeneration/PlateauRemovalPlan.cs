using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Recover only isolated supported geometry from saved terrain, never from actor IDs
	// or map titles. Lowering opens terrain; it must not modify a neighboring plateau.
	sealed class PlateauRemovalPlan
	{
		public readonly IReadOnlyDictionary<CPos, PlateauSurface> Patch;
		public readonly IReadOnlyCollection<CPos> Clearance;
		public readonly IReadOnlyCollection<MiniYamlNode> Decorations;
		static readonly HashSet<int> NativeSlots = new() { 0, 1, 2, 5, 6, 7, 16, 24, 26, 30 };
		static readonly HashSet<int> ClosedSlots = new() { 0, 1, 2, 5, 6, 7, 8, 9, 10, 12, 13, 14, 16, 18, 20, 22, 24, 26, 28, 30, 100, 105 };

		PlateauRemovalPlan(HashSet<CPos> cells, HashSet<CPos> clearance, List<MiniYamlNode> decorations)
		{
			Patch = cells.ToDictionary(c => c, _ => new PlateauSurface(0));
			Clearance = clearance;
			Decorations = decorations;
		}

		internal static bool Decoration(string type)
		{
			const string wall = "terrain.rubberduck.plateauwall";
			const string native = "terrain.rubberduck.nativecliff";
			const string closed = "terrain.rubberduck.closedcliff";
			const string mountain = "terrain.rubberduck.mountainwall";
			return type == "terrain.rubberduck.cliff17" ||
				(type.StartsWith(mountain, StringComparison.Ordinal) && int.TryParse(type[mountain.Length..], out var direction) && direction >= 0 && direction < 4) ||
				(type.StartsWith(wall, StringComparison.Ordinal) && int.TryParse(type[wall.Length..], out var index) && index >= 0 && index < 100) ||
				(type.StartsWith(native, StringComparison.Ordinal) && int.TryParse(type[native.Length..], out var slot) && NativeSlots.Contains(slot)) ||
				(type.StartsWith(closed, StringComparison.Ordinal) && int.TryParse(type[closed.Length..], out var closedSlot) &&
					ClosedSlots.Contains(closedSlot));
		}

		public static PlateauRemovalPlan Create(Map map, IEnumerable<MiniYamlNode> actors, CPos clicked)
		{
			if (!map.Rules.TerrainInfo.Id.StartsWith("RUBBERDUCK-", StringComparison.Ordinal) || !map.Contains(clicked) ||
				map.Height[clicked] != 4 || map.Ramp[clicked] != 0 || map.Tiles[clicked].Type != 1000)
				throw new InvalidOperationException("Click the flat grass roof of an empty height-four plateau.");
			var cells = new HashSet<CPos>();
			var pending = new Stack<CPos>();
			pending.Push(clicked);
			while (pending.Count != 0)
			{
				var c = pending.Pop();
				if (!map.Contains(c)) throw new InvalidOperationException("Plateau touches the map boundary.");
				if (cells.Contains(c) || (map.Height[c] == 0 && map.Ramp[c] == 0)) continue;
				var tile = map.Tiles[c].Type;
				if (map.Height[c] > 4 || (tile != 1000 && tile != 3992 && (tile < 14001 || tile > 14004)))
					throw new InvalidOperationException("Plateau contains unsupported or modified terrain.");
				cells.Add(c);
				if (cells.Count > 4096) throw new InvalidOperationException("Connected plateau is too large for this isolated edit.");
				for (var dx = -1; dx <= 1; dx++)
					for (var dy = -1; dy <= 1; dy++)
						if (dx != 0 || dy != 0) pending.Push(c + new CVec(dx, dy));
			}

			// Native feet are at most one cell beyond the roof, including convex diagonals.
			// Never flood arbitrary low Cliff terrain: shared/extended aprons are rejected.
			foreach (var c in cells.ToArray())
				for (var dx = -1; dx <= 1; dx++)
					for (var dy = -1; dy <= 1; dy++)
					{
						var neighbor = c + new CVec(dx, dy);
						if (map.Contains(neighbor) && map.Height[neighbor] == 0 && map.Tiles[neighbor].Type == 3992) cells.Add(neighbor);
					}
			var clearance = new HashSet<CPos>();
			foreach (var c in cells)
				for (var dx = -3; dx <= 3; dx++)
					for (var dy = -3; dy <= 3; dy++) clearance.Add(c + new CVec(dx, dy));
			foreach (var c in clearance)
			{
				if (!map.Contains(c)) throw new InvalidOperationException("Leave three cells between the plateau and map boundary.");
				if (map.Resources[c].Type != 0) throw new InvalidOperationException("Resources on or beside this plateau are protected.");
				if (!cells.Contains(c) && (map.Height[c] != 0 || map.Ramp[c] != 0 || map.Tiles[c].Type != 1000))
					throw new InvalidOperationException("Nearby terrain or a shared cliff foot prevents isolated removal.");
			}
			var decorations = new List<MiniYamlNode>();
			foreach (var node in actors)
			{
				var actor = new ActorReference(node.Value.Value, node.Value.ToDictionary());
				var location = actor.GetOrDefault<LocationInit>();
				if (location == null || !clearance.Contains(location.Value)) continue;
				if (!cells.Contains(location.Value) || !Decoration(actor.Type) || actor.GetOrDefault<OwnerInit>()?.InternalName != "Neutral" ||
					node.Value.Nodes.Any(n => n.Key != "Location" && n.Key != "Owner"))
					throw new InvalidOperationException("Start positions, actors and customized cliff objects are protected.");
				decorations.Add(node);
			}
			if (decorations.Count == 0) throw new InvalidOperationException("No supported plateau cliff objects were found.");
			return new PlateauRemovalPlan(cells, clearance, decorations);
		}
	}
}
