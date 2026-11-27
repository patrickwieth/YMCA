# Mountain Valleys: larger duels and non-forward home ramps

Local Game change, following the user's explicit 1on1 pacing request. Desert stays
an intentionally compact rush map. No release/deployment or existing map rewrite.

## Behavior

- Automatic **two-player Mountain Valleys** plans are now **112x112**, previously
  80x80: 1.96 times the plan area. The wizard and CLI share `SetAutomaticSize`, so
  both use this policy. Explicit CLI sizes are still respected.
- Mountain home ramps in two-player games use the actual opposite spawn as their
  avoidance target. Directions are evaluated in native isometric cell axes, not
  staggered storage coordinates. The planner chooses the closest-to-lateral exit
  in the half-plane facing away from the opponent (nonpositive dot product).
- Ties prefer the same rotational sense relative to the opponent, rather than
  the same global cardinal direction for both players. If a permissible exit
  cannot be fitted, the layout is retried; there is no enemy-facing fallback.
- Existing ramp width/height, terrain passability, resource budget policy and
  starting-building footprints are unchanged. Enlarging/reorienting the layout
  necessarily changes resource locations and the generated map UID.
- Desert remains **80x80** at 2P. Other presets and multiplayer mountain sizes
  were not enlarged in this request; e.g. Mountain Valleys 4P remains 96x96.
  Existing saved maps/replays retain their original bytes. Generate a new map
  to use the new size/ramp policy.

## Checks

`--check-mountain-duel OUTPUT_DIRECTORY` generates seeds 1,2,3,7,11,42,43,101 in
Patches and Glitter modes, covering finite and infinite resources. It validates
both starts, exactly one usable home ramp each, non-forward orientation, native
height-aware paths between opponents and the existing gameplay/economy guards.
All cases passed. Operational and Strategic 2P seed42 also generated successfully.
The cardinal plan paths measured 145–213 steps; these are not unit travel times.

Native Patches seed42 checks passed all 14 construction-yard footprint types at
both starts. Both real harvesters reached their resource destinations (18-cell
native paths each). Engineer, jeep and medium tank reached the opposite start.
Those actual unit paths use diagonal movement and are therefore shorter than the
cardinal planning diagnostic. Native overview and both home views were inspected;
ramp accesses are on the non-opponent-facing sides.

Fresh before/after exports for Desert 2P42 and Mountain Valleys 4P42 had identical
contents for every map-package entry. Evidence: `art-source/mountain-duel-followup/`.
Earlier biome-only evidence remains a historical phase boundary: this later,
authorized layout change supersedes the old Mountain Valleys 2P size, not its
Barren styling. This is focused validation, not a claim of final playtest balance.
