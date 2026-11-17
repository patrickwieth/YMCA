# GDI Missile Tower — static PNG base + real voxel turret

## Current: pass-16 — turret texture-only revision

User requested a new cut-and-paste texture pass on the rotating turret ONLY.
Only `mods/ca/bits/gdi/buildings/missile-tower-turret.vxl` was replaced at runtime.
Base PNG, HVA, palette, render scale, turret mount, weapons and muzzle offsets are
unchanged. The editable mesh vertices/faces/world matrices and exported voxel
coordinates/normals/bounds/density match pass14 exactly.

Gold housing surfaces now have individual clamped native-resolution atlases instead
of repeated UV swatches. Broad source-color tonal steps describe bevels and panel
faces; bounded original turret armor fragments are placed inside the actual UV
regions. Every pixel is copied from the original source and has a source-coordinate
lookup. This includes controlled source-color assembly, not a claim that hidden
rear faces are complete components recovered from the supplied front view.
Steel mechanisms, cyan optics and missile materials are unchanged.

Pass15 was reviewed and retained as an intermediate: calmer material, but side panels
were too sparse. Pass16 adds bounded armor details to actual panel interiors and
continuous housing strips. Full native engine-transform rotation, front/rear/side
and end-on views were reviewed. See `pass-16/before-after-four-views-2x.png` (old top,
new bottom), `engine-rotation-review.png`, and `engine-rotation.gif`.

`tools/test_gdi_missile_texture_only.py` checks exact geometry, pixel provenance,
unchanged other materials, base and registration. The engine VXL/HVA transform
check passes with the same maximum muzzle error0.240px. Live GPU review is pending.
No running game/Blender stopped; all source and earlier passes preserved.

Reproduction for a NEW pass: run `retexture_gdi_missile_model.py` in background Blender
(plan), `prepare_gdi_missile_panel_textures.py` in Python, then the model script with
`--apply`, voxelize/pack, `render_gdi_missile_texture_review.py` in background Blender,
and `compare_gdi_missile_textures.py`. All accept `--pass N`.

## Previous: pass-14 — freestanding base, corrected voxel size and pivot

Runtime assets match pass14 byte-for-byte. External paving is alpha-cut away;
RenderVoxels scale is 0.85 and the voxel grid/bounds transform is corrected (the
old transform also inflated/displaced the geometry). Mount: `0,11,1527`.
Full native engine-transform rotation and front/side/rear views were reviewed.
The headless engine check confirms the actual VXL/HVA bearing transform and muzzle
registration (maximum error 0.240 native pixels). Live GPU/game firing remains untested.

Texture clarification: source rectangles are genuine unscaled source cuts, but the
model maps and repeats those swatches using UVs. This is NOT a one-to-one collage
of complete source details. Pass14 uses broader gold donors; inferred rear/upper
surfaces remain reconstructed. Do not describe the material as individually pasted
source components or claim the source provenance alone proves visual fidelity.

## Previous: pass-12 — repaired base, ATWR runtime binding installed locally

The user clarified that removing the right-side turbines must restore the concealed
building and paving, not leave a transparent hole. Pass12 copies native side-wall,
ladder/pipe continuation and clean paving rectangles into the removed region. No
painting, recoloring, mirroring or rescaling. All cuts are recorded in its manifest.
The original and passes1–11 remain unchanged; voxel geometry remains revision9.

Installed on **ATWR / Advanced Guard Tower** via
`rules/gdi/missile-tower-art.yaml` and `sequences/gdi-missile-tower.yaml`.
Authoritative runtime files are directly under `mods/ca/bits/gdi/buildings/`:
`missile-tower-base.png`, `missile-tower-turret.vxl/.hva`, `missile-tower-palette.png`.
The old mini-bib is removed; the repaired source already contains its ground.

Native scale1, base anchor Bottom0,-16, mount0,0,1493. The engine's fixed-point
CameraPitch86 and missile offsets429,±192,1003 align both middle-row tips through
64 facings within0.255 native pixels. Relay marker absolute elevation is preserved.
Cost/health/footprint/weapons/ammo/reload/yaw/faction prerequisites are unchanged.
Exact palette, ambient1/diffuse0 preserve the original cut colors. Turret is hidden
during construction; the stationary base uses the existing building animation policy.

`--check-gdi-missile-tower` loads actual runtime PNG/VXL/HVA/palette, actor dependencies,
and checks physical muzzle registration. The full default sprite set and gameplay
before/after regression tests pass in an isolated current-code runtime. Live GPU/firing
review remains pending. A concurrent Chinese selection fix requires rebuilt Common DLL;
normal copy is currently blocked by the running game. Do not stop it: close normally
and run `test-gebaeude.cmd` to rebuild/load all latest changes.

Current preview: `pass-12/voxel-comparison-2x.png`, `pass-12/voxel-rotation.gif`.

## Previous artifact: pass-11 (preserved; transparent-cut version rejected)

Latest user request: remove the turbine clutter at the lower right of the static base.
`pass-11/base.png` uses a binary native-pixel cut; no ground or hidden tower surfaces
were painted underneath. The cut polygon is recorded in its `source-cuts/manifest.json`.
All remaining opaque lower-base pixels are unchanged. Model, VXL/HVA, textures and
palette are byte-identical to pass10; source and all earlier passes are preserved.

Updated previews:
- `pass-11/voxel-comparison-2x.png`
- `pass-11/voxel-rotation.gif`
- `pass-11/voxel-eight-views.png`

`tools/art/audit_gdi_missile_tower.py --pass 11` passes, including the explicit removal
mask. No runtime integration.

## Previous complete reconstruction: pass-10 (preserved)

The user confirmed that no uncropped reference exists and explicitly authorized
reconstruction of missing geometry. The original128x128 PNG remains byte-for-byte
unchanged (SHA256 `d2ee8229382a6db3f21c1cd5663f91b168408de08f44986b8f3155745d1356dc`).

- `pass-10/base.png`: one stationary128x128 PNG, native tower/yard pixels. Binary
  exterior-background and machinery masks; only the occluded bearing face is filled
  with copied native metal pixels. The visible rim and lower architecture are retained.
- `pass-10/missile-turret.vxl` / `.hva`: actual single-limb voxel turret, not a sprite
  atlas pretending to be a voxel. Rounded twin3-round magazines, projecting rounded
  missile noses, recessed mouths, machined spine, elevation trunnions, hydraulic
  supports, stepped yaw bearing, rear hatches/cooling fins/conduits and tracking head.
- `pass-10/turret-palette.png`: exact8-bit source-color palette. Future runtime binding
  MUST use this via PaletteFromPng. The VXL format's embedded6-bit palette is lossy
  by definition and is not the authoritative color source. RA2 normal type.
- `pass-10/turret.blend`: editable, packed native cut textures and actual geometry.
- `pass-10/voxel-preview.blend`: checked exported voxel cells, not the smooth mesh.

Pass10 preserves pass9's geometry/VXL byte-for-byte and refines only the static base:
its originally visible bearing rim is no longer overwritten by the inferred top face.
Earlier passes and review images are retained. No runtime asset, actor binding, gameplay,
approved GDI AA pixels or unrelated work was modified. No processes stopped, no commit,
push or release.

## Texture policy: cut & paste only

`source-cuts/manifest.json` records every rectangular source cut. Original RGBA pixels
are copied at integer native dimensions. There is no texture rescaling, painting,
recoloring, generated RGB noise, texture baking from the reference camera, or copying
unrelated building details onto hidden faces. Metallic tones are selected from bounded
original dark/medium/light cuts; geometry and separate native-tone facets describe
bevels, curvature and recesses. Continuous cylindrical/rounded-shell UV strips avoid
restarting the same tiny texture at every facet. UV scale is one native texel per model
unit; perspective/voxel rasterization is naturally separate from pixel-copy texture
construction. Tiny plain source-metal patches are used for dark recessed metal and the
inferred flat bearing floor, not to replace all material variation.

Voxelization samples the actual nearest triangulated surface and its barycentric UV.
The packer reads the corresponding original texture texel directly, with no camera
projection, nearest-color fitting or fallback colors. Every voxel's palette color is
traceable to a specific original `(x,y)` in `pass-10/source-pixel-lookup.npz`.

The source is clipped through the upper launcher. Magazine roofs, tracking-head upper
casing, rear access hardware and hidden bearing face are inferred, not claimed to have
been recovered from an unseen image. The lowermost supplied yard remains cropped as
in the source; no fabricated scenery extension was painted around it.

## Actual visual review

- `pass-10/voxel-comparison-2x.png`: supplied source vs exported-voxel composite.
- `pass-10/voxel-eight-views.png`: front, side, rear and end-on directions.
- `pass-10/voxel-rotation.gif`: full rotation over the stationary PNG base.
- `pass-10/voxel-rotation-review.png`: all native directional views.
- `pass-10/comparison-2x.png`, `rotation.gif`: smooth modeling mesh equivalents.

Iterations corrected overly mottled metal, a too-high mount, oversized touching tube
mouths, broad flat-looking front rails, poorly separated missile tones, discontinuous
per-facet UVs, inward shell normals and the upper assembly's screen registration.
The source's paired launchers, dark round noses, bronze curved housings, exposed steel
mechanisms and cyan optics were compared directly. Hidden roofs/rear mechanisms were
reviewed during rotation rather than accepting only the source-facing image.

These are native Blender albedo renders of the model/exported voxel cells, NOT live
OpenRA screenshots. Engine voxel lighting, sprite/voxel depth interaction, gameplay
mount and firing alignment still need checking if runtime integration is authorized.
Checks or a pass number are not artistic approval.

## Technical verification

- `tools/art/audit_gdi_missile_tower.py --pass 10`: source hash; exact copied texture
  rectangles; unchanged visible base RGB; every voxel color traced to the original;
  binary VXL column round trip; HVA; unclipped full rotations.
- Engine `VxlReader` and `HvaReader` loaded the final files successfully through the
  isolated `.art-validation/gdi-missile-tower/ReaderCheck.csproj` harness. Reader log
  is copied to `pass-10/engine-reader.txt`.
- Data: one rotating limb, identity HVA,96x92x86 voxel lattice,115067 shell voxels,
 101 source RGB colors. These are validation details, not a claim of visual quality.

## Tools / reproduction

Use a NEW pass number to preserve earlier studies. Background Blender3.6 only:

1. `python tools/art/prepare_gdi_missile_tower.py`
2. Blender `-b --python tools/art/model_gdi_missile_tower.py -- --pass N --all`
3. `python tools/art/review_gdi_missile_tower.py --pass N` (snapshots source cuts)
4. Blender `-b --python tools/art/voxelize_gdi_missile_tower.py -- --pass N`
5. `python tools/art/pack_gdi_missile_tower.py --pass N`
6. Blender `-b --python tools/art/preview_gdi_missile_voxel.py -- --pass N --all`
7. `python tools/art/review_gdi_missile_tower.py --pass N --voxel`
8. `python tools/art/audit_gdi_missile_tower.py --pass N`

`source-cuts/` is the current preparation workspace; every completed pass stores its
own immutable cut snapshot and the Blender images are packed. Pass10's base-only
refinement is documented above; its model/export are deliberately unchanged from9.
