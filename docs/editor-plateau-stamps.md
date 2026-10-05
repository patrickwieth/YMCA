# Rubberduck editor: atomic plateau stamps

## Available workflow

The Rubberduck editor has two stamp buttons at the upper left (plus [Lower empty plateau](editor-plateau-removal.md) and the
[Grow/join and Trim brushes](editor-plateau-shaping.md) for existing terrain):

- **Plateau + four ramps**: click clear ground to place one height-four plateau.
- **Remove plateau stamp**: click inside a plateau created with this tool to remove it.
- Right-click cancels the tool; ordinary editor Undo/Redo applies to the entire operation.

The first supported shape is the shared `PlateauTopology.CalibrationPlateau`: a filled
15x15 CPos footprint with four three-wide, four-level ramps and blocked retaining strips.
`RubberduckPlateauRenderer.AddActors` plans the same derived wall vocabulary used by the
generator's fallback renderer. Clear roofs remain terrain, not overlapping grass actors.

This is deliberately **not a general freehand height brush**. A 21x21 clear, flat grass
area is required, and the complete clearance must fit inside the map. Overlapping
plateaus, resources and actors are rejected before any changes. Removal requires an
unchanged stamp with its saved `EditorPlateau_X_Y_*` actor IDs. Existing generated
Mountain Valleys plateaus are not silently treated as editable stamps. Eligible empty
components can instead be lowered with the separate tool linked above.

Stamps currently use derived faces, not the native contour/apron planner. The separate shape brushes support freehand growth, joining and trimming, with shared
native contour replanning and preserved ramps. Whole-plateau deletion remains limited
to isolated empty components with protected clearance. The
buttons and status messages are currently English; localization and smaller-resolution
layout polishing remain open.

## Atomic changes and previews

`PlateauTerrainEditAction` captures terrain tiles/indices and heights once. Execute,
Undo and Redo replay those values deterministically; no random variants are rerolled.
The actor patch applies through the real `EditorActorLayer`, preserving all unrelated
actors and IDs. Failed actor operations restore their partial actor changes and then
restore the terrain. Reapplying an already applied action is rejected.

The existing editor tracks stamp actors in its normal map actor definitions. Therefore
no custom save format or temporary session-only ownership registry is required.

Both `PlateauFaceBody` and `NativeCliffBody` now expose editor previews using the same
clipping implementation as gameplay. `TerrainRenderRevision` invalidates cached slices
when tiles or heights change, including changes to neighboring terrain. It uses one
subscription pair per map and weak map keys, not one subscription per actor. The first
render after a transaction rebuilds invalidated slices.

This refreshes **clipping**, not arbitrary native contour selection after freehand edits.
Large-map editor performance still needs profiling because a terrain revision currently
invalidates all wall previews for the map, not only a spatially indexed subset.

## Mod-side integration

No fetched-engine edits are required. `mods/ca/chrome/editor.yaml` is a mod-side copy of
the current common editor chrome, with the added logic and controls. `mod.yaml` loads
this copy instead of `common|chrome/editor.yaml`. OpenRA's widget loader rejects duplicate
root definitions, so an overlay file with another `EDITOR_WORLD_ROOT` is not sufficient.
Review the copied chrome against upstream when changing engine versions.

## Tests and evidence

After building CA and setting the normal utility environment:

```
OpenRA.Utility ca --rubberduck-editor-test OUTPUT
```

This tests three direct Do/Undo cycles and rollback after an injected actor-patch failure.
It also exports `rubberduck-editor-test.oramap`, a diagnostic that opens the real editor
and exercises the actual button callbacks, brush action factory and `EditorActionManager`.
The fixture must only be installed temporarily.

`%TEMP%/ymca-editor-plateau-checked` contains the reviewed editor screenshot, transaction
results and a runtime log with nine PASS entries:

- Placement and clipping-cache refresh/recovery for both wall renderers.
- Undo and redo of placement.
- Rejection of overlap, resources and occupied deletion, without state loss.
- Removal, undo removal, redo removal, and restoration for the screenshot.
- Save/reload equality of tiles, heights, ramps, resources and actor IDs/definitions.

The editor writes its save-test package beside the validated, hashed temporary probe log;
it does not accept an arbitrary map-supplied save path.

The actual saved package was then tested as a game, **without regenerating its terrain**:

```
OpenRA.Utility ca --rubberduck-editor-playtest SAVED_EDITOR_FIXTURE OUTPUT
```

`%TEMP%/ymca-editor-plateau-play` contains the reviewed ingame screenshot and **16 successful
ascent/descent legs plus eight paired steep-edge checks**, using Light Infantry and Heavy
Tanks on all four orientations. These movement tests use diagnostic actors; ordinary
saved stamps contain only the terrain decorations and the map author's actors.

A native-generation runtime regression in `%TEMP%/ymca-editor-native-regression` also
passed 32 movement legs, eight low-foot checks, eight paired edge checks and MCV deployment;
its ingame screenshot was reviewed after the renderer refactoring.

Release build and `git diff --check` pass. This evidence does not certify unrestricted
terrain painting, native-piece reassignment around arbitrary edits, or every tileset/UI size.
