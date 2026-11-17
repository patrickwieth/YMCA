# Freehand plateau shaping and joining

The Rubberduck editor now offers **Grow / join plateau** and **Trim plateau**.
These extend the [stamp](editor-plateau-stamps.md) and
[whole empty plateau removal](editor-plateau-removal.md) workflows.

## Usage

- Drag with the left mouse button to draw a three-cell-wide stroke.
- Hold **Shift** for single-cell detail work.
- Release to validate and apply the entire stroke in **one Undo step**.
- Right-click cancels without changing the map.
- Yellow outlines are drafts, not promises that the stroke is valid. A rejection is
  explained in the status line and leaves terrain and actors unchanged.

Grow raises eligible grass/old native-foot cells to height four. Draw between two
accessible roofs to join them with a walkable raised connection. Trim lowers painted
roof cells to zero, then replans their boundary and any required blocked cliff feet.
A trimmed cell may therefore become a low cliff foot rather than walkable grass.

The normal height-aware editor picker selects visible terrain. A low cell hidden by a
foreground roof cannot be selected by clicking that roof's image; the generated-map
mouse test explicitly uses an exposed edge rather than bypassing picking.

## Topology, protection and shared artwork planning

`PlateauShapePlan` reconstructs affected elevated terrain and nearby/shared-foot
components from saved map layers. It preserves existing ramp geometry and a two-cell
clearance around ramps, including landings and retaining strips. Resources, starts,
actors and actual building footprints are protected. An occupied plateau can be edited
**away from these protected cells**; its economy and units are not silently removed.

The complete stroke is planned without changing the map. Old feet are reconsidered;
new feet and full native-piece sockets are selected by the production
`RubberduckNativeCliffPlanner`, through a map-coordinate adapter. The same
`RubberduckPlateauRenderer` fills unsupported contours. This is not an editor-only
texture approximation, but neither is it a claim of complete native artwork coverage.

Only changed terrain cells enter the transaction, preserving unrelated tile variants,
resources and actors. Affected cliff definitions are replaced together with the terrain.
`PlateauActorEdit.Replace` restores the old graph if adding new pieces fails; Undo and
Redo use captured terrain/actor definitions, not rerun random selection. Both wall caches
invalidate through the existing terrain revision mechanism.

A height- and corner-aware cardinal connectivity check runs before applying the stroke:

1. Previously connected surviving cells may not be split into separate regions.
2. Every affected walkable raised roof must retain access to low ground through a ramp.
3. Newly reserved native feet must not touch protected terrain.

The ground model includes editor resource terrain overrides (Ore, Gems and both Tiberium
variants); otherwise an editor-loaded ore field would incorrectly look like a hole in
the roof. Actors are conservatively treated as occupied cells for planning. These checks
prevent isolation, not changes in route length, team symmetry or competitive balance.

## Supported limits

- Height-four grass plateaus, Clear 1000, Cliff 3992 and ramp templates 14001–14004.
- Shape strokes preserve existing ramps. Use the separate [Move ramp tool](editor-ramp-relocation.md)
  to relocate/rotate a supported ramp, or the four-ramp stamp for a new independent plateau.
- Strokes of at most 2048 cells; at most 8192 elevated cells in the affected neighborhood.
- Boundary clearance is required. Unsupported terrain, custom cliff initializers and
  unsupported Rubberduck decoration types are rejected.
- Cliff actor IDs can change during replanning. Undo restores the originals. Custom
  scripts depending on decoration actor IDs/rule replacements are not certified.
- Broad material-transition cleanup, complete native coverage, arbitrary elevations,
  automatic symmetric editing, localization and large-map performance remain separate work.

## Validation and evidence

Evidence root: `%TEMP%/ymca-plateau-shape`.

### Joined/trimmed roofs with existing economy

`final` has nine preflight PASS results and **14 real-editor checks**. Its two plateaus
include a resource field and a refinery. Tests cover:

- Refusal of resource/building/ramp/approach/boundary edits and an inaccessible detached roof.
- Joining the roofs and trimming a corner, with native cliff replanning.
- Refusal of a second connection that would seal a low courtyard after its inward ramps
  were closed in a temporary negative fixture.
- Cancellation and no mutation while dragging; protection rechecked at release.
- Rollback after partial new-cliff creation fails.
- Complete Undo/Redo chains and save/reload equality of terrain, resources and actor IDs.

The join changes 57 cells and selects 17 native pieces; the trim changes 12 cells.
The final editor screenshot was reviewed. Earlier `first`/`checked` folders are intermediate
runs, not substitutes for the final editor transaction evidence.

`play` runs the actual editor-saved geometry, without regenerating terrain: **16 ascent/
descent legs on all four original ramp orientations, eight raised-connection traversal
segments, and eight paired steep-edge checks** passed with Light Infantry/Heavy Tanks.
Bridge probes require height four throughout, visit a waypoint on the new connection,
and reject routes that descend to the valley. The ingame screenshot was reviewed.
`final/gameplay-equivalence.txt` verifies that the final editor save has byte-identical
`map.bin` and actor definitions to the saved map used by this runtime test.

### Existing generated native plateau

`generated-visible` uses a normal 128-square Mountain Valleys map (3 players, seed 43).
A real Shift-click extends an exposed edge at CPos `148,-98`: **15 terrain changes and
81 native pieces** after replanning. Five real-editor checks cover the non-mutating draft,
extension, exact Undo/Redo and save/reload. This is not a stamp-only integration.

`generated-play` loads that actual editor save and verifies that resources, original ramps
and non-cliff actors remain intact. Infantry and a Heavy Tank completed **eight traversal
segments**, repeatedly visiting the newly raised cell. Editor and ingame images were reviewed.
Earlier `generated` and `generated-front` fixtures selected cells hidden by projected roofs;
those mouse tests failed and were replaced, not counted as successful edits.

Utility commands (after the normal Release build/environment setup):

```
OpenRA.Utility ca --rubberduck-plateau-shape-test OUTPUT
OpenRA.Utility ca --rubberduck-plateau-shape-playtest EDITOR_SAVE PLAY_OUTPUT
OpenRA.Utility ca --rubberduck-existing-shape-test SOURCE_MAP OUTPUT
OpenRA.Utility ca --rubberduck-existing-shape-playtest SOURCE_MAP EDITOR_SAVE PLAY_OUTPUT
```

Editor diagnostics write their actual save beside a validated, hashed temporary probe log.
Diagnostic maps are installed only for tests and removed afterward. No source artwork or
fetched engine files are modified. Release build and `git diff --check` pass.
