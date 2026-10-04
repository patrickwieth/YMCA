# Closed original-family cliffs on actual height-four terrain

Historical stage: **implemented opt-in test path**. Current status is documented
in the [installed-family rollout](rubberduck-closed-family-rollout.md): the normal
editor stamp now uses this vocabulary; procedural conversion remains gated.
The flat original-art reference has now been transferred to real terrain heights,
blocked feet, physical vehicle movement and the shared editor transaction system.
The subsequent [contact/ramp work](rubberduck-closed-contact-and-ramp.md) adds
same-family side-matte reconstruction and one three-wide rear ramp. Ramp-end visual
acceptance and global integration were still open at this stage; the evidence
below predates the normal-editor switch.

## Implementation

- `MapGeneration/RubberduckOriginalClosedReference.cs` now lives in the shared
  map-generation namespace (moved from utility commands). Its five roof/contour
  templates are used by both the reference and the new height-four stamp plan.
- `RubberduckClosedPlateauPlan` produces actual height-4, clear roofs and a
  height-0, blocked one-cell foot ring. Every one of its 24 low foot cells has
  exactly one cliff actor anchor. This is an anchor/occupancy assertion, **not a
  claim that artwork covers every pixel of every blocked cell**.
- The plan rejects non-flat terrain, resources, supplied protected cells and
  actor locations in the changed footprint. Its caller must supply full actor
  footprints where applicable. It is limited to these isolated templates, not
  arbitrary generator topology, choke-point preservation or player economy.
- `NativeCliffBody.SourceGrid` is opt-in, default false. Source-grid actors sit on
  reserved height-zero feet, with source-rectangle top-left (-64,-192). Runtime
  and `CliffEditorPreview` share the same terrain-dependent clipping function.
  Legacy production anchors and the default renderer branch remain unchanged.
- `RubberduckClosedCliffGeometry` is shared by fitting and renderer depth. Front
  column drop is fitted 224->192; rear drop 96->64. Inward side pieces use two
  planes meeting on y=64+x (18) and y=192-x (22). All 256 seam-column continuity
  checks pass, but this does **not** resolve the remaining dark side contact.
- These are explicitly **fitted derivatives**, not pixel-identical original
  artwork. Source roof overlays remain unscaled, outer padding is retained, and
  opaque dark pixels are not blanket-keyed away. Per-export provenance records
  original hashes, source slots, fitting and the outstanding visual defect.
- `PlateauTerrainEditAction` and `PlateauActorEdit` apply/revert the same plan in
  the real editor. There is no engine patch and no new ordinary editor button.

## Reproduction

```
--rubberduck-closed-plateau-test SOURCE OUTPUT -1
--rubberduck-closed-plateau-test SOURCE OUTPUT 0
--rubberduck-closed-plateau-test SOURCE OUTPUT 1 editor=true
--rubberduck-closed-plateau-test SOURCE OUTPUT 3 editor=true shaded=true
```

Notch -1 is the rectangle, 0..3 the four L shapes. Output includes the map-local
fitted PNGs, `closed-height-four.oramap`, provenance and a dedicated hashed runtime
log path. No installed cliff PNG or ordinary generated map is overwritten.

## Actual-client evidence

Root: `%TEMP%/ymca-closed-height4`.

- `rectangle`, `notch-0` through `notch-3`: five height-four client captures, each
  with two successful round trips (roof and low ground) and two foot-barrier
  checks. This is **20 movement legs and 10 foot checks**, not ramp traversal.
  Each foot check examines all 24 blocked feet in all four entry directions.
- `corrected-notch-3`: runtime rerun after introducing the two-plane inward-side
  model. Four legs/two foot checks pass, but the dark seam is still visible; this
  capture is **not** evidence of a visual repair.
- `editor-normal-pass` and `editor-shaded-pass`: real editor views and six PASS
  records per session: protected roof/foot/resource rejection, placement plus
  live clipping invalidation/restoration, exact Undo, exact Redo, injected partial
  actor failure with terrain/actor rollback, and save/reload equivalence.
  Snapshots include tile/index, height, ramp, resources and sorted actor IDs/data.
  Save/reload also checks embedded fitted PNG bytes.

The screenshot may precede the completed route; completion is established by the
runtime log's physical target visit and return, not by the unit's pictured pose.
Shaded evidence here is editor testing, not a separate shaded movement matrix.

Earlier `editor`, `editor-final`, `editor-checked`, `editor-normal-final` attempts
are incomplete/failed and must not be counted as passing sessions. Fixes included
installing the probe on EditorWorld, not querying unresolved sequences on a bare
reloaded Map, and snapshotting embedded art before Map.Save replaces its package.
The enclosing first multi-map command also timed out before the editor capture;
the separately captured `*-pass` sessions are authoritative.

## Remaining integration gates

1. Finish broader visual acceptance of the reconstructed side contacts (see the
   follow-up above), without hiding collision with grass or deleting shadows. Visible blocked dirt strips also remain visible;
   foot assignment is not visual coverage proof.
2. Extend the first rear-ramp integration to other mouths and validate placement,
   buildability, resources and protected landings. The original no-ramp fixtures
   below remain isolated; `ramp=true` now tests real ascent and descent.
3. Extend planning to production components and existing editor shaping/removal,
   with full protection/connectivity checks and safe fallback policy. The existing
   87,-23 native/vertical junction has not been changed.
4. Run the wider normal/shaded movement, generated-map and editor matrix before
   enabling this vocabulary globally. No overall visual acceptance is claimed.
