# Conservative coast geometry smoothing

`RubberduckCoastSmoother` reduces raster corners in newly generated maps. Unlike the
[material overlay](rubberduck-material-transitions.md), this changes the land/water plan
before cliff artwork is selected. It is not a blanket redraw of installed maps.

## Constraints

- Exchange a land bank cell for a nearby water bank cell, preserving exact land/water
  area. Candidates lie on the original boundary and are touched at most once; the pass
  cannot progressively erode inland. Partner cells are within three cardinal steps.
- Apply complete symmetry groups: twofold for two-team continents, fourfold where the
  generator uses fourfold symmetry. Island generators use their integer center; Water
  Basins uses the half-cell center. Odd/non-grid rotational symmetries, non-square
  fourfold layouts and raised plateaus are left unchanged rather than rounded.
- A swap group must strictly reduce the local corner score without increasing boundary
  length. Diagonal pinches carry a larger penalty. Geometry is measured in actual
  isometric CPos coordinates, not by treating staggered map rows as square world cells.
- Each intermediate flip must be topologically simple for **both** cardinal land and
  water: its surrounding rings must remain connected. This prevents splitting,
  merging or erasing components; it is deliberately conservative.
- All planned features and home owners are protected with clearance. Mountain/shallow
  terrain and map edges are excluded too. Previously reserved launches are retained.
- Water Basins runs before Glitter placement. Archipelago runs after same-team links
  and launch reservation but before neutral economy. Migration runs before central
  resources. Continents protects its already placed home economy and links.

This does not certify equal naval travel times or minimum widths for every ship.
Pairing changes is not a substitute for long-match balance tests.

## Current integration and limits

The pass is integrated into Archipelago, Continents, Migration and Water Basins.
Two Sides is unchanged. It may intentionally accept **zero** changes: all tested
Migration cases, and some protected/fourfold Archipelago cases, remain unchanged.
The pass reduces eligible raster corners; it does not remove every visible staircase
or fix native rock sockets, end pieces, roof texture or water animation. Hand-edited
coasts retain their authored geometry. Bridges remain deferred.

## Evidence

Root `%TEMP%/ymca-rock-coast`:

- `smoothing-symmetry-preflight`: **10 checks**. The symmetric reference coast's corner
  score drops from 132 to 84 with identical land/water area. Checks cover component
  identity, protected landing/resource clearance, determinism, frozen geometry,
  integer-centered islands, complete fourfold groups and unsupported-symmetry rejection.
- `smoothing-symmetry-matrix`: **32 completed generation/validation/export cases**:
  four presets, 2/4/8/12 players, seeds 42/43. These are not 32 runtime matches.
- `smoothing-final-runtime`: actual exported Archipelago 2P, Continents 4P and Water
  Basins 4P seed 42. **Five assertions each**: every rendered corner socket rejects
  craft/passenger entry, plus rock boarding/unloading rejection, beach unloading,
  actual inland movement and reboarding. Runtime `map.bin` matches its source export.
  Screenshots were reviewed; visible remaining raster teeth are not claimed fixed.

Earlier `smooth-*`, `smoothing-matrix` and `smoothing-runtime` contain intermediate
experiments, including an overly restrictive adjacent-only swap and a twofold-only
version. The final implementation retains fourfold symmetry instead of smoothing just
opposite pairs on fourfold layouts.

Reproduce the pure topology tests with:

```
OpenRA.Utility ca --rubberduck-coast-smoothing-test OUTPUT
```

Release build, source-art integrity and diff checks are separate from graphical and
movement evidence. No engine patch or source-art modification is required.
