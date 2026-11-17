# Soviet flak — real voxel recoil test

## Installed locally

- Source: pass13 Blender model. Two auxiliary tubes moved out from the receiver,
  lengthened, with visible bores and red terminal bands. Both move with the barrel;
  no additional weapons were added. Main gun/base forms retained.
- `bits/soviet/buildings/experimental/flak-{base,turret,barrel}.{vxl,hva}` are real
  voxel models, one static pose each. Grid density32, model bounds calibrated to
 20 native pixels/model unit at RenderVoxels.Scale1. Ground remains within1x1.
- Source-color indexed PNG palette; actual geometry normals use the RA2 table.
  Material-role albedo differentiates recesses/edges and retains bounded coating
  wear. The bore's inner walls are dark, not a flat painted muzzle dot.
- `FLAK.SOVIET.VOXEL` is an isolated editor/test actor. SAM and production menus
  are unchanged. The test actor borrows existing FLAK-23-AA, not new balance rules.
- `WithVoxelBarrelAxis` moves only the barrel group using Armament.Recoil. This
  includes both upper sliding rods; sleeves, receiver, yokes and base stay still.
  Recoil109 / recovery12, local direction(-761,0,-594). The Z factor includes the
  game's camera/world projection conversion, not merely raw sine42 degrees.
- Main muzzle registered at1337,0,1846. Current CA runtime DLL builds successfully.

## Checks versus live verification

`--check-soviet-flak-voxel` uses actual engine VXL/HVA readers, actor dependency
construction and WRot/BodyOrientation projection. All three components load; only
one HVA pose each. Recoil projection matches the voxel camera to <=0.056px across
32 directions. Tests also check1x1 bounds and unchanged resolved SAM rules.

`voxel-four-views.png` / `voxel-recoil.gif` render the exported voxel cells in
background Blender. They are NOT game screenshots; engine GPU palette lighting
may differ. Live firing/overlap/appearance has not yet been observed.

## Local test

Run `python -B tools/prepare_flak_voxel_test.py` to create/update the separate map
`mods/ca/maps/soviet-flak-voxel-test.oramap`. In the game select
**Flak Voxel Recoil Test (local art)**. The original SAM position contains the new
voxel actor. Select it and Ctrl+click nearby ground to observe recoil. Only this
map overrides its weapon to existing FLAK-23-AG, enabling a ground target without
having to spawn hostile aircraft. Allied AGUN uses its approved installed art;
the map's old HPAD position uses the new Soviet helipad. Original testmap1 untouched.

Reproduction:

1. Background Blender: `model_soviet_flak.py -- --pass 13`.
2. Background Blender: `voxelize_soviet_flak.py`.
3. Python: `pack_soviet_flak_voxel.py`.
4. Background Blender: `preview_soviet_flak_voxel.py`.
5. Python: `review_soviet_flak_voxel.py`.
6. Build CA; run `--check-soviet-flak-voxel` and Python installation regressions.

No release, push, or deployment. Existing SAM gameplay remains unchanged.
