using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Reserve physical space before selecting artwork. A rejected native contour keeps
	// the existing edge renderer; artwork must never silently occupy walkable low land.
	static class RubberduckNativeCliffPlanner
	{
		readonly struct Face
		{
			public readonly CPos Cell;
			public readonly int Direction;
			public Face(CPos cell, int direction) { Cell = cell; Direction = direction; }
			public (int X, int Y) Start => Direction == 1 ? (Cell.X, Cell.Y + 1) : (Cell.X + 1, Cell.Y);
			public (int X, int Y) End => (Cell.X + 1, Cell.Y + 1);
		}

		public static void Apply(MapPlan plan)
		{
			if (plan.PlateauSurfaces.Count == 0) return;
			var surfaces = plan.PlateauSurfaces.ToDictionary(p => MountainPlateauPlanner.Cell(p.Key.U, p.Key.V), p => p.Value);
			var baseline = MountainPlateauPlanner.Reachable(plan, plan.Spawns[0]);
			// Editor replans may carry protected low feet from the existing component.
			// They participate in closure but must never be re-added or rolled back as
			// newly reserved ground.
			var reservedFeet = surfaces.Where(p => p.Value.Height == 0 && p.Value.Ramp == 0 && p.Value.Blocked).Select(p => p.Key).ToHashSet();
			var foot = new HashSet<CPos>(reservedFeet);
			bool Free(CPos cell)
			{
				var p = MountainPlateauPlanner.Point(cell);
				return plan.Contains(p.U, p.V) && !surfaces.ContainsKey(cell) &&
					plan.TerrainAt(p.U, p.V) == PlannedTerrain.Land && plan.FeaturesAt(p.U, p.V) == PlannedFeature.None;
			}
			foreach (var pair in surfaces.OrderBy(p => p.Key.X).ThenBy(p => p.Key.Y))
			{
				if (pair.Value.Height != 4 || pair.Value.Ramp != 0) continue;
				// Apply the same apron rule on every compass side, not only visible faces.
				for (var direction = 0; direction < 4; direction++)
				{
					var outward = PlateauTopology.Directions[direction];
					var lateral = PlateauTopology.Directions[(direction + 1) % 4];
					var candidate = pair.Key + outward;
					if (!Free(candidate)) continue;
					var spacious = true;
					for (var depth = 2; depth <= 3; depth++)
						for (var lane = -2; lane <= 2; lane++)
							{
								var cell = candidate + outward * depth + lateral * lane;
								var p = MountainPlateauPlanner.Point(cell);
								if (!plan.Contains(p.U, p.V) || surfaces.ContainsKey(cell) || plan.TerrainAt(p.U, p.V) != PlannedTerrain.Land)
									spacious = false;
							}
					var point = MountainPlateauPlanner.Point(candidate);
					for (var dx = -2; dx <= 2; dx++)
						for (var dy = -2; dy <= 2; dy++)
							if (!plan.Contains(point.U + dx, point.V + dy) || plan.HasFeature(point.U + dx, point.V + dy,
								PlannedFeature.Entrance | PlannedFeature.ResourceGenerator |
								PlannedFeature.TechBuilding | PlannedFeature.Spawn | PlannedFeature.BuildClearance | PlannedFeature.Bottleneck)) spacious = false;
					if (spacious) foot.Add(candidate);
				}
			}
			// Complete convex feet only where both adjacent cardinal apron cells fit.
			foreach (var cell in surfaces.Where(p => p.Value.Height == 4 && p.Value.Ramp == 0).Select(p => p.Key))
				for (var d = 0; d < 4; d++)
				{
					var a = PlateauTopology.Directions[d]; var b = PlateauTopology.Directions[(d + 1) % 4];
					if (foot.Contains(cell + a) && foot.Contains(cell + b) && Free(cell + a + b)) foot.Add(cell + a + b);
				}
			foreach (var cell in foot)
			{
				if (reservedFeet.Contains(cell)) continue;
				var p = MountainPlateauPlanner.Point(cell);
				plan.SetTerrain(p.U, p.V, PlannedTerrain.Mountain);
				plan.PlateauSurfaces.Add(p, new PlateauSurface(0, 0, true));
			}
			var reachable = MountainPlateauPlanner.Reachable(plan, plan.Spawns[0]);
			if (Enumerable.Range(0, baseline.Length).Any(i => baseline[i] && !reachable[i] &&
				!foot.Contains(MountainPlateauPlanner.Cell(i % plan.Width, i / plan.Width))))
			{
				foreach (var cell in foot)
				{
					if (reservedFeet.Contains(cell)) continue;
					var p = MountainPlateauPlanner.Point(cell);
					plan.SetTerrain(p.U, p.V, PlannedTerrain.Land);
					plan.PlateauSurfaces.Remove(p);
				}
				plan.ValidationMessages.Add("Native cliff apron rejected: it would disconnect previously reachable terrain.");
				return;
			}
			var faces = new List<Face>();
			foreach (var pair in surfaces)
			{
				if (pair.Value.Height != 4 || pair.Value.Ramp != 0) continue;
				for (var direction = 0; direction < 2; direction++)
				{
					var other = pair.Key + PlateauTopology.Directions[direction];
					var point = MountainPlateauPlanner.Point(other);
					if (plan.Contains(point.U, point.V) && (!surfaces.ContainsKey(other) || reservedFeet.Contains(other)) &&
						(foot.Contains(other) || plan.TerrainAt(point.U, point.V) == PlannedTerrain.Land)) faces.Add(new Face(pair.Key, direction));
				}
			}
			var totalFaces = faces.Count;
			bool SolidFoot(CPos cell) => foot.Contains(cell) ||
				(surfaces.TryGetValue(cell, out var s) && s.Ramp == 0 && (s.Height == 4 || s.Blocked));
			// Trim unsafe endpoints before grouping. Otherwise a single protected ramp
			// mouth would reject an entire otherwise usable plateau front.
			faces = faces.Where(f => foot.Contains(f.Cell + PlateauTopology.Directions[f.Direction]) &&
				SolidFoot(f.Cell + new CVec(1, 0)) && SolidFoot(f.Cell + new CVec(0, 1)) && SolidFoot(f.Cell + new CVec(1, 1))).ToList();
			var vertices = new Dictionary<(int, int), List<int>>();
			for (var i = 0; i < faces.Count; i++)
				foreach (var vertex in new[] { faces[i].Start, faces[i].End })
				{
					if (!vertices.TryGetValue(vertex, out var list)) vertices.Add(vertex, list = new List<int>());
					list.Add(i);
				}
			var seen = new HashSet<int>();
			for (var first = 0; first < faces.Count; first++)
			{
				if (seen.Contains(first)) continue;
				var component = new HashSet<int>();
				var pending = new Stack<int>(); pending.Push(first);
				while (pending.Count > 0)
				{
					var index = pending.Pop();
					if (!component.Add(index)) continue;
					seen.Add(index);
					foreach (var vertex in new[] { faces[index].Start, faces[index].End })
						foreach (var next in vertices[vertex]) pending.Push(next);
				}
				if (component.Any(i => !foot.Contains(faces[i].Cell + PlateauTopology.Directions[faces[i].Direction]))) continue;
				var pieces = new List<(CPos Cell, int Slot)>();
				var replaced = new HashSet<int>();
				var fits = true;
				foreach (var vertex in component.SelectMany(i => new[] { faces[i].Start, faces[i].End }).Distinct())
				{
					var touching = vertices[vertex];
					if (touching.Count > 2) { fits = false; break; }
					var a = faces[touching[0]];
					if (touching.Count == 1 && a.Start == vertex)
						pieces.Add((a.Cell, a.Direction == 1 ? 30 : 26));
					else if (touching.Count == 1 || faces[touching[1]].Direction != a.Direction)
					{
						if (a.End == vertex && (touching.Count == 1 || faces[touching[1]].End == vertex))
						{
							foreach (var delta in new[] { new CVec(1, 0), new CVec(0, 1), new CVec(1, 1) })
								if (!SolidFoot(a.Cell + delta)) fits = false;
							pieces.Add((a.Cell, 24));
						}
						else if (touching.Count == 2 && a.Start == vertex && faces[touching[1]].Start == vertex)
						{
							var sw = a.Direction == 1 ? a : faces[touching[1]];
							var anchor = sw.Cell - new CVec(1, 0);
							if (!surfaces.TryGetValue(anchor, out var s) || s.Height != 4 || s.Ramp != 0) fits = false;
							pieces.Add((anchor, 16));
							replaced.UnionWith(touching);
						}
						else fits = false;
					}
				}
				if (!fits) continue;
				foreach (var index in component)
				{
					var face = faces[index];
					plan.NativeCliffFaces.Add((MountainPlateauPlanner.Point(face.Cell), face.Direction));
					if (!replaced.Contains(index))
						pieces.Add((face.Cell, (face.Direction == 1 ? 0 : 5) + (int)(unchecked((uint)(face.Cell.X * 73856093 ^ face.Cell.Y * 19349663)) % 3)));
				}
				foreach (var piece in pieces) plan.NativeCliffPieces.Add((MountainPlateauPlanner.Point(piece.Cell), piece.Slot));
			}
			plan.ValidationMessages.Add($"Native cliff integration: {foot.Count} low apron cells, {plan.NativeCliffFaces.Count}/{totalFaces} front faces, {plan.NativeCliffPieces.Count} complete pieces; unsupported contours retain the derived renderer.");
		}
	}
}
