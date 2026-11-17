# Soviet SAM — Blender reconstruction and native isometric sprites

## Current status

Passes 01–12 are archived. Pass12 is installed locally on the existing `SAM`
actor in **Game / modular**, by explicit user request. No commit, push or release.
The source PNG and earlier passes are preserved. The model is reconstructed from
one view, not an exact recovery of the artist's original mesh. Rear/underside
mechanisms are inferred. No runtime change to Nod SAM or the separate flak actors.

Latest review: [source comparison](pass-12/review/comparison-2x.png),
[all directions](pass-12/review/all32-native.png),
[rotation](pass-12/review/rotation.gif), [parts](pass-12/review/parts-2x.png).
The source is 256px; the review explicitly shows the smaller native model render,
not an enlargement pretending to have the source's full pixel detail.

## Latest follow-up: 75% size, 1×1 building/bib

At user request the SAM base, launcher, placement and damaged-state sequences now
use **Scale0.75**. Original PNGs and Blender geometry are unchanged. Sequence offsets
remain unchanged because the engine scales sprite offsets together with the art.
Turret attachment is now `(0,0,726)`, muzzle `(496,-291,953)`, selection height99.
Explicit footprint `x`, dimensions1×1, ±512 hit rectangle and Soviet1×1 bib are bound.
Russia/Iraq vs NK faction assignment and all weapon stats remain unchanged. Flak
resolved traits and its sprite/muzzle check remain unchanged. Engine muzzle error
is0.123px across32 facings. Build and relevant regression tests pass.

[75%/1×1 layout preview](integration-pass12/preview-75pct-1x1.png) is a composed
nearest-neighbor layout check, not a live screenshot. Live match review remains
pending. The original Scale1 integration measurements below are historical.

## Follow-up: faction assignment and test-map display

User explicitly assigned Soviet SAM to **Russia and Iraq**, and sprite flak to
**North Korea**. SAM now requires Soviet structures and excludes NK structures;
`FLAK.SOVIET.SPRITE` is buildable in the Defense queue with radar + NK structures.
The voxel study remains unbuildable. Bot AA lists and build weights include flak.
Weapon statistics/costs remain unchanged; the earlier gameplay-preservation notes
below describe the art integration before this separately requested faction split.

Both disposable building-test generators now show **both** actors: existing flak
`Actor37` at `(148,-56)` and `SovietSamDisplay: sam` at `(154,-62)`, with a fifth
unarmed airborne target. The normal `test-gebaeude.cmd` path generates this layout.
`testmap1-debug.oramap` and `soviet-flak-voxel-test.oramap` were regenerated and their
actors checked. Original `testmap1.oramap` remains byte-identical. No running match
was restarted; start the building test again to see the added SAM.

## What a pass means

A pass is an archived modeling/material correction followed by native rendering
and visual review against the source and other directions. It is **not** a Blender
render layer (normal/depth/shadow), nor merely another export. A pass count is not
proof of resemblance. Pass12 is the requested integration milestone, not a claim
that invisible surfaces are known or every source pixel has been recovered.

### Reviewed changes in this continuation

- **07:** restored foundation/rack proportions, longer outriggers, service terminal,
  conduits and 26px/model-unit review density. Review still found unreadable red
  inserts and overly dark broad sides.
- **08:** corrected red material shading (the dark ramp had mapped inserts to gray),
  lifted dark steel surfaces, added bounded flank panel rims, vents and round ports.
- **09:** replaced the thin-looking pedestal foot with a stepped service drum,
  reinforced brace sleeves, drum vents/slats and front keypad console.
- **10:** broke up the large plain roofs with raised armor and seams; added launch
  cover bevels, rear service closures/locking bars and mast panel trim.
- **11:** moved upper warning junctions toward the launch face, added continuous
  slab-edge warning tabs, cover tonal feet/hinges and concentric elevation bosses.
- **12:** added the underslung warning apron, rear harness/collars and mast equipment
  housings. Registered the yaw grid exactly for engine facing export. Rendered and
  inspected the complete rotation, including side/rear/end-on views.

Passes 07–11 have eight-direction reviews; pass12 has all32. Earlier pass directories
remain as supplied from the preceding work. No later filenames overwrite them.

## Reproduction (from Game)

Requires Blender **3.6**, Python3, Pillow and NumPy. The Blender script uses the
3.6 EEVEE API. Use a background process; do not terminate an open Blender/game.

```powershell
& 'C:\Program Files\Blender Foundation\Blender 3.6\blender.exe' -b -t 4 --python tools/art/model_soviet_sam.py -- --pass 12 --all
python tools/art/review_soviet_sam.py --pass 12
python tools/art/integrate_soviet_sam.py
```

Re-running a pass overwrites that pass's generated files; use a new pass number
for new art revisions. Integration deliberately selects pass12, not an arbitrary
latest folder.

### Geometry and materials

`tools/art/model_soviet_sam.py` constructs actual mesh geometry: bevelled octagonal
canister sections, inset launch faces, hollow port rims, stepped cylindrical
bearings, yoke/clevises, rams, braces, panels, rails, fasteners and cables. The fixed
foundation and mast are separate from the hardware parented to `turret_pivot`.
The pivot is at model Z1.9; canister elevation is34 degrees.

The source PNG is packed into the `.blend` for provenance. Material colors are
sampled from the bounded launcher/pedestal region and mapped to steel, white,
yellow, red and concrete ramps. **This is source-palette material reconstruction,
not a UV photo projection.** No scenery, trees or foreign image patches are pasted
onto hidden faces. Materials use emission colors selected from polygon orientation
against a fixed light direction, with deliberate tonal offsets for recesses,
bevels and inserts. Mechanical surface details are modeled, not random RGB noise.

Rendering: orthographic camera `(7,-7,sqrt(98)/sqrt(3))` aimed at the origin,
30-degree elevation, transparent RGBA, Standard view transform, no dithering,
EEVEE one sample/filter0.01. Native frame192×192, density26px/unit, common ground
pixel `(96,146)`. No resizing is performed for runtime assets. Nearest-neighbor
magnification is only used in labeled review images.

`review_soviet_sam.py` quantizes to the sampled palette and uses a second surface-ID
render to assign single-owner interior contours. Tiny screws, red/yellow inserts
and lamps are protected from contour erasure. It checks alpha bounds, exports the
fixed base separately and produces native assemblies, comparison boards and GIF.
`geometry.json` records palette/source SHA256/parts/facings; `validation.json`
records image bounds. These technical checks do not replace visual review.

## Runtime integration

`tools/art/integrate_soviet_sam.py` copies the base and reorders the turret frames
without resampling. Engine direction0 uses Blender225 degrees: source frame20,
then21…31,0…19. Files:

- `mods/ca/bits/soviet/buildings/sam-base.png`
- `mods/ca/bits/soviet/buildings/sam-turret32.png`
- `mods/ca/bits/soviet/buildings/sam-place.png`
- `mods/ca/sequences/soviet-sam.yaml` → `sam.soviet.native`

The existing `SAM` actor now uses `WithSpriteBody` + `WithSpriteTurret` instead of
the embedded turret body. Both are Scale1. Base offset `(0,-50)`, turret sequence
offset `(0,-7)`, turret attachment `(0,0,968)`, CameraPitch85,32 facings. The muzzle
is registered at the upper launch cover: `LocalOffset: 661,-388,1271`.
An omnidirectional inherited-engine muzzle effect avoids legacy directional
misregistration. Selection decoration height132 clears the complete rotation.
Damaged states reuse the new art (no dedicated damage artwork yet); placement and
existing ten-frame construction are resolved. The turret is hidden while building.

Nike weapon values, reload, range, damage, cost, power, detection, prerequisites,
footprint, hit shape and Soviet bib are unchanged. This is not the Nod four-shot
ammo mechanic. Derived flak actors explicitly retain their former image/muzzle
and render traits; resolved before/after snapshots verify they are unchanged.

## Validation and remaining live check

- Release configuration build completed.
- `--check-soviet-sam-art`: actual PNG frame loading, engine-resolved sprite paths,
  damaged/placement/construction dependencies and all32 muzzle projections;
  maximum projection error0.131px.
- New `tools/test_soviet_sam_installation.py`: source hashes, all archived passes,
  native pixel/frame ordering, resolved paths and gameplay/isolation checks.
- Soviet building, Soviet flak and Nod SAM regression checks pass. Historical
  gameplay baselines are retained; only the explicit approved SAM rendering and
  muzzle/selection registration changes are normalized in older tests.

**A live match/editor visual check has not been performed.** Start a fresh game or
building test to load the local changes; an already running match retains its old
rules. Check placement, turret turning/target switching, firing, construction and
health-bar placement there. No running game or Blender process was stopped.
Snapshots and numeric registration are in `integration-pass12/`.
