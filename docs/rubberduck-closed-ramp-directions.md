# Four-direction closed-plateau ramps

Current integration: [normal-editor rollout and safer end contacts](rubberduck-closed-family-rollout.md).
The inset projection and test-only deployment described below are historical.

Follow-up: [shared contours and protected normal-editor feet](rubberduck-shared-contour-and-editor.md).

## Implemented and exercised

The shared `RubberduckClosedPlateauPlan` supports all four uphill directions.
`RampOffset` rotates cells about the unchanged rectangular roof center `(2,-2)`;
it does not rotate sprite pixels or change the exterior original-cliff selection.
The exporter and actual-editor transaction probe use that same coordinate mapping.

```
--rubberduck-closed-plateau-test SOURCE OUTPUT -1 ramp=true direction=0
--rubberduck-closed-plateau-test SOURCE OUTPUT -1 ramp=true direction=1
--rubberduck-closed-plateau-test SOURCE OUTPUT -1 ramp=true direction=2
--rubberduck-closed-plateau-test SOURCE OUTPUT -1 ramp=true direction=3
```

Directions are uphill `+X,+Y,-X,-Y`, corresponding to engine ramps `1,2,3,4`.
Omitting direction preserves the previous rear-ramp fixture. A direction without
`ramp=true`, multiple direction arguments, and values outside 0..3 are rejected.
`editor=true` and `shaded=true` may be combined with every direction.

Each plan has the same 52-cell reservation: 12 slope cells, 8 blocked high retaining
guards, 5 clear high roof cells, 21 blocked low feet and 6 low approach cells.
The utility checks rotation equivalence of every surface height, ramp and blocked
flag, as well as equality of roof/foot/slope/landing sets. Each lane also retains
the corner-height continuity checks. The small fixture is **not** a demonstration
of a buildable production base core.

## Protection corrected

The previous location-only actor check missed buildings extending into the stamp
from an anchor outside it. The planner now reserves the complete BuildingInfo
footprint, including clear bays. Editor callers can pass current actor definitions;
the probe passes `EditorActorLayer.Save()` rather than relying on stale map actors.
Non-building custom multicell occupancy and unrelated route/landing protection
still require caller-supplied protected cells.

Editor negative tests now cover **every reserved cell** separately for protected
land and resources, not just selected example cells. A temporary live refinery is
placed with its anchor outside the patch and its footprint overlapping it. Planning
must reject it; removing the temporary actor must restore the exact snapshot.

## Evidence and limits

Evidence: `%TEMP%/ymca-closed-directions`.

- `play-0` through `play-3`: actual client runs, normal tileset. Each uses two tanks
  and infantry, one per lane: six ascent/descent legs, three foot checks and three
  retaining-guard checks. Total: 24 legs, 12 foot checks, 12 guard checks.
- `editor-0` and `editor-2`: normal editor; `editor-1` and `editor-3`: shaded editor.
  Seven PASS categories each: per-cell protection/resources, live building footprint,
  stamp/live clipping, exact Undo, exact Redo, injected rollback, save/reload.
- `mouth-2`: **rejected** visual experiment. Filling an exposed terminal matte with
  the opposite original face covered only part of it; its donor silhouette does
  not span the entire join. Removed from active code; snapshots retained under
  `rejected-mouth-code`. This is not an accepted ramp-mouth repair.

### Retained diagnostic end-plane projection

The initial front mouths expose a substantial black interior between the leaning
native exterior and vertical retaining strips. The retained opt-in derivative maps
a triangle of the opposite original face into this existing end plane. Its target
vertices are (0,64), (0,224), (64,256), mirrored for the other side. Barycentric donor
sampling is inset 15 percent to avoid sampling the source's transparent bevel.
Only the connected opaque near-black interior is eligible; all alpha and all
non-connected original RGB remain unchanged and are asserted. The original source
files, existing terrain and collision are unchanged. This is **projected derived
art, not a verified untouched authored ramp template**.

- `projected-mouth-2`: intermediate projection without inset; residual gaps remained.
- `closed-end-2`, `closed-end-3`: actual-client views of the retained inset projection,
  with six movement legs, three foot checks and three guard checks each.
- `end-editor-2`, `end-editor-3`: shaded-editor reruns using these derivatives,
  including the seven transaction/protection categories and embedded-art reload.

The large black patch is filled, but texture discontinuities and thin edge contacts
remain visible. These captures do **not** establish final visual acceptance. The
rear mouths also retain unfinished terminal geometry. No collision was relaxed or
grass painted over blocked ground to conceal it.

Consequently the closed-family path remains opt-in. Ordinary production generator
and editor brushes are **not switched**. Complete mouth geometry, general production
contours, consistent native/fallback joins and broad gameplay/visual acceptance
remain unfinished. This work must not be described as completing all terrain work.
