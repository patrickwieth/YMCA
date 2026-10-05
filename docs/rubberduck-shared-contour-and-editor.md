# Shared contour planning and protected editor feet

Follow-up: [installed closed family and normal-editor rollout](rubberduck-closed-family-rollout.md).

Bridges remain a separate future task pending the user's sprites. They are not a
dependency for this terrain work, and no substitute bridge art was added.

## Normal editor bug fixed

`PlateauShapePlan` used to clear every old low cliff foot before asking the native
planner to rebuild the apron. Some of those feet belong to the protected area
around a ramp. Even a distant edit could therefore attempt to clear protected
collision and be rejected. This was reproduced against a freshly generated
Mountain Valleys 4-player seed-42 map: no qualifying native extension was found.

The editor now carries protected old feet into the replan unchanged.
`RubberduckNativeCliffPlanner` recognizes these as existing reservations:

- they support native faces and convex closure;
- they are not re-added to the terrain dictionary;
- rollback of new reservations does not clear existing reservations;
- convex apron completion is driven by actual flat height-four roof cells, not
  by the retained low-foot cells themselves.

The same generated map now supports the native extension at **67,-16**, with a
32-cell patch and five native pieces after replanning. Existing protection and
connectivity checks remain in force; neither was weakened to make the test pass.

The regression fixture finder also skips valid fallback-only edits and prioritizes
native boundaries instead of choosing the first unrelated supported edit.

## Shared production/editor composition

`RubberduckPlateauRenderer.ComposeNative` is now the single normal composition
entry point used by `RubberduckMapExporter` and `PlateauShapePlan`. It keeps fallback
face suppression and native actor emission together. Existing actor ordering,
IDs, terrain values and art vocabulary are preserved. This is **not** activation
of the opt-in SourceGrid family in ordinary production maps.

## General closed contour primitive

`RubberduckClosedCliffContour` removes the five-fixture/20-edge restriction from
the closed-family boundary algorithm. It accepts a connected roof-cell set and
validates exact projected edges, corner vertices and one unique anchor per reserved
foot cell. Empty/disconnected roofs, opposing faces, pinched/branched corners,
duplicate or displaced anchors and incomplete closures are rejected.

`RubberduckOriginalClosedReference` delegates to it and retains its original five
shapes and negative closure checks. This enables general planning without silently
accepting geometry for which the source vocabulary has no safe composition. It is
not yet a global renderer switch or proof of arbitrary-shape visual quality.

## Saving diagnostic editor maps

`PlateauEditorProbe.SaveTerrainCopy` centralizes the previously established copy-save
pattern. Closed, ordinary stamp and generated-shape probes now avoid replacing the
live map package with an output package that will be disposed. A post-save package
lookup checks that lazy reads remain possible. This helper is scoped to the
terrain/actor snapshot probes, not a replacement for the general editor save UI.

## Verification

```
--rubberduck-closed-contour-test
```

- 180 deterministic rectangular/L contours across dimensions and translations.
- 543 corrupt/unsupported contour rejections; five original calibration fixtures.
- 52 existing protected feet preserved across native replanning.
- Normal export/editor composition matches the former implementation's actor YAML
  and terrain, including IDs/order and non-mutating editor planning.
- All nine presets exported with 4 players and seed 42 through their existing
  gameplay validators. This is not a nine-preset client visual matrix.
- Mountain Valleys `map.bin` and `map.yaml` are byte-identical before/after the
  protected-foot planner change.
- Actual generated-map editor: non-mutating draft, extension, exact Undo, exact
  Redo and save/reload passed; final screenshot inspected.

Evidence: `%TEMP%/ymca-closed-shared/{matrix,editor-final}`. The earlier failed
fixture searches were the red reproduction, not successful editor sessions.

## Still open

The opt-in front-mouth projections still have visible texture contacts; the
complete closed-family art has not replaced the normal production vocabulary.
Production native/fallback junctions, wider visual acceptance and rollout are not
completed by this change. Bridges await separately supplied sprites.
