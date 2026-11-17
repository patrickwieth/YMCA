# Sentry V13 — paired black/light edges and restrained Gatling tones

User feedback: the original places a one-pixel lighter line immediately inside
black edges; V12's Gatlings use overly broad, extreme dark/light bands even if
those colors individually occur in the original.

Review:
- `comparison-3x.png`: reference / V12 / final V13.
- `weapon-detail-6x.png`: the same comparison focused on the guns, native relative
  pixel scale enlarged nearest-neighbour (no separate resizing to force a match).
- `base-assembly-preview-3x.png`, `rotation-check-2x.png`.
- Final output: `final-turret-000.png`, `045`, `180`, `315`.

## Changes

**Plate edges:** optional paired-highlight finishing mode. A one-native-pixel,
source-gray highlight is placed just inside selected upper/left black plate
boundaries. It advances one restrained gray step, not automatically to the
brightest available gray. Existing bright bevels are not broadened; red paint,
emblems, rivets and thin gun parts are excluded from this plate operation.

**Gatlings:** weapon-only material copies use original middle grays (42/38/39,
65/63/65 and 85/81/82), lower directional contrast and no broad glossy contribution.
Body materials, lights, camera, geometry and texture image data remain unchanged.
The first test removed too much weapon contrast and looked dull. The final version
keeps thin boundaries of the preceding black bands and a single upper line of its
bright strips. Broad band interiors retain the calmer middle-gray metal. Small
ink-adjacent accents use the original 121/113/105; most narrow glints use 101/96/92.
True bore material is retained, rather than lightening every dark pixel uniformly.

The controlled finishing step uses V12's SAME-GEOMETRY image as a tone-location
reference, with an exact-alpha assertion. It does not invent noise or new artwork.
For additional angles, matching control renders would be needed; this is still
only a four-view study, not a ready-made full rotation export.

## Actual area check

`weapon-tone-area-report.json` compares the same 1942 weapon pixels in the main
view: deep-black pixels decrease from 707 to 426; very bright pixels (max RGB >=110)
from 186 to 13. Other narrower highlights remain in medium source grays. This
measures our change in distribution, NOT a numerical match to the reference.
`edge-finish-report.json` adds 180 paired highlight pixels in the main view.

All four final views: original colors only, binary alpha, identical V12 silhouette,
no clipping, unchanged 200px canvas/pivot/world-pixel density. Geometry hashes match.
The V11 finisher's original defaults were regression-tested in a temporary directory:
all four outputs are byte-identical to the prior V11 PNGs. Earlier attempts untouched.

## Reproduce from Game

```text
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v13.py
python tools/art/compare_sentry_v12.py --attempt attempt-13 --previous-attempt attempt-12 --previous-prefix final-turret- --previous-label V12 --label "V13 / paired edges" --expect-same-silhouette
"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe" --background --python tools/art/render_sentry_v11_edges.py -- --input-attempt attempt-13 --output-attempt attempt-13/edges --blend-file sentry-v13.blend --output-blend sentry-v13-edge-study.blend
python tools/art/finish_sentry_v11_edges.py --input-attempt attempt-13 --output-attempt attempt-13/edges --before-label "V13 before edges" --after-label "V13 paired edges" --paired-highlights --weapon-tone-reference-attempt attempt-12
python tools/art/compare_sentry_v12.py --attempt attempt-13 --previous-attempt attempt-12 --previous-prefix final-turret- --previous-label V12 --label "V13 / paired edges" --expect-same-silhouette --final
```

`sentry-v13.blend` stores the material pass. Pixel-width ink/highlights/tone-band
finishing are separate sprite processing; a plain Blender render is intermediate.
No source-asset edits, GDI/gameplay changes, commit, push or deployment in this pass.
