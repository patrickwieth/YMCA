# Ion Defense — historical V4 game binding

**Superseded by [V5](../../attempt-05/README.md).** The numbers below describe the
retained V4 checkpoint, not the active game assets. `resolved-before.yaml` remains
the original gameplay baseline used by the active integration tests.

The existing **Ion_Defense** actor now uses `ion_defense.native`, not a new art-test
actor. It keeps its normal production entry and original icon/muzzle sequences.
The existing building-test map already places it as Actor84 at **169,-35**, in the
GDI block beside the Naval Yard. No source map or extra placement was required.

## Rendering

- Separate stationary `heavy-blaster-base.png` and 32-facing `heavy-blaster-turret32.png`.
- Mod assets: `mods/ca/bits/gdi/buildings/experimental/`, byte-identical to V4 exports.
- Both 160px frames at Scale 1, Center anchor, embedded PNG frame metadata.
- Base sequence offset `-7,-25`; ground-contact center approximately `(86.5,104.758)`.
- Turret sequence offset `-1,-10`, compensating the actual world turret offset.
- `WithSpriteBody` retains sequence `base`; `WithSpriteTurret` replaces the embedded
  renderer so the separate parts also work correctly in actor previews.
- No recoil displacement added. Turret speed 8 and initial facing 0 stay unchanged.
- Legacy `WithBuildingBib` and projected SHP `WithShadow` removed. The modeled base
  provides the foundation; no extra decorative concrete was invented.
- Damaged variants use the same art; there is no separate damaged reconstruction.
- Selection *decoration* bounds `1810,1810,0,0`, height 68 cover all 32 views.
  Mouse bounds, hit shapes, occupied cells and local building center are unchanged.

## Explicit attachment changes (not a claim of byte-identical firing origins)

The old weapon origin would be substantially below the new muzzle. The model's
muzzle is at `(x=-.125, y=-3.405, z=2.4225)` with pivot `(-.125,.2,.7)`.
The new attachment is:

- `BodyOrientation.CameraPitch: 85` (~30 degrees, matching the rendered projection)
- `Turreted.Offset: 0,-136,339`
- `Armament.LocalOffset: 1631,0,675` (previously `1000,0,80`)

This intentionally changes the physical projectile/muzzle starting position and
its local projection. The turret sprite's sequence offset compensates the world
offset, so the model remains seated on the unchanged base. Using actual engine
WRot/BodyOrientation math, the muzzle projection differs from the rendered model
by at most **0.609 native pixel** over all 32 directions.

Weapon `IonZap.Defense`, range/damage/fire delay, HP, cost, power, production rules,
detection, targeting, 2x2 footprint, exits and collision remain unchanged. The
original renderer's pause condition controlled animation (one frame here); the
existing AttackTurreted power/EMP/warp pause conditions remain unchanged.

The six original `Inherits` keys were given unique suffixes, preserving their
order/values, because a second actor-rule fragment otherwise exposes a MiniYaml
merge duplicate-key failure.

## Checks

- Release build passed.
- Real engine PNG loaders: 1 base frame and 32 turret frames, all native 160px.
- Actor trait construction and all 32 engine muzzle projections passed.
- Four binding tests compare resolved rules against `resolved-before.yaml`, allowing
  only documented art/attachment changes; original icon/muzzle overlays retained.
- Disposable test-map binding equals the normal actor binding; source archive intact.
- Nine source-art tests, GDI art check, selection-trait check and PNG-case audit pass.

**Live GPU appearance and firing have not been reviewed.** In particular, confirm
muzzle effect size/placement and selection appearance in the actual game.
Run `Game/test-gebaeude.cmd` to rebuild/load the test map. No game was restarted
or stopped by the assistant; no commit, push, release or deployment.
