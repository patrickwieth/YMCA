using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Opt-in stamp plan used by the height-four exporter and editor transaction probe.
	// Requires the map-local closedcliff vocabulary; never changes ordinary generators.
	sealed class RubberduckClosedPlateauPlan
	{
		public readonly Dictionary<CPos, PlateauSurface> Patch;
		public readonly List<MiniYamlNode> Actors;
		public readonly HashSet<CPos> Roof;
		public readonly HashSet<CPos> Feet;
		public readonly HashSet<CPos> Ramps = new();
		public readonly HashSet<CPos> Landings = new();

		// Rotate topology about the rectangle center, not sprite pixels. Direction
		// is the uphill cardinal direction; 0 retains the original -X mouth.
		public static CVec RampOffset(int x, int y, int direction)
		{
			if (direction < 0 || direction > 3) throw new ArgumentOutOfRangeException(nameof(direction));
			return new CVec(2, -2) + PlateauTopology.Directions[direction] * (x - 2) +
				PlateauTopology.Directions[(direction + 1) % 4] * (y + 2);
		}

		public RubberduckClosedPlateauPlan(Map map, CPos center, int notch, IEnumerable<CPos> protectedCells, bool rearRamp = false, int rampDirection = 0, IEnumerable<MiniYamlNode> actors = null)
		{
			CPos RampCell(int x, int y) => center + RampOffset(x, y, rampDirection);
			if (rampDirection < 0 || rampDirection > 3) throw new ArgumentOutOfRangeException(nameof(rampDirection));
			if (!rearRamp && rampDirection != 0) throw new ArgumentException("A ramp direction requires an enabled ramp.");
			if (notch < -1 || notch > 3) throw new ArgumentOutOfRangeException(nameof(notch));
			if (rearRamp && notch != -1) throw new InvalidOperationException("The first ramp integration requires the full rectangular roof.");
			Roof = RubberduckOriginalClosedReference.Roof(notch).Select(c => center + c).ToHashSet();
			Feet = new HashSet<CPos>();
			foreach (var c in Roof)
				for (var x = -1; x <= 1; x++)
					for (var y = -1; y <= 1; y++)
						if (!Roof.Contains(c + new CVec(x, y))) Feet.Add(c + new CVec(x, y));
			if (rearRamp)
				for (var lane = -3; lane <= -1; lane++)
					for (var x = -2; x <= -1; x++) Landings.Add(RampCell(x, lane));
			var protection = protectedCells.ToHashSet();
			foreach (var actor in actors ?? map.ActorDefinitions)
			{
				var location = new ActorReference(actor.Value.Value, actor.Value.ToDictionary()).GetOrDefault<LocationInit>();
				if (location == null) continue;
				protection.Add(location.Value);
				// Reserve the full authored building footprint, including its clear
				// bays. A location outside the patch does not imply a disjoint actor.
				var building = map.Rules.Actors[actor.Value.Value].TraitInfoOrDefault<BuildingInfo>();
				if (building != null)
					protection.UnionWith(building.Footprint.Keys.Select(offset => location.Value + offset));
			}
			foreach (var c in Roof.Concat(Feet).Concat(Landings))
				if (!map.Contains(c) || map.Height[c] != 0 || map.Ramp[c] != 0 ||
					map.Tiles[c].Type != 1000 && map.Tiles[c].Type != 1020 || map.Resources[c].Type != 0 || protection.Contains(c))
					throw new InvalidOperationException("Closed plateau requires unoccupied flat ground, including its complete blocked foot ring.");
			Patch = Roof.ToDictionary(c => c, _ => new PlateauSurface(4));
			foreach (var c in Feet) Patch.Add(c, new PlateauSurface(0, 0, true));
			if (rearRamp)
			{
				foreach (var c in Landings) { Patch[c] = new PlateauSurface(0); Feet.Remove(c); }
				for (var x = 0; x < 4; x++)
				{
					for (var lane = -3; lane <= -1; lane++)
					{
						var c = RampCell(x, lane);
						Patch[c] = new PlateauSurface((byte)x, (byte)(rampDirection + 1)); Ramps.Add(c); Roof.Remove(c);
					}
					foreach (var lane in new[] { -4, 0 }) Patch[RampCell(x, lane)] = new PlateauSurface(4, 0, true);
				}
				for (var lane = -3; lane <= -1; lane++)
					for (var x = -2; x < 4; x++)
						if (!PlateauTopology.Continuous(Patch[RampCell(x, lane)], Patch[RampCell(x + 1, lane)], rampDirection))
							throw new InvalidOperationException("Closed plateau ramp lane is discontinuous.");
			}
			Actors = new List<MiniYamlNode>();
			var anchors = new HashSet<CPos>();
			foreach (var piece in RubberduckOriginalClosedReference.Walls(notch))
			{
				var cell = center + piece.Offset;
				if (rearRamp && Landings.Contains(cell)) continue;
				if (!Feet.Contains(cell) || !anchors.Add(cell)) throw new InvalidOperationException("Source-grid actor lacks a blocked low-foot anchor.");
				var mouthEnd = rearRamp && (rampDirection == 2 && cell == RampCell(-1, 0) ||
					rampDirection == 3 && cell == RampCell(-1, -4));
				var actor = new ActorReference("calibration.closedcliff" + (piece.Slot + (mouthEnd ? 100 : 0))) { new LocationInit(cell), new OwnerInit("Neutral") };
				Actors.Add(new MiniYamlNode("ClosedCliff" + Actors.Count, actor.Save()));
			}
			if (!Feet.SetEquals(anchors)) throw new InvalidOperationException("Blocked foot ring has an unassigned cliff cell.");
			if (rearRamp)
				// Retaining strips are the established derived ramp vocabulary, not
				// newly claimed native ramp artwork. Exterior full-height cliffs stay native.
				RubberduckPlateauRenderer.AddActors(map, Patch, Actors, (c, d) => Ramps.Contains(c + PlateauTopology.Directions[d]));
		}
	}
}
