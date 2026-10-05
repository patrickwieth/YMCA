using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	// A stroke is planned without mutating the map. The generator's native planner
	// owns apron/socket selection; this adapter supplies editor topology/protection.
	sealed class PlateauShapePlan
	{
		public readonly IReadOnlyDictionary<CPos, PlateauSurface> Patch;
		public readonly IReadOnlyCollection<MiniYamlNode> BeforeActors;
		public readonly IReadOnlyCollection<MiniYamlNode> AfterActors;
		public readonly int NativePieces;

		PlateauShapePlan(Dictionary<CPos, PlateauSurface> patch, List<MiniYamlNode> before, List<MiniYamlNode> after, int nativePieces)
		{ Patch = patch; BeforeActors = before; AfterActors = after; NativePieces = nativePieces; }

		static PlateauSurface Surface(Map map, CPos cell)
		{
			if (map.Ramp[cell] > 4) throw new InvalidOperationException("Unsupported ramp in plateau edit.");
			return new PlateauSurface(map.Height[cell], map.Ramp[cell], map.Tiles[cell].Type == 3992);
		}
		static bool Same(PlateauSurface a, PlateauSurface b) => a.Height == b.Height && a.Ramp == b.Ramp && a.Blocked == b.Blocked;
		static IEnumerable<CPos> Around(CPos c, int radius)
		{
			for (var x = -radius; x <= radius; x++)
				for (var y = -radius; y <= radius; y++) yield return c + new CVec(x, y);
		}

		public static PlateauShapePlan Create(Map map, IEnumerable<MiniYamlNode> definitions, IEnumerable<CPos> stroke,
			bool grow, IReadOnlyCollection<CPos> actorProtection) => Create(map, definitions, stroke, grow, actorProtection, null, null);

		internal static PlateauShapePlan MoveRamp(Map map, IEnumerable<MiniYamlNode> definitions,
			IReadOnlyDictionary<CPos, PlateauSurface> replacement, IReadOnlyCollection<CPos> oldRamp,
			IReadOnlyCollection<CPos> actorProtection) => Create(map, definitions, replacement.Keys, false, actorProtection, replacement, oldRamp.ToHashSet());

		static PlateauShapePlan Create(Map map, IEnumerable<MiniYamlNode> definitions, IEnumerable<CPos> stroke,
			bool grow, IReadOnlyCollection<CPos> actorProtection, IReadOnlyDictionary<CPos, PlateauSurface> replacement, HashSet<CPos> oldRamp)
		{
			if (!map.Rules.TerrainInfo.Id.StartsWith("RUBBERDUCK-", StringComparison.Ordinal))
				throw new InvalidOperationException("Plateau shaping requires Rubberduck terrain.");
			var brush = stroke.ToHashSet();
			if (brush.Count == 0 || brush.Count > 2048) throw new InvalidOperationException("Use a stroke of 1–2048 cells.");
			var actors = definitions.ToArray();
			var playable = map.AllCells.Where(map.Contains).ToHashSet();
			var elevated = playable.Where(c => map.Height[c] > 0 || map.Ramp[c] != 0).ToHashSet();
			var affected = new HashSet<CPos>();
			var pending = new Queue<CPos>();
			foreach (var c in brush)
			{
				if (Around(c, 4).Any(n => !playable.Contains(n))) throw new InvalidOperationException("Leave four cells of space to the map boundary.");
				foreach (var n in Around(c, 3)) if (elevated.Contains(n)) pending.Enqueue(n);
			}
			// Include nearby/shared-foot components so one stroke can join two roofs.
			while (pending.Count > 0)
			{
				var c = pending.Dequeue();
				if (!affected.Add(c)) continue;
				if (affected.Count > 8192) throw new InvalidOperationException("Too much connected terrain; use a smaller isolated edit.");
				foreach (var n in Around(c, 3)) if (elevated.Contains(n) && !affected.Contains(n)) pending.Enqueue(n);
			}
			if (affected.Count == 0) throw new InvalidOperationException("Extend an existing plateau; use the stamp tool to create a new one.");
			foreach (var c in affected)
			{
				var type = map.Tiles[c].Type;
				if (map.Height[c] > 4 || (type != 1000 && type != 3992 && (type < 14001 || type > 14004)))
					throw new InvalidOperationException("Connected terrain contains an unsupported surface.");
				if (Around(c, 3).Any(n => !playable.Contains(n))) throw new InvalidOperationException("Connected plateau is too close to the map boundary.");
			}
			var oldFeet = affected.SelectMany(c => Around(c, 1)).Where(c => playable.Contains(c) && map.Height[c] == 0 && map.Tiles[c].Type == 3992).ToHashSet();
			var ownedCells = affected.Concat(oldFeet).ToHashSet();
			var beforeActors = new List<MiniYamlNode>();
			var protectedCells = actorProtection.ToHashSet();
			foreach (var node in actors)
			{
				var reference = new ActorReference(node.Value.Value, node.Value.ToDictionary());
				var location = reference.GetOrDefault<LocationInit>();
				if (location == null) continue;
				if (PlateauRemovalPlan.Decoration(reference.Type) && ownedCells.Contains(location.Value))
				{
					if (reference.GetOrDefault<OwnerInit>()?.InternalName != "Neutral" || node.Value.Nodes.Any(n => n.Key != "Location" && n.Key != "Owner"))
						throw new InvalidOperationException("Customized cliff objects cannot be replanned safely.");
					beforeActors.Add(node);
				}
				else
				{
					if (ownedCells.Contains(location.Value) && reference.Type.StartsWith("terrain.rubberduck.", StringComparison.OrdinalIgnoreCase))
						throw new InvalidOperationException("Unsupported terrain decoration prevents cliff replanning.");
					protectedCells.Add(location.Value);
				}
			}
			var rampProtection = affected.Where(c => map.Ramp[c] != 0 && (oldRamp == null || !oldRamp.Contains(c))).SelectMany(c => Around(c, 2)).Where(playable.Contains).ToHashSet();
			var protectedTerrain = protectedCells.Concat(rampProtection).Concat(playable.Where(c => map.Resources[c].Type != 0)).ToHashSet();
			// Existing feet are committed collision, not vacant candidates for a new
			// apron. Preserve them while editing, then release only orphaned feet.
			var surfaces = ownedCells.ToDictionary(c => c, c => Surface(map, c));
			foreach (var c in brush)
			{
				var original = Surface(map, c);
				var next = replacement != null ? replacement[c] : new PlateauSurface(grow ? (byte)4 : (byte)0);
				if (Same(original, next)) continue;
				if (protectedTerrain.Contains(c)) throw new InvalidOperationException("The stroke touches a ramp, landing, resource, start or actor footprint.");
				if (replacement == null && map.Tiles[c].Type != 1000 && !(oldFeet.Contains(c) && original.Height == 0))
					throw new InvalidOperationException("Paint grass roofs/ground or their native feet, not retaining walls or other terrain.");
				surfaces[c] = next;
			}
			if (!brush.Any(c => surfaces.TryGetValue(c, out var value) && !Same(Surface(map, c), value)))
				throw new InvalidOperationException("This stroke does not change the plateau shape.");

			foreach (var foot in oldFeet)
				if (!protectedTerrain.Contains(foot) && surfaces[foot].Height == 0 && surfaces[foot].Ramp == 0 &&
					!Around(foot, 1).Any(c => surfaces.TryGetValue(c, out var s) && (s.Height > 0 || s.Ramp != 0)))
					surfaces[foot] = default;

			// MapPlan uses one-cell MPos padding. This adapter operates in map coordinates,
			// not generator export coordinates, so its results map directly back via Cell().
			var nativeProtection = protectedTerrain.Concat(surfaces.Where(p => p.Value.Ramp != 0)
				.SelectMany(p => Around(p.Key, 2))).ToHashSet();
			var native = new MapPlan(map.MapSize.X - 2, map.MapSize.Y - 2);
			var startFound = false;
			for (var x = 0; x < native.Width; x++)
				for (var y = 0; y < native.Height; y++)
				{
					var c = MountainPlateauPlanner.Cell(x, y);
					if (!playable.Contains(c)) { native.SetTerrain(x, y, PlannedTerrain.Water); continue; }
					var value = surfaces.TryGetValue(c, out var after) ? after : Surface(map, c);
					var supportedGround = map.Tiles[c].Type == 1000 || surfaces.ContainsKey(c);
					native.SetTerrain(x, y, supportedGround && !value.Blocked && (value.Height == 0 || surfaces.ContainsKey(c)) ? PlannedTerrain.Land : PlannedTerrain.Mountain);
					if (surfaces.ContainsKey(c) && (value.Height > 0 || value.Ramp != 0 || value.Blocked)) native.PlateauSurfaces.Add(new MPos(x, y), value);
					if (nativeProtection.Contains(c)) native.AddFeature(x, y, PlannedFeature.BuildClearance);
					if (!startFound && native.IsTraversable(x, y) && value.Height == 0 && value.Ramp == 0)
					{ native.Spawns.Add(new PlanPoint(x, y)); startFound = true; }
				}
			if (!startFound) throw new InvalidOperationException("No surrounding low ground exists.");
			RubberduckNativeCliffPlanner.Apply(native);
			foreach (var pair in native.PlateauSurfaces) surfaces[MountainPlateauPlanner.Cell(pair.Key.U, pair.Key.V)] = pair.Value;
			var patch = surfaces.Where(p => !Same(Surface(map, p.Key), p.Value)).ToDictionary(p => p.Key, p => p.Value);
			if (patch.Count == 0) throw new InvalidOperationException("This stroke does not change the plateau.");
			if (patch.Keys.Any(protectedTerrain.Contains)) throw new InvalidOperationException("Replanned cliff feet would modify protected terrain.");
			ValidateConnectivity(map, playable, patch, surfaces, protectedCells);

			if (beforeActors.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.closedcliff", StringComparison.Ordinal)))
			{
				var closed = new RubberduckClosedCliffSelection(map, surfaces);
				if (surfaces.Any(p => RubberduckClosedCliffSelection.Elevated(p.Value) && !closed.Cells.Contains(p.Key)))
					throw new InvalidOperationException("This stroke cannot preserve the closed cliff contour and its reserved feet.");
			}

			var afterActors = new List<MiniYamlNode>();
			var nativeFaces = native.NativeCliffFaces.Select(f => (MountainPlateauPlanner.Cell(f.Point.U, f.Point.V), f.Direction)).ToHashSet();
			var nativePieces = native.NativeCliffPieces.Select(piece => (Cell: MountainPlateauPlanner.Cell(piece.Point.U, piece.Point.V), piece.Slot));
			RubberduckPlateauRenderer.ComposeNative(map, surfaces, afterActors, nativeFaces, nativePieces, "Native", false);
			if (beforeActors.Any(n => n.Value.Value.StartsWith("terrain.rubberduck.mountainwall", StringComparison.Ordinal)))
				RubberduckMountainRenderer.UseMountainFaces(afterActors);
			var ids = actors.Select(n => n.Key).ToHashSet(StringComparer.Ordinal);
			var serial = 0;
			var prefix = "EditorShape0_";
			while (ids.Any(id => id.StartsWith(prefix, StringComparison.Ordinal))) prefix = "EditorShape" + ++serial + "_";
			afterActors = afterActors.Select((n, i) => new MiniYamlNode(prefix + i, n.Value)).ToList();
			return new PlateauShapePlan(patch, beforeActors, afterActors, native.NativeCliffPieces.Count);
		}

		static void ValidateConnectivity(Map map, HashSet<CPos> playable, Dictionary<CPos, PlateauSurface> patch,
			Dictionary<CPos, PlateauSurface> surfaces, HashSet<CPos> occupied)
		{
			(Dictionary<CPos, int> Labels, HashSet<int> Low) Label(bool after)
			{
				var ground = new Dictionary<CPos, PlateauSurface>();
				foreach (var c in playable)
				{
					if (occupied.Contains(c)) continue;
					if (after && patch.TryGetValue(c, out var changed))
					{ if (!changed.Blocked) ground.Add(c, changed); }
					else if (map.GetTerrainInfo(c).Type is "Clear" or "Road" or "Rough" or "Sand" or "Dirt" or "Ore" or "Gems" or "Tiberium" or "BlueTiberium") ground.Add(c, Surface(map, c));
				}
				var labels = new Dictionary<CPos, int>();
				var low = new HashSet<int>();
				var index = 0;
				foreach (var first in ground.Keys)
				{
					if (labels.ContainsKey(first)) continue;
					var queue = new Queue<CPos>(); queue.Enqueue(first); labels.Add(first, ++index);
					while (queue.Count > 0)
					{
						var c = queue.Dequeue(); var value = ground[c];
						if (value.Height == 0 && value.Ramp == 0) low.Add(index);
						for (var d = 0; d < 4; d++)
						{
							var n = c + PlateauTopology.Directions[d];
							if (labels.ContainsKey(n) || !ground.TryGetValue(n, out var neighbor) || !PlateauTopology.Continuous(value, neighbor, d)) continue;
							labels.Add(n, index); queue.Enqueue(n);
						}
					}
				}
				return (labels, low);
			}
			var before = Label(false); var after = Label(true);
			var destinations = new Dictionary<int, int>();
			foreach (var pair in before.Labels)
			{
				if (patch.ContainsKey(pair.Key)) continue;
				var destination = after.Labels[pair.Key];
				if (destinations.TryGetValue(pair.Value, out var previous) && previous != destination)
					throw new InvalidOperationException("The stroke would disconnect previously connected terrain.");
				destinations[pair.Value] = destination;
			}
			foreach (var pair in surfaces)
				if (!pair.Value.Blocked && pair.Value.Height > 0 && !occupied.Contains(pair.Key) &&
					(!after.Labels.TryGetValue(pair.Key, out var label) || !after.Low.Contains(label)))
					throw new InvalidOperationException($"Shaped roof at {pair.Key} must remain reachable from low ground through a ramp.");
		}
	}
}
