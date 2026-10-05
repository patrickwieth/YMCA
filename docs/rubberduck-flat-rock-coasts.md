# Flat rock coasts: all corner orientations and water presets

## Terrain contract

Ground level 0 and land plateaus at 4 are the intended two levels. Water cliffs
have **pseudoelevation only**, always height 0. Their bank and water-side rock
footprint are impassable. Ordinary shore remains accessible according to unit
locomotors; ground units still cannot walk into deep water. Bridges remain deferred.

## Shared contour planner and artwork

`RubberduckRockCoastRenderer` builds an edge/vertex graph, rather than independently
capping each face. It handles all four convex and all four concave corner orientations.
At an inward turn, the corner replaces the two overlapping faces. Straight joins have
no extra caps; open ends get native rock abutments at distinct start/end sockets.
The front sprites are calibrated at the start vertex and rear sprites at the end vertex;
the opposite endpoint is translated by the edge vector. Previously both ends used the
same bank anchor, collapsing two caps into one on a single-cell run and misplacing one
end on longer runs. End-piece water footprints are now reserved and movement-tested too.
Branching vertices and diagonally
pinched passages are rejected rather than concealed with overlapping rock pieces.

The original `grassland/rock_cliffs.png` supplies the unwarped faces and joints.
Logical corner IDs 24–31 use source slots 24–31 except rear convex ID 28, which uses
source rock-pile slot 16. The source V-shaped rear rim was unsuitable as an outside
corner and left a gap/protruding roof; the solid abutment replaces it. Rear inner rim
placement was corrected separately. Four directional end abutments also use slot 16.
No source PNG is modified, stretched or recolored.

48 PNGs in `mods/ca/bits/terrain/rubberduck/derived/coasts` carry frame metadata and
`provenance.txt`: 12 faces, 8 joints, 4 abutments, plus 24 legacy alpha-end variants
retained for calibration comparisons. The shared contour renderer does **not** use
those alpha-end variants. Rules/sequences are registered in `mods/ca/mod.yaml`.
Variants depend on coordinates, without consuming simulation RNG.

`RubberduckRockCoastPlan` checks flat terrain, protected cells/resources, water-side
clearance, surviving cardinal land/water connectivity and remaining landing access
before mutation. Convex corners reserve their diagonal water footprint too.
Collision-only tiles preserve the underlying material:

- 14010: water image with Cliff movement terrain.
- 14011: sand image with Cliff movement terrain.
- 14012: grass image with Cliff movement terrain.

## Editor

**Rock coast** places seven-cell straight banks, five-cell convex corner legs, or
seven-cell inward-bay legs. Convex stamps leave clearance for the corrected end pieces
before a nearby turn; longer contours can still be traced using Shift.
All four convex corners and four concave corners are supported. Click the land corner
for an inward bay; the tool finds its diagonal water quadrant.

**Shift-drag** traces an existing bank freely, with interpolated mouse positions and a
512-cell limit. Release validates and commits the entire stroke as one Undo step;
right-click cancels. Invalid terrain, resources or blocked access reject the whole
stroke. Plain clicks retain the seven-cell stamp behavior. This adds rock banks along
existing shores; it does not sculpt land/water or change elevation. Overlapping strokes
can now extend or join supported existing coast components.
The yellow outline is an unvalidated draft.

`RockCoastBrush` uses the shared planner/renderer and actual editor actor footprints.
Resources and unsupported coast compositions are protected. Terrain and actor definitions
are captured by `PlateauTerrainEditAction`/`PlateauActorEdit.Replace` as one Undo/Redo
operation. The tile-only overload retains exact original heights.

**Ctrl-click** a blocked bank or water-side footprint to remove the entire connected
rock-coast component. `RubberduckCoastRemovalPlan` reconstructs its complete collision
footprint and canonical actor graph without mutating the map. Missing, duplicated or
customized artwork, resources, non-flat terrain and occupied/shared footprints are
rejected. Both terrain and artwork are removed in one undoable transaction. Recovery
does not depend on editor-specific actor IDs, though arbitrary historical compositions
are intentionally unsupported. Removal restores canonical grass/sand/water tiles (index
0); Undo restores the exact blocked state, not a guessed earlier brush history.

**Alt-click** a blocked land-bank cell to trim that one cell. The complete component is
validated first, then the remaining footprint and end pieces are rebuilt against a
nonmutating restored terrain view. The new footprint must be a subset of the old one:
trimming can only open terrain, never acquire blocked cells elsewhere. The new end
pieces need clear water; tight side-facing inner cuts can therefore be rejected.
Removing one bank cell does not necessarily make a navigable landing: surviving end
pieces may still occupy its water approach. It shares the
same atomic terrain/actor Undo/Redo and protection checks as whole-component removal.

**Extend/join:** draw with Shift across an existing bank end or through the gap between
two supported components. Plain stamps also use the extension planner. It recovers all
nearby affected components, retains their banks, and rebuilds the combined contour,
removing internal end caps. Old collision footprints may not disappear during extension.
The combined bank limit is 512 cells; recovered footprints are capped at 4096 cells.
No-op strokes and unsafe/customized components are rejected without mutation.

Connectivity and landing checks use the currently playable terrain, not a temporarily
restored coast. Thus joining cannot erase isolated land/water components or close the
last landing. In particular, a single trimmed convex tip may become cardinally isolated;
rejoining that tip is conservatively rejected. Undo remains the way to reverse that trim.
To redraw unsupported geometry completely, first use a supported whole-component removal.
Do not delete only artwork: that leaves collision behind.

## Generator

`RubberduckCoastTopology` reserves landing patches near players and at opposite sides
of each traversable land component and each water component. Coast selection is then
marked in the plan, before export. It now covers **Archipelago, Continents, Migration
and Water Basins**. **Two Sides** retains its existing defensive CliffShore/LaunchShore
policy rather than having it overwritten.

For Water Basins, the coast policy runs **before Glitter placement**. Glitter skips
reserved CliffShore cells; existing resources are not deleted to make a later decoration
fit. Resources on other presets remain protected. Migration's six Oil Derricks per
player and resource-free starting islands are unchanged.

`RubberduckGeneratedRockCoasts` processes connected coast components, including turns,
rather than turning individual eligible cells into isolated rock posts. Accepted
components have 3–512 bank cells. The arbitrary 32-stamp cap was removed. Actor locations
have a conservative five-cell margin; important planned features and launch patches have
additional clearance. Resource cells themselves are protected exactly. Invalid or unsafe
components remain ordinary shore. The pass does not reroute or close a landing just to
fit artwork. Existing map packages are not rewritten; new exports use this policy.

Connectivity and reserved landings are verified, but this is not a certification of
identical naval route lengths or every multiplayer matchup.

## Current evidence

Root: `%TEMP%/ymca-rock-coast`.

### Side-corner alignment correction

The east/west **outer** joints (26/30) incorrectly used the inner-joint offsets
`128,32` / `-128,32`. Unlike inner joints, their actor anchors share the bank cell
with both adjoining faces. Correct offsets are `64,0` / `-64,0`: the source roof
apex `(64,64)` then meets the rear-face socket at `(+/-64,-64)`. This removes the
outward horns and closes the visible side gaps without substituting rock piles.

Only the two installed PNGs' offset metadata changes. Their full RGBA pixels remain
identical to the original sheet; collision, resources, actor graphs and Undo data do
not change. Existing maps using the shared sequences benefit without regeneration.
The utility now checks decoded offsets and every source RGBA channel for both joints.

Evidence under the root above:
- `side-alignment`: normal in-game gallery inspected; 18 runtime assertions.
- `side-final-gallery-shaded`: shaded gallery inspected; 18 runtime assertions.
- `side-final-editor-corners-shaded`: installed-art editor regression, 90 assertions
  including all eight corners, legacy recovery and save/reload.
- `side-final-generated`: actual Water Basins 4P42 map using installed art, six runtime
  assertions; terrain unchanged from its earlier export.
- All 48 final exported PNGs match the installed copies byte-for-byte.

`gallery-shaded` is now a utility fixture mode. The `side-connected` experiment that
added another rear face to inner joints produced protruding rims and was reverted;
it is not installed. Inner joints retain their original art and offsets. This fixes
the specific side placement defect, not all plateau/material seams or protected
raster teeth described in the broader terrain work.

### Endpoint correction

- `end-editor-checked` and `end-shaded-editor`: **90 editor assertions each**. All eight
  corners still place/remove correctly. Six corner trims succeed; two tight inward
  trims are now explicit rejection tests because their new cap would extend onto land.
  Legacy-cap terrain/art recovery and exact Undo are tested, alongside live material
  updates, resource protection, trimming/joining/extensions and save/reload.
- `end-gallery-final`: **18 runtime assertions**, including every corner socket and all
  eight open-end footprints, plus four-direction real cargo unloading/inland travel/
  reboarding. Gallery art is now loaded from its just-exported embedded PNGs, not stale
  installed files. Four single-cell preflights check two distinct cap locations.
- `end-matrix`: **20 exports**, all five water presets, players 4/8, seeds 42/43.
- `end-runtime/{two-sides,water-basins}`: six assertions per actual generated 4P42 map,
  including all cap footprints. `end-shaded-play`: five assertions on the actual shaded
  editor save. These are representative runtime maps, not all 20 exports played.
- Final gallery/editor/generated screenshots were inspected. The `side-sockets`
  experiment replacing side corner wedges with isolated rock piles created visible
  gaps and was **rejected**; neither those PNGs nor that art change were installed.
  `end-editor`, `end-editor-v2`, `end-editor-v3` retain failed fixture assumptions that
  ignored the new clearance or incorrectly expected every inward trim to be rejected.

Legacy saved coasts are recognized by their complete old footprint and actor graph,
not silently upgraded during selection or load. Their old geometry can be removed and
undone exactly. Repainting/extending uses the new validated footprint; it may reject
an old location that lacks cap clearance. Original art is unchanged. The subsequent
side-corner alignment fix is documented above; movement assertions alone do not
certify visual quality.

### Earlier evidence (before the endpoint correction)

- `join-checked`: **92 actual-editor checks**, retaining prior removal/trim coverage,
  rejoining four inner-corner orientations, rejecting four isolated convex-tip closures,
  Shift-mouse joining a split bank without internal caps, overlapping extension,
  resource rejection, exact Undo/Redo and save/reload. `join` is the initial failed test
  that incorrectly expected isolated convex-tip closure to succeed; protection was kept.
- `join-play`: four real transport/cargo assertions on the exact final editor save.
  The `joined` fixture mode explicitly targets the reclosed bank cell at the former gap,
  rather than an unrelated original rock bank. It rejects craft/passenger entry,
  unloading and boarding there; an ordinary beach still supports unloading, inland
  travel and boarding. `map.bin` is identical to the editor save. Both screenshots reviewed.

- `trim`: **73 actual-editor checks**, including trimming and exact Undo for all eight
  corner orientations, an actual Alt-click opening a straight bank, exact Redo, and
  save/reload of the split coast. The previous removal/freehand checks remain included.
- `trim-gap-play`: four real cargo/movement assertions on that exact saved terrain.
  The new `trimmed` playtest mode specifically selects an opening flanked by rock banks,
  rather than an unrelated pre-existing beach. Cargo unloads, reaches inland goals and
  boards again through the newly cut gap; remaining rock still rejects entry/boarding.
  `map.bin` is byte-identical to the editor save. Screenshots were reviewed.
  The first route search rejected this fixture because a spaced passenger goal fell
  on surviving rock; trimmed-mode goals now sit two cells farther inland. Terrain and
  collision were not altered to accommodate the probe. `trim-play` is the earlier
  ordinary-beach regression, not evidence of traversal through the new gap.

- `removal-checked`: **54 actual-editor checks**, including removal and exact Undo for
  all eight corner orientations, Ctrl-click removal of a freehand bank, exact Redo,
  resource rejection and rejection of a missing actor without terrain mutation. The
  restored coast is saved/reloaded. This is editor-state evidence, not a new long-match
  or generated-coast removal certification. `removal` preserves an initial failed test
  that incorrectly assumed inward banks were sand rather than grass; the fixture lookup
  was corrected without relaxing the removal planner.

- `freehand-final`: **32 real-editor checks**, retaining the eight-corner matrix and
  adding interpolated Shift-drag, nonmutating draft, atomic Undo/exact Redo and rejection
  of the entire resource-crossing stroke. The saved layout contains the freehand stroke.
- `freehand-play`: **four actual cargo/movement assertions** on that editor save:
  rock rejection, beach unloading, inland arrival and reboarding. Both new screenshots
  were reviewed. This straight-bank fixture has no corner sockets to count.

- `editor-all-corners`: **27 real-editor checks** including actual mouse placement of
  all 8 corner types, exact Undo for each, repeated general Undo/Redo, resource protection,
  tile-transaction failure rollback and save/reload equality. The last saved stamp is
  played directly in `complete-editor-play`, without regenerating its terrain.
- `eight-corners-final`: a cross-shaped island exercises all 8 corner orientations
  (12 actual joints), with four beach gaps. **17 runtime assertions** check all corner
  bank/water sockets against landing craft, infantry and tank movement, then actual
  blocked rock boarding/unloading, successful beach unloading, inland arrival and boarding
  again in each direction. The final whole-fixture screenshot was reviewed.
- `complete-matrix`: **42 successful generation/gameplay-validation/export-reload cases**:
  five water presets, players 2/4/8/12, seeds 42/43, plus Strategic Two Sides and Water Basins.
  A harness timeout interrupted the batch; incomplete cases were rerun and all 42 logs
  checked for completed validation, not counted on the basis of build success.
- `complete-runtime/{two-sides,archipelago,continents,migration,water-basins}`: actual
  generated 4P seed-42 maps, one transport/cargo route per preset. Each passes the four
  real cargo/movement assertions plus a check of **all rendered corner sockets** on that
  map. These are five runtime maps, not 42 runtime matches.
- `complete-editor-play`: five assertions on the actual editor save: its rendered corner
  sockets, rock rejection, beach unloading, inland movement and boarding again.
- Original source hashes remain unchanged. Final editor, gallery and five generated-map
  screenshots were inspected. Build and `git diff --check` are separate checks.

The cargo probe waits for exit activities to finish before issuing inland orders,
spaces passenger goals and records arrivals independently so later vehicle nudging
cannot invalidate an already-observed arrival. It removes only its own failed-boarding
probe actor before the independent landing test. Earlier timing/nudging failures are
preserved in historical evidence, not counted as successes. This does not certify the
older unrelated shore-tank calibration failure.

## Reproduction

After a Release build and the usual utility environment:

```
OpenRA.Utility ca --rubberduck-rock-coast-test SOURCE OUTPUT gallery
OpenRA.Utility ca --rubberduck-rock-coast-test SOURCE OUTPUT editor-corners
OpenRA.Utility ca --rubberduck-rock-coast-playtest MAP OUTPUT generated
OpenRA.Utility ca --rubberduck-rock-coast-playtest TRIMMED_SAVE OUTPUT trimmed
OpenRA.Utility ca --rubberduck-rock-coast-playtest JOINED_FIXTURE_SAVE OUTPUT joined
```

The last command also supports the one-stamp save from `editor-corners`; it discovers
an actual blocked bank and a reachable landing on the saved map. `runtime-results-path.txt`
identifies the hash-validated temporary log; editor saves sit beside that log. Temporary
diagnostic map packages are removed from installed maps afterwards. No fetched engine
patch is required.

## Material-edge rendering

The shared runtime/editor [material transition overlay](rubberduck-material-transitions.md)
now softens ordinary grass/sand/water boundaries without changing collision or source
assets. This is a first visual pass, not a fix for the underlying rasterized coast shape
or all native cliff sockets. The linked document records normal/shaded checks and limits.

## Scope still not covered

This completes the formerly missing **corner directions, inward turns and coast policy
for the other four water presets**. It is not a claim that the whole terrain project is
finished. Remaining broader work includes native
phased-water and smooth grass/sand/water blends, shaded/occlusion/faction regression,
large-map profiling and long-match balance. The [coast smoothing pass](rubberduck-coast-smoothing.md) reduces eligible raster
corners in new maps while preserving area and generator symmetry groups. Protected or
unsupported contours still visibly zigzag; individual native joints still need polish. Bridges are still deferred.
