# Soviet flak — sprite replacement

## Current approved pedestal: pass15 modeled render

User approved pass15. Runtime `flak-base.png` and `flak-turret32.png` now match15
byte-for-byte. Base Scale1/offset0,-40, turret Scale1/offset0,-29; attachment243,
relative muzzle1337,0,1603.1×1 Soviet bib retained. Engine checks pass (0.189px),
with no gameplay/stock SAM changes. Source pedestal PNG is preserved unused.

## Previous pedestal: supplied original and1×1 bib (historical)

User requested the existing `bits/soviet/buildings/flak base.png` rather than the
modeled pedestal. It is now referenced directly (all base/idle states), Scale0.4,
Center offset0,-1; original bytes untouched. At ground center80.5,46 its entire
nontransparent silhouette has diamond extent61px versus the64px limit, fitting
inside the restored128×64 `sovietbib1x1` overlay. Turret remains native Scale1.

Turret attachment lowered from243 to131 to seat it on the original bearing.
Relative muzzle1337,0,1603 is unchanged, following that attachment. Engine check
includes the mounting shift and passes at0.189px maximum. Gameplay stays1×1;
only base art/scale, mounting and decorative bib changed. Offline32-facing hybrid
review: `original-base-all32.png`; no live screenshot claim. The modeled base
export is retained as history, no longer used at runtime.

## Initial sprite-only integration (historical modeled base)

User rejected the live voxel presentation and requested rendered sprites instead.
Pass14 renders the existing pass13 model at all32 facings; no voxel screenshots,
model redesign or overwriting of supplied artwork. Full rotation reviewed.

`FLAK.SOVIET.SPRITE` uses fixed `flak-base.png` and rotating `flak-turret32.png`
in `mods/ca/bits/soviet/buildings/`. Native192 frames, Scale1, facing start20.
Base Center offset0,-40, turret0,-29, pivot0,0,243. Muzzle1337,0,1603 registers
with the elevated barrel opening; engine maximum error0.189px over32 facings.

The actor inherits prototype gameplay, but removes RenderVoxels and every voxel
body/turret/barrel trait. It adds sprite body/turret and generic China construction.
No fake whole-turret recoil: Recoils=false / Recoil=0. There is currently no separate
barrel recoil in the sprite version. Footprint remains1×1, FLAK-23-AA unchanged.
Old voxel actor, exports and source scenes remain archival; stock SAM stays intact.

Both normal `test-gebaeude.cmd` preparation and the dedicated map now place the
sprite actor at Actor37,148,-56. Ground-fire override is only in disposable tests.
The dedicated package retains its legacy filename `soviet-flak-voxel-test.oramap`
but is titled **Flak Sprite Test (local art)**. Original testmap1 bytes unchanged.

`--check-soviet-flak-sprite` loads actual PNG frames and resolved sprite/construction/
damaged sequences, checks dependencies, rejects remaining voxel rendering and
validates muzzle projection. Python regressions cover assets, actor and map binding.
These checks are not a live GPU/appearance approval. Restart the normal building
test to view the replacement. No commit/push/release performed.
