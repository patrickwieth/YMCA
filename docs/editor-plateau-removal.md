# Lowering existing empty plateaus

The Rubberduck editor now has **Lower empty plateau**, in addition to the
[stamp tools](editor-plateau-stamps.md). Hover a flat height-four grass roof to see
an outline and the affected cell/cliff-object counts. Click to lower the complete
isolated component to grass at height zero. Right-click cancels. One Undo restores
the original terrain, ramp templates, tile variants, cliff feet and actor definitions.

This works on supported saved generator terrain: it does not require the stamp
actor-ID convention or a generated map title. For freehand reshaping and joining, use
the separate [shape brushes](editor-plateau-shaping.md), which can edit unoccupied edges
of an occupied plateau without touching its protected contents.

## Conservative boundary and ownership rules

`PlateauRemovalPlan` reconstructs the component from the terrain layers:

- Starts on Clear template 1000 at height four.
- Follows connected positive-height/ramp cells, including diagonal retaining tips.
- Supports grass, Cliff 3992, and the four existing ramp templates, up to height four.
- Includes low Cliff feet immediately beside the component, including corner feet.
  It does **not** flood arbitrary connected low cliff terrain.
- Requires three cells of flat, clear grass clearance. Nearby elevation, shared or
  extended feet, resources, unsupported terrain and map-boundary conflicts are refused.
- Only ordinary neutral plateau-wall actors, the ten supported native cliff slots,
  and the known retaining-cap decoration can be removed. Custom initializers are refused.
- Spawns, buildings, units, other decorations and their actual editor footprints are
  protected. A building anchored outside the clearance is still checked for overlap.
- Limits the elevated connected component to 4096 cells.

These strict conditions intentionally reject many dense generated locations. The
80-square 3-player seed-42 test had no eligible component; rejection is not permission
to flatten surrounding terrain or delete economy actors to make the artwork fit.

The normal generator decoration rules are assumed. Arbitrary custom rule replacements
under the same actor names are not certified. Removal also intentionally opens new paths;
it is not a symmetry- or balance-preserving operation. Map authors must review gameplay
balance after edits.

## Transactions and rendering

`PlateauActorEdit` is shared with the existing stamp workflow. If an actor operation
fails after partial removal, it restores the affected actor previews before the terrain
action rolls its tile/height snapshot back. Every click performs fresh preflight checks;
a stale hover outline never authorizes an edit. The existing terrain revision mechanism
refreshes both native and fallback wall caches through undo/redo.

The outline uses grid-derived world coordinates, not rectangular world axes: CPos axes
are diagonal on the isometric grid. It traces the affected terrain footprint, not the
source sprites' irregular alpha silhouette. There can be height discontinuities in the
outline where its boundary crosses retaining strips and low feet.

## Evidence

Evidence root: `%TEMP%/ymca-plateau-removal`.

- `source128`: ordinary Mountain Valleys, 3 players, seed 43, size 128x128, resources None,
  finite, tech None. The tested empty component had **117 cells and 68 cliff objects**,
  including native pieces and a 12-cell four-level ramp.
- `final`: seven preflight PASS results and **ten real-editor PASS results**. Tests use
  the actual button, brush mouse handler, actor layer and history manager. They cover
  resource/actor protection, a refinery footprint whose anchor lies outside the clearance,
  partial actor-removal failure, complete lowering, two exact Undo/Redo cycles, and
  save/reload of all terrain layers and actor definitions. The before/outline and lowered
  screenshots were reviewed.
- `final-play`: the actual editor-saved lowered map, without regenerated terrain. Its
  exporter compares every unaffected cell/resource and all remaining actor definitions
  against the source map. **16 actual movement legs and eight bidirectional entry checks**
  passed with Light Infantry and Heavy Tanks crossing former blocked cliff feet in all
  four directions. The ingame screenshot was reviewed.
- `stamp-regression`: the previous stamp workflow still passes its direct transaction
  tests and all **nine real-editor checks**, including both cliff-preview caches and save/reload.

Reproduction, with the usual utility environment:

```
OpenRA.Utility ca --rubberduck-plateau-removal-test SOURCE_MAP OUTPUT
# Temporarily install/run the emitted editor diagnostic.
# runtime-results-path.txt identifies its hashed temporary log;
# the actual editor save is alongside it with extension .oramap.
OpenRA.Utility ca --rubberduck-plateau-removal-playtest SOURCE_MAP EDITOR_SAVE PLAY_OUTPUT
```

Diagnostic saves are restricted to the hashed terrain-probe temporary directory.
Diagnostic maps are removed from the installed map folder after testing. No original
artwork or fetched engine files are changed.

While creating the resource-free source fixture, a separate Mountain Valleys defect
surfaced: resources None with finite=false still placed generators and failed validation.
Generator placement now also checks the resource layout. Both finite values passed
3-player seed-42 gameplay validation after this fix.

Release build and `git diff --check` pass. The newer shape brushes and their native contour reassignment have separate test
coverage linked above. Large-map performance, localization and all UI sizes remain
outside this removal test coverage.
