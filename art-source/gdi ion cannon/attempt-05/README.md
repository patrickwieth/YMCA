# Ion Defense V5 — broader cannon and restored original base perimeter

Historical oversized game version, superseded by [V10 native160](../attempt-10-preview/integration/README.md).
Model: `heavy-blaster-v5.blend`; preserved native parts: `export/`.

## Changes requested after live V4 review

- Gun, receiver and muzzle cross sections are **65% wider in actual geometry**,
  around the existing longitudinal axis. This is not a stretched rendered PNG.
- Support spacing is increased by 25%, separately from the stronger gun widening.
- Native density is now 40px/model-unit rather than 20. All 32 turret views were
  rendered directly at **320x320**, Scale 1. This gives the existing 2x2 defense a
  roughly 200px-wide foundation instead of V4's roughly 100px foundation. Overall
  height/length also increase in screen pixels; this is an intentional sizing
  correction, not a claim that only the image width changed.
- Visible foundation armor, pipes, gear, feet and perimeter pixels are restored
  directly from the original supplied art. V4's straight/chopped reconstructed
  edges are no longer used for those visible regions. The hidden center/deck and
  socket use the stationary rendered underlay.
- The old left deck patch accidentally included part of the reference barrel.
  Removed that patch and explicitly excluded the original muzzle lip from the
  fixed-base mask. No turret fragment should remain permanently on the base.
- The higher native density retains more original source-pixel detail. Textures
  remain source-only and nearest sampled; no procedural noise/weathering.

## Reviewed/rejected roof changes

`roof-pass-01` retains the first wider render. `roof-pass-02` attempted a smoother
physical roof/conduit but lost too much source detail. `roof-pass-03` restored
source cuts on that roof but introduced dark patch artifacts. Those alternatives
were rejected; the selected export retains the more source-faithful original roof
UVs. The separate `refine_heavy_blaster_v5_roof_study.py` writes only into
`roof-study-rebuild`, not over the selected version.

Some end-on texture foreshortening and inferred rear surfaces remain imperfect.
This update is not a declaration that every unseen surface now matches the source.

## Review

- [Original / V4 / wider V5 at equal model density](export/reference-comparison-2x.png)
- [Actual native in-game size comparison, multiple angles](export/native-size-comparison.png)
- [Separate base and turret](export/separate-parts.png)
- [Rotation preview](export/rotation-preview.gif)

The base is a source-cut/rendered-underlay composite, so the `.blend` alone does
not produce the final base PNG. Run the packer too. The original 1024px source is
untouched. Its visible base pixels are nearest-sampled once into a native working
asset; the turret itself is directly rendered, never upscaled from V4 sprites.

## Active binding

Existing actor `Ion_Defense`, image `ion_defense.native`, same production entry.
Files under `mods/ca/bits/gdi/buildings/experimental/` retain their existing names:

- `heavy-blaster-base.png`: one 320x320 frame.
- `heavy-blaster-turret32.png`: 32 frames, 2560x1280 sheet.

Embedded frame metadata remains mandatory. Sequence Scale stays 1. Base offset
`-14,-48`; turret sequence offset `-2,-19`; turret world offset `0,-272,656`.
CameraPitch stays 85. Muzzle local offset becomes `3263,0,1350` to match the larger
rendered geometry. These are explicit projectile-origin/visual attachment changes;
weapon/range/damage/turn speed/cost/power and the 2x2 occupied cells stay unchanged.

Selection decoration bounds are `3801,3801,0,0`, height 130. Mouse bounds and hit
shapes remain unchanged. Existing Actor84 at `169,-35` uses this version.

## Validation

- Release build passes; actual engine loaders read 1+32 native 320px frames.
- All 32 engine muzzle projections are within **0.515 native pixel** of the model.
- Four active-binding tests plus twelve retained/new art tests pass.
- V5 tests verify physical width ratios, actual native frame dimensions, source
  palette, alpha, clipping, fixed-base control and exact source pixels in the
  restored visible-base mask. Original source SHA256 remains unchanged.
- GDI-art/selection checks and PNG-case audit pass. Protected Nod conyard unchanged.

Live appearance/firing after this revision is still pending. Start
`Game/test-gebaeude.cmd` to reload it. No game was stopped/restarted by the assistant,
and no commit, push or deployment was made.

Reproduce from Game:

```sh
blender.exe --background --python tools/art/refine_heavy_blaster_v5.py
python tools/art/pack_heavy_blaster_v5.py
python tools/art/integrate_ion_defense_v5.py
python -B -m unittest discover -s tools -p 'test_heavy_blaster*.py'
python -B -m unittest discover -s tools -p test_ion_defense_art.py
```
