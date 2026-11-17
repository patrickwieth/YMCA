# Allied quad flak — initial modeled study, NOT finished

The supplied PNG is preserved unchanged. The source SHA256 and camera/pivot data
are recorded in each pass's `geometry.json`.

Actual source: `quad_cannon_flak_def...-133903681-0.png`.
Builder: `tools/art/model_allied_flak.py` (background Blender 3.6).

## Current hybrid study: pass15

User liked14 and requested the thick loading ends project farther behind the
cradle. Pass15 moves the barrel/breech group0.28 model units backward along its
existing axis and extends only the rear of the thick breech cylinders another0.32,
with their rear caps seated on the extended ends. Front barrel length and diameters
are preserved relative to14; the barrels simply sit farther aft.

All32 facings rendered and inspected, including exposed rear/end-on loading ends.
`rear-breech-check.json` verifies exactly the intended vertex displacements;
all other meshes/transforms and original-base PNG bytes are unchanged.
Three current hybrid export tests pass. User subsequently approved installation:
pass15 is now installed on AGUN, with source pixels preserved, independent parts,
and calibrated physical muzzle offsets. See `integration-pass15/README.md`.
Live firing/appearance remains unobserved. Art review:
`pass-15/review/eight-views-2x.png`.

## Previous hybrid study: pass14

After accepting the general turret form, the user requested shorter barrels, rear
ends seated farther into the suspension, and the original pedestal instead of a
modeled substitute. Pass14 shortens the forward barrel sections20%, seats them0.16
model units deeper, extends the rear sleeves0.32 units and moves the associated
breech/return hardware back. Other turret geometry remains as in13.

The fixed sprite uses the supplied original's visible pedestal pixels, unscaled and
unrecolored. The original composite hides some of the top behind its old turret;
that small occluded top region is filled with adjacent source metal, explicitly
recorded in `original-base.json`. The supplied original PNG is untouched.
32 turret facings composite over this base. `test_allied_flak_original_base.py`
checks original visible pixels and fixed-base composition. Historical full-3D
assembly controls still describe pass13, not this sprite/model hybrid. No actor
integration. See `pass-14/review/eight-views-2x.png`.

## Previous reconstruction: pass13

The previous pass07 was explicitly rejected for insufficient source detail. It has
now been substantially reworked rather than offered for the same feedback again:

- Pass08: broad pedestal stiles/feet, service hatches, plinth anchors and faded marks;
  independent circular breeches, locking collars, return sleeves/rods, cover screws
  and elevation drive parts. Surface IDs now use two channels, avoiding ID wrapping.
- Pass09: removed old boxes that were burying the new breeches, lowered the supporting
  core, added bounded paint scuffs and coherent tonal shading. Interior contours are
  steel-dark rather than indiscriminately black.
- Pass10: the rendered base was over-segmented; removed redundant narrow ribs and
  restored broader armor faces with sparse hatches. Reference-only white-background
  isolation makes comparisons fair; the supplied original is untouched.
- Pass11: reference landmark inspection showed the gun cluster too low relative to
  its axle. Raised the gun cluster0.32 model units and added connected saddle struts;
  fixed base, pivot and axle remain unchanged. This is an art-study muzzle change,
  not any live weapon/gameplay modification.
- Pass12-13: exact layer-control checks revealed a few unstable barrel pixels. Ray
  inspection found overlapping same-radius jacket/muzzle surfaces and almost
  coincident inner jacket/barrel walls. Separated the joints and embedded the inner
  sleeve walls safely; all five raw assembly/layer controls now match exactly.

Current32 native facings were rendered and inspected, including reference, side,
rear/end-on and the full rotation sheet. Geometry audit checks outward closed meshes,
four real bores, fixed/moving parentage and one-cell ground extent. Source palette,
alpha, sheet order and no-clipping checks pass. These technical results and the pass
number are NOT artistic approval. No runtime integration or live firing test.
[Current internal rotation sheet](pass-13/review/all-32-native.png).

## Historical reconstruction: pass07

Pass03 rebuilt the beveled plinth, paneled pedestal, blue flange, dogleg yokes,
independent breeches, copper service lines and four stepped hollow barrels.
Pass04 added geometry-owned turret contours and removed only border-connected
near-white background pixels from the packed working texture, not the original.
Pass05 extended contour IDs to the fixed base and beveled the breech housings;
review found reversed bevel normals, corrected in pass06. Pass07 adds raised yoke
edging, heel fasteners and separate breech cooling insets. All 32 native facings
were rendered and visually inspected, including end-on and reverse views.
[Current rotation sheet](pass-07/review/all-32-native.png).

The source is fully opaque, not alpha-masked. No white-background material is
projected onto the mechanism. The pennant is a bounded source cut; mechanical
surface colors come from the source palette. Surface wear still merits further
refinement; this is not user approval, an engine-loader test or game integration.
Current Python regressions verify native export, stationarity and five exact raw
base+turret/full-3D composites. Original artwork and map remain unchanged.

## Historical state: pass-02

- A real `.blend` model with separate `STATIONARY BASE` and `ROTATING TURRET`.
- Four hollow tubes in a 2x2 cross-section, all physically elevated 40 degrees,
  individual collars/receivers, paired cradle/trunnions, feed boxes, support links,
  sight flag/mast and a fixed armored pedestal/plate.
- Bounded source texture cuts for the pedestal vents and blue pennant. Other
  surfaces use source-palette directional shading, not full-image projection.
- Native 256x256 padded frames at 20px/model-unit; no resized render/runtime scale.
- Pass01 eight-view review exposed buried pedestal texture plates. Pass02 moves
  them to the octagonal face centers and adds axle caps, rear panels and roof ribs.
- 32 directly rendered directions in pass02. The structure is still too plain,
  the texture coverage/detail density insufficient and overlapping barrels need
  better visual separation. This is an initial model, not an approved final asset.

[Eight-view working review](pass-02/review/eight-views-2x.png)
[Source comparison](pass-02/review/source-comparison-2x.png)
[Rotation](pass-02/review/rotation.gif)
Model: `pass-02/allied-flak-model.blend`.

The preview does not borrow the GDI bib. No actor, weapon, balance, production or
map placement has been added. Source-hidden faces are inferred. Earlier pass01
remains unchanged.

`test_native_turret_working_studies.py` validates the source hash recorded at render,
source-only palette/binary alpha, native frame metadata/order, no clipping,
stationary-base control and five full-3D versus separate-layer raw composites.
This is not a live GPU/firing test or a new engine-loader validation for the draft.

```sh
blender.exe --background --python tools/art/model_allied_flak.py -- --pass 2 --all
python tools/art/review_gdi_small_turrets.py --kind allies --pass 2
python -B -m unittest discover -s tools -p test_native_turret_working_studies.py
```
