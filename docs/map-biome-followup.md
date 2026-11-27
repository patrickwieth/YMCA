# Generated biomes, curated map picker and nuclear-site alignment

Subsequent authorized change: Mountain Valleys 1on1 is now enlarged to 112x112
with non-opponent-facing home ramps; see `mountain-duel-followup.md`. The layout
preservation claims below describe this earlier biome-only phase.

Local Game worktree only. No release, push, deployment, user-game termination or
historical map-package rewrite. Generate new maps to see the biome changes.

## Generated maps

- **Plains -> Desert** is a display-name change for the existing Arabia preset.
  Serialized enum/recipe values stay unchanged. CLI aliases Arabia and Plains
  remain valid; Desert is also accepted. Layout, starting resources and oil policy
  are unchanged. New maps use predominantly sand and a few sparse Dune-style trees
  (`t04`, `t08`, `t09`, `t18`, original DESERT sprite mappings and palette).
- **Mountain Valleys** uses the same dirt/Barren material family as handcrafted
  Glitters. Sparse trees use original BARREN sprites/palette. Existing Tiberium
  regenerators remain: field regenerators in infinite Patches mode and scattered
  generators in infinite Glitter mode; none with finite resources.
- **Two Sides** retains its terrain/layout and gains loose groups using the same
  tree variants as the handcrafted naval map (`t01`, `t05`, `t11`, `t16`).

`GeneratedBiomeScenery` runs after the physical terrain/cliff/bridge export. It
uses a separate coordinate hash, never the layout/resource random stream. Trees
are nonblocking decorations, not new pathfinding obstacles. Starts, ramps, bridge
approaches, resource generators, oil actors and planned entrances are protected.
For Glitter, trees use existing resource-free gaps instead of demanding a large
empty patch in a mostly resource-covered map. No resource cells are cleared.
Tree density is deliberately low, especially for Desert. Seed42 examples yielded
7/9 Desert trees, 6/10 Mountain Valleys trees and 21/31 Two Sides trees at 2P/4P.
These are validation examples, not fixed counts or balance changes.

Sand/dirt are cosmetic here: BaseTerrainOverrides retain the original gameplay
terrain so normal building placement and movement speeds do not change. Roofs,
slopes and green cliff fringes also receive biome-local artwork, avoiding green
roof/ramp islands on an otherwise barren map. New ramp template IDs 14101–14104
and 14201–14204 retain the original ramp shapes/terrain behavior. Original source
art is preserved; derived images retain alpha and PNG registration metadata.
Only explicitly green fringe pixels are changed on the closed-cliff sprites.
`tools/prepare_generated_biome_art.py` reproduces those derivatives from the
existing dirt/sand materials. No global cliff/roof image is redirected.

Custom parameter configurations keep their explicitly chosen terrain behavior.
Continents, Migration and Archipelago remain unchanged; their full map entries
were byte-identical in the before/after checks.

## Handcrafted picker

The wizard's Handcrafted Maps button now opens a restricted pool containing only
the three curated installed system packages: Glitters Barren, Dune Patrol and
Two Sides Naval. It checks canonical package names, Handcrafted Maps category,
lobby availability, excludes diagnostic categories/review titles and respects
mode compatibility. Multiplayer also intersects the server's allowed map pool;
it no longer bypasses this filter through the ordinary map chooser callback.
Random selection uses the same restricted pool. Modes without compatible authored
maps show an explicit empty-state message, not a fallback list of generated/tests.
Historical maps remain available by UID for replays. They are not deleted or
repacked. Adding a future curated map requires extending the package-name catalog.

Native menu testing showed exactly those three maps, exercised the empty Strategic
case, cancellation, generator/custom navigation and changing maps in an existing
lobby. The opponent and team/spawn configuration survived Desert -> Two Sides.

## Soviet nuclear-site correction

The shared `sovietbib4x3` sequence already existed for the service depot and carried
Offset `32,-16`. Simply naming that image on the nuclear plant therefore moved its
foundation away from the 4x3 cells. The nuclear-only overlay offset `-567,-362,0`
transforms through BodyOrientation to screen `-32,+16`, canceling that inherited
registration for both grid and player remap. The service depot is unchanged.

The former explicit DecorationBounds also described an axis-aligned screen diamond,
not a true 4x3 cell-axis footprint. The final override now uses
DecorationFootprint `4,3` without explicit DecorationBounds. Body scale remains 1;
its visible ground anchor is centered using Offset `3,-53` in both states. The
construction offset was regenerated accordingly. Footprint, health, power, cost
and weapons remain as previously approved.

`--check-nuclear-site` loads actual sequence sprites and verifies the combined
world/sequence offset is exactly zero for grid and remap, plus the 4x3 selection.
The resulting native selection and bib are aligned. Earlier art/test phase
snapshots remain frozen; the later registration change is checked separately.

## Validation/evidence

`--check-generated-biomes OUTPUT` compares independently generated plans with and
without cosmetic scenery at 2P/4P seed42, for the three changed presets and three
unchanged controls. Every cell retains gameplay terrain, height, ramp and resource
values, and all original actors remain identical. The cosmetic stage can only be
omitted through the internal exporter constructor used by this diagnostic; normal
menu/server generation always uses the new biome behavior.

Evidence and focused Python tests: `art-source/map-biome-followup/` and
`tools/test_map_biome_followup.py`. The isolated build, active-map checker, native
menu probe and nuclear sprite-registration check passed. Native terrain captures
cover all three biomes. This does not claim full release validation.

## Still open: Glitters angular edges

No production Glitters package or terrain geometry has been changed in this pass.
A map-local rounded clipping experiment was rendered and rejected as insufficient;
its source was removed from active code and retained only under `.tmp/` for study.
The exact remaining problem edge needs a marked screenshot to distinguish thin
rear lips, side sockets and intentionally angular island contours. Do not present
the experiment as a completed corner fix or alter the previously accepted map
shape/resources speculatively.
