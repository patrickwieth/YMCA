# Nod SAM site — extracted fixed base + modeled rotating launcher

**Pass12 is artistically approved by the user**, following the requested minimum
ten passes. Five visibility-only ammo states (0–4 missiles), each with32 directions,
are now exported under `ammo-variants/`. The approved four-missile pixels and all
model geometry are preserved. **Now installed on NSAM**, including ammo-dependent
rendering and the requested4/10/30 burst. See `integration/README.md`; live firing
review is still pending.
No existing Nod/Soviet SAM, weapon, runtime sequence or supplied image was changed.

## Open first

- `preview-2x.png`: supplied reference, matching model angle, inferred opposite view.
- `pass-12/review/eight-2x.png`: side/rear/end-on review.
- `pass-12/review/all32-native.png`: complete native rotation.
- `rotation.gif`: fixed base, rotating turret.
- `pass-12/review/parts-2x.png`: the two independent parts.
- `pass-12/nod-sam.blend`: editable actual3D turret, packed material resources,
  fixed camera and a non-rendered radar depth guide. Base pixels are composited
  separately by the export script, not reconstructed as a new building mesh.

## Separation and source preservation

The supplied `sam_site-1179307279-0.png` is unchanged. No scenery, trucks, flags,
tracks or unrelated buildings are exported. The attached fixed roof radar and
service cabinet remain in the extracted foundation layer. The complete fixed
base is one PNG; the rotating turret is one32-facing sheet.

`source-study/preserved-mask.png` identifies11,151 retained source pixels, checked
byte-for-byte against the original. `inferred-mask.png` separately identifies the
1,921 hidden/repaired roof/bearing pixels: these were obscured by the old launcher
and are NOT claimed to be extracted original art. No visible facade recoloring,
resizing or projection of the whole source image onto geometry was performed.

The final native canvas is256×256 at20px/model-unit. The fixed mounting pivot is
source pixel119,115; ground reference133,181. The32-facing export starts with model
angle45 degrees (source facing index4), matching the existing +Y study convention.
Those are export coordinates, not a claim of verified engine actor integration.

## Actual3D construction

Four separate missiles with shaped red ogives, cylindrical stepped jackets,
exhaust recesses and four tail fins each; separate launch rails, saddles and
crossmembers; open paired red-armored yokes; trunnions, hydraulic cylinders,
cables, gearbox, ventilation, fasteners and the cream identification strokes.
Materials use a bounded launcher-region source palette, fixed world lighting,
polygon-normal tonal steps and authored local machining fields. No arbitrary
RGB noise or photographed scenery is projected over the model.

Only the turret rotates. The base image and camera remain fixed. Hidden rear and
underside mechanisms, staggered rail layout and radar depth are inferred from a
single view. The final depth reconstruction centers the rack over the bearing
without changing the missiles' reference-angle projection. This also avoids the
low sideways overhang seen in the earlier rotations.

## Iteration / visual-review record

| Pass | Main change and review outcome |
| --- | --- |
|01|Native camera, four missiles, separate race and support; basic support too solid.|
|02|Curved segmented ogives instead of linear cones; preserved four-tip silhouette.|
|03|Retaining bands, jacket seams, darker body tones and owned contours.|
|04|Actual tail fins and recessed exhausts; rear no longer plain capped cylinders.|
|05|Split launch rails, saddles, fasteners and connecting members.|
|06|Opened support frames, recessed red panels and identification strokes.|
|07|Trunnion drums/rims/bolts and elevation hydraulics; inspected end-on views.|
|08|Harnesses, foot anchors and rear service-box ventilation.|
|09|Broadened narrow support/race, protected narrow paint from over-dark outlines, authored local coating; full32-facing review.|
|10|Rear drive, screws, plate fasteners, native contour highlights and radar depth guide; full32-facing review exposed excess bare plate and problematic overlap.|
|11|Reduced bare-plate glare, thinner jackets, gearbox/seams and revised depth study. Full rotation exposed lateral rack overhang hidden in the original view.|
|12|Reconstructed rack depth to center it in3D while preserving the source view; raised/reconnected corresponding supports. Full32/native/side/end-on review.|

Passes01–08 retain eight native directions each;09–12 retain all32. Every pass has
its own blend, raw renders, surface IDs, geometry report and finished comparisons.
Pass counts and successful exports are NOT proof of artistic approval.

## Repeat / checks

```sh
python -B tools/art/prepare_nod_sam_base.py
blender --background --python tools/art/model_nod_sam.py -- --pass 12 --all
python -B tools/art/review_nod_sam.py --pass 12
python -B -m unittest discover -s tools -p test_nod_sam_study.py
```

Checks cover original/base preservation, explicit inferred-mask separation,
archived distinct passes, four missiles, native palette/binary alpha, no frame
clipping, correct sheet ordering, fixed-base composition and balanced final rack.
They do not establish artistic approval, engine alignment or live rendering.

Pass12 approved; all five ammo states are now bound to NSAM. Runtime build and
sprite/condition/muzzle checks pass; no live firing review claimed. No commit/push/release.
