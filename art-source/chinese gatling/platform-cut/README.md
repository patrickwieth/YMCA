# Supplied flat mounting platform — native cut/paste, installed

## Latest: approved platform, hazard band removed

The user approved pass03 except for the black/yellow band. Current runtime is pass04:
only that perimeter strip is removed and existing wall pieces slide inward/up.
Visible width120 instead of128; unchanged128x112 canvas, Scale1, mount and gun.
No new/recolored/resampled pixels. `pass-04/source-pixel-origins.npy` records every
visible output pixel's exact pass03 coordinate. The bearing remains byte-identical.
See pass-04/cut-review-4x.png and all32-native.png; prior runtime files are preserved
in integration-pass04/before-*. Reproduce with trim_chinese_gatling_platform.py,
then install_chinese_gatling_platform.py. Existing1x1 bib remains unchanged.

## Preserved pass03 construction history

Source: `../mount_base_flat_plat...-1843695069-0.png` (128x128 RGBA, opaque background).
Original and earlier cuts preserved. The previous red/tan plinth is archived under
`integration/before-*`; the original pass03 Blender model also remains unchanged.

Final: `pass-03/platform.png`,128x112 canvas,128px maximum visible width. It is used
at Scale1, NOT shrunk like the turret. Terrain, rubble, background, cropped staircase
and grass visible through the original ladder were removed. Rear/front aprons move
inward; wall strips shorten the foundation; complete hazard-strip runs close the
formerly cropped side facets. The machined bearing is copied intact. Only native
source pixels and integer cut/paste/90-degree strip rotations are used, no blur,
rescaling or generated texture. Side-wall extensions are reconstructed source cuts.

`pass-03/cut-review-3x.png`, `all32-native.png`, `rotation-native.gif` were inspected.
These are native composed previews, not live screenshots. Earlier versions had
missing side edging and grass remnants; those were corrected before installation.

Registration: the supplied mount point64,39 maps exactly onto existing turret mount
0,0,604. Body Center offset0,-9.696132596685084 at Scale1. The original320x320 turret
frames, Scale0.65, offset0,-34, muzzle offsets623,+/-257,834 and all eight weapon
profiles are byte-/value-preserved. Decoration bounds grow slightly to1504 to contain
the new native platform; occupied1x1 footprint, selector class and height79 stay.
The existing Soviet1x1 decorative bib remains underneath; no grass/background is
part of the new platform PNG. User requested source-ground removal, not bib removal.

`install_chinese_gatling_platform.py` promotes the cut and updates the normal binding
pipeline. The historical pass03 exporter now refuses to overwrite the supplied
platform. `test_chinese_gatling_art.py` checks source pixels, native base, unchanged
atlas/mount, selection, normal/damaged bindings and gameplay. Engine checker verifies
actual sprite loads plus the unchanged muzzle projections. Live firing still needs
review after restart. No commit, push or release.
