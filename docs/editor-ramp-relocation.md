# Moving and rotating plateau ramps

The Rubberduck editor's **Move ramp** tool relocates a complete three-wide,
four-level ramp as one terrain/actor transaction.

1. Choose **Move ramp** and click any cell on the existing ramp.
2. Click the new outer roof-edge cell that should become the low end of the ramp.
   For a small overlapping shift, this can be a cell in the selected ramp.
3. The edge determines the new facing automatically. Right-click cancels selection.
4. Undo/Redo restores/replays both openings, their retaining strips, native feet and
   cliff actors together.

This supports changes between all four orientations and one-cell overlapping shifts.
It does not create arbitrary-width or arbitrary-height slopes.

## Additional ramps and individual closure

**Add ramp** needs no source selection: click a free straight outer roof edge to
create another three-wide, four-level access. The same destination geometry and
resource/actor protections apply. Existing ramps are retained.

**Close ramp** takes one click on a supported complete ramp. Its slope and retaining
strips become height-four grass, with the cliff boundary and blocked native apron
replanned. Closing the last accessible route is rejected before any mutation. This
uses the shared connectivity validator, not merely a count of remaining ramp tiles.
Each addition or closure is one independent Undo/Redo action. Right-click cancels.

Latest add/close evidence: `%TEMP%/ymca-ramp-add-close`:

- `checked/preflight-results.txt`: add and close in all four orientations, last-access
  rejection, and the previous 16-direction/eight-overlap relocation regression passed.
- `checked/runtime-results.txt`: 23 editor checks, including actual add/close mouse
  clicks, resource protection for both tools, exact Undo/Redo, last-access rejection
  without mutation, restoring retained accesses, and save/reload. The final save is
  produced by **separate Add and Close operations**, not by the Move tool.
- `play/runtime-results.txt`: 16 real ascent/descent legs on that save (Infantry and
  Heavy Tanks, new and retained ramps), plus eight blocked-entry/pathfinding checks
  at the closed opening. Final editor and gameplay screenshots were reviewed.

The runtime fixture remains a radius-12 plateau with a newly added north-facing
ramp and a separately closed east-facing ramp. This does not certify every generated
contour or faction. Arbitrary heights/widths and the remaining native-art coverage
are still open. Bridges are deferred pending suitable assets.

## Geometry and safety

`PlateauRampMovePlan` recognizes the full 3x4 slope from any of its lanes/levels,
plus the two retaining strips and clear upper/lower landings. It revalidates the
source at the second click rather than trusting old selection state.

The source is virtually closed to height-four grass before validating the destination.
This allows overlapping shifts without leaving an old guard strip in the new lane.
The destination replaces the overlapping part of that closure. It requires:

- A straight edge, five cells wide and five roof rows deep (four slope rows plus landing).
- Two low approach rows. Only the immediate apron row may replace Cliff 3992;
  the outer row must already be clear ground.
- No changes to protected resources, actors, building footprints or other ramps.
- Unambiguous edge orientation and the shared editor boundary clearances.

`PlateauShapePlan` now accepts the verified ramp patch internally. Only the selected
ramp is exempted from the ordinary shape brush's ramp protection. The new ramp and
landings are protected from native-foot reservation. The usual cardinal/corner-aware
connectivity checks still run, preventing an inaccessible roof or disconnected surviving
terrain. This is not automatic team-symmetry or path-length balancing.

The production native cliff planner and fallback renderer rebuild both boundaries;
`PlateauTerrainEditAction` and `PlateauActorEdit.Replace` preserve atomic Undo/Redo.
The source opening becomes a blocked cliff edge except where the new ramp overlaps it.
No fetched engine changes or source-art modifications are required.

## Evidence

Evidence root: `%TEMP%/ymca-ramp-move`.

- `overlap-checked/preflight-results.txt`: all **16 old/new facing combinations**, eight
  **one-cell overlapping shifts** (both signs, all orientations), and source-resource
  protection passed. The matrix applies/undoes terrain patches and verifies ramp recognition.
- `overlap-checked/runtime-results.txt`: **12 real-editor checks**, covering selection,
  cancel, changed-source rejection, destination resources, overlapping shift/undo, actual
  two-click move/rotation, repeated exact Undo/Redo and save/reload equality of terrain,
  resources and actor definitions.
- `final-play`: the actual final editor save, without regenerating geometry. Light Infantry
  and Heavy Tanks passed **16 ascent/descent legs** across the moved ramp and three retained
  ramps. Eight high/low entry/pathfinding checks rejected the now-closed original opening.
  Editor and ingame screenshots were reviewed with both changed edges in view.
- `shape-regression`: the existing join/trim preflight and negative connectivity checks
  still passed after introducing the shared ramp-patch path.

The runtime relocation is east-facing to north-facing on a radius-12 plateau fixture.
The 16-way matrix is not 16 runtime matches. Overlapping shifts have planning and editor
coverage, not a separate ingame movement matrix. Arbitrary generated contours, shaded
art and every faction/UI resolution are not certified by this fixture.

Commands, after a Release build and the normal utility environment:

```
OpenRA.Utility ca --rubberduck-ramp-move-test OUTPUT
# Temporarily run its editor fixture; the saved map is beside the validated hashed log.
OpenRA.Utility ca --rubberduck-ramp-move-playtest EDITOR_SAVE PLAY_OUTPUT
```

The initial `first`, `final`, `reviewed`, `play` and `reviewed-play` folders contain
intermediate evidence. `overlap-checked` and `final-play` are the latest runs. Diagnostic
maps are removed from the installed map folder after testing. Release build and
`git diff --check` pass.
