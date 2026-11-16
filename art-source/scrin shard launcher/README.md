# Scrin Shard Launcher — source reconstruction and1×1 replacement

## Latest surface-density follow-up: pass43 — review only

User clarified that the main deficiency is large empty uniform surfaces, whereas
source veins and small organic/mechanical details cover the armor. Studies39–43
address the interiors of those surfaces, not merely their shading or outer edges.

- Connected embedded vascular branches, return loops, dark channels, small junctions
  and recessed gills now occupy the receiver, cervical carapace, shin/calf shells,
  battery housing and accelerator side/underside surfaces.
- Broad rear/end caps have real planar UV charts instead of collapsing onto one
  texture column. Open accelerator cradles have a proper non-repeating surface unwrap.
- A single shared vascular field crosses the five receiver plates; the first overly
  regular/repeated small patterns were rejected rather than presented as the result.
- Main crystal treatment remains38's; no model vertices, topology, silhouettes,
  dimensions, muzzle, pivot or gameplay changed. Hidden surface decoration is inferred.

Review: `pass-43/review/material-before-after-source.png` shows original /38 /43.
`material-rotation-native.gif` compares38 and43 at intended scale. Source-facing,
side, rear, end-on and full-turn native renders were inspected. These are compositions,
not live screenshots. No claim of exact source reproduction or user approval.

`paint_scrin_surface_networks.py` authors the interior material fields at native
surface density without random noise or foreign image stamps. Separate EEVEE draw
passes showed a few unstable nearest-texel colors;43 exports each full native pose
and takes turret RGB from that pose using independently rendered/verified holdout
alpha. Independent renders are retained in `independent-parts/`. Fixed colors are
checked stable over every full pose; no alpha or reference-image pixels are pasted
in to conceal geometry. `native-part-color-audit.json` records the actual method.

Saved geometry equals installed30; raw alpha equals30 and all32 native assemblies
match their exported base/turret composition. Material checks also verify broad
surface fields and that38's crystal pixels stay unchanged. Runtime PNGs remain30.
The game was not stopped and these studies were NOT installed, committed or pushed.

## Historical material revision: pass38 — review only

User requested source-like TEXTURES after rejecting pass30's material appearance.
Passes31–38 are separate material studies. No runtime PNG/YAML was replaced in this
revision: **the game still uses30**. The model's vertices, topology, dimensions,
attachments, muzzle and every raw frame's alpha silhouette match installed30 exactly.
Smooth shading and surface UV/material treatment change, not the shape.

Review:
- `pass-38/review/material-before-after-source.png`: original / installed30 / material38,
  same native density, pose and ground alignment.
- `pass-38/review/material-before-after-source-2x.png`: enlarged material comparison.
- `pass-38/review/material-rotation-native.gif`: installed/new side-by-side rotation at
  intended scale. Composed renders, NOT live screenshots.
- `pass-38/review/{comparison,all32}.png`: source/side/rear/end-on/full-turn review.
- `pass-38/shard-launcher.blend`: editable scene with packed material maps.

Material work:
- Source-sampled dark slate/blue-grey tonal ramps, smoother curved surface normals,
  controlled stepped bevels and recessed fields. No procedural RGB noise.
- Bounded source carapace samples, corrected shin-cut registration, painted shin
  channel surfaces and corresponding receiver-axle engraving. No whole-image projection.
- Dark vascular margins with a connected fine colored crest. Per-part native-pixel
  finishing respects visibility and leaves alpha/geometry unchanged; it does not paint
  across occlusion. Short isolated highlight fragments are rejected.
- Crystal surfaces use a true circumferential unwrap, distinct source-colored facets
  and deliberately drawn branching marks. Hidden facet decoration is inferred. This
  replaces the noisy/streaked planar photo projection, not the crystalline geometry.

Iterations explicitly rejected the too-pale31 treatment, grey armor contamination in
loose cyan selection, overlapping/uncoordinated crystal marks, UV edge clamping,
photographic smearing, overly broad white facets and disconnected vessel highlights.
The final palette/cuts live under `material-reference/source38/`; previous studies and
material libraries remain preserved. Sources are never recolored or overwritten.

A tighter render-camera depth range eliminates a few unstable pixels on touching
surfaces without changing camera projection or mesh coordinates. Raw fixed-base and
full/split control renders now match exactly. Saved-mesh audit confirms equality to30
and fixed/moving separation; source, runtime30 and protected-file hashes remain intact.
`python tools/test_scrin_shard_materials.py` validates those invariants; existing runtime
regression tests still pass. These checks do not establish artistic likeness/approval.

Texture tools: `prepare_scrin_shard_material_reference.py`,
`finish_scrin_shard_source_materials.py`, `review_scrin_shard_materials.py`, plus the
existing model/review/audit scripts. Generate the final source library with revision38.
No commit, push, release, runtime installation or process termination was performed.

## Current runtime: pass30 installed

User authorized installing the latest study. SHAR now directly uses the pass30 base
and32-facing turret PNGs; pass22's installed PNGs and YAML bindings are backed up in
`integration-pass30/`. Original reference and all study passes remain intact.

Scale0.58,384×384 frames; base0,-88, turret0,-59.423128; mount0,0,375;
muzzle577,-35,1490. The fractional turret offset cancels the integer engine mount
so both sprites retain the same ground registration. Selection height77 and
1920×1920 decoration bounds enclose the rendered poses.1×1 footprint, turn speed,
initial facing and the existing five-shot AA-only weapon remain unchanged.

The isolated engine check resolves the actual PNGs/sequences and verifies all32
muzzle projections: maximum0.139px. Seven regression tests pass, including exact
installed pixels, previous-install backups, source hashes, selection containment,
split/full occlusion and resolved gameplay equality. Protected Nod Conyard, test map
and Scrin weapon files retain their pre-install hashes. Live GPU/firing review remains
pending after restart. No running game was stopped; no commit/push/release.

See `integration-pass30/{binding.json,engine-check.txt,all32-native-1x1.png,rotation-native.gif}`.
These previews are compositions, not live game screenshots. Study metadata below
records the state BEFORE this installation; it does not supersede the current binding.

## Historical review study: pass30 — initially not installed

User rejected the appearance of installed22 and requested further passes. New studies
23–30 are preserved independently. Current study: `pass-30/shard-launcher.blend`.
The actual game STILL uses22; no sprite, rule, weapon or gameplay binding was replaced.

Review artifacts:
- `pass-30/review/before-after-source.png`: original, installed22 and study30 at the
  SAME yaw/native pixel density. Source ground contact is aligned, not rescaled.
- `pass-30/review/comparison.png`: source and new source/side/rear/opposite views.
- `pass-30/review/all32.png`: complete native rotation, including end-on views.
- `pass-30/review/rotation-native.gif`: composed1×1-scale study, not a game capture.

Actual changes and findings:
-23 replaces the low, thin foundation/long bare neck proportions with convex standing
  shin armor, stronger knees, stepped calf shields and vascular returns.
-24 widens the rooted stance, exposes recessed shin channels and reduces the oversized
  cyan wedge. Receiver armor becomes real overlapping curved plates.
-25 corrects inward plate normals, compacts the upper gun, restores restrained dark
  vascular colors, adds recessed cheek vanes and tapered root forks.
-26 reconnects detached terminal claws, preserves rigid housing cross-sections, exposes
  cervical light channels and adds bounded source cuts only on matching armor/crystal
  surfaces. Crystal UV candidates are rejected when they include non-crystal imagery.
-27 recesses the yaw collar BETWEEN the tall knees instead of perching it on top.
  A dorsal inset/grille replaces an empty receiver surface. Repeating mottled coatings
  are removed so the physical curvature/tonal steps carry the surfaces.
-28/29 provide a genuine swept central cavity and rerouted fixed harness. Checks found
  collisions in the low-collar iterations; the holder's inward surfaces were corrected.
  Rounded open-channel rims, curled inner teeth and a continuous battery lip replace
  bare edges. All four crystals and their harness remain stationary.
-30 adds coherent native-density bevel/recess material fields rather than RGB noise
  or repeated image stamps. Actual mesh geometry is identical to29.

Source/canonical/side/rear/end-on/full-rotation renders were inspected throughout.
The source-style asymmetry, beveled armor, inset mechanisms and material boundaries
are developed further; this is a reviewable revision, not a claim of artistic approval
or an exact reconstruction of unseen source geometry.

Read-only saved-scene audit measures24.9978 degrees, verifies four fixed shards and
fixed vertices through rotation, and confirms all moving parts are yaw-parented.
Actual meshes have no fixed/moving surface intersections at64 sampled yaw angles.
Full raw renders exactly match the fixed/holdout-turret compositions in the reference
and three cardinal control views. All32 frames remain inside the canvas.
See `pass-30/{saved-scene-audit,swept-collision-audit}.json`.

The higher peripheral knees make a whole-part vertical gap misleading: the central
bearing is recessed and clearance is now radial. The old `rotation_clearance` and
`bearing_clearance` metadata values are envelope differences, NOT proof of collision.
Reference pose uses yaw30 (-22.5 degrees) to match the source's projected gun direction;
the source image is translated to align its inferred ground contact at source y184.
Neither source pixels nor their density are changed. Hidden/rear forms remain inferred.

Tools: `refine_scrin_shard_pass23.py`, `finish_scrin_shard_pass30.py`,
`audit_scrin_shard_study.py`, and the existing model/review/swept-audit tools.
The new pivot is1.18 model units; a future installation MUST recalculate mount, muzzle,
selection and sprite offsets together. Do not copy these sprites over the installed
ones using22's registration. Runtime regression tests still pass against untouched22.
Original source SHA256 is unchanged. No game/Blender was stopped; no commit/push/release.

## Historical runtime: pass22 —25 degrees, fixed four-shard battery

Pass22 is now the actual runtime binding. The earlier35-degree pass17 is superseded.
The battery, its harness and new standing pedestal are all part of the fixed base.
The foundation has broad beveled standing armor, bounded source shin cuts, recessed
knee intakes, exposed inter-leg louvers, ground-root returns and source olive-grey
soil contact. The accelerator is shorter/flared with five broad overlapping shell
plates rather than eight short pipe segments. The rear sensory organ was compacted
to clear the stationary battery; canonical, side/rear/end-on and full rotations reviewed.

Pass18 established the fixed battery and replacement support;19 recovered source
armor/soil;20 moved mechanisms out from behind the legs;21 raised the battery to a
more source-like position and exposed rear-organ collisions;22 fixes those collisions
and gives the battery a grounded armored pedestal. Historical passes remain intact.

Actual saved meshes were checked at64 yaw samples (no fixed/moving intersections),
all fixed vertices audited unchanged throughout rotation, and the measured cannon
angle is25 degrees. Fixed-collection holdout masks prevent the turret drawing through
the tall stationary battery. Split sprites match complete native renders in all four
control views. These checks complement visual review, not artistic approval.

Runtime: both PNGs384×384, Scale0.58, base offset0,-88, turret offset0,-36,
mount0,0,681; muzzle638,-35,1570, max engine projection error0.187px. Selection
height104, decoration1920×1920; actual1×1 occupancy/gameplay unchanged.
`integration-pass22/rotation-native.gif` is the composed rotation, not a live capture.
Previous installed PNGs are backed up there. Live firing/GPU review remains pending.

## Historical requested revision (now implemented)

Pass17 is too steep. User now requests **25 degrees**, the **four-shard battery fixed
to the stationary base**, and significantly closer source matching for both the base
and turret. The radial/rounded reconstructed foundation and current cannon housing
are not accepted as sufficiently source-like. The original has substantial angular
standing leg armor, exposed ribbed mechanisms, a layered curved receiver and a wider
split accelerator. Preserve source and all previous passes.

This was pending during the Advanced Reactor binding. It is now implemented above;
the taller fixed battery uses swept collision and occlusion checks instead of the old
whole-part vertical-gap test.

## Historical installation: pass17 /35-degree elevation

The requested steeper cannon is now installed. Pass15 explored25 degrees, pass16
raised the accelerator to35 degrees and kept its hanging arteries rigid with it,
and pass17 added curved cervical returns into the moving collar. The fixed thorax,
roots and base PNG are **byte-for-byte unchanged** from pass14. The side magazine
remains yaw-local. Source, side, rear, end-on and all32 rotation views were reviewed.

Current scene: `pass-17/shard-launcher.blend`; rotation preview:
`integration-pass17/rotation-native.gif`. These are renders, not live screenshots.
The turret canvas is384×384 to avoid clipping the raised tip; original pixel density
and runtime scale0.58 remain unchanged. Turret offset0,-36; mount still0,0,681;
new muzzle646,-37,1978; selection height123. Base canvas/offset remain320×320/0,-56.
The actual-grid engine muzzle check reports max0.161px. Split components match full
assembly renders in the control views; all32 frames have unclipped alpha bounds.
Read-only saved-model audit verifies fixed vertices and separate rotating envelope.
Gameplay/AA stats are unchanged. Live GPU/firing review remains pending.

## Historical initial integration: pass14

Pass14 is integrated on **SHAR**, on branch **isometric**, using native PNGs from
`mods/ca/bits/scrin/buildings/shard-launcher-{base,turret32}.png`.
The supplied `shard_launcher-332414650-0.png` is unchanged:
SHA256 `2b07087a6ecf8b2db760d42832deb313fa7476a158897dfea6b0cc25c3c43464`.
All earlier scenes/renders remain in pass-01 through pass-14. No commit/push/release.

**Review files:**
- `pass-14/shard-launcher.blend`: editable full model, packed source/coating textures.
- `pass-14/review/comparison.png`: source beside canonical, side, rear and opposite side.
- `pass-14/review/all32.png`: full native rotation contact sheet.
- `integration-pass14/rotation-native.gif`: composed128×64-cell-scale rotation.
- `integration-pass14/all32-native-1x1.png`: actual intended runtime size/contact sheet.
These are model renders/compositions, **not live game screenshots**.

## Mechanical separation requested by the user

The original image supplies one view. The reconstruction has a fixed armored thorax,
upright armored feet, rooted tentacles and lower yaw race. Everything above the race,
including the side crystal magazine, rotates as one organism. Accelerator arteries
return into its own neck/collar; none is anchored to a fixed foot. The fixed roots
return only into the fixed thorax. A recessed gap separates both assemblies.

The saved Blender model was reopened read-only and audited across32 rotations:
all fixed vertices remain stationary, every upper mesh is parented to turret_pivot,
fixed top1.975, moving bottom2.14 (clearance0.165 model units). Yaw preserves this
vertical separation for every angle, not just exported facings. The audit and model
hash are in `integration-pass14/separation-audit.json`. Split renders match complete
assembly renders pixel-for-pixel in the four independently rendered control views.
Hidden/rear anatomy, the bearing and inner mechanisms are explicitly inferred.

## Actual review iterations

The requested ten passes were a minimum, not an acceptance test:

1. Separate receiver/channel/crown/neck/fixed-root volumes. Reference comparison
   exposed excessive blank surfaces, a flat support and intersecting lower geometry.
2. Larger exposed core, ground/neck registration and separated bearing envelope.
3. Neck segmentation, cheek mechanisms and four distinct crown sockets.
4. Correct outward shell normals; add upper vascular returns and replace the flat
   square ground slab with a study of rooted pads.
5. Faceted/branched crystal spine and magazine vascular harness; avoid a flat cyan strip.
6. Denser overlapping accelerator armor, substantial cheek rings, larger crown shards
   and embracing socket armor.
7. Convex principal leg armor, protected vessels and rib cages.
8. Reduce the radial fan to four main anchors; add receiver fins/plates and bounded
   native-density source armor cuts. Raise the moving lower envelope for visible clearance.
9. Conduit stitches and recessed receiver engravings.
10. Curled terminal jaws, closed upper return loops and less overexposed crown facets.
    Direct reference comparison still rejected the flat fan-like base and sparse anatomy.
11. Replace the support with an upright thorax and four standing limbs. Add actual
    visible cervical/accelerator return loops, ribbed shoulders and scalloped magazine plates.
12. Move receiver plates onto its true outer envelope (earlier details were partly
    buried), widen accelerator shell, add rear sensory organ and thorax vents/vascular strands.
13. Reposition the crown forward in depth instead of lowering it into the fixed base;
    add physical crystal refractive ridges/flutes. Its connecting tissue remains yaw-local.
14. Part-local chitin finish using bounded source armor cuts at native texel density,
    retaining directional tonal steps and rejecting cyan/vein samples from metal coating.
    Render/review all32 directions, plus stationary controls, then export.

No indiscriminate whole-reference projection or RGB noise. Large surfaces use
normal-dependent source-color tonal steps; armor is curved/segmented geometry,
crystals are faceted volumes, and tendrils are swept tubes. Source knee/shin cuts
are restricted to corresponding convex armor plates; hidden surfaces remain inferred.

## Runtime binding and preservation

- New sequence `shar.native` inherits legacy SHAR overlays, icon and shared construction.
- Body/damaged body use the fixed PNG. Turret/damaged turret use the32-facing PNG.
  Damaged states reuse the new art; no separate damage reconstruction is claimed.
- Scale0.58; frame320×320. Body offset0,-56; turret offset0,-4; mount0,0,681.
- Export order `(4+i)%32` matches the engine compass for the model's +Y accelerator.
- Actual128×64 grid projection, classic perspective fudge disabled. Shard spawn point
  is at the luminous crystal tip: LocalOffset860,-37,872.
-1×1 footprint and original collision shape retained. Mouse bounds1448×1448, height95;
  selection decoration1760×1760/height95 encloses the overhanging turret in all facings.
- Legacy Tesla bib removed: the supplied-style fixed foundation provides the ground art.
- Existing ShardLauncher remains **AA only**, five shards,4-tick burst spacing,
 25-tick reload. Cost, health, power, range, prerequisites, turning and death behavior
  are unchanged. No weapon definition or another faction was edited.
- Existing Scrin SHAR display in the disposable building test map uses this actor;
  the authoritative testmap1.oramap was not modified.

Files: `rules/scrin/shard-launcher-art.yaml` loads after the older China/Scrin2x layer;
`sequences/scrin-shard-launcher.yaml` defines direct native filenames. Earlier2x SHAR
selection/muzzle settings are superseded only for this replacement.

## Checks and remaining live review

.NET6 compilation passed in isolated output. Engine checker
`--check-scrin-shard-launcher-art` resolves and loads real native/damaged/construction
assets, checks1×1/AA preservation and32 muzzle projections (max0.209px).
New source/pixel/parentage/compass/selection/gameplay tests pass. China/Scrin scale
regressions pass with the explicit SHAR art exception. Disposable map generation,
water preparation and China18/Scrin17 validation pass with the fresh isolated DLL.

The first direct map check encountered the older engine/bin DLL from before the
master merge (missing SpawnActorOnDeathFromConditionSourceInfo); repeated with the
fresh DLL successfully. Once no OpenRA process was running, the normal local build
also succeeded and installed the fresh DLL. The direct engine checker and disposable
map test then passed without a temporary-binary workaround. No running game was
stopped. Start the building-test launcher to review it. Live GPU/firing confirmation
is pending; no live screenshot or in-game approval is claimed.

Scripts: model_scrin_shard_launcher.py, refine_scrin_shard_launcher.py,
finish_scrin_shard_materials.py, review_scrin_shard_launcher.py,
export_scrin_shard_launcher.py and audit_scrin_shard_rotation.py in tools/art/.
