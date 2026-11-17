using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class PlateauRampMovePlan
	{
		public readonly CPos Toe;
		public readonly int Direction;
		public readonly IReadOnlyDictionary<CPos, PlateauSurface> Original;

		PlateauRampMovePlan(CPos toe, int direction, Dictionary<CPos, PlateauSurface> original)
		{ Toe = toe; Direction = direction; Original = original; }

		static bool Matches(Map map, CPos c, byte height, byte ramp, bool blocked)
		{
			return map.Contains(c) && map.Height[c] == height && map.Ramp[c] == ramp &&
				map.Tiles[c].Type == (blocked ? 3992 : ramp == 0 ? 1000 : 14000 + ramp);
		}

		public static PlateauRampMovePlan Find(Map map, CPos clicked)
		{
			if (!map.Contains(clicked) || map.Ramp[clicked] < 1 || map.Ramp[clicked] > 4 || map.Height[clicked] > 3)
				throw new InvalidOperationException("First click a cell on an existing four-level ramp.");
			var ramp = map.Ramp[clicked];
			var direction = (ramp + 1) % 4;
			var outward = PlateauTopology.Directions[direction];
			var lateral = PlateauTopology.Directions[(direction + 1) % 4];
			for (var offset = -1; offset <= 1; offset++)
			{
				var toe = clicked + outward * map.Height[clicked] + lateral * offset;
				var original = new Dictionary<CPos, PlateauSurface>();
				var valid = true;
				for (var step = 0; step < 4; step++)
					for (var lane = -2; lane <= 2; lane++)
					{
						var c = toe - outward * step + lateral * lane;
						var guard = Math.Abs(lane) == 2;
						var s = new PlateauSurface(guard ? (byte)4 : (byte)step, guard ? (byte)0 : ramp, guard);
						valid &= Matches(map, c, s.Height, s.Ramp, s.Blocked);
						original.Add(c, s);
					}
				for (var lane = -1; lane <= 1; lane++)
					valid &= Matches(map, toe - outward * 4 + lateral * lane, 4, 0, false) &&
						Matches(map, toe + outward + lateral * lane, 0, 0, false);
				if (valid) return new PlateauRampMovePlan(toe, direction, original);
			}
			throw new InvalidOperationException("Needs a complete three-wide ramp, retaining strips and clear landings.");
		}

		public PlateauShapePlan Move(Map map, IEnumerable<MiniYamlNode> actors, CPos target, IReadOnlyCollection<CPos> protectedCells)
		{
			// Selection is only a hint: re-read the source at the second click.
			var current = Find(map, Toe);
			if (current.Toe != Toe || current.Direction != Direction) throw new InvalidOperationException("Selected ramp changed; select it again.");
			if (target == Toe) throw new InvalidOperationException("Choose a different ramp position.");
			var replacement = Original.Keys.ToDictionary(c => c, _ => new PlateauSurface(4));
			foreach (var pair in Destination(map, target, Original)) replacement[pair.Key] = pair.Value;
			return PlateauShapePlan.MoveRamp(map, actors, replacement, Original.Keys.ToArray(), protectedCells);
		}

		public static PlateauShapePlan Add(Map map, IEnumerable<MiniYamlNode> actors, CPos target, IReadOnlyCollection<CPos> protectedCells)
		{
			return PlateauShapePlan.MoveRamp(map, actors, Destination(map, target, new Dictionary<CPos, PlateauSurface>()),
				Array.Empty<CPos>(), protectedCells);
		}

		public PlateauShapePlan Close(Map map, IEnumerable<MiniYamlNode> actors, IReadOnlyCollection<CPos> protectedCells)
		{
			var current = Find(map, Toe);
			if (current.Toe != Toe || current.Direction != Direction) throw new InvalidOperationException("Selected ramp changed; select it again.");
			return PlateauShapePlan.MoveRamp(map, actors, Original.Keys.ToDictionary(c => c, _ => new PlateauSurface(4)),
				Original.Keys.ToArray(), protectedCells);
		}

		static Dictionary<CPos, PlateauSurface> Destination(Map map, CPos target, IReadOnlyDictionary<CPos, PlateauSurface> original)
		{
			var candidates = new List<Dictionary<CPos, PlateauSurface>>();
			for (var direction = 0; direction < 4; direction++)
			{
				var outward = PlateauTopology.Directions[direction];
				var lateral = PlateauTopology.Directions[(direction + 1) % 4];
				var ramp = (byte)((direction + 2) % 4 + 1);
				var patch = new Dictionary<CPos, PlateauSurface>();
				var valid = true;
				for (var step = -2; step <= 4; step++)
					for (var lane = -2; lane <= 2; lane++)
					{
						var c = target - outward * step + lateral * lane;
						if (!map.Contains(c)) { valid = false; continue; }
						if (step >= 0)
						{
							// Validate against the virtually closed source, allowing small overlapping shifts.
							if (!original.ContainsKey(c) && !Matches(map, c, 4, 0, false)) valid = false;
							if (step < 4) patch[c] = Math.Abs(lane) == 2 ? new PlateauSurface(4, 0, true) : new PlateauSurface((byte)step, ramp);
						}
						else
						{
							// The new mouth may replace a native apron, but not raised terrain,
							// water, resources or someone else's ramp/retaining strips.
							if (original.ContainsKey(c) || map.Height[c] != 0 || map.Ramp[c] != 0 ||
								(map.Tiles[c].Type != 1000 && (step != -1 || map.Tiles[c].Type != 3992))) valid = false;
							if (Math.Abs(lane) <= 1) patch[c] = default;
						}
					}
				if (valid) candidates.Add(patch);
			}
			if (candidates.Count != 1) throw new InvalidOperationException("Choose a straight, empty plateau edge with five roof cells of depth and two clear approach rows.");
			return candidates[0];
		}
	}
}
