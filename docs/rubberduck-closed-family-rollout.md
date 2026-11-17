# Closed-family integration: ordinary editor and conservative component selection

## Live integration

The normal **Plateau + four ramps** editor button now creates the installed
closed-cliff family, not map-local calibration actors. Its 15x15 elevated footprint
retains its four three-wide, four-level ramps. It explicitly reserves 52 additional
blocked low-foot cells inside the already required ten-cell clear edit area. Ramp
approaches stay open. This is a documented footprint change for **new stamps only**.

The installed vocabulary has 22 sprites/rules/sequences, including rear and inward
orientations and the two front-mouth derivatives. The two added manifest entries
leave existing mod configuration intact. Original source files and legacy native
art are not replaced.

Normal removal/lowering recognizes the new actors. Stamp deletion distinguishes
new closed-family stamps from old stamps and checks the appropriate terrain before
acting; saved legacy stamps are not silently assigned a new footprint. Normal
shaping/replanning recognizes both actor families. It retains existing foot
reservations and releases only unprotected feet no longer adjacent to elevated
terrain; a small edit no longer discards a valid complete ring. Straight variants
are keyed to world coordinates so moving a component's local origin does not
reshuffle unchanged walls. Terrain and actor edits still
use the same atomic action and rollback implementation.

`RubberduckClosedCliffSelection`, called by the shared export/editor composer,
selects an entire compatible connected component or leaves it on the old renderer.
It requires exact full-height boundaries, supported corners, clear downhill ramp
openings, and existing blocked, flat, resource-free low-foot anchors. It does not
reserve new cells during rendering or paint grass over blocked terrain. Internal
retaining strips remain the established derived ramp vocabulary.

## Generator limit at this stage (superseded)

The [topology-first generator follow-up](rubberduck-closed-generator-planning.md)
now reserves compatible footprints before economy and demonstrates 436 closed
actors on the four homes of an actual production map (736 across eight filled
plateaus in the separate seed-43 export). The zero-selection results below
are the earlier rollout baseline, not the current Mountain Valleys result.

The generator export path now contains the same automatic selector. **The nine
4-player seed-42 regression exports selected zero closed-family components.** Their
existing irregular/mixed-height contours and incomplete reserved rings correctly
remain on the old vocabulary. These runs prove compatibility, not completion of
procedural-map conversion. Whole-map procedural rollout still requires compatible
footprint planning without violating collision, economy or protected approaches.

## Front-mouth contact correction

The former 15-percent inset distorted the donor rock strata and left an opaque
black outline. The current derivative retains affine end-plane coordinates and
searches for a nearest opaque donor pixel along the same source row only when the
projected relief pixel is transparent (bounded vertical fallback for empty rows).
It reconstructs the one-pixel opaque near-black matte fringe adjoining the known
connected matte. This is an explicit derived AA treatment, not untouched original
RGBA. Source alpha, translucent authored shadows, and RGB outside that narrow mask
are asserted unchanged. No gameplay surface or collision changes for this fix.

Actual client captures show the large black end region and the conspicuous outlined
panel replaced by a substantially cleaner rock contact. This is not certification
of all zoom/filtering settings or all arbitrary production junctions.

## Reproducible assets

```
--rubberduck-closed-cliff-art SOURCE OUTPUT
```

The command retains its five shape recipes and two mouth recipes, checks identical
slot output across recipes, exports PNGs, rules, sequences and source/output hashes.
It refuses to write into the immutable source tree. Installed files live under
`mods/ca/bits/terrain/rubberduck/derived/closed-cliffs/`; provenance is adjacent.

## Verification

Evidence: `%TEMP%/ymca-closed-rollout`.

- `editor-pass`: nine ordinary-editor PASS records including placement, preview
  invalidation, exact Undo/Redo, protection, deletion/restoration and save/reload.
- `editor-shaded`: the same ordinary-editor checks in the shaded tileset.
- `final-editor-normal`, `final-editor-shaded`: final installed art and ten PASS
  records each, including a separate legacy-stamp deletion/restoration regression.
- `shape`: first grow/replan run, which exposed unnecessarily discarded reservations
  and reverted to the older family. Superseded by `shape-preserved`: five actual
  editor PASS records, a four-cell patch instead of 27, **54 closed actors and zero
  legacy actors** after the extension. Undo/Redo and save/reload remain exact.
  At this stage unsupported edited contours fell back. The generator follow-up
  now rejects edits that would discard an existing closed family before committing,
  rather than silently downgrading that component.
- `play`: the actual editor-saved terrain, not regenerated terrain. Each run drove
  infantry and a tank through all four ramps: 16 ascent/descent legs and eight
  blocked-foot checks. `final.png` uses the updated installed mouth art. The log
  contains two runs; do not confuse the doubled records with extra unique routes.
- `contact`: map-local close view of the corrected front-mouth projection.
- `matrix`: all nine presets, 4 players, seed 42; existing gameplay validation.
- Headless contour tests additionally check installed-family selection on a complete
  four-ramp footprint, rejection when one foot is unreserved, and normal lowering
  of every new foot and actor.

`editor` is the initial failed probe: it looked for a fallback wall replaced by the
new family. The probe was corrected to check the actual closed-family corner; use
`editor-pass`, not that failed run.

Bridges remain separate pending the supplied sprites. Broader procedural conversion,
production native/fallback contacts and visual/performance acceptance remain open;
this document does not claim all terrain work is finished.
