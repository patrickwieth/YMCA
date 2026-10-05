using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class PlannedRubberduckBridge
	{
		public readonly CPos Start;
		public readonly bool AlongY;
		public readonly int Length;
		public readonly int LandDepth;
		public readonly int ApproachLength;
		public CVec Step => AlongY ? new CVec(0, 1) : new CVec(1, 0);
		public PlannedRubberduckBridge(CPos start, bool alongY, int length, int landDepth = 1, int approachLength = 2)
		{
			Start = start; AlongY = alongY; Length = length; LandDepth = landDepth; ApproachLength = approachLength;
		}
		public IEnumerable<CPos> Approaches()
		{
			if (LandDepth < 2) yield break; // Older/simple fixtures keep their original ground.
			for (var i = 1; i <= ApproachLength; i++)
			{
				yield return Start - Step * i;
				yield return Start + Step * (Length - 1 + i);
			}
		}
		// Use the same odd projection-border parity as the exported map. The
		// remaining even height border is a constant CPos translation.
		public static CPos Cell(int x, int y) => new MPos(x + 1, y + 1).ToCPos(MapGridType.RectangularIsometric);
		public static MPos Point(CPos cell)
		{
			var p = cell.ToMPos(MapGridType.RectangularIsometric);
			return new MPos(p.U - 1, p.V - 1);
		}
		public IEnumerable<CPos> Cells()
		{
			for (var i = 0; i < Length; i++) yield return Start + Step * i;
		}
	}

	static class RubberduckBridgePlanner
	{
		const PlannedFeature Protected = PlannedFeature.Resource | PlannedFeature.RichResource |
			PlannedFeature.ResourceGenerator | PlannedFeature.TechBuilding | PlannedFeature.BuildClearance | PlannedFeature.Spawn;

		public static bool Connect(MapPlan plan, IReadOnlyList<PlanPoint> continents)
		{
			bool Is(CPos c, PlannedTerrain terrain)
			{
				var p = PlannedRubberduckBridge.Point(c);
				return plan.Contains(p.U, p.V) && plan.TerrainAt(p.U, p.V) == terrain;
			}
			bool ClearEnd(CPos c)
			{
				for (var x = -2; x <= 2; x++)
					for (var y = -2; y <= 2; y++)
					{
						var p = PlannedRubberduckBridge.Point(c + new CVec(x, y));
						if (!plan.Contains(p.U, p.V) || plan.HasFeature(p.U, p.V, Protected)) return false;
					}
				return true;
			}
			// Small custom continents keep one approach cell rather than erasing
			// economy to fit the longer two-team approach. The inland anchor is equal.
			var approachLength = continents.Count == 2 ? 2 : 1;
			var bankHalfWidth = continents.Count == 2 ? 1 : 0;
			bool BankFits(CPos shore, CVec inland, CVec side)
			{
				if (!ClearEnd(shore)) return false;
				// Require an existing dry landing pad. Do not fill a bay or flatten
				// a blocked bank just to conceal an unsupported bridge end.
				for (var depth = 1; depth <= approachLength + 1; depth++)
					for (var lateral = -bankHalfWidth; lateral <= bankHalfWidth; lateral++)
					{
						var c = shore + inland * depth + side * lateral;
						var p = PlannedRubberduckBridge.Point(c);
						if (!Is(c, PlannedTerrain.Land) || plan.HasFeature(p.U, p.V, Protected)) return false;
						// Protect complete nearby economy actor footprints, without
						// demanding an unrelated 5x7 empty rectangle on small islands.
						for (var x = -1; x <= 1; x++)
							for (var y = -1; y <= 1; y++)
							{
								var n = PlannedRubberduckBridge.Point(c + new CVec(x, y));
								if (plan.Contains(n.U, n.V) && plan.HasFeature(n.U, n.V, PlannedFeature.TechBuilding | PlannedFeature.ResourceGenerator)) return false;
							}
					}
				return true;
			}
			// Endpoint ownership comes from actual land connectivity, not proximity
			// to a continent center (which can misclassify irregular bays/islets).
			var owners = new Dictionary<CPos, int>();
			var directions = new[] { new CVec(1, 0), new CVec(-1, 0), new CVec(0, 1), new CVec(0, -1) };
			for (var i = 0; i < continents.Count; i++)
			{
				var origin = PlannedRubberduckBridge.Cell(continents[i].X, continents[i].Y);
				if (owners.ContainsKey(origin) || !Is(origin, PlannedTerrain.Land)) return false;
				var pending = new Queue<CPos>(); pending.Enqueue(origin); owners.Add(origin, i);
				while (pending.Count != 0)
				{
					var c = pending.Dequeue();
					foreach (var d in directions)
						if (Is(c + d, PlannedTerrain.Land) && !owners.ContainsKey(c + d))
						{
							owners.Add(c + d, i); pending.Enqueue(c + d);
						}
				}
			}
			int Owner(CPos c) => owners.TryGetValue(c, out var owner) ? owner : -1;
			var candidates = new List<(PlannedRubberduckBridge Bridge, int A, int B)>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (plan.TerrainAt(x, y) != PlannedTerrain.Land) continue;
					var start = PlannedRubberduckBridge.Cell(x, y);
					foreach (var alongY in new[] { false, true })
					{
						var step = alongY ? new CVec(0, 1) : new CVec(1, 0);
						if (!Is(start - step, PlannedTerrain.Land) || !Is(start + step, PlannedTerrain.Water)) continue;
						var length = 1;
						while (Is(start + step * length, PlannedTerrain.Water)) length++;
						var end = start + step * length;
						var side = alongY ? new CVec(1, 0) : new CVec(0, 1);
						if (length < 2 || !Is(end, PlannedTerrain.Land) || !BankFits(start, -step, side) || !BankFits(end, step, side)) continue;
						var a = Owner(start); var b = Owner(end);
						if (a >= 0 && b >= 0 && a != b)
							candidates.Add((new PlannedRubberduckBridge(start - step, alongY, length + 3, 2, approachLength), Math.Min(a, b), Math.Max(a, b)));
					}
				}

			var chosen = new List<PlannedRubberduckBridge>();
			// Two continents receive an atomic half-turn pair, not two unrelated
			// shortest crossings that could favor one team's approach routes.
			if (continents.Count == 2)
			{
				var center = PlannedRubberduckBridge.Cell((plan.Width - 1) / 2, (plan.Height - 1) / 2);
				var lookup = candidates.ToDictionary(c => (c.Bridge.Start, c.Bridge.AlongY, c.Bridge.Length), c => c.Bridge);
				foreach (var candidate in candidates.OrderBy(c => c.Bridge.Length).ThenBy(c => c.Bridge.Start.X).ThenBy(c => c.Bridge.Start.Y))
				{
					var a = candidate.Bridge;
					var end = a.Start + a.Step * (a.Length - 1);
					var opposite = new CPos(2 * center.X - end.X, 2 * center.Y - end.Y);
					if (!lookup.TryGetValue((opposite, a.AlongY, a.Length), out var b) ||
						a.Cells().Any(c => b.Cells().Any(d => Math.Abs(c.X - d.X) <= 4 && Math.Abs(c.Y - d.Y) <= 4))) continue;
					chosen.Add(a); chosen.Add(b); break;
				}
				if (chosen.Count != 2) return false;
			}
			var occupied = new HashSet<CPos>();
			for (var link = 0; continents.Count != 2 && link < continents.Count; link++)
			{
				var a = Math.Min(link, (link + 1) % continents.Count);
				var b = Math.Max(link, (link + 1) % continents.Count);
				var candidate = candidates.Where(c => c.A == a && c.B == b)
					.OrderBy(c => c.Bridge.Length).ThenBy(c => c.Bridge.Start.X).ThenBy(c => c.Bridge.Start.Y)
					.FirstOrDefault(c => !c.Bridge.Cells().Any(occupied.Contains)).Bridge;
				if (candidate == null) return false;
				chosen.Add(candidate);
				foreach (var c in candidate.Cells())
					for (var x = -4; x <= 4; x++)
						for (var y = -4; y <= 4; y++) occupied.Add(c + new CVec(x, y));
			}

			// Commit only after every required link fits. Water remains water at
			// export: ShallowWater here denotes the planned destructible crossing.
			foreach (var bridge in chosen)
			{
				foreach (var c in bridge.Cells())
				{
					var p = PlannedRubberduckBridge.Point(c);
					if (plan.TerrainAt(p.U, p.V) == PlannedTerrain.Water) plan.SetTerrain(p.U, p.V, PlannedTerrain.ShallowWater);
					plan.AddFeature(p.U, p.V, PlannedFeature.DestructibleBridge | PlannedFeature.Bottleneck | PlannedFeature.LaunchShore);
				}
				foreach (var approach in bridge.Approaches())
				{
					var p = PlannedRubberduckBridge.Point(approach);
					plan.AddFeature(p.U, p.V, PlannedFeature.LaunchShore | PlannedFeature.Bottleneck);
				}
				foreach (var end in new[] { bridge.Start, bridge.Start + bridge.Step * (bridge.Length - 1) })
					for (var x = -2; x <= 2; x++)
						for (var y = -2; y <= 2; y++)
						{
							var p = PlannedRubberduckBridge.Point(end + new CVec(x, y));
							plan.AddFeature(p.U, p.V, PlannedFeature.LaunchShore);
						}
			}
			plan.Bridges.AddRange(chosen);
			return true;
		}
	}
}
